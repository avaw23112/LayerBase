using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SharedFieldAnalyzer : IIncrementalGenerator
{
    private const string ProvideAttributeName = "LayerBase.DI.ProvideAttribute";
    private const string UseAttributeName = "LayerBase.DI.UseAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provideFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            ProvideAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx,  _) => GetFieldInfo(ctx, true));

        var useFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            UseAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx,  _) => GetFieldInfo(ctx, false));

        var allFields = provideFields.Collect().Combine(useFields.Collect());

        context.RegisterSourceOutput(allFields, static (spc, pair) => Analyze(spc, pair.Left, pair.Right));
    }

    private static FieldInfo? GetFieldInfo(GeneratorAttributeSyntaxContext ctx, bool isProvide)
    {
        var fieldSymbol = (IFieldSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];

        if (attr.ConstructorArguments.Length < 2) return null;

        var ownerType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        var localKey = attr.ConstructorArguments[1].Value as string;

        if (ownerType == null || string.IsNullOrEmpty(localKey)) return null;

        return new FieldInfo(
            fieldSymbol.ContainingType,
            fieldSymbol.Name,
            fieldSymbol.Type,
            ownerType,
            localKey,
            isProvide,
            fieldSymbol.Locations.FirstOrDefault());
    }

    private static void Analyze(SourceProductionContext    spc, ImmutableArray<FieldInfo?> provides,
                                ImmutableArray<FieldInfo?> uses)
    {
        var validProvides = provides.Where(p => p != null).Select(p => p!).ToImmutableArray();
        var validUses = uses.Where(p => p != null).Select(p => p!).ToImmutableArray();

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
                         string           localKey,       bool   isProvide, Location?   location)
        {
            ContainingType = containingType;
            Name = name;
            Type = type;
            OwnerType = ownerType;
            LocalKey = localKey;
            IsProvide = isProvide;
            Location = location;
        }

        public INamedTypeSymbol ContainingType { get; }
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public ITypeSymbol OwnerType { get; }
        public string LocalKey { get; }
        public bool IsProvide { get; }
        public Location? Location { get; }
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
            "LocalKey '{0}' is consumed as '{1}' but published as '{2}'. Types must be compatible, and [Use] only allows read-only projections.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor OrphanUse = new(
            "LBG403",
            "Orphan Shared field Use",
            "LocalKey '{0}' is consumed via [Use] but no [Provide] provider was found in this compilation.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);
    }
}

