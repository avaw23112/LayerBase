using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SharedFieldAnalyzer : IIncrementalGenerator
{
    private const string PublicAttributeName = "LayerBase.DI.PublicAttribute";
    private const string FromAttributeName = "LayerBase.DI.FromAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var publicFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            PublicAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx, _) => GetFieldInfo(ctx, true));

        var fromFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            FromAttributeName,
            static (node, _) => node is FieldDeclarationSyntax,
            static (ctx, _) => GetFieldInfo(ctx, false));

        var allFields = publicFields.Collect().Combine(fromFields.Collect());

        context.RegisterSourceOutput(allFields, static (spc, pair) => Analyze(spc, pair.Left, pair.Right));
    }

    private static FieldInfo GetFieldInfo(GeneratorAttributeSyntaxContext ctx, bool isPublic)
    {
        var fieldSymbol = (IFieldSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];
        
        var scope = (int)attr.ConstructorArguments[0].Value!;
        var key = (string)attr.ConstructorArguments[1].Value!;
        
        return new FieldInfo(
            fieldSymbol.ContainingType,
            fieldSymbol.Name,
            fieldSymbol.Type,
            scope,
            key,
            isPublic,
            fieldSymbol.Locations.FirstOrDefault());
    }

    private static void Analyze(SourceProductionContext spc, ImmutableArray<FieldInfo> publics, ImmutableArray<FieldInfo> froms)
    {
        // 1. Check for Public conflicts
        var publicMap = new Dictionary<string, FieldInfo>();
        foreach (var p in publics)
        {
            var uniqueKey = $"{p.Scope}_{p.Key}";
            if (p.Scope == 1) // Layer scope
            {
                // In real world, we need to know WHICH layer. 
                // For simplicity in analyzer, we check within same containing type or context.
                // But usually Key should be unique per Layer Type.
                // uniqueKey += "_" + GetOwnerLayer(p.Owner); 
            }

            if (publicMap.TryGetValue(uniqueKey, out var existing))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.PublicConflict, p.Location, p.Key, p.Owner.Name, existing.Owner.Name));
            }
            else
            {
                publicMap[uniqueKey] = p;
            }
        }

        // 2. Check for From matches and type compatibility
        foreach (var f in froms)
        {
            var uniqueKey = $"{f.Scope}_{f.Key}";
            if (publicMap.TryGetValue(uniqueKey, out var p))
            {
                if (!IsTypeCompatible(p.Type, f.Type))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeMismatch, f.Location, f.Key, f.Type.Name, p.Type.Name));
                }
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.OrphanFrom, f.Location, f.Key));
            }
        }
    }

    private static bool IsTypeCompatible(ITypeSymbol source, ITypeSymbol target)
    {
        // Basic check: same type or assignable
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
        public FieldInfo(INamedTypeSymbol owner, string name, ITypeSymbol type, int scope, string key, bool isPublic, Location? location)
        {
            Owner = owner;
            Name = name;
            Type = type;
            Scope = scope;
            Key = key;
            IsPublic = isPublic;
            Location = location;
        }

        public INamedTypeSymbol Owner { get; }
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public int Scope { get; }
        public string Key { get; }
        public bool IsPublic { get; }
        public Location? Location { get; }
    }

    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor PublicConflict = new(
            "LBG401",
            "Shared field Public conflict",
            "Key '{0}' is published by multiple owners: {1} and {2}. Shared keys must be unique within their scope.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor TypeMismatch = new(
            "LBG402",
            "Shared field type mismatch",
            "Key '{0}' is consumed as '{1}' but published as '{2}'. Types must be compatible.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor OrphanFrom = new(
            "LBG403",
            "Orphan Shared field From",
            "Key '{0}' is consumed via [From] but no [Public] provider was found in this compilation.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);
    }
}
