using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CallReceiverAnalyzer : DiagnosticAnalyzer
{
    private const string CallAttributeMetadataName = "LayerBase.Call.CallAttribute";
    private const string ScopeLocalCallHandlerMetadataName = "LayerBase.Call.IScopeLocalCallHandler`2";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Diagnostics.CallReceiverMustBeAsync);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var callAttribute = compilationContext.Compilation.GetTypeByMetadataName(CallAttributeMetadataName);
            var scopeLocalCallHandler =
                compilationContext.Compilation.GetTypeByMetadataName(ScopeLocalCallHandlerMetadataName);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, callAttribute, scopeLocalCallHandler),
                SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol? callAttribute,
        INamedTypeSymbol? scopeLocalCallHandler)
    {
        if (context.Symbol is not IMethodSymbol method) return;
        if (method.MethodKind != MethodKind.Ordinary) return;
        if (method.IsAsync) return;
        if (method.ContainingType.TypeKind == TypeKind.Interface) return;

        if (!IsCallReceiver(method, callAttribute, scopeLocalCallHandler)) return;

        var location = method.Locations.FirstOrDefault(static candidate => candidate.IsInSource)
                       ?? method.Locations.FirstOrDefault();
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.CallReceiverMustBeAsync,
            location,
            method.Name,
            method.ContainingType.ToDisplayString()));
    }

    private static bool IsCallReceiver(
        IMethodSymbol method,
        INamedTypeSymbol? callAttribute,
        INamedTypeSymbol? scopeLocalCallHandler)
    {
        if (HasCallAttribute(method, callAttribute))
            return true;

        if (scopeLocalCallHandler == null)
            return false;

        return method.Name == "HandleAsync" &&
               method.ContainingType.AllInterfaces.Any(iface =>
                   SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, scopeLocalCallHandler));
    }

    private static bool HasCallAttribute(IMethodSymbol method, INamedTypeSymbol? callAttribute)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null) continue;

            if (callAttribute != null &&
                SymbolEqualityComparer.Default.Equals(attributeClass, callAttribute))
                return true;

            var name = attributeClass.Name;
            if (name is "CallAttribute" or "SubscribeScopeCallAttribute" or "ScopeCallAttribute")
                return true;
        }

        return false;
    }

#pragma warning disable RS2008
    public static class Diagnostics
    {
        private const string Category = "CallReceiverAnalyzer";

        public static readonly DiagnosticDescriptor CallReceiverMustBeAsync =
            new(
                "LBG305",
                "Call receiver method must be async",
                "Call receiver method '{0}' on '{1}' must be declared async",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008
}
