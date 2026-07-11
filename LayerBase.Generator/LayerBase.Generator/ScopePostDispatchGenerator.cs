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
public sealed class ScopePostDispatchGenerator : IIncrementalGenerator
{
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ScopeEventRequestAttributeName = "LayerBase.Scope.ScopeEventAttribute`1";
    private const string ScopeEventHandlerAttributeName = "LayerBase.Scope.ScopeEventAttribute";
    private const string ScopeAttributeName = "LayerBase.Scope.ScopeAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestTypes = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      ScopeEventRequestAttributeName,
                                      static (_, _) => true,
                                      static (ctx, _) => GetRequestInfo(ctx))
                                  .Where(static item => item != null)!;

        var handlers = context.SyntaxProvider
                              .ForAttributeWithMetadataName(
                                  ScopeEventHandlerAttributeName,
                                  static (_, _) => true,
                                  static (ctx, _) => GetHandlerCandidate(ctx));

        var combined = requestTypes.Collect().Combine(handlers.Collect());
        context.RegisterSourceOutput(combined, static (spc, source) =>
            Generate(spc, source.Left, source.Right));
    }

    private static ScopeEventRequestInfo? GetRequestInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol eventType)
        {
            return null;
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(static attr =>
            attr.AttributeClass?.OriginalDefinition.ToDisplayString() ==
            "LayerBase.Scope.ScopeEventAttribute<TScope>");

        if (attribute?.AttributeClass == null || attribute.AttributeClass.TypeArguments.Length < 1)
        {
            return null;
        }

        return new ScopeEventRequestInfo(
            eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static ScopeEventHandlerCandidate GetHandlerCandidate(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol method ||
            method.ContainingType == null)
        {
            return ScopeEventHandlerCandidate.Invalid(
                new ScopeEventDiagnostic(Diagnostics.InvalidSignature, Location.None, "<unknown>"));
        }

        if (method.Parameters.Length != 1 ||
            !method.ReturnsVoid ||
            method.IsStatic)
        {
            return ScopeEventHandlerCandidate.Invalid(
                new ScopeEventDiagnostic(
                    Diagnostics.InvalidSignature,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    method.Name));
        }

        if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax syntax ||
            syntax.Parent is not ClassDeclarationSyntax classDeclaration ||
            !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return ScopeEventHandlerCandidate.Invalid(
                new ScopeEventDiagnostic(
                    Diagnostics.OwnerMustBePartial,
                    method.ContainingType.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                    method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        INamedTypeSymbol serviceType = method.ContainingType;
        if (!ImplementsIService(serviceType))
        {
            return ScopeEventHandlerCandidate.Invalid(
                new ScopeEventDiagnostic(
                    Diagnostics.OwnerMustImplementIService,
                    serviceType.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                    serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        string scopeType = ResolveServiceScopeType(serviceType);

        ITypeSymbol eventType = method.Parameters[0].Type;
        string bridgeName = "__LayerBaseScopeEvent_" + SanitizeIdentifier(method.Name) + "_" + SanitizeIdentifier(
            eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        return ScopeEventHandlerCandidate.Valid(new ScopeEventHandlerInfo(
            serviceType.Name,
            serviceType.ContainingNamespace.ToDisplayString(),
            serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.Name,
            bridgeName,
            eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            scopeType,
            method.Locations.FirstOrDefault() ?? Location.None));
    }

    private static string ResolveServiceScopeType(INamedTypeSymbol serviceType)
    {
        foreach (AttributeData attr in serviceType.GetAttributes())
        {
            if (attr.AttributeClass?.OriginalDefinition.ToDisplayString() ==
                "LayerBase.Scope.ScopeAttribute<TScope>" &&
                attr.AttributeClass.TypeArguments.Length > 0)
            {
                return attr.AttributeClass.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return "";
    }

    private static void Generate(
        SourceProductionContext spc,
        ImmutableArray<ScopeEventRequestInfo?> nullableRequests,
        ImmutableArray<ScopeEventHandlerCandidate> candidates)
    {
        foreach (ScopeEventHandlerCandidate candidate in candidates)
        {
            if (candidate.Diagnostic != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    candidate.Diagnostic.Descriptor,
                    candidate.Diagnostic.Location,
                    candidate.Diagnostic.MessageArgument));
            }
        }

        var requests = nullableRequests
                       .Where(static item => item != null)
                       .Select(static item => item!)
                       .GroupBy(static item => item.EventType)
                       .Select(static group => group.First())
                       .OrderBy(static item => item.EventType, StringComparer.Ordinal)
                       .ToImmutableArray();

        var handlers = candidates
                       .Select(static item => item.Handler)
                       .Where(static item => item != null)
                       .Select(static item => item!)
                       .OrderBy(static item => item.EventType, StringComparer.Ordinal)
                       .ThenBy(static item => item.ServiceType, StringComparer.Ordinal)
                       .ThenBy(static item => item.MethodName, StringComparer.Ordinal)
                       .ToImmutableArray();

        var declaredEventTypes = new HashSet<string>(
            requests.Select(static request => request.EventType),
            StringComparer.Ordinal);
        foreach (ScopeEventHandlerInfo handler in handlers)
        {
            if (!declaredEventTypes.Contains(handler.EventType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.EventTypeMustDeclareScopeEvent,
                    handler.Location,
                    handler.EventType));
            }
        }

        if (requests.Length == 0 || handlers.Length == 0)
        {
            return;
        }

        var eventIds = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < requests.Length; i++)
        {
            eventIds.Add(requests[i].EventType, i);
        }

        var bindings = handlers
                       .Where(handler => eventIds.ContainsKey(handler.EventType))
                       .ToImmutableArray();

        if (bindings.Length == 0)
        {
            return;
        }

        IReadOnlyDictionary<string, int> serviceSlots = AllocateServiceSlots(bindings);

        GenerateBridges(spc, bindings);
        GenerateDispatcher(spc, bindings, eventIds, serviceSlots);
    }

    private static IReadOnlyDictionary<string, int> AllocateServiceSlots(
        ImmutableArray<ScopeEventHandlerInfo> bindings)
    {
        var slots = new Dictionary<string, int>(StringComparer.Ordinal);
        var uniqueServices = bindings
                             .Select(static b => (b.ServiceType, b.ScopeType))
                             .Distinct()
                             .OrderBy(static s => s.ScopeType, StringComparer.Ordinal)
                             .ThenBy(static s => s.ServiceType, StringComparer.Ordinal)
                             .ToList();

        string? currentScope = null;
        int slotIndex = 0;
        for (int i = 0; i < uniqueServices.Count; i++)
        {
            var (serviceType, scopeType) = uniqueServices[i];
            if (scopeType != currentScope)
            {
                currentScope = scopeType;
                slotIndex = 0;
            }

            if (!slots.ContainsKey(serviceType))
            {
                slots[serviceType] = slotIndex++;
            }
        }

        return slots;
    }

    private static void GenerateBridges(SourceProductionContext spc, ImmutableArray<ScopeEventHandlerInfo> bindings)
    {
        foreach (var group in bindings.GroupBy(static binding => binding.ServiceType))
        {
            ScopeEventHandlerInfo first = group.First();
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            if (first.Namespace != "<global namespace>")
            {
                builder.AppendLine($"namespace {first.Namespace}");
                builder.AppendLine("{");
            }

            builder.AppendLine($"    partial class {first.ServiceName}");
            builder.AppendLine("    {");
            foreach (ScopeEventHandlerInfo binding in group)
            {
                builder.AppendLine($"        internal void {binding.BridgeName}({binding.EventType} message)");
                builder.AppendLine("        {");
                builder.AppendLine($"            {binding.MethodName}(message);");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");

            if (first.Namespace != "<global namespace>")
            {
                builder.AppendLine("}");
            }

            spc.AddSource($"{SanitizeIdentifier(first.ServiceType)}.ScopeEventBridge.g.cs", builder.ToString());
        }
    }

    private static void GenerateDispatcher(
        SourceProductionContext spc,
        ImmutableArray<ScopeEventHandlerInfo> bindings,
        IReadOnlyDictionary<string, int> eventIds,
        IReadOnlyDictionary<string, int> serviceSlots)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Scope");
        builder.AppendLine("{");
        builder.AppendLine("    public static class GeneratedScopePostDispatcher");
        builder.AppendLine("    {");
        builder.AppendLine("        public static void Dispatch(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopePostMessage message)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (scope == null)");
        builder.AppendLine("            {");
        builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(scope));");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            switch (message.EventId)");
        builder.AppendLine("            {");

        foreach (var group in bindings.GroupBy(static binding => binding.EventType))
        {
            int eventId = eventIds[group.Key];
            builder.AppendLine($"                case {eventId}:");
            builder.AppendLine($"                    Dispatch_{eventId}(scope, message);");
            builder.AppendLine("                    return;");
        }

        builder.AppendLine("                default:");
        builder.AppendLine("                    throw new global::System.InvalidOperationException($\"Unknown scope event id {message.EventId}.\");");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();

        foreach (var group in bindings.GroupBy(static binding => binding.EventType))
        {
            int eventId = eventIds[group.Key];
            string eventType = group.First().EventType;
            builder.AppendLine($"        private static void Dispatch_{eventId}(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopePostMessage message)");
            builder.AppendLine("        {");
            builder.AppendLine($"            var payload = ({eventType})message.Payload;");

            int handlerIndex = 0;
            foreach (ScopeEventHandlerInfo binding in group)
            {
                string serviceVariable = $"service_{handlerIndex}";
                if (serviceSlots.TryGetValue(binding.ServiceType, out int slotIndex))
                {
                    builder.AppendLine($"            var {serviceVariable} = ({binding.ServiceType})scope.Services[{slotIndex}];");
                    builder.AppendLine($"            {serviceVariable}.{binding.BridgeName}(payload);");
                }
                else
                {
                    builder.AppendLine($"            var {serviceVariable} = FindService<{binding.ServiceType}>(scope.Services);");
                    builder.AppendLine($"            {serviceVariable}.{binding.BridgeName}(payload);");
                }
                handlerIndex++;
            }

            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        private static TService FindService<TService>(global::LayerBase.DI.IService[] services)");
        builder.AppendLine("            where TService : class, global::LayerBase.DI.IService");
        builder.AppendLine("        {");
        builder.AppendLine("            for (int i = 0; i < services.Length; i++)");
        builder.AppendLine("            {");
        builder.AppendLine("                if (services[i] is TService service)");
        builder.AppendLine("                {");
        builder.AppendLine("                    return service;");
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            throw new global::System.InvalidOperationException($\"Scope service '{typeof(TService).FullName}' is not registered in the target scope.\");");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        spc.AddSource("LayerBase.Scope.GeneratedScopePostDispatcher.g.cs", builder.ToString());
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

    private static bool ImplementsIService(INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(static candidate =>
            candidate.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            "global::" + IServiceMetadataName);
    }

    private sealed record ScopeEventRequestInfo(string EventType);

    private sealed record ScopeEventHandlerCandidate(
        ScopeEventHandlerInfo? Handler,
        ScopeEventDiagnostic? Diagnostic)
    {
        public static ScopeEventHandlerCandidate Valid(ScopeEventHandlerInfo handler)
        {
            return new ScopeEventHandlerCandidate(handler, null);
        }

        public static ScopeEventHandlerCandidate Invalid(ScopeEventDiagnostic diagnostic)
        {
            return new ScopeEventHandlerCandidate(null, diagnostic);
        }
    }

    private sealed record ScopeEventDiagnostic(
        DiagnosticDescriptor Descriptor,
        Location Location,
        string MessageArgument);

    private sealed record ScopeEventHandlerInfo(
        string ServiceName,
        string Namespace,
        string ServiceType,
        string MethodName,
        string BridgeName,
        string EventType,
        string ScopeType,
        Location Location);

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "ScopePostDispatchGenerator";

        public static readonly DiagnosticDescriptor InvalidSignature =
            new(
                "LBSE001",
                "Invalid [ScopeEvent] method signature",
                "Method '{0}' uses [ScopeEvent] but must be an instance method with exactly one parameter and void return type",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustBePartial =
            new(
                "LBSE002",
                "[ScopeEvent] owner must be partial",
                "Type '{0}' uses [ScopeEvent] and must be declared partial so the source generator can emit the private method bridge",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustImplementIService =
            new(
                "LBSE003",
                "[ScopeEvent] owner must implement IService",
                "Type '{0}' uses [ScopeEvent] but must implement LayerBase.DI.IService so it can be hosted in a ScopeRuntime",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor EventTypeMustDeclareScopeEvent =
            new(
                "LBSE004",
                "[ScopeEvent] message type must declare scope",
                "Type '{0}' is handled by [ScopeEvent] but must declare [ScopeEvent<TScope>] so the generator can assign a stable scope event id",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008
}
