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
public sealed class CallAutoBindGenerator : IIncrementalGenerator
{
    private const string CallAttributeMetadataName = "LayerBase.Call.CallAttribute";
    private const string LayerMetadataName = "LayerBase.Layers.Layer";
    private const string ServiceMetadataName = "LayerBase.DI.IService";
    private const string LayerContextMetadataName = "LayerBase.DI.ILayerContext";
    private const string CancellationTokenMetadataName = "System.Threading.CancellationToken";
    private const string LBTaskMetadataName = "LayerBase.Async.LBTask`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var callMethods = context.SyntaxProvider
                                 .ForAttributeWithMetadataName(
                                     CallAttributeMetadataName,
                                     static (node, _) => node is MethodDeclarationSyntax,
                                     static (ctx,  _) => new CallMethodCandidate((IMethodSymbol)ctx.TargetSymbol))
                                 .Collect();

        var source = context.CompilationProvider.Combine(callMethods);
        context.RegisterSourceOutput(source, static (spc, value) => Execute(spc, value.Left, value.Right));
    }

    private static void Execute(SourceProductionContext             spc, Compilation compilation,
                                ImmutableArray<CallMethodCandidate> candidates)
    {
        if (candidates.IsDefaultOrEmpty) return;

        var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
        var serviceSymbol = compilation.GetTypeByMetadataName(ServiceMetadataName);
        var layerContextSymbol = compilation.GetTypeByMetadataName(LayerContextMetadataName);

        if (layerSymbol == null || serviceSymbol == null) return;

        var diagnostics = new List<Diagnostic>();
        var validBindings = new List<CallMethodBinding>();

        foreach (var candidate in candidates)
        {
            var method = candidate.Method;
            var ownerType = method.ContainingType;
            var location = method.Locations.FirstOrDefault();

            if (!IsPartial(ownerType))
            {
                diagnostics.Add(
                    Diagnostic.Create(Diagnostics.OwnerMustBePartial, location, ownerType.ToDisplayString()));
                continue;
            }

            if (ownerType.IsAbstract)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.OwnerCannotBeAbstract, location,
                    ownerType.ToDisplayString()));
                continue;
            }

            var ownerKind = GetOwnerKind(ownerType, layerSymbol, serviceSymbol, layerContextSymbol);
            if (ownerKind == CallOwnerKind.Invalid)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedOwner, location, ownerType.ToDisplayString()));
                continue;
            }

            if (!TryCreateBinding(method, ownerType, ownerKind, out var binding, out var expectedSignature))
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.InvalidSignature,
                    location,
                    method.Name,
                    expectedSignature ?? "LBTask<TResponse> Handle(TRequest request)",
                    ownerType.ToDisplayString()));
                continue;
            }

            validBindings.Add(binding!);
        }

        foreach (var diagnostic in diagnostics)
            spc.ReportDiagnostic(diagnostic);

        var groupedBindings = validBindings
            .GroupBy(static binding => binding.OwnerType, SymbolEqualityComparer.Default);

        foreach (var group in groupedBindings)
        {
            if (group.Key is not INamedTypeSymbol ownerType) continue;

            var sourceText = GeneratePartial(ownerType, group.ToList());
            if (string.IsNullOrWhiteSpace(sourceText)) continue;

            spc.AddSource(CreateHintName(ownerType), SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    private static bool TryCreateBinding(
        IMethodSymbol          method,
        INamedTypeSymbol       ownerType,
        CallOwnerKind          ownerKind,
        out CallMethodBinding? binding,
        out string?            expectedSignature)
    {
        expectedSignature =
            "LBTask<TResponse> Handle(TRequest request) or LBTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)";
        binding = null;

        if (method.IsStatic || method.IsGenericMethod || method.ReturnsVoid)
            return false;

        if (method.Parameters.Length is < 1 or > 2)
            return false;

        var requestParameter = method.Parameters[0];
        if (requestParameter.RefKind != RefKind.None)
            return false;

        if (method.Parameters.Length == 2)
        {
            var cancellationParameter = method.Parameters[1];
            if (cancellationParameter.RefKind != RefKind.None ||
                cancellationParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                "global::System.Threading.CancellationToken")
                return false;
        }

        if (requestParameter.Type is not ITypeSymbol requestType || !requestType.IsValueType)
            return false;

        if (method.ReturnType is not INamedTypeSymbol returnType ||
            !returnType.IsGenericType ||
            returnType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
            "global::LayerBase.Async.LBTask<T>")
            return false;

        var responseType = returnType.TypeArguments[0];
        if (!responseType.IsValueType)
            return false;

        binding = new CallMethodBinding(
            ownerType,
            ownerKind,
            method.Name,
            SanitizeIdentifier(method.Name + "_" +
                               method.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line),
            requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.Parameters.Length == 2);
        return true;
    }

    private static CallOwnerKind GetOwnerKind(
        INamedTypeSymbol  ownerType,
        INamedTypeSymbol  layerSymbol,
        INamedTypeSymbol  serviceSymbol,
        INamedTypeSymbol? layerContextSymbol)
    {
        if (InheritsFrom(ownerType, layerSymbol))
            return CallOwnerKind.Layer;

        if (Implements(ownerType, serviceSymbol))
            return CallOwnerKind.Service;

        if (layerContextSymbol != null && Implements(ownerType, layerContextSymbol))
            return CallOwnerKind.Invalid;

        return CallOwnerKind.Invalid;
    }

    private static bool Implements(INamedTypeSymbol ownerType, INamedTypeSymbol interfaceSymbol)
    {
        return ownerType.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol));
    }

    private static bool InheritsFrom(INamedTypeSymbol ownerType, INamedTypeSymbol baseType)
    {
        for (var current = ownerType; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
                typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;

        return false;
    }

    private static string GeneratePartial(INamedTypeSymbol ownerType, IReadOnlyList<CallMethodBinding> bindings)
    {
        if (bindings.Count == 0) return string.Empty;

        var ownerDisplay = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var ownerIdentifier = ownerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var ns = ownerType.ContainingNamespace is { IsGlobalNamespace: false } namespaceSymbol
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// This file was generated by CallAutoBindGenerator.");

        if (!string.IsNullOrEmpty(ns))
        {
            builder.Append("namespace ").Append(ns).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(ownerIdentifier)
               .AppendLine(" : global::LayerBase.Call.IAutoCallBinder");
        builder.AppendLine("{");
        builder.AppendLine(
            "    void global::LayerBase.Call.IAutoCallBinder.AutoBindCalls(global::LayerBase.Layers.Layer layer)");
        builder.AppendLine("    {");

        foreach (var binding in bindings)
            builder.Append("        global::LayerBase.Call.LayerCallRegistrationBridge.Register<")
                   .Append(binding.RequestDisplay)
                   .Append(", ")
                   .Append(binding.ResponseDisplay)
                   .Append(">(layer, new __GeneratedCallHandler_")
                   .Append(binding.GeneratedIdentifier)
                   .Append("(this));")
                   .AppendLine();

        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var binding in bindings)
        {
            builder.Append("    private sealed class __GeneratedCallHandler_")
                   .Append(binding.GeneratedIdentifier)
                   .Append(" : global::LayerBase.Call.ILayerCallHandler<")
                   .Append(binding.RequestDisplay)
                   .Append(", ")
                   .Append(binding.ResponseDisplay)
                   .AppendLine(">");
            builder.AppendLine("    {");
            builder.Append("        private readonly ").Append(ownerDisplay).AppendLine(" _owner;");
            builder.AppendLine();
            builder.Append("        public __GeneratedCallHandler_").Append(binding.GeneratedIdentifier).Append("(")
                   .Append(ownerDisplay).AppendLine(" owner)");
            builder.AppendLine("        {");
            builder.AppendLine("            _owner = owner;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        public global::LayerBase.Async.LBTask<").Append(binding.ResponseDisplay)
                   .Append("> HandleAsync(").Append(binding.RequestDisplay)
                   .AppendLine(" request, global::System.Threading.CancellationToken cancellationToken = default)");
            builder.AppendLine("        {");
            builder.Append("            return _owner.").Append(binding.MethodName).Append("(request");
            if (binding.TakesCancellationToken)
                builder.Append(", cancellationToken");
            builder.AppendLine(");");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(ns))
            builder.AppendLine("}");

        return builder.ToString();
    }

    private static string CreateHintName(INamedTypeSymbol ownerType)
    {
        var display = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{SanitizeIdentifier(display)}.CallAutoBind.g.cs";
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return builder.ToString();
    }

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "CallAutoBindGenerator";

        public static readonly DiagnosticDescriptor InvalidSignature =
            new(
                "LBG301",
                "Invalid [Call] method signature",
                "Method '{0}' uses [Call] but must match '{1}'. Owner '{2}' supports [Call] only for LBTask<TResponse> request/response methods.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor UnsupportedOwner =
            new(
                "LBG302",
                "Unsupported [Call] owner",
                "Type '{0}' uses [Call] on a method but [Call] methods are only supported on Layer and IService types. ILayerContext modules must not declare [Call].",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustBePartial =
            new(
                "LBG303",
                "[Call] owner must be partial",
                "Type '{0}' uses [Call] and must be declared partial so the source generator can emit call registration.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerCannotBeAbstract =
            new(
                "LBG304",
                "[Call] owner cannot be abstract",
                "Type '{0}' uses [Call] and cannot be abstract because generated call registration requires a concrete owner instance.",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class CallMethodCandidate
    {
        public CallMethodCandidate(IMethodSymbol method)
        {
            Method = method;
        }

        public IMethodSymbol Method { get; }
    }

    private sealed class CallMethodBinding
    {
        public CallMethodBinding(
            INamedTypeSymbol ownerType,
            CallOwnerKind    ownerKind,
            string           methodName,
            string           generatedIdentifier,
            string           requestDisplay,
            string           responseDisplay,
            bool             takesCancellationToken)
        {
            OwnerType = ownerType;
            OwnerKind = ownerKind;
            MethodName = methodName;
            GeneratedIdentifier = generatedIdentifier;
            RequestDisplay = requestDisplay;
            ResponseDisplay = responseDisplay;
            TakesCancellationToken = takesCancellationToken;
        }

        public INamedTypeSymbol OwnerType { get; }
        public CallOwnerKind OwnerKind { get; }
        public string MethodName { get; }
        public string GeneratedIdentifier { get; }
        public string RequestDisplay { get; }
        public string ResponseDisplay { get; }
        public bool TakesCancellationToken { get; }
    }

    private enum CallOwnerKind
    {
        Invalid = 0,
        Layer = 1,
        Service = 2
    }
}