using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ScopeRuntimeHostGenerator : IIncrementalGenerator
{
    private const string ScopeOptionsAttributeName = "LayerBase.Scope.ScopeOptionsAttribute";
    private const string LayerBaseTypeName = "LayerBase.Layers.Layer";
    private const string ScopeEventHandlerAttributeName = "LayerBase.Scope.ScopeEventAttribute";
    private const string ScopeCallHandlerAttributeName = "LayerBase.Scope.ScopeCallAttribute";
    private const string ScopeEventRequestAttributeName = "LayerBase.Scope.ScopeEventAttribute`1";
    private const string ScopeCallRequestAttributeName = "LayerBase.Scope.ScopeCallAttribute`2";
    private const string ScopeAttributeName = "LayerBase.Scope.ScopeAttribute`1";
    private const string AssemblyModuleAttributeName = "LayerBase.Modules.AssemblyModuleAttribute";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const int ScopeClockModeFixedRate = 1;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var scopeOptionsDiagnostics = context.SyntaxProvider
                                             .ForAttributeWithMetadataName(
                                                 ScopeOptionsAttributeName,
                                                 static (_, _) => true,
                                                 static (ctx, cancellationToken) => GetScopeOptionsDiagnostic(ctx, cancellationToken));

        var scopeAttributeDiagnostics = context.SyntaxProvider
                                               .ForAttributeWithMetadataName(
                                                   ScopeAttributeName,
                                                   static (_, _) => true,
                                                   static (ctx, _) => GetScopeAttributeDiagnostic(ctx));

        var eventHandlers = context.SyntaxProvider
                                   .ForAttributeWithMetadataName(
                                       ScopeEventHandlerAttributeName,
                                       static (_, _) => true,
                                       static (ctx, _) => ctx.TargetSymbol is IMethodSymbol);

        var callHandlers = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      ScopeCallHandlerAttributeName,
                                      static (_, _) => true,
                                      static (ctx, _) => ctx.TargetSymbol is IMethodSymbol);

        var eventRequests = context.SyntaxProvider
                                   .ForAttributeWithMetadataName(
                                       ScopeEventRequestAttributeName,
                                       static (_, _) => true,
                                       static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol);

        var callRequests = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      ScopeCallRequestAttributeName,
                                      static (_, _) => true,
                                      static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol);

        var scopeOptions = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      ScopeOptionsAttributeName,
                                      static (_, _) => true,
                                      static (ctx, _) => GetScopeOptionsInfo(ctx))
                                  .Where(static item => item != null)!;

        var scopedServices = context.SyntaxProvider
                                    .ForAttributeWithMetadataName(
                                        ScopeAttributeName,
                                        static (_, _) => true,
                                        static (ctx, _) => GetScopedServiceInfo(ctx))
                                    .Where(static item => item != null)!;

        var dispatcherInputs = eventHandlers.Collect()
                                           .Combine(callHandlers.Collect())
                                           .Combine(eventRequests.Collect())
                                           .Combine(callRequests.Collect());

        var scopeDiagnostics = scopeOptionsDiagnostics.Collect()
                                                     .Combine(scopeAttributeDiagnostics.Collect());
        var scopeInputs = scopeOptions.Collect()
                                      .Combine(scopedServices.Collect())
                                      .Combine(scopeDiagnostics);

        var layerTypes = context.SyntaxProvider
                                 .CreateSyntaxProvider(
                                     static (node, _) => node is ClassDeclarationSyntax cds &&
                                                         cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                                     static (ctx, _) => GetLayerType(ctx))
                                 .Where(static item => item != null)!;

        var assemblyModules = context.SyntaxProvider
                                     .ForAttributeWithMetadataName(
                                         AssemblyModuleAttributeName,
                                         static (_, _) => true,
                                         static (_, _) => true);

        var combined = dispatcherInputs.Combine(scopeInputs)
                                       .Combine(layerTypes.Collect())
                                       .Combine(assemblyModules.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var dispatcherInput = source.Left.Left.Left;
            var scopeInput = source.Left.Left.Right;
            var collectedLayerTypes = source.Left.Right;
            bool moduleMode = source.Right.Length > 0;

            foreach (ScopeDiagnostic? diagnostic in scopeInput.Right.Left)
            {
                if (diagnostic.HasValue)
                {
                    spc.ReportDiagnostic(diagnostic.Value.ToDiagnostic());
                }
            }

            foreach (ScopeDiagnostic? diagnostic in scopeInput.Right.Right)
            {
                if (diagnostic.HasValue)
                {
                    spc.ReportDiagnostic(diagnostic.Value.ToDiagnostic());
                }
            }

            bool hasEventHandler = dispatcherInput.Left.Left.Left.Any(static value => value);
            bool hasCallHandler = dispatcherInput.Left.Left.Right.Any(static value => value);
            bool hasEventRequest = dispatcherInput.Left.Right.Any(static value => value);
            bool hasCallRequest = dispatcherInput.Right.Any(static value => value);
            Generate(
                spc,
                hasEventRequest && hasEventHandler,
                hasCallRequest && hasCallHandler,
                scopeInput.Left.Left,
                scopeInput.Left.Right,
                collectedLayerTypes,
                moduleMode);
        });
    }

    private static ScopeDiagnostic? GetScopeOptionsDiagnostic(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol scopeSymbol)
        {
            return null;
        }

        if (scopeSymbol.ContainingType != null)
        {
            return null;
        }

        foreach (AttributeData attribute in context.Attributes)
        {
            int clock = 0;
            int tickRateHz = 0;

            if (attribute.ConstructorArguments.Length > 1)
            {
                clock = GetIntValue(attribute.ConstructorArguments[1], clock);
            }

            if (attribute.ConstructorArguments.Length > 2)
            {
                tickRateHz = GetIntValue(attribute.ConstructorArguments[2], tickRateHz);
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "clock")
                {
                    clock = GetIntValue(namedArgument.Value, clock);
                }
                else if (namedArgument.Key == "tickRateHz")
                {
                    tickRateHz = GetIntValue(namedArgument.Value, tickRateHz);
                }
            }

            Location location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
                                ?? scopeSymbol.Locations.FirstOrDefault()
                                ?? Location.None;

            if (tickRateHz < 0)
            {
                return new ScopeDiagnostic(
                    Diagnostics.NegativeTickRate,
                    location,
                    scopeSymbol.Name);
            }

            if (clock == ScopeClockModeFixedRate && tickRateHz <= 0)
            {
                return new ScopeDiagnostic(
                    Diagnostics.FixedRateRequiresPositiveTickRate,
                    location,
                    scopeSymbol.Name);
            }
        }

        return null;
    }

    private static ScopeDiagnostic? GetScopeAttributeDiagnostic(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol serviceSymbol ||
            ImplementsIService(serviceSymbol))
        {
            return null;
        }

        return new ScopeDiagnostic(
            Diagnostics.ScopeAttributeOwnerMustImplementIService,
            serviceSymbol.Locations.FirstOrDefault() ?? Location.None,
            serviceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static ScopeOptionsInfo? GetScopeOptionsInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol scopeSymbol)
        {
            return null;
        }

        if (scopeSymbol.ContainingType != null)
        {
            return null;
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(static attr =>
            attr.AttributeClass?.ToDisplayString() == ScopeOptionsAttributeName);
        if (attribute == null)
        {
            return null;
        }

        int threading = 0;
        int clock = 0;
        int tickRateHz = 0;
        int stopPolicy = 0;

        if (attribute.ConstructorArguments.Length > 0)
        {
            threading = GetIntValue(attribute.ConstructorArguments[0], threading);
        }

        if (attribute.ConstructorArguments.Length > 1)
        {
            clock = GetIntValue(attribute.ConstructorArguments[1], clock);
        }

        if (attribute.ConstructorArguments.Length > 2)
        {
            tickRateHz = GetIntValue(attribute.ConstructorArguments[2], tickRateHz);
        }

        if (attribute.ConstructorArguments.Length > 3)
        {
            stopPolicy = GetIntValue(attribute.ConstructorArguments[3], stopPolicy);
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == "threading")
            {
                threading = GetIntValue(namedArgument.Value, threading);
            }
            else if (namedArgument.Key == "clock")
            {
                clock = GetIntValue(namedArgument.Value, clock);
            }
            else if (namedArgument.Key == "tickRateHz")
            {
                tickRateHz = GetIntValue(namedArgument.Value, tickRateHz);
            }
            else if (namedArgument.Key == "stopPolicy")
            {
                stopPolicy = GetIntValue(namedArgument.Value, stopPolicy);
            }
        }

        bool isPartial = IsPartial(scopeSymbol);
        Location location = scopeSymbol.Locations.FirstOrDefault() ?? Location.None;

        return new ScopeOptionsInfo(
            scopeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            scopeSymbol.ContainingNamespace.ToDisplayString(),
            scopeSymbol.Name,
            GetAccessibility(scopeSymbol),
            isPartial,
            location,
            threading,
            clock,
            tickRateHz,
            stopPolicy);
    }

    private static ScopedServiceInfo? GetScopedServiceInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol serviceSymbol ||
            !ImplementsIService(serviceSymbol))
        {
            return null;
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(static attr =>
            attr.AttributeClass?.OriginalDefinition.ToDisplayString() ==
            "LayerBase.Scope.ScopeAttribute<TScope>");

        INamedTypeSymbol? scopeType = attribute?.AttributeClass?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        if (scopeType == null)
        {
            return null;
        }

        return new ScopedServiceInfo(
            serviceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            scopeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static int GetIntValue(TypedConstant value, int fallback)
    {
        return value.Value is int intValue ? intValue : fallback;
    }

    private static void Generate(
        SourceProductionContext spc,
        bool hasPostDispatcher,
        bool hasCallDispatcher,
        ImmutableArray<ScopeOptionsInfo?> nullableDefinitions,
        ImmutableArray<ScopedServiceInfo?> nullableScopedServices,
        ImmutableArray<LayerTypeInfo> layerTypes,
        bool moduleMode)
    {
        var definitions = nullableDefinitions
                          .Where(static item => item != null)
                          .Select(static item => item!)
                          .GroupBy(static item => item.ScopeType)
                          .Select(static group => group.First())
                          .OrderBy(static item => item.ScopeType, StringComparer.Ordinal)
                          .ToImmutableArray();

        var scopedServices = nullableScopedServices
                             .Where(static item => item != null)
                             .Select(static item => item!)
                             .GroupBy(static item => item.ServiceType)
                             .Select(static group => group.First())
                             .OrderBy(static item => item.ScopeType, StringComparer.Ordinal)
                          .ThenBy(static item => item.ServiceType, StringComparer.Ordinal)
                          .ToImmutableArray();

        foreach (ScopeOptionsInfo definition in definitions)
        {
            if (!moduleMode && !definition.IsPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ScopeOptionsOwnerMustBePartial,
                    definition.Location,
                    definition.ScopeType));
            }
        }

        var partialDefinitions = definitions
                                 .Where(static item => item.IsPartial)
                                 .ToImmutableArray();
        if (!moduleMode)
        {
            GenerateScopeOptionPartials(spc, partialDefinitions);
        }

        bool shouldGenerateRegistrar = definitions.Length > 0 && layerTypes.Length > 0 && (hasPostDispatcher || hasCallDispatcher);

        bool hasGeneratedPlanner = !moduleMode && GeneratePlanner(spc, partialDefinitions, scopedServices);
        if (!hasPostDispatcher && !hasCallDispatcher && !hasGeneratedPlanner)
        {
            return;
        }

        string postDispatcher = hasPostDispatcher
            ? "global::LayerBase.Scope.GeneratedScopePostDispatcher.Dispatch"
            : "null";
        string callDispatcher = hasCallDispatcher
            ? "global::LayerBase.Scope.GeneratedScopeCallDispatcher.Dispatch"
            : "null";
        string scopeTypeResolver = hasGeneratedPlanner
            ? "global::LayerBase.Scope.GeneratedScopeRuntimePlanner.TryGetScopeId"
            : "null";

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Scope");
        builder.AppendLine("{");
        builder.AppendLine("    public static class GeneratedScopeRuntimeHostFactory");
        builder.AppendLine("    {");

        if (hasPostDispatcher || hasCallDispatcher)
        {
            builder.AppendLine("        static GeneratedScopeRuntimeHostFactory()");
            builder.AppendLine("        {");

            if (hasPostDispatcher && postDispatcher != "null")
            {
                builder.AppendLine("            global::LayerBase.Scope.GlobalDispatcherRegistry.PostDispatcher = global::LayerBase.Scope.GeneratedScopePostDispatcher.Dispatch;");
            }

            if (hasCallDispatcher && callDispatcher != "null")
            {
                builder.AppendLine("            global::LayerBase.Scope.GlobalDispatcherRegistry.CallDispatcher = global::LayerBase.Scope.GeneratedScopeCallDispatcher.Dispatch;");
            }

            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        public static global::LayerBase.Scope.ScopeRuntimeHost Create(global::System.Collections.Generic.IReadOnlyList<global::LayerBase.DI.IService> services, global::LayerBase.Scope.ScopeRuntimeOptions? options = null, global::LayerBase.Actor.ActorWorld? sharedActorWorld = null, global::LayerBase.LayerRuntime? owningRuntime = null)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (services == null)");
        builder.AppendLine("            {");
        builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(services));");
        builder.AppendLine("            }");
        builder.AppendLine();
        if (hasGeneratedPlanner)
        {
            builder.AppendLine("            return Create(global::LayerBase.Scope.GeneratedScopeRuntimePlanner.Build(services), options, sharedActorWorld, owningRuntime);");
        }
        else
        {
            builder.AppendLine("            return Create(global::LayerBase.Scope.ScopeRuntimePlanner.Build(services), options, sharedActorWorld, owningRuntime);");
        }
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public static global::LayerBase.Scope.ScopeRuntimeHost Create(global::System.Collections.Generic.IReadOnlyList<global::LayerBase.Scope.ScopeRuntimePlan> plans, global::LayerBase.Scope.ScopeRuntimeOptions? options = null, global::LayerBase.Actor.ActorWorld? sharedActorWorld = null, global::LayerBase.LayerRuntime? owningRuntime = null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return global::LayerBase.Scope.ScopeRuntimeHost.Create(");
        builder.AppendLine("                plans,");
        builder.AppendLine("                options,");
        builder.AppendLine("                sharedActorWorld,");
        builder.AppendLine("                owningRuntime,");
        builder.AppendLine($"                postDispatcher: {postDispatcher},");
        builder.AppendLine($"                callDispatcher: {callDispatcher},");
        builder.AppendLine($"                scopeTypeResolver: {scopeTypeResolver});");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        spc.AddSource("LayerBase.Scope.GeneratedScopeRuntimeHostFactory.g.cs", builder.ToString());

        if (shouldGenerateRegistrar)
        {
            for (int i = 0; i < layerTypes.Length; i++)
            {
                GenerateRegistrar(spc, layerTypes[i]);
            }
        }
    }

    private static void GenerateRegistrar(SourceProductionContext spc, LayerTypeInfo layer)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (layer.Namespace != "<global namespace>")
        {
            builder.AppendLine($"namespace {layer.Namespace}");
            builder.AppendLine("{");
        }

        builder.AppendLine($"    {layer.Accessibility} partial class {layer.TypeName} : global::LayerBase.Scope.IScopeHostFactoryRegistrar");
        builder.AppendLine("    {");
        builder.AppendLine("        void global::LayerBase.Scope.IScopeHostFactoryRegistrar.RegisterScopeHostFactory()");
        builder.AppendLine("        {");
        builder.AppendLine("            global::LayerBase.Scope.ScopeHostFactory.Register(");
        builder.AppendLine("                static (services, options, sharedActorWorld, owningRuntime) =>");
        builder.AppendLine("                    global::LayerBase.Scope.GeneratedScopeRuntimeHostFactory.Create(services, options, sharedActorWorld, owningRuntime));");
        builder.AppendLine("        }");
        builder.AppendLine("    }");

        if (layer.Namespace != "<global namespace>")
        {
            builder.AppendLine("}");
        }

        spc.AddSource($"{SanitizeIdentifier(layer.FullTypeName)}.ScopeHostFactoryRegistrar.g.cs", builder.ToString());
    }

    private static void GenerateScopeOptionPartials(
        SourceProductionContext spc,
        ImmutableArray<ScopeOptionsInfo> definitions)
    {
        foreach (ScopeOptionsInfo definition in definitions)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            if (definition.Namespace != "<global namespace>")
            {
                builder.AppendLine($"namespace {definition.Namespace}");
                builder.AppendLine("{");
            }

            builder.AppendLine($"    {definition.Accessibility} partial class {definition.ScopeName}");
            builder.AppendLine("    {");
            builder.AppendLine("        internal static global::LayerBase.Scope.ScopeDescriptor __LayerBaseCreateScopeDescriptor(int scopeId)");
            builder.AppendLine("        {");
            builder.AppendLine("            return new global::LayerBase.Scope.ScopeDescriptor(");
            builder.AppendLine("                scopeId,");
            builder.AppendLine($"                \"{definition.ScopeName}\",");
            builder.AppendLine($"                (global::LayerBase.Scope.ScopeThreadingMode){definition.Threading},");
            builder.AppendLine($"                (global::LayerBase.Scope.ScopeClockMode){definition.Clock},");
            builder.AppendLine($"                {definition.TickRateHz},");
            builder.AppendLine($"                (global::LayerBase.Scope.ScopeStopPolicy){definition.StopPolicy});");
            builder.AppendLine("        }");
            builder.AppendLine("    }");

            if (definition.Namespace != "<global namespace>")
            {
                builder.AppendLine("}");
            }

            spc.AddSource($"{SanitizeIdentifier(definition.ScopeType)}.ScopeOptions.g.cs", builder.ToString());
        }
    }

    private static bool GeneratePlanner(
        SourceProductionContext spc,
        ImmutableArray<ScopeOptionsInfo> definitions,
        ImmutableArray<ScopedServiceInfo> scopedServices)
    {
        if (definitions.Length == 0 || scopedServices.Length == 0)
        {
            return false;
        }

        var definitionsByScope = definitions.ToDictionary(
            static item => item.ScopeType,
            static item => item,
            StringComparer.Ordinal);

        var bindings = scopedServices
                       .Where(item => definitionsByScope.ContainsKey(item.ScopeType))
                       .ToImmutableArray();
        if (bindings.Length == 0)
        {
            return false;
        }

        var scopeIds = bindings
                       .Select(static item => item.ScopeType)
                       .Distinct(StringComparer.Ordinal)
                       .OrderBy(static item => item, StringComparer.Ordinal)
                       .Select(static (scopeType, index) => (ScopeType: scopeType, ScopeId: index + 1))
                       .ToDictionary(static item => item.ScopeType, static item => item.ScopeId, StringComparer.Ordinal);

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Scope");
        builder.AppendLine("{");
        builder.AppendLine("    public static class GeneratedScopeRuntimePlanner");
        builder.AppendLine("    {");
        builder.AppendLine("        public static global::System.Collections.Generic.IReadOnlyList<global::LayerBase.Scope.ScopeRuntimePlan> Build(global::System.Collections.Generic.IReadOnlyList<global::LayerBase.DI.IService> services)");
        builder.AppendLine("        {");
        builder.AppendLine("            return global::LayerBase.Scope.ScopeRuntimePlanner.Build(services, TryResolveServiceScope);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public static bool TryGetScopeId(global::System.Type scopeType, out int scopeId)");
        builder.AppendLine("        {");

        foreach (var route in scopeIds.OrderBy(static item => item.Value))
        {
            builder.AppendLine($"            if (scopeType == typeof({route.Key}))");
            builder.AppendLine("            {");
            builder.AppendLine($"                scopeId = {route.Value};");
            builder.AppendLine("                return true;");
            builder.AppendLine("            }");
            builder.AppendLine();
        }

        builder.AppendLine("            scopeId = -1;");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        private static bool TryResolveServiceScope(global::System.Type serviceType, out global::LayerBase.Scope.ScopeRuntimeServiceScopeInfo scopeInfo)");
        builder.AppendLine("        {");

        foreach (ScopedServiceInfo binding in bindings)
        {
            ScopeOptionsInfo definition = definitionsByScope[binding.ScopeType];
            int scopeId = scopeIds[binding.ScopeType];
            builder.AppendLine($"            if (serviceType == typeof({binding.ServiceType}))");
            builder.AppendLine("            {");
            builder.AppendLine("                scopeInfo = new global::LayerBase.Scope.ScopeRuntimeServiceScopeInfo(");
            builder.AppendLine($"                    typeof({definition.ScopeType}),");
            builder.AppendLine($"                    {definition.ScopeType}.__LayerBaseCreateScopeDescriptor({scopeId}));");
            builder.AppendLine("                return true;");
            builder.AppendLine("            }");
            builder.AppendLine();
        }

        builder.AppendLine("            scopeInfo = default;");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        spc.AddSource("LayerBase.Scope.GeneratedScopeRuntimePlanner.g.cs", builder.ToString());
        return true;
    }

    private static bool ImplementsIService(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(static candidate =>
            candidate.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            "global::" + IServiceMetadataName);
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

    private readonly struct ScopeDiagnostic
    {
        public ScopeDiagnostic(DiagnosticDescriptor descriptor, Location location, string messageArgument)
        {
            Descriptor = descriptor;
            Location = location;
            MessageArgument = messageArgument;
        }

        private DiagnosticDescriptor Descriptor { get; }

        private Location Location { get; }

        private string MessageArgument { get; }

        public Diagnostic ToDiagnostic()
        {
            return Diagnostic.Create(Descriptor, Location, MessageArgument);
        }
    }

    private sealed record ScopeOptionsInfo(
        string ScopeType,
        string Namespace,
        string ScopeName,
        string Accessibility,
        bool IsPartial,
        Location Location,
        int Threading,
        int Clock,
        int TickRateHz,
        int StopPolicy);

    private sealed record ScopedServiceInfo(string ServiceType, string ScopeType);

    private sealed record LayerTypeInfo(
        string FullTypeName,
        string TypeName,
        string Namespace,
        string Accessibility);

    private static LayerTypeInfo? GetLayerType(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax cds) return null;
        if (context.SemanticModel.GetDeclaredSymbol(cds) is not INamedTypeSymbol type) return null;
        if (!IsLayerSubclass(type)) return null;

        bool isPartial = cds.Modifiers.Any(SyntaxKind.PartialKeyword);
        if (!isPartial) return null;

        string fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string typeName = type.Name;
        string ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        string accessibility = type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => "public"
        };

        return new LayerTypeInfo(fullName, typeName, ns, accessibility);
    }

    private static bool IsLayerSubclass(INamedTypeSymbol type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                "global::" + LayerBaseTypeName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static class Diagnostics
    {
#pragma warning disable RS2008
        public static readonly DiagnosticDescriptor NegativeTickRate = new(
            "LBSD001",
            "Invalid [ScopeOptions] tick rate",
            "Scope '{0}' declares a negative tickRateHz.",
            "LayerBase.Scope",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FixedRateRequiresPositiveTickRate = new(
            "LBSD002",
            "Invalid [ScopeOptions] fixed-rate tick rate",
            "Scope '{0}' uses FixedRate clock but tickRateHz is not greater than zero.",
            "LayerBase.Scope",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ScopeOptionsOwnerMustBePartial = new(
            "LBSD003",
            "[ScopeOptions] owner must be partial",
            "Scope type '{0}' uses [ScopeOptions] and must be declared partial so the source generator can emit scope options metadata",
            "LayerBase.Scope",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ScopeAttributeOwnerMustImplementIService = new(
            "LBSD004",
            "[Scope<TScope>] owner must implement IService",
            "Type '{0}' uses [Scope<TScope>] and must implement IService so the source generator can bind it to a scope",
            "LayerBase.Scope",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
#pragma warning restore RS2008
    }
}
