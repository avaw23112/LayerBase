using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

internal readonly struct ScopeDefinitionModel
{
    public ScopeDefinitionModel(
        INamedTypeSymbol scopeType,
        int scopeId,
        string identity,
        string fullyQualifiedTypeName,
        Location? location)
    {
        ScopeType = scopeType;
        ScopeId = scopeId;
        Identity = identity;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        Location = location;
    }

    public INamedTypeSymbol ScopeType { get; }

    public int ScopeId { get; }

    public string Identity { get; }

    public string FullyQualifiedTypeName { get; }

    public Location? Location { get; }
}

internal static class ScopeDefinitionCodeGen
{
    private const string ScopeInterfaceMetadataName =
        "LayerBase.Scope.IScopeDefinition";

    private const string ScopeIdentityAttributeMetadataName =
        "LayerBase.Scope.ScopeIdentityAttribute";

    public static bool ImplementsScopeDefinition(INamedTypeSymbol symbol)
    {
        foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
        {
            if (iface.ToDisplayString() == ScopeInterfaceMetadataName)
                return true;
        }
        return false;
    }

    public static string BuildIdentity(INamedTypeSymbol scopeType)
    {
        AttributeData identityAttribute = null;
        foreach (AttributeData attr in scopeType.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ScopeIdentityAttributeMetadataName)
            {
                identityAttribute = attr;
                break;
            }
        }

        if (identityAttribute != null)
        {
            string rawValue = identityAttribute.ConstructorArguments.Length == 1
                ? identityAttribute.ConstructorArguments[0].Value as string
                : null;

            if (!string.IsNullOrWhiteSpace(rawValue))
                return "scope-key:" + rawValue.Trim();
        }

        string assemblyName = scopeType.ContainingAssembly.Name;
        string metadataName = GetFullyQualifiedMetadataName(scopeType);

        return $"scope:{assemblyName}:{metadataName}";
    }

    public static string GetFullyQualifiedMetadataName(INamedTypeSymbol scopeType)
    {
        var parts = new List<string>();
        INamedTypeSymbol current = scopeType;

        while (current != null)
        {
            parts.Add(current.MetadataName);
            current = current.ContainingType;
        }

        parts.Reverse();
        return string.Join("+", parts);
    }

    public static int ComputeScopeId(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("Scope identity is required.", nameof(identity));

        for (int attempt = 0; attempt < 32; attempt++)
        {
            string candidate = attempt == 0
                ? identity
                : identity + "#" + attempt.ToString(CultureInfo.InvariantCulture);

            byte[] input = Encoding.UTF8.GetBytes(candidate);
            byte[] digest;

            using (SHA256 sha256 = SHA256.Create())
                digest = sha256.ComputeHash(input);

            int scopeId =
                ((digest[0] & 0x7F) << 24) |
                (digest[1] << 16) |
                (digest[2] << 8) |
                digest[3];

            if (scopeId != 0)
                return scopeId;
        }

        throw new InvalidOperationException(
            $"Unable to derive a non-zero Scope ID for identity '{identity}'.");
    }

    public static bool TryCreateModel(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol symbol,
        bool reportDiagnostics,
        out ScopeDefinitionModel model)
    {
        model = default;

        if (!ImplementsScopeDefinition(symbol))
            return false;

        if (reportDiagnostics)
        {
            return ValidateScopeShape(context, compilation, symbol, out model);
        }
        else
        {
            return TryGetModel(symbol, out model);
        }
    }

    private static bool ValidateScopeShape(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol symbol,
        out ScopeDefinitionModel model)
    {
        model = default;

        if (symbol.TypeKind != TypeKind.Class)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ScopeMustBeClass, FirstLocation(symbol), symbol.Name));
            return false;
        }

        if (symbol.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ScopeMustNotBeAbstract, FirstLocation(symbol), symbol.Name));
            return false;
        }

        if (symbol.Arity > 0 || IsOrContainsGenericType(symbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ScopeMustNotBeGeneric, FirstLocation(symbol), symbol.Name));
            return false;
        }

        bool sameAssembly = SymbolEqualityComparer.Default.Equals(
            symbol.ContainingAssembly, compilation.Assembly);

        if (!HasAccessibleParameterlessConstructor(symbol, sameAssembly))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ScopeConstructorNotAccessible, FirstLocation(symbol), symbol.Name));
            return false;
        }

        if (symbol.Name != "MainScope")
        {
            foreach (ISymbol member in symbol.GetMembers())
            {
                if ((member is IFieldSymbol || member is IPropertySymbol) &&
                    member.Name == "ScopeId" &&
                    member.DeclaredAccessibility == Accessibility.Public &&
                    member.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ScopeMustNotDeclareStaticScopeId, FirstLocation(symbol), symbol.Name));
                    return false;
                }
            }
        }

        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ScopeIdentityAttributeMetadataName)
            {
                string rawValue = attr.ConstructorArguments.Length == 1
                    ? attr.ConstructorArguments[0].Value as string
                    : null;

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ScopeIdentityValueEmpty, FirstLocation(symbol), symbol.Name));
                    return false;
                }
            }
        }

        if (!TryGetModel(symbol, out model))
        {
            return false;
        }

        return true;
    }

    public static void ReportLocalCollisions(
        SourceProductionContext context,
        IReadOnlyList<ScopeDefinitionModel> models)
    {
        var byIdentity = new Dictionary<string, ScopeDefinitionModel>(StringComparer.Ordinal);
        var byId = new Dictionary<int, ScopeDefinitionModel>();

        foreach (ScopeDefinitionModel model in models)
        {
            if (byId.TryGetValue(model.ScopeId, out ScopeDefinitionModel existingById))
            {
                if (existingById.Identity != model.Identity)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        CollidingScopeId,
                        model.Location ?? Location.None,
                        model.ScopeId, model.Identity, existingById.Identity));
                }
                continue;
            }

            if (byIdentity.TryGetValue(model.Identity, out ScopeDefinitionModel existingByIdentity))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CollidingIdentity,
                    model.Location ?? Location.None,
                    model.Identity, model.ScopeType.Name, existingByIdentity.ScopeType.Name));
                continue;
            }

            byId[model.ScopeId] = model;
            byIdentity[model.Identity] = model;
        }
    }

    private static bool TryGetModel(INamedTypeSymbol symbol, out ScopeDefinitionModel model)
    {
        string identity = BuildIdentity(symbol);
        int scopeId = ComputeScopeId(identity);

        model = new ScopeDefinitionModel(
            symbol,
            scopeId,
            identity,
            symbol.ToDisplayString(),
            FirstLocation(symbol));

        return true;
    }

    private static bool IsOrContainsGenericType(INamedTypeSymbol symbol)
    {
        if (symbol.Arity > 0)
            return true;

        INamedTypeSymbol current = symbol.ContainingType;
        while (current != null)
        {
            if (current.Arity > 0)
                return true;
            current = current.ContainingType;
        }

        return false;
    }

    private static bool HasAccessibleParameterlessConstructor(
        INamedTypeSymbol symbol, bool sameAssembly)
    {
        foreach (IMethodSymbol constructor in symbol.Constructors)
        {
            if (constructor.IsStatic || constructor.Parameters.Length > 0)
                continue;

            if (constructor.DeclaredAccessibility == Accessibility.Public)
                return true;

            if (sameAssembly &&
                (constructor.DeclaredAccessibility == Accessibility.Internal ||
                 constructor.DeclaredAccessibility == Accessibility.ProtectedOrInternal))
            {
                return true;
            }
        }

        return false;
    }

    private static Location FirstLocation(ISymbol symbol)
    {
        ImmutableArray<Location> locations = symbol.Locations;
        return locations.Length > 0 ? locations[0] : Location.None;
    }

    private static readonly DiagnosticDescriptor ScopeMustBeClass = new(
        id: "LBSC003",
        title: "Scope must be a class",
        messageFormat: "Scope '{0}' must be a class.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopeMustNotBeAbstract = new(
        id: "LBSC004",
        title: "Scope must not be abstract",
        messageFormat: "Scope '{0}' must not be abstract.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopeMustNotBeGeneric = new(
        id: "LBSC005",
        title: "Scope must not be generic",
        messageFormat: "Scope '{0}' must not be generic.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopeConstructorNotAccessible = new(
        id: "LBSC006",
        title: "Scope constructor not accessible",
        messageFormat: "Scope '{0}' does not have an accessible parameterless constructor.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopeIdentityValueEmpty = new(
        id: "LBSC007",
        title: "Scope identity value is empty",
        messageFormat: "ScopeIdentity value for '{0}' is empty or whitespace.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CollidingScopeId = new(
        id: "LBSC001",
        title: "Colliding scope ID",
        messageFormat: "Scope ID {0} is used by identity '{1}' and '{2}'.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CollidingIdentity = new(
        id: "LBSC002",
        title: "Colliding scope identity",
        messageFormat: "Scope identity '{0}' is used by '{1}' and '{2}'.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ScopeMustNotDeclareStaticScopeId = new(
        id: "LBSC009",
        title: "Scope must not declare static ScopeId",
        messageFormat: "Scope '{0}' must not declare a static ScopeId field or property.",
        category: "ScopeDefinition",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
