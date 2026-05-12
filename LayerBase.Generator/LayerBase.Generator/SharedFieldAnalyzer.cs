using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SharedFieldAnalyzer : IIncrementalGenerator
{
    private const string ProvideAttributeName = "LayerBase.DI.ProvideAttribute";
    private const string FromAttributeName = "LayerBase.DI.FromAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provideFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            ProvideAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx,  _) => GetFieldInfo(ctx, true));

        var useFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            FromAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx,  _) => GetFieldInfo(ctx, false));

        var allFields = provideFields.Collect().Combine(useFields.Collect());
        var compilationAndFields = context.CompilationProvider.Combine(allFields);

        context.RegisterSourceOutput(compilationAndFields,
            static (spc, pair) => Analyze(spc, pair.Left, pair.Right.Left, pair.Right.Right));
    }

    private static FieldInfo? GetFieldInfo(GeneratorAttributeSyntaxContext ctx, bool isProvide)
    {
        var fieldSymbol = (IFieldSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];

        if (attr.ConstructorArguments.Length < 2) return null;

        var ownerType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        var localKey = attr.ConstructorArguments[1].Value as string;

        if (ownerType == null || string.IsNullOrEmpty(localKey)) return null;

        var syntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        bool isLiteral = syntax != null && syntax.ArgumentList != null && syntax.ArgumentList.Arguments.Count > 1 &&
                         syntax.ArgumentList.Arguments[1].Expression.IsKind(SyntaxKind.StringLiteralExpression);

        return new FieldInfo(
            fieldSymbol.ContainingType,
            fieldSymbol.Name,
            fieldSymbol.Type,
            ownerType,
            localKey,
            isProvide,
            fieldSymbol.Locations.FirstOrDefault() ?? Location.None,
            isLiteral);
    }

    private static void Analyze(SourceProductionContext    spc, Compilation compilation,
                                ImmutableArray<FieldInfo?> provides,
                                ImmutableArray<FieldInfo?> uses)
    {
        var validProvides = provides.Where(p => p != null).Select(p => p!).ToImmutableArray();
        var validUses = uses.Where(p => p != null).Select(p => p!).ToImmutableArray();
        var allValidFields = validProvides.Concat(validUses);

        var layerSymbol = compilation.GetTypeByMetadataName("LayerBase.Layers.Layer");
        var iServiceSymbol = compilation.GetTypeByMetadataName("LayerBase.DI.IService");
        var globalScopeSymbol = compilation.GetTypeByMetadataName("LayerBase.DI.GlobalScope");

        foreach (var f in allValidFields)
        {
            if (f.IsLocalKeyLiteral)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LiteralKeyWarning, f.Location, f.LocalKey));
            }

            if (layerSymbol != null && iServiceSymbol != null && globalScopeSymbol != null)
            {
                bool isValidOwner = SymbolEqualityComparer.Default.Equals(f.OwnerType, globalScopeSymbol) ||
                                    InheritsFrom(f.OwnerType, layerSymbol) ||
                                    f.OwnerType.AllInterfaces.Any(i =>
                                        SymbolEqualityComparer.Default.Equals(i, iServiceSymbol) ||
                                        SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iServiceSymbol));

                if (!isValidOwner)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidOwnerType, f.Location,
                        f.OwnerType.ToDisplayString()));
                }
            }
        }

        // 1. Check for Provide conflicts
        var provideMap = new Dictionary<string, FieldInfo>();
        foreach (var p in validProvides)
        {
            var uniqueKey = $"{p.OwnerType.ToDisplayString()}_{p.LocalKey}";

            if (provideMap.TryGetValue(uniqueKey, out var existing))
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.ProvideConflict, p.Location, p.LocalKey,
                    existing.ContainingType.Name, p.ContainingType.Name));
            else
                provideMap[uniqueKey] = p;
        }

        // 2. Check for Use matches and type compatibility
        foreach (var f in validUses)
        {
            var uniqueKey = $"{f.OwnerType.ToDisplayString()}_{f.LocalKey}";
            if (provideMap.TryGetValue(uniqueKey, out var p))
            {
                if (!IsTypeCompatible(p.Type, f.Type))
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeMismatch, f.Location, f.LocalKey,
                        f.Type.Name, p.Type.Name));
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OrphanUse, f.Location, f.LocalKey));
            }
        }
    }

    private static bool IsTypeCompatible(ITypeSymbol source, ITypeSymbol target)
    {
        // Enforce ReadOnly projection for all Use usages.
        // Block common writable collections:
        if (target is INamedTypeSymbol namedTarget && namedTarget.IsGenericType)
        {
            var name = namedTarget.ConstructUnboundGenericType().ToDisplayString();
            if (name.Contains("ICollection<") ||
                name.Contains("IList<") ||
                name.Contains("IDictionary<") ||
                name.Contains("ISet<") ||
                name.Contains("System.Collections.Generic.List<") ||
                name.Contains("System.Collections.Generic.Dictionary<") ||
                name.Contains("System.Collections.Generic.Queue<") ||
                name.Contains("System.Collections.Generic.Stack<") ||
                name.Contains("System.Collections.Generic.HashSet<") ||
                name.Contains("System.Collections.Generic.LinkedList<"))
                return false;
        }

        if (target.ToDisplayString() == "System.Collections.ICollection" ||
            target.ToDisplayString() == "System.Collections.IList" ||
            target.ToDisplayString() == "System.Collections.IDictionary")
            return false;

        // Check compatibility
        return SymbolEqualityComparer.Default.Equals(source, target) ||
               target.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, source)) ||
               InheritsFrom(source, target);
    }

    private static bool InheritsFrom(ITypeSymbol source, ITypeSymbol target)
    {
        for (var current = source; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, target))
                return true;
        return false;
    }

    private sealed class FieldInfo
    {
        public FieldInfo(INamedTypeSymbol containingType, string name,      ITypeSymbol type, ITypeSymbol ownerType,
                         string           localKey,       bool   isProvide, Location? location, bool isLocalKeyLiteral)
        {
            ContainingType = containingType;
            Name = name;
            Type = type;
            OwnerType = ownerType;
            LocalKey = localKey;
            IsProvide = isProvide;
            Location = location;
            IsLocalKeyLiteral = isLocalKeyLiteral;
        }

        public INamedTypeSymbol ContainingType { get; }
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public ITypeSymbol OwnerType { get; }
        public string LocalKey { get; }
        public bool IsProvide { get; }
        public Location? Location { get; }
        public bool IsLocalKeyLiteral { get; }
    }

    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor ProvideConflict = new(
            "LBG401",
            "Shared field Provide conflict",
            "LocalKey '{0}' is published by multiple owners: {1} and {2}. Shared keys must be unique within their OwnerType.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor TypeMismatch = new(
            "LBG402",
            "Shared field type mismatch",
            "LocalKey '{0}' is consumed as '{1}' but published as '{2}'. Types must be compatible, and [From] only allows read-only projections.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor OrphanUse = new(
            "LBG403",
            "Orphan Shared field From",
            "LocalKey '{0}' is consumed via [From] but no [Provide] provider was found in this compilation.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor InvalidOwnerType = new(
            "LBG404",
            "Invalid Owner Type",
            "OwnerType '{0}' is invalid. Only Layer, Service, or GlobalScope are allowed as OwnerType.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor LiteralKeyWarning = new(
            "LBG405",
            "Literal LocalKey Usage",
            "LocalKey '{0}' is a string literal. It is recommended to use constants for shared field keys to avoid typos.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);
    }
}