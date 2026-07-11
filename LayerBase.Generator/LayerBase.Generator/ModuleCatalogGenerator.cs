using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ModuleCatalogGenerator : IIncrementalGenerator
{
    private const string AssemblyModuleAttributeName = "LayerBase.Modules.AssemblyModuleAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var currentModules = context.SyntaxProvider
                                    .ForAttributeWithMetadataName(
                                        AssemblyModuleAttributeName,
                                        static (_, _) => true,
                                        static (ctx, _) => GetModule(ctx.TargetSymbol, includeInternal: true))
                                    .Where(static item => item != null)!;

        var referencedModules = context.CompilationProvider
                                       .Select(static (compilation, _) => GetReferencedModules(compilation));

        var combined = currentModules.Collect()
                                     .Combine(referencedModules);

        context.RegisterSourceOutput(combined, static (spc, source) =>
            Generate(spc, source.Left, source.Right));
    }

    private static ImmutableArray<ModuleCatalogEntry> GetReferencedModules(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<ModuleCatalogEntry>();
        var visited = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            AddAssemblyModules(assembly, builder, visited);
        }

        return builder
               .Distinct()
               .OrderBy(static item => item.FullTypeName, System.StringComparer.Ordinal)
               .ToImmutableArray();
    }

    private static void AddAssemblyModules(
        IAssemblySymbol assembly,
        ImmutableArray<ModuleCatalogEntry>.Builder builder,
        HashSet<IAssemblySymbol> visited)
    {
        if (!visited.Add(assembly))
        {
            return;
        }

        AddNamespaceModules(assembly.GlobalNamespace, builder, includeInternal: false);

        foreach (IAssemblySymbol referencedAssembly in assembly.Modules.SelectMany(static module => module.ReferencedAssemblySymbols))
        {
            AddAssemblyModules(referencedAssembly, builder, visited);
        }
    }

    private static void AddNamespaceModules(
        INamespaceSymbol ns,
        ImmutableArray<ModuleCatalogEntry>.Builder builder,
        bool includeInternal)
    {
        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
            ModuleCatalogEntry? entry = GetModule(type, includeInternal);
            if (entry != null)
            {
                builder.Add(entry);
            }
        }

        foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
        {
            AddNamespaceModules(child, builder, includeInternal);
        }
    }

    private static ModuleCatalogEntry? GetModule(ISymbol symbol, bool includeInternal)
    {
        if (symbol is not INamedTypeSymbol type ||
            type.ContainingType != null ||
            !HasAssemblyModuleAttribute(type))
        {
            return null;
        }

        if (!includeInternal &&
            type.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        if (type.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
        {
            return null;
        }

        return new ModuleCatalogEntry(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static bool HasAssemblyModuleAttribute(INamedTypeSymbol type)
    {
        foreach (AttributeData attribute in type.GetAttributes())
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            string? originalName = attribute.AttributeClass?.OriginalDefinition.ToDisplayString();
            if (metadataName == AssemblyModuleAttributeName ||
                originalName == AssemblyModuleAttributeName)
            {
                return true;
            }
        }

        return false;
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<ModuleCatalogEntry?> nullableCurrentModules,
        ImmutableArray<ModuleCatalogEntry> referencedModules)
    {
        var modules = nullableCurrentModules
                      .Where(static item => item != null)
                      .Select(static item => item!)
                      .Concat(referencedModules)
                      .Distinct()
                      .OrderBy(static item => item.FullTypeName, System.StringComparer.Ordinal)
                      .ToImmutableArray();

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Modules");
        builder.AppendLine("{");
        builder.AppendLine("    public static class GeneratedModuleCatalog");
        builder.AppendLine("    {");
        builder.AppendLine("        public static global::LayerBase.Modules.ILayerBaseModule[] Create()");
        builder.AppendLine("        {");
        builder.AppendLine("            return new global::LayerBase.Modules.ILayerBaseModule[]");
        builder.AppendLine("            {");

        foreach (ModuleCatalogEntry module in modules)
        {
            builder.AppendLine($"                {module.FullTypeName}.Instance,");
        }

        builder.AppendLine("            };");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("LayerBase.Modules.GeneratedModuleCatalog.g.cs", builder.ToString());
    }

    private sealed record ModuleCatalogEntry(string FullTypeName);
}
