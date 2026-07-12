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
    private const string PublishAttributeName = "LayerBase.DI.PublishAttribute";
    private const string FromAttributeName = "LayerBase.DI.FromAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var publishMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            PublishAttributeName,
            static (node, _) => node is FieldDeclarationSyntax or PropertyDeclarationSyntax,
            static (ctx, _) => GetPublishInfo(ctx));

        var fromFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            FromAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx, _) => GetFromInfo(ctx));

        var allFields = publishMembers.Collect().Combine(fromFields.Collect());
        var compilationAndFields = context.CompilationProvider.Combine(allFields);

        context.RegisterSourceOutput(compilationAndFields,
            static (spc, pair) => Analyze(spc, pair.Left, pair.Right.Left, pair.Right.Right));
    }

    private static ResourceInfo? GetPublishInfo(GeneratorAttributeSyntaxContext ctx)
    {
        var attr = ctx.Attributes[0];
        if (attr.ConstructorArguments.Length < 1) return null;

        var localKey = attr.ConstructorArguments[0].Value as string;
        if (string.IsNullOrEmpty(localKey)) return null;

        var symbol = ctx.TargetSymbol;
        var type = symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };

        if (type == null) return null;

        return new ResourceInfo(
            symbol.ContainingType,
            symbol.Name,
            type,
            symbol.ContainingType,
            localKey!,
            isPublish: true,
            symbol.Locations.FirstOrDefault() ?? Location.None,
            IsLocalKeyLiteral(attr, argumentIndex: 0));
    }

    private static ResourceInfo? GetFromInfo(GeneratorAttributeSyntaxContext ctx)
    {
        var fieldSymbol = (IFieldSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];

        if (attr.ConstructorArguments.Length < 2) return null;

        var providerType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        var localKey = attr.ConstructorArguments[1].Value as string;

        if (providerType == null || string.IsNullOrEmpty(localKey)) return null;

        return new ResourceInfo(
            fieldSymbol.ContainingType,
            fieldSymbol.Name,
            fieldSymbol.Type,
            providerType,
            localKey!,
            isPublish: false,
            fieldSymbol.Locations.FirstOrDefault() ?? Location.None,
            IsLocalKeyLiteral(attr, argumentIndex: 1));
    }

    private static bool IsLocalKeyLiteral(AttributeData attr, int argumentIndex)
    {
        var syntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        return syntax?.ArgumentList != null &&
               syntax.ArgumentList.Arguments.Count > argumentIndex &&
               syntax.ArgumentList.Arguments[argumentIndex].Expression.IsKind(SyntaxKind.StringLiteralExpression);
    }

    private static void Analyze(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<ResourceInfo?> publishes,
        ImmutableArray<ResourceInfo?> uses)
    {
        var validPublishes = publishes.Where(p => p != null).Select(p => p!).ToImmutableArray();
        var validUses = uses.Where(p => p != null).Select(p => p!).ToImmutableArray();
        var allValidFields = validPublishes.Concat(validUses);

        var iServiceSymbol = compilation.GetTypeByMetadataName("LayerBase.DI.IService");
        var iLayerContextSymbol = compilation.GetTypeByMetadataName("LayerBase.DI.ILayerContext");
        var scopeReadSymbol = compilation.GetTypeByMetadataName("LayerBase.Scope.ScopeRead`1");

        foreach (var item in allValidFields)
        {
            if (item.IsLocalKeyLiteral)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LiteralKeyWarning, item.Location, item.LocalKey));
            }

            if (iServiceSymbol != null && iLayerContextSymbol != null && !IsValidOwner(item.ContainingType, iServiceSymbol, iLayerContextSymbol))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidOwnerType, item.Location, item.ContainingType.ToDisplayString()));
            }
        }

        var publishMap = new Dictionary<string, ResourceInfo>();
        foreach (var publish in validPublishes)
        {
            var uniqueKey = $"{publish.ProviderType.ToDisplayString()}_{publish.LocalKey}";
            if (publishMap.TryGetValue(uniqueKey, out var existing))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.PublishConflict, publish.Location, publish.LocalKey, existing.ContainingType.Name, publish.ContainingType.Name));
            }
            else
            {
                publishMap[uniqueKey] = publish;
            }
        }

        foreach (var use in validUses)
        {
            if (!TryGetScopeReadViewType(use.Type, scopeReadSymbol, out ITypeSymbol? viewType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.FromRequiresScopeRead, use.Location, use.LocalKey, use.Type.ToDisplayString()));
                continue;
            }

            var uniqueKey = $"{use.ProviderType.ToDisplayString()}_{use.LocalKey}";
            if (publishMap.TryGetValue(uniqueKey, out var publish))
            {
                if (!IsTypeCompatible(publish.Type, viewType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeMismatch, use.Location, use.LocalKey, viewType.ToDisplayString(), publish.Type.ToDisplayString()));
                }
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OrphanUse, use.Location, use.LocalKey));
            }
        }
    }

    private static bool IsValidOwner(INamedTypeSymbol type, INamedTypeSymbol iServiceSymbol, INamedTypeSymbol iLayerContextSymbol)
    {
        return type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, iServiceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iServiceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i, iLayerContextSymbol) ||
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iLayerContextSymbol));
    }

    private static bool TryGetScopeReadViewType(ITypeSymbol type, INamedTypeSymbol? scopeReadSymbol, out ITypeSymbol viewType)
    {
        if (scopeReadSymbol != null &&
            type is INamedTypeSymbol named &&
            named.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, scopeReadSymbol))
        {
            viewType = named.TypeArguments[0];
            return true;
        }

        viewType = type;
        return false;
    }

    private static bool IsTypeCompatible(ITypeSymbol source, ITypeSymbol target)
    {
        return SymbolEqualityComparer.Default.Equals(source, target) ||
               target.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, source)) ||
               source.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target)) ||
               InheritsFrom(source, target);
    }

    private static bool InheritsFrom(ITypeSymbol source, ITypeSymbol target)
    {
        for (var current = source; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ResourceInfo
    {
        public ResourceInfo(INamedTypeSymbol containingType, string name, ITypeSymbol type, ITypeSymbol providerType,
                            string localKey, bool isPublish, Location? location, bool isLocalKeyLiteral)
        {
            ContainingType = containingType;
            Name = name;
            Type = type;
            ProviderType = providerType;
            LocalKey = localKey;
            IsPublish = isPublish;
            Location = location;
            IsLocalKeyLiteral = isLocalKeyLiteral;
        }

        public INamedTypeSymbol ContainingType { get; }
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public ITypeSymbol ProviderType { get; }
        public string LocalKey { get; }
        public bool IsPublish { get; }
        public Location? Location { get; }
        public bool IsLocalKeyLiteral { get; }
    }

    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor PublishConflict = new(
            "LBG401",
            "Scope resource Publish conflict",
            "LocalKey '{0}' is published by multiple owners: {1} and {2}. Scope resource keys must be unique within their provider type.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor TypeMismatch = new(
            "LBG402",
            "Scope resource type mismatch",
            "LocalKey '{0}' is consumed as '{1}' but published as '{2}'. ScopeRead<TView> view type must be compatible with the published resource.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor OrphanUse = new(
            "LBG403",
            "Orphan scope resource From",
            "LocalKey '{0}' is consumed via [From] but no [Publish] provider was found in this compilation.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor InvalidOwnerType = new(
            "LBG404",
            "Invalid scope resource owner type",
            "Scope resource owner '{0}' is invalid. Only IService or ILayerContext types can declare [Publish] or [From].",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor LiteralKeyWarning = new(
            "LBG405",
            "Literal LocalKey Usage",
            "LocalKey '{0}' is a string literal. It is recommended to use constants for scope resource keys to avoid typos.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor FromRequiresScopeRead = new(
            "LBG406",
            "From requires ScopeRead",
            "LocalKey '{0}' is consumed as '{1}'. [From] fields must use ScopeRead<TView>.",
            "Usage",
            DiagnosticSeverity.Error,
            true);
    }
}
