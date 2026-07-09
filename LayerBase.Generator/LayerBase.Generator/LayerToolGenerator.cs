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
public sealed class LayerToolGenerator : IIncrementalGenerator
{
    private const string LayerToolAttributeName = "LayerBase.Tooling.LayerToolAttribute";
    private const string LayerToolFactoryAttributeName = "LayerBase.Tooling.LayerToolFactoryAttribute";
    private const string LayerToolCreateContextName = "LayerBase.Tooling.LayerToolCreateContext";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var toolAttributes = context.SyntaxProvider
                                    .ForAttributeWithMetadataName(
                                        LayerToolAttributeName,
                                        static (node, _) => node is ClassDeclarationSyntax,
                                        static (ctx, _) => CreateToolAttribute(ctx))
                                    .Where(static item => item is not null)
                                    .Select(static (item, _) => item!);

        var candidateTypes = context.SyntaxProvider
                                    .CreateSyntaxProvider(
                                        static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                                        static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node))
                                    .Where(static symbol => symbol is not null)
                                    .Select(static (symbol, _) => symbol!);

        var collected = toolAttributes.Collect().Combine(candidateTypes.Collect());
        var compilationAndData = context.CompilationProvider.Combine(collected);

        context.RegisterSourceOutput(compilationAndData, static (spc, source) =>
        {
            Execute(spc, source.Left, source.Right.Left, source.Right.Right);
        });
    }

    private static void Execute(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<ToolAttributeInfo> toolAttributes,
        ImmutableArray<INamedTypeSymbol> candidateTypes)
    {
        foreach (var diagnostic in toolAttributes.SelectMany(static info => info.Diagnostics))
        {
            spc.ReportDiagnostic(diagnostic);
        }

        var validToolAttributes = toolAttributes.Where(static info => info.IsValid).ToArray();
        if (validToolAttributes.Length == 0)
        {
            return;
        }

        var createContextType = compilation.GetTypeByMetadataName(LayerToolCreateContextName);
        var registrations = new List<ToolRegistration>();

        foreach (var targetType in candidateTypes)
        {
            foreach (var attribute in targetType.GetAttributes())
            {
                var toolInfo = FindToolAttribute(validToolAttributes, attribute.AttributeClass);
                if (toolInfo == null)
                {
                    continue;
                }

                registrations.Add(CreateRegistration(targetType, attribute, toolInfo, createContextType));
            }
        }

        foreach (var diagnostic in registrations.SelectMany(static registration => registration.Diagnostics))
        {
            spc.ReportDiagnostic(diagnostic);
        }

        ReportDuplicateKeys(spc, registrations);

        var validRegistrations = registrations
                                 .Where(static registration => registration.IsValid)
                                 .GroupBy(static registration => new ContractKey(registration.ContractType, registration.Key),
                                     ContractKeyComparer.Instance)
                                 .Where(static group => group.Count() == 1)
                                 .Select(static group => group.First())
                                 .OrderBy(static registration => registration.ContractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                                 .ThenBy(static registration => registration.Key)
                                 .ThenBy(static registration => registration.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                                 .ToArray();

        if (validRegistrations.Length == 0)
        {
            return;
        }

        var sourceText = GenerateSource(validRegistrations);
        spc.AddSource("LayerToolRegistry.g.cs", SourceText.From(sourceText, Encoding.UTF8));
    }

    private static ToolAttributeInfo? CreateToolAttribute(GeneratorAttributeSyntaxContext context)
    {
        var attributeType = context.TargetSymbol as INamedTypeSymbol;
        if (attributeType == null)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var attributeData = context.Attributes.FirstOrDefault();
        var location = attributeData?.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                       ?? attributeType.Locations.FirstOrDefault();

        if (!InheritsFromAttribute(attributeType))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.AttributeMustInheritAttribute,
                location,
                attributeType.ToDisplayString()));
        }

        INamedTypeSymbol? contractType = null;
        string keyProperty = "Key";
        var allowCache = true;

        if (attributeData != null)
        {
            foreach (var argument in attributeData.NamedArguments)
            {
                switch (argument.Key)
                {
                    case "Contract" when argument.Value.Value is INamedTypeSymbol contract:
                        contractType = contract;
                        break;
                    case "DefaultKeyProperty" when argument.Value.Value is string propertyName:
                        keyProperty = string.IsNullOrWhiteSpace(propertyName) ? "Key" : propertyName;
                        break;
                    case "AllowCache" when argument.Value.Value is bool value:
                        allowCache = value;
                        break;
                }
            }
        }

        if (contractType != null && contractType.TypeKind is not TypeKind.Interface and not TypeKind.Class)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.ContractMustBeInterfaceOrBaseClass,
                location,
                contractType.ToDisplayString()));
        }

        return new ToolAttributeInfo(attributeType, contractType, keyProperty, allowCache, diagnostics.ToImmutable());
    }

    private static ToolRegistration CreateRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ToolAttributeInfo toolInfo,
        INamedTypeSymbol? createContextType)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                       ?? implementationType.Locations.FirstOrDefault();
        var contractType = toolInfo.ContractType ?? implementationType;

        if (!IsAssignableTo(implementationType, contractType))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.TargetMustImplementContract,
                location,
                implementationType.ToDisplayString(),
                contractType.ToDisplayString()));
        }

        var key = GetStringValue(attribute, toolInfo.KeyProperty);
        if (string.IsNullOrWhiteSpace(key))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.KeyCannotBeEmpty,
                location,
                implementationType.ToDisplayString()));
            key = string.Empty;
        }

        var path = GetStringValue(attribute, "Path");
        var cache = toolInfo.AllowCache && GetBoolValue(attribute, "Cache");
        var factoryMethod = FindFactoryMethod(implementationType, createContextType);
        var hasConstructor = HasPublicParameterlessConstructor(implementationType);

        if (factoryMethod == null && !hasConstructor)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.NoCreationPath,
                location,
                implementationType.ToDisplayString()));
        }

        return new ToolRegistration(
            contractType,
            implementationType,
            key ?? string.Empty,
            path,
            cache,
            factoryMethod,
            hasConstructor,
            location,
            diagnostics.ToImmutable());
    }

    private static void ReportDuplicateKeys(SourceProductionContext spc, List<ToolRegistration> registrations)
    {
        var groups = registrations
                     .Where(static registration => !string.IsNullOrWhiteSpace(registration.Key))
                     .GroupBy(static registration => new ContractKey(registration.ContractType, registration.Key),
                         ContractKeyComparer.Instance);

        foreach (var group in groups)
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (var registration in group)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateKey,
                    registration.Location,
                    registration.ContractType.ToDisplayString(),
                    registration.Key));
            }
        }
    }

    private static string GenerateSource(IReadOnlyList<ToolRegistration> registrations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase");
        builder.AppendLine("{");
        builder.AppendLine("    public static partial class GameGeneratedLayerTools");
        builder.AppendLine("    {");
        builder.AppendLine("        public static void Register(global::LayerBase.Tooling.LayerToolRegistry registry)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (registry == null) throw new global::System.ArgumentNullException(nameof(registry));");
        builder.AppendLine();

        foreach (var registration in registrations)
        {
            var contractName = registration.ContractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implementationName = registration.ImplementationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var path = registration.Path == null ? "null" : $"\"{Escape(registration.Path)}\"";
            var cache = registration.Cache ? "true" : "false";
            var factory = registration.FactoryMethod == null
                ? $"static context => new {implementationName}()"
                : $"static context => {implementationName}.{registration.FactoryMethod.Name}(context)";

            builder.Append("            registry.Register<")
                   .Append(contractName)
                   .Append(", ")
                   .Append(implementationName)
                   .AppendLine(">(");
            builder.Append("                key: \"").Append(Escape(registration.Key)).AppendLine("\",");
            builder.Append("                path: ").Append(path).AppendLine(",");
            builder.Append("                cache: ").Append(cache).AppendLine(",");
            builder.Append("                factory: ").Append(factory).AppendLine(");");
            builder.AppendLine();
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static partial class LayerRuntimeBuilderGeneratedExtensions");
        builder.AppendLine("    {");
        builder.AppendLine("        public static global::LayerBase.LayerRuntime.LayersBuilder UseGeneratedLayerTools(");
        builder.AppendLine("            this global::LayerBase.LayerRuntime.LayersBuilder builder)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (builder == null) throw new global::System.ArgumentNullException(nameof(builder));");
        builder.AppendLine("            builder.ConfigureTools(static registry => GameGeneratedLayerTools.Register(registry));");
        builder.AppendLine("            return builder;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static ToolAttributeInfo? FindToolAttribute(
        IEnumerable<ToolAttributeInfo> toolAttributes,
        INamedTypeSymbol? attributeType)
    {
        if (attributeType == null)
        {
            return null;
        }

        foreach (var toolInfo in toolAttributes)
        {
            if (SymbolEqualityComparer.Default.Equals(toolInfo.AttributeType, attributeType))
            {
                return toolInfo;
            }
        }

        return null;
    }

    private static string? GetStringValue(AttributeData attribute, string propertyName)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == propertyName && argument.Value.Value is string value)
            {
                return value;
            }
        }

        if (propertyName == "Key" && attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string constructorKey)
        {
            return constructorKey;
        }

        return null;
    }

    private static bool GetBoolValue(AttributeData attribute, string propertyName)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == propertyName && argument.Value.Value is bool value)
            {
                return value;
            }
        }

        return false;
    }

    private static IMethodSymbol? FindFactoryMethod(
        INamedTypeSymbol implementationType,
        INamedTypeSymbol? createContextType)
    {
        if (createContextType == null)
        {
            return null;
        }

        foreach (var member in implementationType.GetMembers().OfType<IMethodSymbol>())
        {
            if (!member.GetAttributes().Any(static attribute =>
                    attribute.AttributeClass?.ToDisplayString() == LayerToolFactoryAttributeName))
            {
                continue;
            }

            if (!member.IsStatic || member.Parameters.Length != 1)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(member.Parameters[0].Type, createContextType))
            {
                continue;
            }

            if (!IsAssignableTo(member.ReturnType, implementationType))
            {
                continue;
            }

            return member;
        }

        return null;
    }

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
    {
        return type.InstanceConstructors.Any(static constructor =>
            constructor.Parameters.Length == 0 &&
            !constructor.IsStatic &&
            constructor.DeclaredAccessibility == Accessibility.Public);
    }

    private static bool InheritsFromAttribute(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Attribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAssignableTo(ITypeSymbol source, ITypeSymbol target)
    {
        if (SymbolEqualityComparer.Default.Equals(source, target))
        {
            return true;
        }

        if (target.TypeKind == TypeKind.Interface && source is INamedTypeSymbol namedSource)
        {
            return namedSource.AllInterfaces.Any(type => SymbolEqualityComparer.Default.Equals(type, target));
        }

        for (var current = (source as INamedTypeSymbol)?.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "LayerTool";

        public static readonly DiagnosticDescriptor AttributeMustInheritAttribute =
            new(
                "LBTOOL001",
                "LayerTool target must inherit Attribute",
                "Type '{0}' is marked with LayerToolAttribute but does not inherit System.Attribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ContractMustBeInterfaceOrBaseClass =
            new(
                "LBTOOL002",
                "LayerTool contract type is invalid",
                "LayerTool contract '{0}' must be an interface or base class",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor TargetMustImplementContract =
            new(
                "LBTOOL003",
                "LayerTool target must implement contract",
                "Type '{0}' is marked as a LayerTool but does not implement contract '{1}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor NoCreationPath =
            new(
                "LBTOOL004",
                "LayerTool target has no creation path",
                "Type '{0}' must have a public parameterless constructor or a valid LayerToolFactory method",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor DuplicateKey =
            new(
                "LBTOOL005",
                "LayerTool key is duplicated",
                "LayerTool contract '{0}' already has an entry with key '{1}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor KeyCannotBeEmpty =
            new(
                "LBTOOL006",
                "LayerTool key cannot be empty",
                "Type '{0}' has an empty LayerTool key",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class ToolAttributeInfo
    {
        public ToolAttributeInfo(
            INamedTypeSymbol attributeType,
            INamedTypeSymbol? contractType,
            string keyProperty,
            bool allowCache,
            ImmutableArray<Diagnostic> diagnostics)
        {
            AttributeType = attributeType;
            ContractType = contractType;
            KeyProperty = keyProperty;
            AllowCache = allowCache;
            Diagnostics = diagnostics;
        }

        public INamedTypeSymbol AttributeType { get; }
        public INamedTypeSymbol? ContractType { get; }
        public string KeyProperty { get; }
        public bool AllowCache { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public bool IsValid => Diagnostics.IsDefaultOrEmpty;
    }

    private sealed class ToolRegistration
    {
        public ToolRegistration(
            INamedTypeSymbol contractType,
            INamedTypeSymbol implementationType,
            string key,
            string? path,
            bool cache,
            IMethodSymbol? factoryMethod,
            bool hasConstructor,
            Location? location,
            ImmutableArray<Diagnostic> diagnostics)
        {
            ContractType = contractType;
            ImplementationType = implementationType;
            Key = key;
            Path = path;
            Cache = cache;
            FactoryMethod = factoryMethod;
            HasConstructor = hasConstructor;
            Location = location;
            Diagnostics = diagnostics;
        }

        public INamedTypeSymbol ContractType { get; }
        public INamedTypeSymbol ImplementationType { get; }
        public string Key { get; }
        public string? Path { get; }
        public bool Cache { get; }
        public IMethodSymbol? FactoryMethod { get; }
        public bool HasConstructor { get; }
        public Location? Location { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public bool IsValid => Diagnostics.IsDefaultOrEmpty && (FactoryMethod != null || HasConstructor);
    }

    private readonly struct ContractKey
    {
        public ContractKey(INamedTypeSymbol contractType, string key)
        {
            ContractType = contractType;
            Key = key;
        }

        public INamedTypeSymbol ContractType { get; }
        public string Key { get; }
    }

    private sealed class ContractKeyComparer : IEqualityComparer<ContractKey>
    {
        public static readonly ContractKeyComparer Instance = new();

        public bool Equals(ContractKey x, ContractKey y)
        {
            return SymbolEqualityComparer.Default.Equals(x.ContractType, y.ContractType) &&
                   x.Key == y.Key;
        }

        public int GetHashCode(ContractKey obj)
        {
            return (SymbolEqualityComparer.Default.GetHashCode(obj.ContractType) * 397) ^
                   obj.Key.GetHashCode();
        }
    }
}
