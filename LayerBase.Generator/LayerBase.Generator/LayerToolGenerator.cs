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
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.AttributeMustInheritSystemAttribute,
                location,
                attributeType.ToDisplayString()));
        }

        var toolId = "unknown.tool";
        INamedTypeSymbol? contractType = null;
        string keyProperty = "Key";
        var allowCache = true;

        if (attributeData != null)
        {
            if (attributeData.ConstructorArguments.Length > 0 &&
                attributeData.ConstructorArguments[0].Value is string constructorToolId &&
                !string.IsNullOrWhiteSpace(constructorToolId))
            {
                toolId = constructorToolId;
            }

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
                Diagnostics.ContractMustBeInterfaceOrClass,
                location,
                contractType.ToDisplayString()));
        }

        ValidateAttributeProperty(attributeType, "Cache", SpecialType.System_Boolean, Diagnostics.CachePropertyMustBeBool,
            diagnostics);
        ValidateAttributeProperty(attributeType, "Path", SpecialType.System_String, Diagnostics.PathPropertyMustBeString,
            diagnostics);
        ValidateAttributeProperty(attributeType, "Factory", null, Diagnostics.FactoryPropertyMustBeType,
            diagnostics, static type => IsSystemType(type));

        return new ToolAttributeInfo(
            attributeType,
            toolId,
            contractType,
            keyProperty,
            allowCache,
            diagnostics.ToImmutable());
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
        var ownerLayerType = GetTypeValue(attribute, "Layer");
        var ownerServiceType = GetTypeValue(attribute, "Service");
        var ownerManagerType = GetTypeValue(attribute, "Manager");
        var factoryType = GetTypeValue(attribute, "Factory");
        var factoryMethodSelection = FindFactoryMethod(implementationType, createContextType);

        foreach (var diagnostic in factoryMethodSelection.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        if (factoryType != null && !ImplementsLayerToolFactory(factoryType, implementationType))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.ExternalFactoryMustImplementInterface,
                location,
                factoryType.ToDisplayString(),
                implementationType.ToDisplayString()));
            factoryType = null;
        }

        var hasConstructor = HasPublicParameterlessConstructor(implementationType);

        if (factoryMethodSelection.Method == null && factoryType == null && !hasConstructor &&
            factoryMethodSelection.Diagnostics.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.NoCreationPath,
                location,
                implementationType.ToDisplayString()));
        }

        return new ToolRegistration(
            contractType,
            implementationType,
            toolInfo.ToolId,
            key ?? string.Empty,
            path,
            cache,
            ownerLayerType,
            ownerServiceType,
            ownerManagerType,
            factoryMethodSelection.Method,
            factoryType,
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
            var ownerLayerType = FormatTypeof(registration.OwnerLayerType);
            var ownerServiceType = FormatTypeof(registration.OwnerServiceType);
            var ownerManagerType = FormatTypeof(registration.OwnerManagerType);
            var factory = CreateFactoryExpression(registration, implementationName);

            builder.Append("            registry.Register<")
                   .Append(contractName)
                   .Append(", ")
                   .Append(implementationName)
                   .AppendLine(">(");
            builder.Append("                toolId: \"").Append(Escape(registration.ToolId)).AppendLine("\",");
            builder.Append("                key: \"").Append(Escape(registration.Key)).AppendLine("\",");
            builder.Append("                path: ").Append(path).AppendLine(",");
            builder.Append("                cache: ").Append(cache).AppendLine(",");
            builder.Append("                ownerLayerType: ").Append(ownerLayerType).AppendLine(",");
            builder.Append("                ownerServiceType: ").Append(ownerServiceType).AppendLine(",");
            builder.Append("                ownerManagerType: ").Append(ownerManagerType).AppendLine(",");
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

    private static string CreateFactoryExpression(ToolRegistration registration, string implementationName)
    {
        if (registration.FactoryMethod != null)
        {
            return $"static context => {implementationName}.{registration.FactoryMethod.Name}(context)";
        }

        if (registration.ExternalFactoryType != null)
        {
            var factoryName = registration.ExternalFactoryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"static context => context.GetFactory<{factoryName}>().Create(context, context.Registry.GetEntry<{implementationName}>())";
        }

        return $"static context => new {implementationName}()";
    }

    private static string FormatTypeof(INamedTypeSymbol? type)
    {
        return type == null
            ? "null"
            : $"typeof({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
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

    private static INamedTypeSymbol? GetTypeValue(AttributeData attribute, string propertyName)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == propertyName && argument.Value.Value is INamedTypeSymbol value)
            {
                return value;
            }
        }

        return null;
    }

    private static FactoryMethodSelection FindFactoryMethod(
        INamedTypeSymbol implementationType,
        INamedTypeSymbol? createContextType)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var methods = implementationType.GetMembers()
                                        .OfType<IMethodSymbol>()
                                        .Where(static member => member.GetAttributes().Any(static attribute =>
                                            attribute.AttributeClass?.ToDisplayString() ==
                                            LayerToolFactoryAttributeName))
                                        .ToArray();

        if (methods.Length == 0)
        {
            return new FactoryMethodSelection(null, ImmutableArray<Diagnostic>.Empty);
        }

        if (methods.Length > 1)
        {
            foreach (var method in methods)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.MultipleFactoryMethods,
                    method.Locations.FirstOrDefault(),
                    implementationType.ToDisplayString()));
            }

            return new FactoryMethodSelection(null, diagnostics.ToImmutable());
        }

        var member = methods[0];
        if (createContextType == null)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.FactorySignatureInvalid,
                member.Locations.FirstOrDefault(),
                member.ToDisplayString()));
            return new FactoryMethodSelection(null, diagnostics.ToImmutable());
        }

        if (!member.IsStatic ||
            member.Parameters.Length != 1 ||
            !SymbolEqualityComparer.Default.Equals(member.Parameters[0].Type, createContextType) ||
            !IsAssignableTo(member.ReturnType, implementationType))
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.FactorySignatureInvalid,
                member.Locations.FirstOrDefault(),
                member.ToDisplayString()));
            return new FactoryMethodSelection(null, diagnostics.ToImmutable());
        }

        return new FactoryMethodSelection(member, ImmutableArray<Diagnostic>.Empty);
    }

    private static bool ImplementsLayerToolFactory(INamedTypeSymbol factoryType, INamedTypeSymbol implementationType)
    {
        foreach (var type in factoryType.AllInterfaces)
        {
            if (type.OriginalDefinition.MetadataName != "ILayerToolFactory`1" ||
                type.OriginalDefinition.ContainingNamespace.ToDisplayString() != "LayerBase.Tooling")
            {
                continue;
            }

            if (type.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], implementationType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
    {
        return type.InstanceConstructors.Any(static constructor =>
            constructor.Parameters.Length == 0 &&
            !constructor.IsStatic &&
            constructor.DeclaredAccessibility == Accessibility.Public);
    }

    private static void ValidateAttributeProperty(
        INamedTypeSymbol attributeType,
        string propertyName,
        SpecialType? specialType,
        DiagnosticDescriptor descriptor,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        Func<ITypeSymbol, bool>? customValidator = null)
    {
        foreach (var property in attributeType.GetMembers(propertyName).OfType<IPropertySymbol>())
        {
            var isValid = specialType.HasValue
                ? property.Type.SpecialType == specialType.Value
                : customValidator != null && customValidator(property.Type);

            if (!isValid)
            {
                diagnostics.Add(Diagnostic.Create(
                    descriptor,
                    property.Locations.FirstOrDefault(),
                    attributeType.ToDisplayString(),
                    propertyName,
                    property.Type.ToDisplayString()));
            }
        }
    }

    private static bool IsSystemType(ITypeSymbol type)
    {
        var display = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display == "global::System.Type" || display == "global::System.Type?";
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
                "LayerTool can only describe Attribute types",
                "Type '{0}' is marked with LayerToolAttribute but is not an Attribute type",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor AttributeMustInheritSystemAttribute =
            new(
                "LBTOOL002",
                "LayerTool target must inherit System.Attribute",
                "Type '{0}' is marked with LayerToolAttribute but does not inherit System.Attribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ContractMustBeInterfaceOrClass =
            new(
                "LBTOOL003",
                "LayerTool contract type is invalid",
                "LayerTool contract '{0}' must be an interface or class",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor TargetMustImplementContract =
            new(
                "LBTOOL004",
                "LayerTool target must implement contract",
                "Type '{0}' is marked as a LayerTool but does not implement contract '{1}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor KeyCannotBeEmpty =
            new(
                "LBTOOL005",
                "LayerTool key cannot be empty",
                "Type '{0}' has an empty LayerTool key",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor DuplicateKey =
            new(
                "LBTOOL006",
                "LayerTool key is duplicated",
                "LayerTool contract '{0}' already has an entry with key '{1}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor NoCreationPath =
            new(
                "LBTOOL007",
                "LayerTool target has no creation path",
                "Type '{0}' must have a public parameterless constructor, a valid LayerToolFactory method, or a valid external factory",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor FactorySignatureInvalid =
            new(
                "LBTOOL008",
                "LayerToolFactory method signature is invalid",
                "LayerToolFactory method '{0}' must be static, accept LayerToolCreateContext, and return the implementation type",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MultipleFactoryMethods =
            new(
                "LBTOOL009",
                "Multiple LayerToolFactory methods are not allowed",
                "Type '{0}' has multiple LayerToolFactory methods",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor CachePropertyMustBeBool =
            new(
                "LBTOOL010",
                "LayerTool Cache property type is invalid",
                "LayerTool attribute '{0}' property '{1}' must be bool, but was '{2}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor PathPropertyMustBeString =
            new(
                "LBTOOL011",
                "LayerTool Path property type is invalid",
                "LayerTool attribute '{0}' property '{1}' must be string, but was '{2}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor FactoryPropertyMustBeType =
            new(
                "LBTOOL012",
                "LayerTool Factory property type is invalid",
                "LayerTool attribute '{0}' property '{1}' must be System.Type, but was '{2}'",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ExternalFactoryMustImplementInterface =
            new(
                "LBTOOL013",
                "LayerTool external factory type is invalid",
                "LayerTool factory '{0}' must implement ILayerToolFactory<{1}>",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class ToolAttributeInfo
    {
        public ToolAttributeInfo(
            INamedTypeSymbol attributeType,
            string toolId,
            INamedTypeSymbol? contractType,
            string keyProperty,
            bool allowCache,
            ImmutableArray<Diagnostic> diagnostics)
        {
            AttributeType = attributeType;
            ToolId = toolId;
            ContractType = contractType;
            KeyProperty = keyProperty;
            AllowCache = allowCache;
            Diagnostics = diagnostics;
        }

        public INamedTypeSymbol AttributeType { get; }
        public string ToolId { get; }
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
            string toolId,
            string key,
            string? path,
            bool cache,
            INamedTypeSymbol? ownerLayerType,
            INamedTypeSymbol? ownerServiceType,
            INamedTypeSymbol? ownerManagerType,
            IMethodSymbol? factoryMethod,
            INamedTypeSymbol? externalFactoryType,
            bool hasConstructor,
            Location? location,
            ImmutableArray<Diagnostic> diagnostics)
        {
            ContractType = contractType;
            ImplementationType = implementationType;
            ToolId = toolId;
            Key = key;
            Path = path;
            Cache = cache;
            OwnerLayerType = ownerLayerType;
            OwnerServiceType = ownerServiceType;
            OwnerManagerType = ownerManagerType;
            FactoryMethod = factoryMethod;
            ExternalFactoryType = externalFactoryType;
            HasConstructor = hasConstructor;
            Location = location;
            Diagnostics = diagnostics;
        }

        public INamedTypeSymbol ContractType { get; }
        public INamedTypeSymbol ImplementationType { get; }
        public string ToolId { get; }
        public string Key { get; }
        public string? Path { get; }
        public bool Cache { get; }
        public INamedTypeSymbol? OwnerLayerType { get; }
        public INamedTypeSymbol? OwnerServiceType { get; }
        public INamedTypeSymbol? OwnerManagerType { get; }
        public IMethodSymbol? FactoryMethod { get; }
        public INamedTypeSymbol? ExternalFactoryType { get; }
        public bool HasConstructor { get; }
        public Location? Location { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public bool IsValid => Diagnostics.IsDefaultOrEmpty && (FactoryMethod != null || ExternalFactoryType != null || HasConstructor);
    }

    private readonly record struct FactoryMethodSelection(
        IMethodSymbol? Method,
        ImmutableArray<Diagnostic> Diagnostics);

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
