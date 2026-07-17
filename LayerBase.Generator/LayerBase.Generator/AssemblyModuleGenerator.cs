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
    private const string ModuleIgnoreAttributeName = "LayerBase.Modules.ModuleIgnoreAttribute";
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string OwnerServiceAttributeName = "LayerBase.DI.Options.OwnerServiceAttribute";
    private const string LayerToolAttributeName = "LayerBase.Tools.LayerToolAttribute";
    private const string EventMetaDataBaseName = "LayerBase.Event.EventMetaData.EventMetaData`1";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ILayerContextMetadataName = "LayerBase.DI.ILayerContext";
    private const string EventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandler`1";
    private const string AsyncEventHandlerMetadataName = "LayerBase.Core.EventHandler.IEventHandlerAsync`1";
    private const string CallHandlerMetadataName = "LayerBase.Call.IScopeLocalCallHandler`2";
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

        var ownerLayerContributions = context.SyntaxProvider
                                             .ForAttributeWithMetadataName(
                                                 OwnerLayerAttributeName,
                                                 static (node, _) => node is ClassDeclarationSyntax,
                                                 static (ctx, _) => CreateOwnerLayerContributions(ctx))
                                             .SelectMany(static (items, _) => items);

        var ownerServiceContexts = context.SyntaxProvider
                                          .ForAttributeWithMetadataName(
                                              OwnerServiceAttributeName,
                                              static (node, _) => node is ClassDeclarationSyntax,
                                              static (ctx, _) => CreateOwnerServiceContexts(ctx))
                                          .SelectMany(static (items, _) => items);

        var layerToolAttributes = context.SyntaxProvider
                                         .ForAttributeWithMetadataName(
                                             LayerToolAttributeName,
                                             static (node, _) => node is ClassDeclarationSyntax,
                                             static (ctx, _) => CreateLayerToolAttributeInfo(ctx))
                                         .Where(static item => item is not null)
                                         .Select(static (item, _) => item!);

        var layerToolCandidates = context.SyntaxProvider
                                         .CreateSyntaxProvider(
                                             static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                                             static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node))
                                         .Where(static symbol => symbol is INamedTypeSymbol)
                                         .Select(static (symbol, _) => (INamedTypeSymbol)symbol!);

        var layerToolData = layerToolAttributes.Collect()
                                              .Combine(layerToolCandidates.Collect());

        var ownerServiceAndTools = ownerServiceContexts.Collect()
                                                       .Combine(layerToolData);
        var ownerLayerAndService = ownerLayerContributions.Collect()
                                                         .Combine(ownerServiceAndTools);
        var moduleData = modules.Collect().Combine(ownerLayerAndService);
        var combined = context.CompilationProvider.Combine(moduleData);

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var compilation = source.Left;
            var modulesAndContributions = source.Right;
            var ownerLayerAndServiceData = modulesAndContributions.Right;
            var ownerServiceAndToolsData = ownerLayerAndServiceData.Right;
            Execute(
                spc,
                compilation,
                modulesAndContributions.Left,
                ownerLayerAndServiceData.Left,
                ownerServiceAndToolsData.Left,
                ownerServiceAndToolsData.Right.Left,
                ownerServiceAndToolsData.Right.Right);
        });
    }

    private static void Execute(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<ModuleInfo> modules,
        ImmutableArray<OwnerLayerContributionInfo> ownerLayerContributions,
        ImmutableArray<OwnerServiceContextInfo> ownerServiceContexts,
        ImmutableArray<LayerToolAttributeInfo> layerToolAttributes,
        ImmutableArray<INamedTypeSymbol> layerToolCandidates)
    {
        var moduleList = modules.OrderBy(static module => module.ModuleId, StringComparer.Ordinal)
                                .ThenBy(static module => module.TypeName, StringComparer.Ordinal)
                                .ToArray();
        var iServiceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
        var iLayerContextSymbol = compilation.GetTypeByMetadataName(ILayerContextMetadataName);
        var eventHandlerSymbol = compilation.GetTypeByMetadataName(EventHandlerMetadataName);
        var asyncEventHandlerSymbol = compilation.GetTypeByMetadataName(AsyncEventHandlerMetadataName);
        var callHandlerSymbol = compilation.GetTypeByMetadataName(CallHandlerMetadataName);
        var eventMetaDataSymbol = compilation.GetTypeByMetadataName(EventMetaDataBaseName);
        var layerToolContributions = CreateLayerToolContributions(layerToolAttributes, layerToolCandidates);

        var fallbackServices = new List<ServiceContributionInfo>();
        var fallbackEvents = new List<EventContributionInfo>();
        var fallbackContexts = new List<ContextContributionInfo>();
        var fallbackLocalCalls = new List<LocalCallContributionInfo>();
        var fallbackEventHandlers = new List<EventHandlerContributionInfo>();
        var fallbackTools = new List<LayerToolContributionInfo>();
        foreach (var contribution in ownerLayerContributions)
        {
            INamedTypeSymbol? evtMetaEventType = GetEventTypeFromMetaData(contribution.TargetType, eventMetaDataSymbol);
            if (evtMetaEventType != null)
            {
                if (moduleList.Length == 0)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ScopedEventMetadataRequiresModule,
                        contribution.Location,
                        contribution.TargetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        evtMetaEventType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        contribution.OwnerLayerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    continue;
                }

                if (moduleList.Length > 1)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ScopedEventMetadataRequiresSingleModule,
                        contribution.Location,
                        contribution.TargetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        evtMetaEventType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        contribution.OwnerLayerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        string.Join(", ", moduleList.Select(static module =>
                            module.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
                    continue;
                }

                var evtOwnerLayer = ToTypeName(contribution.OwnerLayerType);
                var evtOwnerScope = contribution.OwnerScopeType == null
                    ? "global::LayerBase.Scope.MainScope"
                    : ToTypeName(contribution.OwnerScopeType);

                fallbackEvents.Add(new EventContributionInfo(
                    evtOwnerLayer,
                    evtOwnerScope,
                    ToTypeName(evtMetaEventType),
                    ToTypeName(contribution.TargetType),
                    "global::LayerBase.Core.Event.LayerPrewarmTargets.All"));
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(contribution.OwnerLayerType.ContainingAssembly, compilation.Assembly))
            {
                continue;
            }

            if (moduleList.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresModule,
                    contribution.Location,
                    contribution.TargetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    contribution.OwnerLayerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            if (moduleList.Length > 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresSingleModule,
                    contribution.Location,
                    contribution.TargetType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", moduleList.Select(static module =>
                        module.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
                continue;
            }

            var ownerLayerType = ToTypeName(contribution.OwnerLayerType);
            var ownerScopeType = contribution.OwnerScopeType == null
                ? "global::LayerBase.Scope.MainScope"
                : ToTypeName(contribution.OwnerScopeType);

            var callHandlerInterfaces = GetCallHandlerInterfaces(contribution.TargetType, callHandlerSymbol).ToArray();
            if (callHandlerInterfaces.Length > 0)
            {
                foreach (var callHandler in callHandlerInterfaces)
                {
                    fallbackLocalCalls.Add(new LocalCallContributionInfo(
                        ownerLayerType,
                        ownerScopeType,
                        ToTypeName(callHandler.RequestType),
                        ToTypeName(callHandler.ResponseType),
                        ToTypeName(contribution.TargetType)));
                }

                continue;
            }

            if (!ImplementsInterface(contribution.TargetType, iServiceSymbol) &&
                !ImplementsInterfaceByMetadataName(contribution.TargetType, IServiceMetadataName))
            {
                continue;
            }

            fallbackServices.Add(new ServiceContributionInfo(
                ownerLayerType,
                ownerScopeType,
                ToTypeName(contribution.TargetType),
                ToTypeName(contribution.TargetType),
                "global::LayerBase.DI.ServiceLifetime.Singleton"));
        }

        foreach (var context in ownerServiceContexts)
        {
            if (SymbolEqualityComparer.Default.Equals(context.OwnerServiceType.ContainingAssembly, compilation.Assembly))
            {
                continue;
            }

            if (moduleList.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresModule,
                    context.Location,
                    context.ContextType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    context.OwnerServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            if (moduleList.Length > 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresSingleModule,
                    context.Location,
                    context.ContextType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", moduleList.Select(static module =>
                        module.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
                continue;
            }

            var isLayerContext = ImplementsInterface(context.ContextType, iLayerContextSymbol) ||
                                 ImplementsInterfaceByMetadataName(context.ContextType, ILayerContextMetadataName);
            var eventHandlerInterfaces = GetEventHandlerInterfaces(
                context.ContextType,
                eventHandlerSymbol,
                asyncEventHandlerSymbol).ToArray();
            if (!isLayerContext && eventHandlerInterfaces.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerServiceContextOnlySupportsLayerContext,
                    context.Location,
                    context.ContextType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    context.OwnerServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            var ownerLayerRegistrations = GetOwnerLayerRegistrations(context.OwnerServiceType).ToArray();
            if (ownerLayerRegistrations.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.OwnerServiceContextRequiresOwnerLayer,
                    context.Location,
                    context.ContextType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    context.OwnerServiceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            var ownerScopeType = ReadScopeType(context.OwnerServiceType);
            foreach (var ownerLayerRegistration in ownerLayerRegistrations)
            {
                var ownerLayerTypeName = ToTypeName(ownerLayerRegistration.OwnerLayerType);
                var ownerScopeTypeName = ownerScopeType == null ? "global::LayerBase.Scope.MainScope" : ToTypeName(ownerScopeType);
                if (isLayerContext)
                {
                    fallbackContexts.Add(new ContextContributionInfo(
                        ownerLayerTypeName,
                        ownerScopeTypeName,
                        ToTypeName(context.ContextType),
                        ToTypeName(context.OwnerServiceType)));
                }

                foreach (var eventHandler in eventHandlerInterfaces)
                {
                    fallbackEventHandlers.Add(new EventHandlerContributionInfo(
                        ownerLayerTypeName,
                        ownerScopeTypeName,
                        ToTypeName(eventHandler.EventType),
                        ToTypeName(context.ContextType),
                        ToTypeName(context.OwnerServiceType)));
                }
            }
        }

        foreach (var tool in layerToolContributions)
        {
            if (SymbolEqualityComparer.Default.Equals(tool.OwnerLayerType.ContainingAssembly, compilation.Assembly))
            {
                continue;
            }

            if (moduleList.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.LayerToolRequiresModule,
                    tool.Location,
                    tool.ImplementationType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    tool.ContractType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                continue;
            }

            if (moduleList.Length > 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CrossAssemblyOwnerLayerRequiresSingleModule,
                    tool.Location,
                    tool.ImplementationType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    string.Join(", ", moduleList.Select(static module =>
                        module.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
                continue;
            }

            fallbackTools.Add(new LayerToolContributionInfo(
                ToTypeName(tool.OwnerLayerType),
                ToTypeName(tool.OwnerScopeType),
                ToTypeName(tool.ContractType),
                ToTypeName(tool.ImplementationType),
                tool.LocalKey,
                tool.Cache));
        }

        foreach (var module in moduleList)
        {
            GenerateModule(
                spc,
                module,
                moduleList.Length == 1 ? fallbackServices : Array.Empty<ServiceContributionInfo>(),
                moduleList.Length == 1 ? fallbackContexts : Array.Empty<ContextContributionInfo>(),
                moduleList.Length == 1 ? fallbackLocalCalls : Array.Empty<LocalCallContributionInfo>(),
                moduleList.Length == 1 ? fallbackEventHandlers : Array.Empty<EventHandlerContributionInfo>(),
                moduleList.Length == 1 ? fallbackTools : Array.Empty<LayerToolContributionInfo>(),
                moduleList.Length == 1 ? fallbackEvents : Array.Empty<EventContributionInfo>());
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

    private static ImmutableArray<OwnerLayerContributionInfo> CreateOwnerLayerContributions(
        GeneratorAttributeSyntaxContext context)
    {
        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
        if (HasAttribute(targetSymbol, ModuleIgnoreAttributeName))
        {
            return ImmutableArray<OwnerLayerContributionInfo>.Empty;
        }

        var ownerScope = ReadScopeType(targetSymbol);
        var builder = ImmutableArray.CreateBuilder<OwnerLayerContributionInfo>();

        foreach (var attribute in context.Attributes)
        {
            if (!IsAttribute(attribute, OwnerLayerAttributeName)) continue;
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol ownerLayerType) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new OwnerLayerContributionInfo(targetSymbol, ownerLayerType, ownerScope, location));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<OwnerServiceContextInfo> CreateOwnerServiceContexts(
        GeneratorAttributeSyntaxContext context)
    {
        var contextSymbol = (INamedTypeSymbol)context.TargetSymbol;
        if (HasAttribute(contextSymbol, ModuleIgnoreAttributeName))
        {
            return ImmutableArray<OwnerServiceContextInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<OwnerServiceContextInfo>();

        foreach (var attribute in context.Attributes)
        {
            if (!IsAttribute(attribute, OwnerServiceAttributeName)) continue;
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol ownerServiceType) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new OwnerServiceContextInfo(contextSymbol, ownerServiceType, location));
        }

        return builder.ToImmutable();
    }

    private static LayerToolAttributeInfo? CreateLayerToolAttributeInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol attributeType)
        {
            return null;
        }

        var layerToolAttribute = context.Attributes.FirstOrDefault(static attribute =>
            IsAttribute(attribute, LayerToolAttributeName));
        if (layerToolAttribute == null)
        {
            return null;
        }

        var ownerLayerType = ReadTypeConstructorArgument(layerToolAttribute, 1);
        var ownerScopeType = ReadTypeConstructorArgument(layerToolAttribute, 2);
        if (ownerLayerType == null || ownerScopeType == null)
        {
            return null;
        }

        var contractType = ReadTypeNamedArgument(layerToolAttribute, "Contract");
        var keyProperty = ReadStringNamedArgument(layerToolAttribute, "DefaultKeyProperty");
        if (string.IsNullOrWhiteSpace(keyProperty))
        {
            keyProperty = "Key";
        }

        var allowCache = ReadBoolNamedArgument(layerToolAttribute, "AllowCache") ?? true;
        var location = layerToolAttribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
        return new LayerToolAttributeInfo(attributeType, ownerLayerType, ownerScopeType, contractType, keyProperty!, allowCache, location);
    }

    private static ImmutableArray<LayerToolDeclarationInfo> CreateLayerToolContributions(
        ImmutableArray<LayerToolAttributeInfo> toolAttributes,
        ImmutableArray<INamedTypeSymbol> candidateTypes)
    {
        if (toolAttributes.IsDefaultOrEmpty || candidateTypes.IsDefaultOrEmpty)
        {
            return ImmutableArray<LayerToolDeclarationInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<LayerToolDeclarationInfo>();
        foreach (var implementationType in candidateTypes)
        {
            if (HasAttribute(implementationType, ModuleIgnoreAttributeName))
            {
                continue;
            }

            foreach (var attribute in implementationType.GetAttributes())
            {
                var toolInfo = FindLayerToolAttribute(toolAttributes, attribute.AttributeClass);
                if (toolInfo == null)
                {
                    continue;
                }

                var contractType = toolInfo.ContractType ?? implementationType;
                var localKey = ReadStringValue(attribute, toolInfo.KeyProperty)
                               ?? ReadStringValue(attribute, "LocalKey")
                               ?? "default";
                if (string.IsNullOrWhiteSpace(localKey))
                {
                    localKey = "default";
                }

                var cache = toolInfo.AllowCache && (ReadBoolNamedArgument(attribute, "Cache") ?? true);
                var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();

                builder.Add(new LayerToolDeclarationInfo(
                    implementationType,
                    contractType,
                    toolInfo.OwnerLayerType,
                    toolInfo.OwnerScopeType,
                    localKey!,
                    cache,
                    location));
            }
        }

        return builder.ToImmutable();
    }

    private static void GenerateModule(
        SourceProductionContext spc,
        ModuleInfo module,
        IReadOnlyList<ServiceContributionInfo> fallbackServices,
        IReadOnlyList<ContextContributionInfo> fallbackContexts,
        IReadOnlyList<LocalCallContributionInfo> fallbackLocalCalls,
        IReadOnlyList<EventHandlerContributionInfo> fallbackEventHandlers,
        IReadOnlyList<LayerToolContributionInfo> fallbackTools,
        IReadOnlyList<EventContributionInfo> fallbackEvents)
    {
        var services = fallbackServices.OrderBy(static service => service.OwnerLayerType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.OwnerScopeType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.ServiceType, StringComparer.Ordinal)
                                       .ThenBy(static service => service.ImplementationType, StringComparer.Ordinal)
                                       .ToImmutableArray();
        var contexts = fallbackContexts.OrderBy(static context => context.OwnerLayerType, StringComparer.Ordinal)
                                       .ThenBy(static context => context.OwnerScopeType, StringComparer.Ordinal)
                                       .ThenBy(static context => context.OwnerServiceType, StringComparer.Ordinal)
                                       .ThenBy(static context => context.ContextType, StringComparer.Ordinal)
                                       .ToImmutableArray();
        var localCalls = fallbackLocalCalls.OrderBy(static call => call.OwnerScopeType, StringComparer.Ordinal)
                                           .ThenBy(static call => call.RequestType, StringComparer.Ordinal)
                                           .ThenBy(static call => call.ResponseType, StringComparer.Ordinal)
                                           .ThenBy(static call => call.OwnerLayerType, StringComparer.Ordinal)
                                           .ThenBy(static call => call.HandlerType, StringComparer.Ordinal)
                                           .ToImmutableArray();
        var eventHandlers = fallbackEventHandlers.OrderBy(static handler => handler.OwnerLayerType, StringComparer.Ordinal)
                                                 .ThenBy(static handler => handler.OwnerScopeType, StringComparer.Ordinal)
                                                 .ThenBy(static handler => handler.EventType, StringComparer.Ordinal)
                                                 .ThenBy(static handler => handler.HandlerType, StringComparer.Ordinal)
                                                 .ThenBy(static handler => handler.OwnerServiceType, StringComparer.Ordinal)
                                                 .ToImmutableArray();
        var tools = fallbackTools.OrderBy(static tool => tool.OwnerLayerType, StringComparer.Ordinal)
                                  .ThenBy(static tool => tool.OwnerScopeType, StringComparer.Ordinal)
                                  .ThenBy(static tool => tool.ContractType, StringComparer.Ordinal)
                                  .ThenBy(static tool => tool.LocalKey, StringComparer.Ordinal)
                                  .ThenBy(static tool => tool.ImplementationType, StringComparer.Ordinal)
                                  .ToImmutableArray();

        var events = fallbackEvents.OrderBy(static ev => ev.OwnerScopeType, StringComparer.Ordinal)
                                    .ThenBy(static ev => ev.EventType, StringComparer.Ordinal)
                                    .ThenBy(static ev => ev.OwnerLayerType, StringComparer.Ordinal)
                                    .ThenBy(static ev => ev.MetaDataType, StringComparer.Ordinal)
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
        AppendContextArray(source, indent, contexts);
        AppendLocalCallArray(source, indent, localCalls);
        AppendEventHandlerArray(source, indent, eventHandlers);
        AppendLayerToolArray(source, indent, tools);
        AppendEventArray(source, indent, events);
        source.AppendLine();

        source.Append(indent).Append("    public static ").Append(module.TypeName).Append(" Instance { get; } = new ")
              .Append(module.TypeName).AppendLine("();");
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

    private static void AppendContextArray(
        StringBuilder source,
        string indent,
        ImmutableArray<ContextContributionInfo> contexts)
    {
        if (contexts.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.ContextContribution>(),");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.ContextContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var context in contexts)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.ContextContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(context.ContextType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(context.OwnerServiceType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(context.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(context.OwnerScopeType).AppendLine(")),");
        }

        source.Append(indent).AppendLine("            },");
    }

    private static void AppendLocalCallArray(
        StringBuilder source,
        string indent,
        ImmutableArray<LocalCallContributionInfo> localCalls)
    {
        if (localCalls.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.LocalCallContribution>(),");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.LocalCallContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var localCall in localCalls)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.LocalCallContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(localCall.RequestType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(localCall.ResponseType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(localCall.HandlerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(localCall.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(localCall.OwnerScopeType).AppendLine(")),");
        }

        source.Append(indent).AppendLine("            },");
    }

    private static void AppendEventHandlerArray(
        StringBuilder source,
        string indent,
        ImmutableArray<EventHandlerContributionInfo> eventHandlers)
    {
        if (eventHandlers.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.EventHandlerContribution>(),");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.EventHandlerContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var eventHandler in eventHandlers)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.EventHandlerContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(eventHandler.EventType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(eventHandler.HandlerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(eventHandler.OwnerServiceType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(eventHandler.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(eventHandler.OwnerScopeType).AppendLine(")),");
        }

        source.Append(indent).AppendLine("            },");
    }

    private static void AppendLayerToolArray(
        StringBuilder source,
        string indent,
        ImmutableArray<LayerToolContributionInfo> tools)
    {
        if (tools.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.LayerToolContribution>(),");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.LayerToolContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var tool in tools)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.LayerToolContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(tool.ContractType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(tool.ImplementationType).AppendLine("),");
            source.Append(indent).Append("                    \"").Append(Escape(tool.LocalKey)).AppendLine("\",");
            source.Append(indent).Append("                    typeof(").Append(tool.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(tool.OwnerScopeType).AppendLine("),");
            source.Append(indent).Append("                    ").Append(tool.Cache ? "true" : "false").AppendLine("),");
        }

        source.Append(indent).AppendLine("            },");
    }

    private static void AppendEventArray(
        StringBuilder source,
        string indent,
        ImmutableArray<EventContributionInfo> events)
    {
        if (events.IsDefaultOrEmpty)
        {
            source.Append(indent).AppendLine("            global::System.Array.Empty<global::LayerBase.Modules.EventContribution>());");
            return;
        }

        source.Append(indent).AppendLine("            new global::LayerBase.Modules.EventContribution[]");
        source.Append(indent).AppendLine("            {");

        foreach (var ev in events)
        {
            source.Append(indent).AppendLine("                global::LayerBase.Modules.EventContribution.ForTypes(");
            source.Append(indent).Append("                    typeof(").Append(ev.EventType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(ev.OwnerLayerType).AppendLine("),");
            source.Append(indent).Append("                    typeof(").Append(ev.OwnerScopeType).AppendLine("),");
            source.Append(indent).Append("                    static () => new ").Append(ev.MetaDataType).AppendLine("(),");
            source.Append(indent).Append("                    ").Append(ev.PrewarmTargets).AppendLine("),");
        }

        source.Append(indent).AppendLine("            });");
    }

    private static INamedTypeSymbol? GetEventTypeFromMetaData(
        INamedTypeSymbol candidate,
        INamedTypeSymbol? eventMetaDataSymbol)
    {
        if (eventMetaDataSymbol == null)
            return null;

        for (INamedTypeSymbol? current = candidate;
             current != null;
             current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            if (!SymbolEqualityComparer.Default.Equals(
                    current.OriginalDefinition,
                    eventMetaDataSymbol))
            {
                continue;
            }

            return current.TypeArguments.Length == 1
                ? current.TypeArguments[0] as INamedTypeSymbol
                : null;
        }

        return null;
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
            if (original.ContainingNamespace.ToDisplayString() != ScopeAttributeNamespace)
            {
                continue;
            }

            if (original.MetadataName == ScopeAttributeMetadataName &&
                attributeClass.TypeArguments.Length == 1)
            {
                return attributeClass.TypeArguments[0] as INamedTypeSymbol;
            }

            if (original.MetadataName == "ScopeAttribute" &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is INamedTypeSymbol scopeType)
            {
                return scopeType;
            }
        }

        return null;
    }

    private static bool ImplementsInterface(INamedTypeSymbol? type, INamedTypeSymbol? interfaceSymbol)
    {
        if (type == null || interfaceSymbol == null) return false;

        return type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol));
    }

    private static bool ImplementsInterfaceByMetadataName(INamedTypeSymbol type, string metadataName)
    {
        if (string.IsNullOrWhiteSpace(metadataName)) return false;

        return type.AllInterfaces.Any(i =>
        {
            var candidate = i.OriginalDefinition is INamedTypeSymbol named ? named : i;
            var display = candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return display switch
            {
                "global::LayerBase.DI.IService" when metadataName == IServiceMetadataName => true,
                "global::LayerBase.DI.ILayerContext" when metadataName == ILayerContextMetadataName => true,
                "global::LayerBase.Core.EventHandler.IEventHandler<TValue>" when metadataName == EventHandlerMetadataName => true,
                "global::LayerBase.Core.EventHandler.IEventHandlerAsync<TValue>" when metadataName == AsyncEventHandlerMetadataName => true,
                "global::LayerBase.Call.IScopeLocalCallHandler<TRequest, TResponse>" when metadataName == CallHandlerMetadataName => true,
                _ => false
            };
        });
    }

    private static IEnumerable<CallHandlerImplementation> GetCallHandlerInterfaces(
        INamedTypeSymbol handlerType,
        INamedTypeSymbol? callHandlerSymbol)
    {
        foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
        {
            var isCallHandler = callHandlerSymbol != null
                ? SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, callHandlerSymbol)
                : IsCallHandlerByMetadataName(iface);
            if (!isCallHandler || iface.TypeArguments.Length != 2) continue;

            yield return new CallHandlerImplementation(iface.TypeArguments[0], iface.TypeArguments[1]);
        }
    }

    private static bool IsCallHandlerByMetadataName(INamedTypeSymbol iface)
    {
        var display = iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display == "global::LayerBase.Call.IScopeLocalCallHandler<TRequest, TResponse>";
    }

    private static IEnumerable<EventHandlerImplementation> GetEventHandlerInterfaces(
        INamedTypeSymbol handlerType,
        INamedTypeSymbol? eventHandlerSymbol,
        INamedTypeSymbol? asyncEventHandlerSymbol)
    {
        foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
        {
            var isEventHandler = eventHandlerSymbol != null
                ? SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, eventHandlerSymbol)
                : IsEventHandlerByMetadataName(iface);
            var isAsyncEventHandler = asyncEventHandlerSymbol != null
                ? SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, asyncEventHandlerSymbol)
                : IsAsyncEventHandlerByMetadataName(iface);
            if (!isEventHandler && !isAsyncEventHandler) continue;
            if (iface.TypeArguments.Length != 1) continue;

            yield return new EventHandlerImplementation(iface.TypeArguments[0]);
        }
    }

    private static bool IsEventHandlerByMetadataName(INamedTypeSymbol iface)
    {
        var display = iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display == "global::LayerBase.Core.EventHandler.IEventHandler<TValue>";
    }

    private static bool IsAsyncEventHandlerByMetadataName(INamedTypeSymbol iface)
    {
        var display = iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display == "global::LayerBase.Core.EventHandler.IEventHandlerAsync<TValue>";
    }

    private static IEnumerable<OwnerLayerRegistrationInfo> GetOwnerLayerRegistrations(INamedTypeSymbol serviceType)
    {
        foreach (var attribute in serviceType.GetAttributes())
        {
            if (!IsAttribute(attribute, OwnerLayerAttributeName)) continue;
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol ownerLayerType) continue;

            yield return new OwnerLayerRegistrationInfo(ownerLayerType);
        }
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal);
    }

    private static bool HasAttribute(INamedTypeSymbol symbol, string metadataName)
    {
        return symbol.GetAttributes().Any(attribute => IsAttribute(attribute, metadataName));
    }

    private static string? ReadStringArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as string
            : null;
    }

    private static bool? ReadBoolNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is bool value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadStringNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadStringValue(AttributeData attribute, string propertyName)
    {
        var named = ReadStringNamedArgument(attribute, propertyName);
        if (named != null)
        {
            return named;
        }

        if (propertyName == "Key" &&
            attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string key)
        {
            return key;
        }

        return null;
    }

    private static INamedTypeSymbol? ReadTypeNamedArgument(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is INamedTypeSymbol value)
            {
                return value;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? ReadTypeValue(AttributeData attribute, string propertyName)
    {
        return ReadTypeNamedArgument(attribute, propertyName);
    }

    private static INamedTypeSymbol? ReadTypeConstructorArgument(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as INamedTypeSymbol
            : null;
    }

    private static LayerToolAttributeInfo? FindLayerToolAttribute(
        ImmutableArray<LayerToolAttributeInfo> toolAttributes,
        INamedTypeSymbol? attributeType)
    {
        if (attributeType == null)
        {
            return null;
        }

        foreach (var toolAttribute in toolAttributes)
        {
            if (SymbolEqualityComparer.Default.Equals(toolAttribute.AttributeType, attributeType))
            {
                return toolAttribute;
            }
        }

        return null;
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
                "Cross-assembly owner contribution requires AssemblyModule",
                "Type '{0}' targets external owner type '{1}' and must be compiled with an [AssemblyModule] root in the same assembly",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor CrossAssemblyOwnerLayerRequiresSingleModule =
            new(
                "LBMOD002",
                "Cross-assembly owner contribution requires exactly one AssemblyModule",
                "Type '{0}' targets an external owner type but this assembly has multiple modules ({1}). Keep one module root for automatic fallback or split the feature assembly",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerServiceContextRequiresOwnerLayer =
            new(
                "LBMOD003",
                "Cross-assembly OwnerService context requires OwnerLayer on service",
                "Context '{0}' targets external owner service '{1}', but that service does not declare OwnerLayer",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor CrossAssemblyOwnerServiceContextOnlySupportsLayerContext =
            new(
                "LBMOD004",
                "Cross-assembly OwnerService module fallback only supports layer contexts and event handlers",
                "Type '{0}' targets external owner service '{1}', but the current AssemblyModule OwnerService fallback only supports ILayerContext and IEventHandler",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ScopedEventMetadataRequiresModule =
            new(
                "LBMOD010",
                "Scoped event metadata requires exactly one AssemblyModule",
                "EventMetaData '{0}' for event '{1}' with OwnerLayer '{2}' requires exactly one [AssemblyModule] in the same assembly, but none was found",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ScopedEventMetadataRequiresSingleModule =
            new(
                "LBMOD011",
                "Scoped event metadata cannot be assigned because multiple AssemblyModules exist",
                "EventMetaData '{0}' for event '{1}' with OwnerLayer '{2}' has multiple candidate modules: {3}. Keep one module root or split the feature assembly",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor LayerToolRequiresModule =
            new(
                "LBMOD005",
                "LayerTool contribution requires AssemblyModule",
                "LayerTool implementation '{0}' for contract '{1}' requires an [AssemblyModule] root in the same assembly",
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

    private sealed class OwnerLayerContributionInfo
    {
        public OwnerLayerContributionInfo(
            INamedTypeSymbol targetType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol? ownerScopeType,
            Location? location)
        {
            TargetType = targetType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            Location = location;
        }

        public INamedTypeSymbol TargetType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol? OwnerScopeType { get; }

        public Location? Location { get; }
    }

    private sealed class OwnerServiceContextInfo
    {
        public OwnerServiceContextInfo(
            INamedTypeSymbol contextType,
            INamedTypeSymbol ownerServiceType,
            Location? location)
        {
            ContextType = contextType;
            OwnerServiceType = ownerServiceType;
            Location = location;
        }

        public INamedTypeSymbol ContextType { get; }

        public INamedTypeSymbol OwnerServiceType { get; }

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

    private sealed class ContextContributionInfo
    {
        public ContextContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string contextType,
            string ownerServiceType)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ContextType = contextType;
            OwnerServiceType = ownerServiceType;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string ContextType { get; }

        public string OwnerServiceType { get; }
    }

    private sealed class LocalCallContributionInfo
    {
        public LocalCallContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string requestType,
            string responseType,
            string handlerType)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            RequestType = requestType;
            ResponseType = responseType;
            HandlerType = handlerType;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string RequestType { get; }

        public string ResponseType { get; }

        public string HandlerType { get; }
    }

    private sealed class LayerToolDeclarationInfo
    {
        public LayerToolDeclarationInfo(
            INamedTypeSymbol implementationType,
            INamedTypeSymbol contractType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol ownerScopeType,
            string localKey,
            bool cache,
            Location? location)
        {
            ImplementationType = implementationType;
            ContractType = contractType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            LocalKey = localKey;
            Cache = cache;
            Location = location;
        }

        public INamedTypeSymbol ImplementationType { get; }

        public INamedTypeSymbol ContractType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol OwnerScopeType { get; }

        public string LocalKey { get; }

        public bool Cache { get; }

        public Location? Location { get; }
    }

    private sealed class LayerToolContributionInfo
    {
        public LayerToolContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string contractType,
            string implementationType,
            string localKey,
            bool cache)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ContractType = contractType;
            ImplementationType = implementationType;
            LocalKey = localKey;
            Cache = cache;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string ContractType { get; }

        public string ImplementationType { get; }

        public string LocalKey { get; }

        public bool Cache { get; }
    }

    private sealed class LayerToolAttributeInfo
    {
        public LayerToolAttributeInfo(
            INamedTypeSymbol attributeType,
            INamedTypeSymbol ownerLayerType,
            INamedTypeSymbol ownerScopeType,
            INamedTypeSymbol? contractType,
            string keyProperty,
            bool allowCache,
            Location? location)
        {
            AttributeType = attributeType;
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            ContractType = contractType;
            KeyProperty = keyProperty;
            AllowCache = allowCache;
            Location = location;
        }

        public INamedTypeSymbol AttributeType { get; }

        public INamedTypeSymbol OwnerLayerType { get; }

        public INamedTypeSymbol OwnerScopeType { get; }

        public INamedTypeSymbol? ContractType { get; }

        public string KeyProperty { get; }

        public bool AllowCache { get; }

        public Location? Location { get; }
    }

    private sealed class EventContributionInfo
    {
        public EventContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string eventType,
            string metaDataType,
            string prewarmTargets)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            EventType = eventType;
            MetaDataType = metaDataType;
            PrewarmTargets = prewarmTargets;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string EventType { get; }

        public string MetaDataType { get; }

        public string PrewarmTargets { get; }
    }

    private sealed class EventHandlerContributionInfo
    {
        public EventHandlerContributionInfo(
            string ownerLayerType,
            string ownerScopeType,
            string eventType,
            string handlerType,
            string ownerServiceType)
        {
            OwnerLayerType = ownerLayerType;
            OwnerScopeType = ownerScopeType;
            EventType = eventType;
            HandlerType = handlerType;
            OwnerServiceType = ownerServiceType;
        }

        public string OwnerLayerType { get; }

        public string OwnerScopeType { get; }

        public string EventType { get; }

        public string HandlerType { get; }

        public string OwnerServiceType { get; }
    }

    private readonly record struct CallHandlerImplementation(ITypeSymbol RequestType, ITypeSymbol ResponseType);

    private readonly record struct EventHandlerImplementation(ITypeSymbol EventType);

    private readonly record struct OwnerLayerRegistrationInfo(INamedTypeSymbol OwnerLayerType);
}
