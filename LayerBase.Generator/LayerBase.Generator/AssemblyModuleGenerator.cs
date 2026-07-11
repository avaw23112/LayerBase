using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class AssemblyModuleGenerator : IIncrementalGenerator
{
    private const string AssemblyModuleAttributeName = "LayerBase.Modules.AssemblyModuleAttribute";
    private const string ModuleIgnoreAttributeName = "LayerBase.Modules.ModuleIgnoreAttribute";
    private const string LayerBaseTypeName = "LayerBase.Layers.Layer";
    private const string ScopeOptionsAttributeName = "LayerBase.Scope.ScopeOptionsAttribute";
    private const string ScopeAttributeName = "LayerBase.Scope.ScopeAttribute<TScope>";
    private const string ScopeCallContractAttributeName = "LayerBase.Scope.ScopeCallAttribute<TScope, TResult>";
    private const string ScopeEventContractAttributeName = "LayerBase.Scope.ScopeEventAttribute<TScope>";
    private const string ScopeCallHandlerAttributeName = "LayerBase.Scope.ScopeCallAttribute";
    private const string ScopeEventHandlerAttributeName = "LayerBase.Scope.ScopeEventAttribute";
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string OwnerServiceAttributeName = "LayerBase.DI.Options.OwnerServiceAttribute";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ILayerContextMetadataName = "LayerBase.DI.ILayerContext";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modules = context.SyntaxProvider
                             .ForAttributeWithMetadataName(
                                 AssemblyModuleAttributeName,
                                 static (_, _) => true,
                                 static (ctx, _) => GetModule(ctx))
                             .Where(static item => item != null)!;

        var types = context.SyntaxProvider
                           .CreateSyntaxProvider(
                               static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                               static (ctx, _) => GetTypeContribution(ctx))
                           .Where(static item => item != null)!;

        var handlers = context.SyntaxProvider
                              .CreateSyntaxProvider(
                                  static (node, _) => node is MethodDeclarationSyntax,
                                  static (ctx, _) => GetHandlerContribution(ctx))
                              .Where(static item => item != null)!;

        var combined = modules.Collect()
                              .Combine(types.Collect())
                              .Combine(handlers.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) =>
            Generate(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static ModuleInfo? GetModule(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol ||
            symbol.ContainingType != null)
        {
            return null;
        }

        return new ModuleInfo(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            GetAccessibility(symbol),
            IsPartial(symbol),
            symbol.Locations.FirstOrDefault() ?? Location.None);
    }

    private static TypeContribution? GetTypeContribution(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax declaration)
        {
            return null;
        }

        INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(declaration);
        if (symbol == null ||
            symbol.ContainingType != null)
        {
            return null;
        }

        bool ignored = HasAttribute(symbol, ModuleIgnoreAttributeName);
        string typeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (InheritsFrom(symbol, LayerBaseTypeName))
        {
            return TypeContribution.LayerContract(typeName);
        }

        AttributeData? scopeOptions = FindAttribute(symbol, ScopeOptionsAttributeName);
        if (scopeOptions != null)
        {
            return TypeContribution.ScopeDefinition(
                typeName,
                GetIntArgument(scopeOptions, 0, "threading", 0),
                GetIntArgument(scopeOptions, 1, "clock", 0),
                GetIntArgument(scopeOptions, 2, "tickRateHz", 0),
                GetIntArgument(scopeOptions, 3, "stopPolicy", 0));
        }

        AttributeData? scopeCall = FindAttributeByOriginalDefinition(symbol, ScopeCallContractAttributeName);
        if (scopeCall?.AttributeClass is { TypeArguments.Length: >= 2 } callAttribute)
        {
            return TypeContribution.MessageContract(
                typeName,
                callAttribute.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                callAttribute.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "Call");
        }

        AttributeData? scopeEvent = FindAttributeByOriginalDefinition(symbol, ScopeEventContractAttributeName);
        if (scopeEvent?.AttributeClass is { TypeArguments.Length: >= 1 } eventAttribute)
        {
            return TypeContribution.MessageContract(
                typeName,
                eventAttribute.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "void",
                "Event");
        }

        if (!ignored && ImplementsInterface(symbol, IServiceMetadataName))
        {
            var ownerLayers = symbol.GetAttributes()
                                    .Where(static attr => attr.AttributeClass?.ToDisplayString() == OwnerLayerAttributeName)
                                    .Select(static attr => GetTypeArgument(attr, 0))
                                    .Where(static value => value != null)
                                    .Select(static value => value!)
                                    .Distinct(StringComparer.Ordinal)
                                    .OrderBy(static value => value, StringComparer.Ordinal)
                                    .ToImmutableArray();

            AttributeData? scopeAttribute = FindAttributeByOriginalDefinition(symbol, ScopeAttributeName);
            string ownerScope = "global::LayerBase.Scope.MainScope";
            if (scopeAttribute?.AttributeClass is { TypeArguments.Length: >= 1 } scopeAttr)
            {
                ownerScope = scopeAttr.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            return TypeContribution.Service(
                typeName,
                ownerLayers,
                ownerScope,
                symbol.Name);
        }

        AttributeData? ownerService = FindAttribute(symbol, OwnerServiceAttributeName);
        if (!ignored && ownerService != null && ImplementsInterface(symbol, ILayerContextMetadataName))
        {
            string? serviceType = GetTypeArgument(ownerService, 0);
            if (serviceType != null)
            {
                return TypeContribution.Context(typeName, serviceType, symbol.Name);
            }
        }

        return null;
    }

    private static HandlerContribution? GetHandlerContribution(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodSyntax)
        {
            return null;
        }

        IMethodSymbol? method = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
        if (method?.ContainingType == null ||
            HasAttribute(method.ContainingType, ModuleIgnoreAttributeName))
        {
            return null;
        }

        if (!ImplementsInterface(method.ContainingType, IServiceMetadataName) ||
            method.Parameters.Length != 1)
        {
            return null;
        }

        string? kind = null;
        if (HasAttribute(method, ScopeCallHandlerAttributeName))
        {
            kind = "Call";
        }
        else if (HasAttribute(method, ScopeEventHandlerAttributeName))
        {
            kind = "Event";
        }

        if (kind == null)
        {
            return null;
        }

        AttributeData? scopeAttribute = FindAttributeByOriginalDefinition(method.ContainingType, ScopeAttributeName);
        string ownerScope = "global::LayerBase.Scope.MainScope";
        if (scopeAttribute?.AttributeClass is { TypeArguments.Length: >= 1 } scopeAttr)
        {
            ownerScope = scopeAttr.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return new HandlerContribution(
            method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ownerScope,
            kind);
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<ModuleInfo?> nullableModules,
        ImmutableArray<TypeContribution?> nullableTypes,
        ImmutableArray<HandlerContribution?> nullableHandlers)
    {
        var modules = nullableModules
                      .Where(static item => item != null)
                      .Select(static item => item!)
                      .GroupBy(static item => item.FullTypeName)
                      .Select(static group => group.First())
                      .OrderBy(static item => item.FullTypeName, StringComparer.Ordinal)
                      .ToImmutableArray();
        if (modules.Length == 0)
        {
            return;
        }

        var types = nullableTypes
                    .Where(static item => item != null)
                    .Select(static item => item!)
                    .ToImmutableArray();
        var handlers = nullableHandlers
                       .Where(static item => item != null)
                       .Select(static item => item!)
                       .OrderBy(static item => item.MessageType, StringComparer.Ordinal)
                       .ThenBy(static item => item.ServiceType, StringComparer.Ordinal)
                       .ToImmutableArray();

        var layerContracts = types
                             .Where(static item => item.Kind == TypeContributionKind.LayerContract)
                             .Select(static item => item.TypeName)
                             .Distinct(StringComparer.Ordinal)
                             .OrderBy(static item => item, StringComparer.Ordinal)
                             .ToImmutableArray();
        var scopeDefinitions = types
                               .Where(static item => item.Kind == TypeContributionKind.ScopeDefinition)
                               .GroupBy(static item => item.TypeName)
                               .Select(static group => group.First())
                               .OrderBy(static item => item.TypeName, StringComparer.Ordinal)
                               .ToImmutableArray();
        var messageContracts = types
                               .Where(static item => item.Kind == TypeContributionKind.MessageContract)
                               .GroupBy(static item => item.TypeName)
                               .Select(static group => group.First())
                               .OrderBy(static item => item.TypeName, StringComparer.Ordinal)
                               .ToImmutableArray();
        var services = types
                       .Where(static item => item.Kind == TypeContributionKind.Service)
                       .GroupBy(static item => item.TypeName)
                       .Select(static group => group.First())
                       .OrderBy(static item => item.TypeName, StringComparer.Ordinal)
                       .ToImmutableArray();
        var contexts = types
                       .Where(static item => item.Kind == TypeContributionKind.Context)
                       .GroupBy(static item => item.TypeName)
                       .Select(static group => group.First())
                       .OrderBy(static item => item.TypeName, StringComparer.Ordinal)
                       .ToImmutableArray();

        GenerateFactories(context, services, contexts);

        foreach (ModuleInfo module in modules)
        {
            if (!module.IsPartial)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.AssemblyModuleOwnerMustBePartial,
                    module.Location,
                    module.FullTypeName));
                continue;
            }

            GenerateModulePartial(context, module);
            GenerateManifest(
                context,
                module,
                layerContracts,
                scopeDefinitions,
                messageContracts,
                services,
                contexts,
                handlers);
        }
    }

    private static void GenerateModulePartial(SourceProductionContext context, ModuleInfo module)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (module.Namespace != "<global namespace>")
        {
            builder.AppendLine($"namespace {module.Namespace}");
            builder.AppendLine("{");
        }

        builder.AppendLine($"    {module.Accessibility} partial class {module.Name} : global::LayerBase.Modules.ILayerBaseModule");
        builder.AppendLine("    {");
        builder.AppendLine($"        public static {module.Name} Instance {{ get; }} = new {module.Name}();");
        builder.AppendLine();
        builder.AppendLine($"        public global::LayerBase.Modules.ModuleManifest Manifest => Generated{module.Name}Manifest.Value;");
        builder.AppendLine();
        builder.AppendLine("        global::LayerBase.Modules.ModuleManifest global::LayerBase.Modules.ILayerBaseModule.Manifest => Manifest;");
        builder.AppendLine("    }");

        if (module.Namespace != "<global namespace>")
        {
            builder.AppendLine("}");
        }

        context.AddSource($"{SanitizeIdentifier(module.FullTypeName)}.AssemblyModule.g.cs", builder.ToString());
    }

    private static void GenerateManifest(
        SourceProductionContext context,
        ModuleInfo module,
        ImmutableArray<string> layerContracts,
        ImmutableArray<TypeContribution> scopeDefinitions,
        ImmutableArray<TypeContribution> messageContracts,
        ImmutableArray<TypeContribution> services,
        ImmutableArray<TypeContribution> contexts,
        ImmutableArray<HandlerContribution> handlers)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (module.Namespace != "<global namespace>")
        {
            builder.AppendLine($"namespace {module.Namespace}");
            builder.AppendLine("{");
        }

        builder.AppendLine($"    internal static class Generated{module.Name}Manifest");
        builder.AppendLine("    {");
        builder.AppendLine("        internal static readonly global::LayerBase.Modules.ModuleManifest Value = new global::LayerBase.Modules.ModuleManifest(");
        AppendLayerContracts(builder, layerContracts);
        builder.AppendLine(",");
        AppendScopeDefinitions(builder, scopeDefinitions);
        builder.AppendLine(",");
        AppendMessageContracts(builder, messageContracts);
        builder.AppendLine(",");
        AppendServices(builder, services);
        builder.AppendLine(",");
        AppendContexts(builder, contexts);
        builder.AppendLine(",");
        AppendHandlers(builder, handlers);
        builder.AppendLine(");");
        builder.AppendLine("    }");

        if (module.Namespace != "<global namespace>")
        {
            builder.AppendLine("}");
        }

        context.AddSource($"{SanitizeIdentifier(module.FullTypeName)}.ModuleManifest.g.cs", builder.ToString());
    }

    private static void GenerateFactories(
        SourceProductionContext context,
        ImmutableArray<TypeContribution> services,
        ImmutableArray<TypeContribution> contexts)
    {
        if (services.Length == 0 && contexts.Length == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Modules");
        builder.AppendLine("{");
        builder.AppendLine("    internal static class GeneratedModuleFactories");
        builder.AppendLine("    {");

        foreach (TypeContribution service in services)
        {
            string method = GetServiceFactoryName(service.TypeName);
            builder.AppendLine($"        internal static global::LayerBase.DI.IService {method}()");
            builder.AppendLine("        {");
            builder.AppendLine($"            return new {service.TypeName}();");
            builder.AppendLine("        }");
            builder.AppendLine();

            string bindMethod = GetServiceBindingName(service.TypeName);
            builder.AppendLine($"        internal static void {bindMethod}(global::LayerBase.DI.IService service, global::LayerBase.Scope.ScopeRuntime ownerScope, int serviceSlot)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (service is global::LayerBase.Scope.IGeneratedScopeServiceBinding binding)");
            builder.AppendLine("            {");
            builder.AppendLine("                binding.BindScope(ownerScope, serviceSlot);");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        foreach (TypeContribution contextContribution in contexts)
        {
            string method = GetContextFactoryName(contextContribution.TypeName);
            builder.AppendLine($"        internal static global::LayerBase.DI.ILayerContext {method}(global::LayerBase.DI.IService ownerService)");
            builder.AppendLine("        {");
            builder.AppendLine($"            return new {contextContribution.TypeName}();");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("LayerBase.Modules.GeneratedModuleFactories.g.cs", builder.ToString());
    }

    private static void AppendLayerContracts(StringBuilder builder, ImmutableArray<string> layerContracts)
    {
        builder.AppendLine("            layerContracts: new global::LayerBase.Modules.LayerContractContribution[]");
        builder.AppendLine("            {");
        foreach (string layer in layerContracts)
        {
            builder.AppendLine($"                new global::LayerBase.Modules.LayerContractContribution(typeof({layer}).TypeHandle),");
        }
        builder.Append("            }");
    }

    private static void AppendScopeDefinitions(StringBuilder builder, ImmutableArray<TypeContribution> scopeDefinitions)
    {
        builder.AppendLine("            scopeDefinitions: new global::LayerBase.Modules.ScopeDefinitionContribution[]");
        builder.AppendLine("            {");
        foreach (TypeContribution scope in scopeDefinitions)
        {
            builder.AppendLine("                new global::LayerBase.Modules.ScopeDefinitionContribution(");
            builder.AppendLine($"                    typeof({scope.TypeName}).TypeHandle,");
            builder.AppendLine($"                    (global::LayerBase.Scope.ScopeThreadingMode){scope.Threading},");
            builder.AppendLine($"                    (global::LayerBase.Scope.ScopeClockMode){scope.Clock},");
            builder.AppendLine($"                    {scope.TickRateHz},");
            builder.AppendLine($"                    (global::LayerBase.Scope.ScopeStopPolicy){scope.StopPolicy}),");
        }
        builder.Append("            }");
    }

    private static void AppendMessageContracts(StringBuilder builder, ImmutableArray<TypeContribution> messageContracts)
    {
        builder.AppendLine("            messageContracts: new global::LayerBase.Modules.ScopeMessageContractContribution[]");
        builder.AppendLine("            {");
        foreach (TypeContribution message in messageContracts)
        {
            string resultType = message.ResultType == "void" ? "typeof(void)" : $"typeof({message.ResultType})";
            builder.AppendLine("                new global::LayerBase.Modules.ScopeMessageContractContribution(");
            builder.AppendLine($"                    typeof({message.TypeName}).TypeHandle,");
            builder.AppendLine($"                    typeof({message.TargetScopeType}).TypeHandle,");
            builder.AppendLine($"                    {resultType}.TypeHandle,");
            builder.AppendLine($"                    global::LayerBase.Modules.ScopeMessageKind.{message.MessageKind}),");
        }
        builder.Append("            }");
    }

    private static void AppendServices(StringBuilder builder, ImmutableArray<TypeContribution> services)
    {
        builder.AppendLine("            services: new global::LayerBase.Modules.ServiceContribution[]");
        builder.AppendLine("            {");
        for (int i = 0; i < services.Length; i++)
        {
            TypeContribution service = services[i];
            builder.AppendLine("                new global::LayerBase.Modules.ServiceContribution(");
            builder.AppendLine($"                    typeof({service.TypeName}).TypeHandle,");
            builder.AppendLine("                    new global::System.RuntimeTypeHandle[]");
            builder.AppendLine("                    {");
            foreach (string ownerLayer in service.OwnerLayerTypes)
            {
                builder.AppendLine($"                        typeof({ownerLayer}).TypeHandle,");
            }
            builder.AppendLine("                    },");
            builder.AppendLine($"                    typeof({service.OwnerScopeType}).TypeHandle,");
            builder.AppendLine($"                    global::LayerBase.Modules.GeneratedModuleFactories.{GetServiceFactoryName(service.TypeName)},");
            builder.AppendLine($"                    global::LayerBase.Modules.GeneratedModuleFactories.{GetServiceBindingName(service.TypeName)},");
            builder.AppendLine($"                    {i}),");
        }
        builder.Append("            }");
    }

    private static void AppendContexts(StringBuilder builder, ImmutableArray<TypeContribution> contexts)
    {
        builder.AppendLine("            contexts: new global::LayerBase.Modules.ContextContribution[]");
        builder.AppendLine("            {");
        for (int i = 0; i < contexts.Length; i++)
        {
            TypeContribution context = contexts[i];
            builder.AppendLine("                new global::LayerBase.Modules.ContextContribution(");
            builder.AppendLine($"                    typeof({context.TypeName}).TypeHandle,");
            builder.AppendLine($"                    typeof({context.OwnerServiceType}).TypeHandle,");
            builder.AppendLine($"                    global::LayerBase.Modules.GeneratedModuleFactories.{GetContextFactoryName(context.TypeName)},");
            builder.AppendLine($"                    {i}),");
        }
        builder.Append("            }");
    }

    private static void AppendHandlers(StringBuilder builder, ImmutableArray<HandlerContribution> handlers)
    {
        builder.AppendLine("            handlers: new global::LayerBase.Modules.ScopeHandlerContribution[]");
        builder.AppendLine("            {");
        for (int i = 0; i < handlers.Length; i++)
        {
            HandlerContribution handler = handlers[i];
            builder.AppendLine("                new global::LayerBase.Modules.ScopeHandlerContribution(");
            builder.AppendLine($"                    typeof({handler.MessageType}).TypeHandle,");
            builder.AppendLine($"                    typeof({handler.ServiceType}).TypeHandle,");
            builder.AppendLine($"                    typeof({handler.ScopeType}).TypeHandle,");
            builder.AppendLine($"                    {i},");
            builder.AppendLine($"                    global::LayerBase.Modules.ScopeMessageKind.{handler.Kind}),");
        }
        builder.Append("            }");
    }

    private static AttributeData? FindAttribute(ISymbol symbol, string metadataName)
    {
        return symbol.GetAttributes()
                     .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == metadataName);
    }

    private static AttributeData? FindAttributeByOriginalDefinition(ISymbol symbol, string metadataName)
    {
        return symbol.GetAttributes()
                     .FirstOrDefault(attr => attr.AttributeClass?.OriginalDefinition.ToDisplayString() == metadataName ||
                                             attr.AttributeClass?.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + metadataName);
    }

    private static bool HasAttribute(ISymbol symbol, string metadataName)
    {
        return FindAttribute(symbol, metadataName) != null ||
               FindAttributeByOriginalDefinition(symbol, metadataName) != null;
    }

    private static string? GetTypeArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index)
        {
            return null;
        }

        return attribute.ConstructorArguments[index].Value is ITypeSymbol type
            ? type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;
    }

    private static int GetIntArgument(AttributeData attribute, int index, string name, int fallback)
    {
        int value = fallback;
        if (attribute.ConstructorArguments.Length > index &&
            attribute.ConstructorArguments[index].Value is int constructorValue)
        {
            value = constructorValue;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == name &&
                namedArgument.Value.Value is int namedValue)
            {
                value = namedValue;
            }
        }

        return value;
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, string metadataName)
    {
        return type.AllInterfaces.Any(candidate => candidate.ToDisplayString() == metadataName ||
                                                   candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + metadataName);
    }

    private static bool InheritsFrom(INamedTypeSymbol type, string metadataName)
    {
        INamedTypeSymbol? current = type.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == metadataName ||
                current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + metadataName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol symbol)
    {
        return symbol.DeclaringSyntaxReferences
                     .Select(static reference => reference.GetSyntax())
                     .OfType<ClassDeclarationSyntax>()
                     .Any(static syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword));
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

    private static string GetServiceFactoryName(string typeName) => "CreateService_" + SanitizeIdentifier(typeName);

    private static string GetServiceBindingName(string typeName) => "BindService_" + SanitizeIdentifier(typeName);

    private static string GetContextFactoryName(string typeName) => "CreateContext_" + SanitizeIdentifier(typeName);

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        return builder.ToString();
    }

    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor AssemblyModuleOwnerMustBePartial = new(
            "LBM001",
            "[AssemblyModule] owner must be partial",
            "Module type '{0}' uses [AssemblyModule] and must be declared partial so the source generator can emit module metadata",
            "LayerBase.Modules",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    private sealed class ModuleInfo
    {
        public ModuleInfo(
            string name,
            string ns,
            string fullTypeName,
            string accessibility,
            bool isPartial,
            Location location)
        {
            Name = name;
            Namespace = ns;
            FullTypeName = fullTypeName;
            Accessibility = accessibility;
            IsPartial = isPartial;
            Location = location;
        }

        public string Name { get; }
        public string Namespace { get; }
        public string FullTypeName { get; }
        public string Accessibility { get; }
        public bool IsPartial { get; }
        public Location Location { get; }
    }

    private enum TypeContributionKind
    {
        LayerContract,
        ScopeDefinition,
        MessageContract,
        Service,
        Context
    }

    private sealed class TypeContribution
    {
        private TypeContribution(TypeContributionKind kind, string typeName)
        {
            Kind = kind;
            TypeName = typeName;
            OwnerLayerTypes = ImmutableArray<string>.Empty;
            OwnerScopeType = "global::LayerBase.Scope.MainScope";
            TargetScopeType = "global::LayerBase.Scope.MainScope";
            ResultType = "void";
            MessageKind = "Event";
            OwnerServiceType = "";
        }

        public TypeContributionKind Kind { get; }
        public string TypeName { get; }
        public int Threading { get; private set; }
        public int Clock { get; private set; }
        public int TickRateHz { get; private set; }
        public int StopPolicy { get; private set; }
        public string TargetScopeType { get; private set; }
        public string ResultType { get; private set; }
        public string MessageKind { get; private set; }
        public ImmutableArray<string> OwnerLayerTypes { get; private set; }
        public string OwnerScopeType { get; private set; }
        public string OwnerServiceType { get; private set; }

        public static TypeContribution LayerContract(string typeName) => new(TypeContributionKind.LayerContract, typeName);

        public static TypeContribution ScopeDefinition(
            string typeName,
            int threading,
            int clock,
            int tickRateHz,
            int stopPolicy)
        {
            return new TypeContribution(TypeContributionKind.ScopeDefinition, typeName)
            {
                Threading = threading,
                Clock = clock,
                TickRateHz = tickRateHz,
                StopPolicy = stopPolicy
            };
        }

        public static TypeContribution MessageContract(
            string typeName,
            string targetScopeType,
            string resultType,
            string kind)
        {
            return new TypeContribution(TypeContributionKind.MessageContract, typeName)
            {
                TargetScopeType = targetScopeType,
                ResultType = resultType,
                MessageKind = kind
            };
        }

        public static TypeContribution Service(
            string typeName,
            ImmutableArray<string> ownerLayerTypes,
            string ownerScopeType,
            string typeShortName)
        {
            return new TypeContribution(TypeContributionKind.Service, typeName)
            {
                OwnerLayerTypes = ownerLayerTypes,
                OwnerScopeType = ownerScopeType
            };
        }

        public static TypeContribution Context(string typeName, string ownerServiceType, string typeShortName)
        {
            return new TypeContribution(TypeContributionKind.Context, typeName)
            {
                OwnerServiceType = ownerServiceType
            };
        }
    }

    private sealed class HandlerContribution
    {
        public HandlerContribution(
            string messageType,
            string serviceType,
            string scopeType,
            string kind)
        {
            MessageType = messageType;
            ServiceType = serviceType;
            ScopeType = scopeType;
            Kind = kind;
        }

        public string MessageType { get; }
        public string ServiceType { get; }
        public string ScopeType { get; }
        public string Kind { get; }
    }
}
