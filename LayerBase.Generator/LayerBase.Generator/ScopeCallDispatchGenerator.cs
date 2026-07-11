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
public sealed class ScopeCallDispatchGenerator : IIncrementalGenerator
{
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ScopeCallRequestAttributeName = "LayerBase.Scope.ScopeCallAttribute`2";
    private const string ScopeCallHandlerAttributeName = "LayerBase.Scope.ScopeCallAttribute";
    private const string LBTaskKindName = "LBTask";
    private const string LBTaskNamespace = "LayerBase.Async";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestTypes = context.SyntaxProvider
                                  .ForAttributeWithMetadataName(
                                      ScopeCallRequestAttributeName,
                                      static (_, _) => true,
                                      static (ctx, _) => GetRequestInfo(ctx))
                                  .Where(static item => item != null)!;

        var handlers = context.SyntaxProvider
                              .ForAttributeWithMetadataName(
                                  ScopeCallHandlerAttributeName,
                                  static (_, _) => true,
                                  static (ctx, _) => GetHandlerCandidate(ctx));

        var combined = requestTypes.Collect().Combine(handlers.Collect());
        context.RegisterSourceOutput(combined, static (spc, source) =>
            Generate(spc, source.Left, source.Right));
    }

    private static ScopeCallRequestInfo? GetRequestInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol requestType)
        {
            return null;
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(static attr =>
            attr.AttributeClass?.OriginalDefinition.ToDisplayString() ==
            "LayerBase.Scope.ScopeCallAttribute<TScope, TResult>");

        if (attribute?.AttributeClass == null || attribute.AttributeClass.TypeArguments.Length < 2)
        {
            return null;
        }

        ITypeSymbol resultType = attribute.AttributeClass.TypeArguments[1];
        return new ScopeCallRequestInfo(
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static ScopeCallHandlerCandidate GetHandlerCandidate(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol method ||
            method.ContainingType == null)
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(Diagnostics.InvalidSignature, Location.None, "<unknown>"));
        }

        if (method.Parameters.Length != 1 ||
            method.ReturnsVoid ||
            method.IsStatic)
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(
                    Diagnostics.InvalidSignature,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    method.Name));
        }

        if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax syntax)
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(
                    Diagnostics.InvalidSignature,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    method.Name));
        }

        if (!syntax.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(
                    Diagnostics.MethodMustBeAsync,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    method.Name));
        }

        if (syntax.Parent is not ClassDeclarationSyntax classDeclaration ||
            !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(
                    Diagnostics.OwnerMustBePartial,
                    method.ContainingType.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                    method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        INamedTypeSymbol serviceType = method.ContainingType;
        if (!ImplementsIService(serviceType))
        {
            return ScopeCallHandlerCandidate.Invalid(
                new ScopeCallDiagnostic(
                    Diagnostics.OwnerMustImplementIService,
                    serviceType.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                    serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        string scopeType = ResolveServiceScopeType(serviceType);

        ITypeSymbol requestType = method.Parameters[0].Type;
        ITypeSymbol resultType = method.ReturnType;

        ITypeSymbol handlerReturnType = method.ReturnType;
        bool returnsLBTask = false;
        if (method.ReturnType is INamedTypeSymbol namedReturn &&
            namedReturn.Arity == 1 &&
            namedReturn.OriginalDefinition.Name == LBTaskKindName &&
            namedReturn.OriginalDefinition.ContainingNamespace.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat) == "global::" + LBTaskNamespace)
        {
            resultType = namedReturn.TypeArguments[0];
            returnsLBTask = true;
        }

        string bridgeName = "__LayerBaseScopeCall_" + SanitizeIdentifier(
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        return ScopeCallHandlerCandidate.Valid(new ScopeCallHandlerInfo(
            serviceType.Name,
            serviceType.ContainingNamespace.ToDisplayString(),
            serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.Name,
            bridgeName,
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            returnsLBTask,
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
        ImmutableArray<ScopeCallRequestInfo?> nullableRequests,
        ImmutableArray<ScopeCallHandlerCandidate> candidates)
    {
        foreach (ScopeCallHandlerCandidate candidate in candidates)
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
                       .GroupBy(static item => item.RequestType)
                       .Select(static group => group.First())
                       .OrderBy(static item => item.RequestType, StringComparer.Ordinal)
                       .ToImmutableArray();

        var requestResults = requests.ToDictionary(
            static item => item.RequestType,
            static item => item.ResultType,
            StringComparer.Ordinal);

        var handlers = candidates
                       .Select(static item => item.Handler)
                       .Where(static item => item != null)
                       .Select(static item => item!)
                       .OrderBy(static item => item.RequestType, StringComparer.Ordinal)
                       .ThenBy(static item => item.ServiceType, StringComparer.Ordinal)
                       .ToImmutableArray();

        foreach (ScopeCallHandlerInfo handler in handlers)
        {
            if (!requestResults.TryGetValue(handler.RequestType, out string expectedResultType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.RequestTypeMustDeclareScopeCall,
                    handler.Location,
                    handler.RequestType));
                continue;
            }

            if (!StringComparer.Ordinal.Equals(handler.ResultType, expectedResultType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ResultTypeMismatch,
                    handler.Location,
                    handler.MethodName,
                    handler.RequestType,
                    expectedResultType,
                    handler.ResultType));
            }
        }

        if (requests.Length == 0 || handlers.Length == 0)
        {
            return;
        }

        var callIds = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < requests.Length; i++)
        {
            callIds.Add(requests[i].RequestType, i);
        }

        var bindings = handlers
                       .Where(handler => callIds.ContainsKey(handler.RequestType) &&
                                          requestResults.TryGetValue(handler.RequestType, out string expectedResultType) &&
                                          StringComparer.Ordinal.Equals(handler.ResultType, expectedResultType))
                       .GroupBy(static handler => handler.RequestType)
                       .Select(static group => group.First())
                       .ToImmutableArray();

        if (bindings.Length == 0)
        {
            return;
        }

        IReadOnlyDictionary<string, int> serviceSlots = AllocateServiceSlots(bindings);

        GenerateBridges(spc, bindings);
        GenerateDispatcher(spc, bindings, callIds, serviceSlots);
    }

    private static IReadOnlyDictionary<string, int> AllocateServiceSlots(
        ImmutableArray<ScopeCallHandlerInfo> bindings)
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

    private static void GenerateBridges(SourceProductionContext spc, ImmutableArray<ScopeCallHandlerInfo> bindings)
    {
        foreach (var group in bindings.GroupBy(static binding => binding.ServiceType))
        {
            ScopeCallHandlerInfo first = group.First();
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
            foreach (ScopeCallHandlerInfo binding in group)
            {
                string bridgeReturnType = binding.ReturnsLBTask
                    ? $"global::LayerBase.Async.LBTask<{binding.ResultType}>"
                    : binding.ResultType;
                builder.AppendLine(
                    $"        internal {bridgeReturnType} {binding.BridgeName}({binding.RequestType} message)");
                builder.AppendLine("        {");
                builder.AppendLine($"            return {binding.MethodName}(message);");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");

            if (first.Namespace != "<global namespace>")
            {
                builder.AppendLine("}");
            }

            spc.AddSource($"{SanitizeIdentifier(first.ServiceType)}.ScopeCallBridge.g.cs", builder.ToString());
        }
    }

    private static void GenerateDispatcher(
        SourceProductionContext spc,
        ImmutableArray<ScopeCallHandlerInfo> bindings,
        IReadOnlyDictionary<string, int> callIds,
        IReadOnlyDictionary<string, int> serviceSlots)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace LayerBase.Scope");
        builder.AppendLine("{");
        builder.AppendLine("    public static class GeneratedScopeCallDispatcher");
        builder.AppendLine("    {");
        builder.AppendLine("        public static void Dispatch(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopeCallMessage message)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (scope == null)");
        builder.AppendLine("            {");
        builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(scope));");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            try");
        builder.AppendLine("            {");
        builder.AppendLine("                switch (message.CallId)");
        builder.AppendLine("                {");

        foreach (ScopeCallHandlerInfo binding in bindings)
        {
            int callId = callIds[binding.RequestType];
            builder.AppendLine($"                    case {callId}:");
            builder.AppendLine($"                        Dispatch_{callId}(scope, message);");
            builder.AppendLine("                        return;");
        }

        builder.AppendLine("                    default:");
        builder.AppendLine("                        message.Promise.SetException(new global::System.InvalidOperationException($\"Unknown scope call id {message.CallId}.\"));");
        builder.AppendLine("                        return;");
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine("            catch (global::System.Exception exception)");
        builder.AppendLine("            {");
        builder.AppendLine("                message.Promise.SetException(exception);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();

        foreach (ScopeCallHandlerInfo binding in bindings)
        {
            int callId = callIds[binding.RequestType];
            builder.AppendLine($"        private static void Dispatch_{callId}(global::LayerBase.Scope.ScopeRuntime scope, global::LayerBase.Scope.ScopeCallMessage message)");
            builder.AppendLine("        {");

            if (serviceSlots.TryGetValue(binding.ServiceType, out int slotIndex))
            {
                builder.AppendLine($"            var service = ({binding.ServiceType})scope.Services[{slotIndex}];");
            }
            else
            {
                builder.AppendLine($"            var service = FindService<{binding.ServiceType}>(scope.Services);");
            }

            if (binding.ReturnsLBTask)
            {
                builder.AppendLine($"            var task = service.{binding.BridgeName}(({binding.RequestType})message.Payload);");
                builder.AppendLine($"            var awaiter = task.GetAwaiter();");
                builder.AppendLine("            if (awaiter.IsCompleted)");
                builder.AppendLine("            {");
                builder.AppendLine("                try");
                builder.AppendLine("                {");
                builder.AppendLine($"                    ((global::LayerBase.Scope.ScopePromise<{binding.ResultType}>)message.Promise).SetResult(awaiter.GetResult());");
                builder.AppendLine("                }");
                builder.AppendLine("                catch (global::System.Exception exception)");
                builder.AppendLine("                {");
                builder.AppendLine("                    message.Promise.SetException(exception);");
                builder.AppendLine("                }");
                builder.AppendLine("            }");
                builder.AppendLine("            else");
                builder.AppendLine("            {");
                builder.AppendLine("                awaiter.OnCompleted(() =>");
                builder.AppendLine("                {");
                builder.AppendLine("                    try");
                builder.AppendLine("                    {");
                builder.AppendLine($"                        ((global::LayerBase.Scope.ScopePromise<{binding.ResultType}>)message.Promise).SetResult(awaiter.GetResult());");
                builder.AppendLine("                    }");
                builder.AppendLine("                    catch (global::System.Exception exception)");
                builder.AppendLine("                    {");
                builder.AppendLine("                        message.Promise.SetException(exception);");
                builder.AppendLine("                    }");
                builder.AppendLine("                });");
                builder.AppendLine("            }");
            }
            else
            {
                builder.AppendLine($"            var result = service.{binding.BridgeName}(({binding.RequestType})message.Payload);");
                builder.AppendLine($"            ((global::LayerBase.Scope.ScopePromise<{binding.ResultType}>)message.Promise).SetResult(result);");
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

        spc.AddSource("LayerBase.Scope.GeneratedScopeCallDispatcher.g.cs", builder.ToString());
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

    private sealed record ScopeCallRequestInfo(string RequestType, string ResultType);

    private sealed record ScopeCallHandlerCandidate(
        ScopeCallHandlerInfo? Handler,
        ScopeCallDiagnostic? Diagnostic)
    {
        public static ScopeCallHandlerCandidate Valid(ScopeCallHandlerInfo handler)
        {
            return new ScopeCallHandlerCandidate(handler, null);
        }

        public static ScopeCallHandlerCandidate Invalid(ScopeCallDiagnostic diagnostic)
        {
            return new ScopeCallHandlerCandidate(null, diagnostic);
        }
    }

    private sealed record ScopeCallDiagnostic(
        DiagnosticDescriptor Descriptor,
        Location Location,
        string MessageArgument);

    private sealed record ScopeCallHandlerInfo(
        string ServiceName,
        string Namespace,
        string ServiceType,
        string MethodName,
        string BridgeName,
        string RequestType,
        string ResultType,
        bool ReturnsLBTask,
        string ScopeType,
        Location Location);

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "ScopeCallDispatchGenerator";

        public static readonly DiagnosticDescriptor InvalidSignature =
            new(
                "LBSC001",
                "Invalid [ScopeCall] method signature",
                "Method '{0}' uses [ScopeCall] but must be an instance method with exactly one parameter and a non-void return type",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustBePartial =
            new(
                "LBSC002",
                "[ScopeCall] owner must be partial",
                "Type '{0}' uses [ScopeCall] and must be declared partial so the source generator can emit the private method bridge",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ResultTypeMismatch =
            new(
                "LBSC003",
                "[ScopeCall] result type mismatch",
                "Method '{0}' handles '{1}' but returns '{3}'; it must return async LBTask<{2}> matching the request's ScopeCall attribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustImplementIService =
            new(
                "LBSC004",
                "[ScopeCall] owner must implement IService",
                "Type '{0}' uses [ScopeCall] and must implement LayerBase.DI.IService so it can be hosted in a ScopeRuntime",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor RequestTypeMustDeclareScopeCall =
            new(
                "LBSC005",
                "[ScopeCall] request type must declare scope and result",
                "Type '{0}' is handled by [ScopeCall] but must declare [ScopeCall<TScope, TResult>] so the generator can assign a stable scope call id",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor MethodMustBeAsync =
            new(
                "LBSC006",
                "[ScopeCall] method must be async",
                "Method '{0}' uses [ScopeCall] and must be declared with the async keyword",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008
}
