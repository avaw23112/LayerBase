using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class AssemblyModuleGenerator : IIncrementalGenerator
{
    private const string AssemblyModuleAttributeName = "LayerBase.Modules.AssemblyModuleAttribute";
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string ScopeAttributeNamespace = "LayerBase.Scope";
    private const string ScopeAttributeMetadataName = "ScopeAttribute`1";

    private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modules = context.SyntaxProvider
                             .ForAttributeWithMetadataName(
                                 AssemblyModuleAttributeName,
                                 static (node, _) => node is ClassDeclarationSyntax,
                                 static (ctx, _) => CreateModule(ctx))
                             .Where(static module => module is not null)
                             .Select(static (module, _) => module!);

        var ownerLayerServices = context.SyntaxProvider
                                        .ForAttributeWithMetadataName(
                                            OwnerLayerAttributeName,
                                            static (node, _) => node is ClassDeclarationSyntax,
                                            static (ctx, _) => CreateOwnerLayerServices(ctx))
                                        .SelectMany(static (items, _) => items);

        var combined = context.CompilationProvider.Combine(modules.Collect()
                                                                  .Combine(ownerLayerServices.Collect()));

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var compilation = source.Left;
            var data = source.Right;
            Execute(spc, compilation, data.Left, data.Right);
        });
    }

    private static void Execute(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<ModuleInfo> modules,
        ImmutableArray<OwnerLayerServiceInfo> ownerLayerServices)
    {
        var moduleList = modules.OrderBy(static module => module.ModuleId, StringComparer.Ordinal)
                                .ThenBy(static module => module.TypeName, StringComparer.Ordinal)
                                .ToArray();

        var fallbackServices = new List<ServiceContributionInfo>();
        foreach (var service in ownerLayerServices)
        {
            if (SymbolEqualityComparer.Default.Equals(service.OwnerLayerType.ContainingAssembly, compilation.Assembly))
            {
                continue;
            }

            if (moduleList.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresModule,
                    service.Location,
                    service.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    service.OwnerLayerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            if (moduleList.Length > 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresSingleModule,
                    service.Location,
                    service.ServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", moduleList.Select(static module =>
                        module.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
                continue;
            }

            fallbackServices.Add(new ServiceContributionInfo(
                ToTypeName(service.OwnerLayerType),
                service.OwnerScopeType == null ? "global::LayerBase.Scope.MainScope" : ToTypeName(service.OwnerScopeType),
                ToTypeName(service.ServiceType),
                ToTypeName(service.ServiceType),
                "global::LayerBase.DI.ServiceLifetime.Singleton"));
        }

        foreach (var module in moduleList)
        {
            GenerateModule(spc, module, moduleList.Length == 1 ? fallbackServices : Array.Empty<ServiceContributionInfo>());
        }
    }

    private static ModuleInfo? CreateModule(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var moduleAttribute = context.Attributes.FirstOrDefault(static attribute =>
            IsAttribute(attribute, AssemblyModuleAttributeName));

        if (moduleAttribute == null)
        {
            return null;
        }

        var moduleId = ReadStringArgument(moduleAttribute, 0);
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            moduleId = typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        return new ModuleInfo(
            typeSymbol,
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : typeSymbol.ContainingNamespace.ToDisplayString(),
            GetAccessibility(typeSymbol),
            moduleId!);
    }

    private static ImmutableArray<OwnerLayerServiceInfo> CreateOwnerLayerServices(
        GeneratorAttributeSyntaxContext context)
    {
        var serviceSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var ownerScope = ReadScopeType(serviceSymbol);
        var builder = ImmutableArray.CreateBuilder<OwnerLayerServiceInfo>();

        foreach (var attribute in context.Attributes)
        {
            if (!IsAttribute(attribute, OwnerLayerAttributeName)) continue;
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol ownerLayerType) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new OwnerLayerServiceInfo(serviceSymbol, ownerLayerType, ownerScope, location));
        }

        return builder.ToImmutable();
    }

    private static void GenerateModule(
        SourceProductionContext spc,
        ModuleInfo module,
        IReadOnlyList<ServiceContributionInfo> fallbackServices)
    {
        var services = fallbackServices.OrderBy(static service => service.OwnerLayerType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.OwnerScopeType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.ServiceType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.ImplementationType, StringComparer.Ordinal)
                                       .ToImmutableArray();

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#nullable enable");

        if (!string.IsNullOrEmpty(module.Namespace))
        {
            source.Append("namespace ").Append(module.Namespace).AppendLine();
            source.AppendLine("{");
        }

        var indent = string.IsNullOrEmpty(module.Namespace) ? string.Empty : "    ";
        source.Append(indent).Append(module.Accessibility).Append(" partial class ").Append(module.TypeName)
              .AppendLine(" : global::LayerBase.Modules.IAssemblyModule");
        source.Append(indent).AppendLine("{");

        source.Append(indent).AppendLine("    private static readonly global::LayerBase.Modules.AssemblyModuleManifest __Manifest =");
        source.Append(indent).AppendLine("        new global::LayerBase.Modules.AssemblyModuleManifest(");
        source.Append(indent).Append("            new global::LayerBase.Modules.AssemblyModuleId(\"")
              .Append(Escape(module.ModuleId))
              .AppendLine("\"),");
        AppendServiceArray(source, indent, services);
        source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.ContextContribution>(),");
        source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.LocalCallContribution>(),");
        source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.LayerToolContribution>());");
        source.AppendLine();

        source.Append(indent).AppendLine("    public global::LayerBase.Modules.AssemblyModuleId Id => __Manifest.ModuleId;");
        source.AppendLine();
        source.Append(indent).AppendLine("    public global::LayerBase.Modules.AssemblyModuleManifest Manifest => __Manifest;");
        source.Append(indent).AppendLine("}");

        if (!string.IsNullOrEmpty(module.Namespace))
        {
            source.AppendLine("}");
        }

        spc.AddSource($"{SanitizeHintName(module.Namespace)}_{module.TypeName}.AssemblyModule.g.cs",
            SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void AppendServiceArray(
        StringBuilder source,
        string indent,
        ImmutableArray<ServiceContributionInfo> services)
    {
        if (services.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.ServiceContribution>(),");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.ServiceContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var service in services)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.ServiceContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(service.ServiceType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(service.ImplementationType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(service.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(service.OwnerScopeType).AppendLine("),");
            source.Append(indent).Append("                    ").Append(service.Lifetime).AppendLine("),");
        }

        source.Append(indent).AppendLine("            },");
    }

    private static INamedTypeSymbol? ReadScopeType(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
            {
                continue;
            }

            var original = attributeClass.OriginalDefinition;
            if (original.MetadataName != ScopeAttributeMetadataName ||
                original.ContainingNamespace.ToDisplayString() != ScopeAttributeNamespace ||
                attributeClass.TypeArguments.Length != 1)
            {
                continue;
            }

            return attributeClass.TypeArguments[0] as INamedTypeSymbol;
        }

        return null;
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal);
    }

    private static string? ReadStringArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as string
            : null;
    }

    private static string ToTypeName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(FullyQualifiedTypeFormat);
    }

    private static string GetAccessibility(INamedTypeSymbol symbol)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "internal"
        };
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string SanitizeHintName(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Global";
        }

        var chars = value.Select(static ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        return new string(chars);
    }

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "AssemblyModuleGenerator";

        public static readonly DiagnosticDescriptor CrossAssemblyOwnerLayerRequiresModule =
            new(
                "LBMOD001",
                "Cross-assembly OwnerLayer requires AssemblyModule",
                "Service '{0}' targets external owner layer '{1}' and must be compiled with an [AssemblyModule] root in the same assembly.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor CrossAssemblyOwnerLayerRequiresSingleModule =
            new(
                "LBMOD002",
                "Cross-assembly OwnerLayer requires exactly one AssemblyModule",
                "Service '{0}' targets an external owner layer but this assembly has multiple modules ({1}). Keep one module root for automatic fallback or split the feature assembly.",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class ModuleInfo
    {
        public ModuleInfo(
            INamedTypeSymbol typeSymbol,
            string typeName,
            string? @namespace,
            string accessibility,
            string moduleId)
        {
            TypeSymbol = typeSymbol;
            TypeName = typeName;
            Namespace = @namespace;
            Accessibility = accessibility;
            ModuleId = moduleId;
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public string TypeName { get; }

        public string? Namespace { get; }

        public string Accessibility { get; }

        public string ModuleId { get; }
    }

    private sealed class OwnerLayerServiceInfo
    {
        public OwnerLayerServiceInfo(
            INamedTypeSymbol serviceType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol? ownerScopeType,
            Location? location)
        {
            ServiceType = serviceType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            Location = location;
        }

        public INamedTypeSymbol ServiceType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol? OwnerScopeType { get; }

        public Location? Location { get; }
    }

    private sealed class ServiceContributionInfo
    {
        public ServiceContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string serviceType,
            string implementationType,
            string lifetime)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string ServiceType { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }
    }
}
