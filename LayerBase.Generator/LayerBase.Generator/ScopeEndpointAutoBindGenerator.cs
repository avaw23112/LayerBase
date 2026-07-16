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
public sealed class ScopeEndpointAutoBindGenerator : IIncrementalGenerator
{
    private const string SubscribeScopeCallAttributeMetadataName = "LayerBase.Scope.SubscribeScopeCallAttribute";
    private const string SubscribeScopeEventAttributeMetadataName = "LayerBase.Scope.SubscribeScopeEventAttribute";
    private const string LayerMetadataName = "LayerBase.Layers.Layer";
    private const string IServiceMetadataName = "LayerBase.DI.IService";
    private const string ILayerContextMetadataName = "LayerBase.DI.ILayerContext";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
                                .CreateSyntaxProvider(
                                    static (node, _) => node is MethodDeclarationSyntax,
                                    static (ctx, _) => GetCandidate(ctx))
                                .Where(static candidate => candidate != null)!;

        var source = context.CompilationProvider.Combine(candidates.Collect());
        context.RegisterSourceOutput(source, static (spc, value) => Execute(spc, value.Left, value.Right));
    }

    private static ScopeEndpointMethodCandidate? GetCandidate(GeneratorSyntaxContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (methodDeclaration.AttributeLists.Count == 0)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol method)
            return null;

        var endpointAttributes = method.GetAttributes()
                                       .Select(static attribute => attribute.AttributeClass?.ToDisplayString())
                                       .Where(static name =>
                                           name == "LayerBase.Scope.SubscribeScopeCallAttribute" ||
                                           name == "LayerBase.Scope.SubscribeScopeEventAttribute")
                                       .ToArray();

        return endpointAttributes.Length == 0
            ? null
            : new ScopeEndpointMethodCandidate(method, endpointAttributes);
    }

    private static void Execute(
        SourceProductionContext spc,
        Compilation compilation,
        ImmutableArray<ScopeEndpointMethodCandidate?> candidates)
    {
        if (candidates.IsDefaultOrEmpty) return;

        var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
        if (layerSymbol == null) return;
        var serviceSymbol = compilation.GetTypeByMetadataName(IServiceMetadataName);
        var layerContextSymbol = compilation.GetTypeByMetadataName(ILayerContextMetadataName);

        var diagnostics = new List<Diagnostic>();
        var validBindings = new List<ScopeEndpointBinding>();

        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;

            var method = candidate.Method;
            var ownerType = method.ContainingType;
            var location = method.Locations.FirstOrDefault();

            if (candidate.AttributeNames.Length > 1)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.ConflictingEndpointAttributes, location, method.Name));
                continue;
            }

            if (!IsPartial(ownerType))
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.OwnerMustBePartial,
                    location,
                    ownerType.ToDisplayString()));
                continue;
            }

            if (ownerType.IsAbstract)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.OwnerCannotBeAbstract,
                    location,
                    ownerType.ToDisplayString()));
                continue;
            }

            var ownerKind = GetOwnerKind(ownerType, layerSymbol, serviceSymbol, layerContextSymbol);
            if (ownerKind == ScopeEndpointOwnerKind.Invalid)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.UnsupportedOwner,
                    location,
                    ownerType.ToDisplayString()));
                continue;
            }

            var kind = candidate.AttributeNames[0] == SubscribeScopeCallAttributeMetadataName
                ? ScopeEndpointKind.Call
                : ScopeEndpointKind.Event;

            if (!TryCreateBinding(method, ownerType, ownerKind, kind, out var binding, out var expectedSignature))
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.InvalidSignature,
                    location,
                    method.Name,
                    expectedSignature,
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
        IMethodSymbol method,
        INamedTypeSymbol ownerType,
        ScopeEndpointOwnerKind ownerKind,
        ScopeEndpointKind kind,
        out ScopeEndpointBinding? binding,
        out string expectedSignature)
    {
        binding = null;

        if (kind == ScopeEndpointKind.Call)
        {
            expectedSignature =
                "async LBTask<TResponse> Handle(TRequest request) or async LBTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)";

            if (!method.IsAsync || method.IsStatic || method.IsGenericMethod || method.ReturnsVoid)
                return false;

            if (method.Parameters.Length is < 1 or > 2)
                return false;

            var requestParameter = method.Parameters[0];
            if (requestParameter.RefKind != RefKind.None || !requestParameter.Type.IsValueType)
                return false;

            if (method.Parameters.Length == 2)
            {
                var cancellationParameter = method.Parameters[1];
                if (cancellationParameter.RefKind != RefKind.None ||
                    cancellationParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                    "global::System.Threading.CancellationToken")
                    return false;
            }

            if (method.ReturnType is not INamedTypeSymbol returnType ||
                !returnType.IsGenericType ||
                returnType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                "global::LayerBase.Async.LBTask<T>")
                return false;

            var responseType = returnType.TypeArguments[0];
            if (!responseType.IsValueType)
                return false;

            binding = ScopeEndpointBinding.ForCall(
                ownerType,
                ownerKind,
                method.Name,
                SanitizeIdentifier(method.Name + "_" +
                                   method.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line),
                requestParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.Parameters.Length == 2);
            return true;
        }

        expectedSignature = "void Handle(in TEvent value)";
        if (method.IsStatic || method.IsGenericMethod || !method.ReturnsVoid)
            return false;
        if (method.Parameters.Length != 1)
            return false;

        var eventParameter = method.Parameters[0];
        if (eventParameter.RefKind != RefKind.In || !eventParameter.Type.IsValueType)
            return false;

        binding = ScopeEndpointBinding.ForEvent(
            ownerType,
            ownerKind,
            method.Name,
            SanitizeIdentifier(method.Name + "_" +
                               method.Locations.FirstOrDefault()?.GetLineSpan().StartLinePosition.Line),
            eventParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        return true;
    }

    private static string GeneratePartial(INamedTypeSymbol ownerType, IReadOnlyList<ScopeEndpointBinding> bindings)
    {
        if (bindings.Count == 0) return string.Empty;

        var ownerDisplay = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var ownerIdentifier = ownerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var ns = ownerType.ContainingNamespace is { IsGlobalNamespace: false } namespaceSymbol
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// This file was generated by ScopeEndpointAutoBindGenerator.");

        if (!string.IsNullOrEmpty(ns))
        {
            builder.Append("namespace ").Append(ns).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(ownerIdentifier)
               .AppendLine(" : global::LayerBase.Scope.IAutoScopeEndpointBinder");
        builder.AppendLine("{");
        builder.AppendLine(
            "    void global::LayerBase.Scope.IAutoScopeEndpointBinder.AutoBindScopeEndpoints(global::LayerBase.Layers.Layer layer)");
        builder.AppendLine("    {");

        foreach (var binding in bindings)
        {
            if (binding.Kind == ScopeEndpointKind.Call)
            {
                builder.Append("        global::LayerBase.Scope.ScopeCallRegistrationBridge.RegisterForOwner<")
                       .Append(binding.RequestDisplay)
                       .Append(", ")
                       .Append(binding.ResponseDisplay)
                       .Append(">(layer, this, new __GeneratedScopeCallHandler_")
                       .Append(binding.GeneratedIdentifier)
                       .Append("(this));")
                       .AppendLine();
                continue;
            }

            builder.Append("        global::LayerBase.Scope.ScopeEventRegistrationBridge.RegisterForOwner<")
                   .Append(binding.EventDisplay)
                   .Append(">(layer, this, new __GeneratedScopeEventHandler_")
                   .Append(binding.GeneratedIdentifier)
                   .Append("(this));")
                   .AppendLine();
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var binding in bindings)
        {
            if (binding.Kind == ScopeEndpointKind.Call)
            {
                builder.Append("    private sealed class __GeneratedScopeCallHandler_")
                       .Append(binding.GeneratedIdentifier)
                       .Append(" : global::LayerBase.Scope.IScopeCallHandler<")
                       .Append(binding.RequestDisplay)
                       .Append(", ")
                       .Append(binding.ResponseDisplay)
                       .AppendLine(">");
                builder.AppendLine("    {");
                builder.Append("        private readonly ").Append(ownerDisplay).AppendLine(" _owner;");
                builder.AppendLine();
                builder.Append("        public __GeneratedScopeCallHandler_").Append(binding.GeneratedIdentifier)
                       .Append("(")
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
                continue;
            }

            builder.Append("    private sealed class __GeneratedScopeEventHandler_")
                   .Append(binding.GeneratedIdentifier)
                   .Append(" : global::LayerBase.Scope.IScopeEventHandler<")
                   .Append(binding.EventDisplay)
                   .AppendLine(">");
            builder.AppendLine("    {");
            builder.Append("        private readonly ").Append(ownerDisplay).AppendLine(" _owner;");
            builder.AppendLine();
            builder.Append("        public __GeneratedScopeEventHandler_").Append(binding.GeneratedIdentifier)
                   .Append("(")
                   .Append(ownerDisplay).AppendLine(" owner)");
            builder.AppendLine("        {");
            builder.AppendLine("            _owner = owner;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        public void Handle(in ").Append(binding.EventDisplay).AppendLine(" value)");
            builder.AppendLine("        {");
            builder.Append("            _owner.").Append(binding.MethodName).AppendLine("(in value);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(ns))
            builder.AppendLine("}");

        return builder.ToString();
    }

    private static ScopeEndpointOwnerKind GetOwnerKind(
        INamedTypeSymbol ownerType,
        INamedTypeSymbol layerSymbol,
        INamedTypeSymbol? serviceSymbol,
        INamedTypeSymbol? layerContextSymbol)
    {
        if (InheritsFrom(ownerType, layerSymbol))
            return ScopeEndpointOwnerKind.Layer;
        if (serviceSymbol != null && ImplementsInterface(ownerType, serviceSymbol))
            return ScopeEndpointOwnerKind.Service;
        if (layerContextSymbol != null && ImplementsInterface(ownerType, layerContextSymbol))
            return ScopeEndpointOwnerKind.LayerContext;

        return ScopeEndpointOwnerKind.Invalid;
    }

    private static bool InheritsFrom(INamedTypeSymbol ownerType, INamedTypeSymbol baseType)
    {
        for (var current = ownerType; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

        return false;
    }

    private static bool ImplementsInterface(INamedTypeSymbol ownerType, INamedTypeSymbol interfaceType)
    {
        return ownerType.AllInterfaces.Any(candidate =>
            SymbolEqualityComparer.Default.Equals(candidate, interfaceType));
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
                typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;

        return false;
    }

    private static string CreateHintName(INamedTypeSymbol ownerType)
    {
        var display = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{SanitizeIdentifier(display)}.ScopeEndpointAutoBind.g.cs";
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
        private const string Category = "ScopeEndpointAutoBindGenerator";

        public static readonly DiagnosticDescriptor InvalidSignature =
            new(
                "LBG351",
                "Invalid Scope endpoint method signature",
                "Method '{0}' uses a Scope endpoint attribute but must match '{1}'. Owner '{2}' supports Scope endpoints only for strongly typed methods.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor UnsupportedOwner =
            new(
                "LBG352",
                "Unsupported Scope endpoint owner",
                "Type '{0}' uses a Scope endpoint attribute on a method but Scope endpoint methods are only supported on Layer, IService, or ILayerContext types.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerMustBePartial =
            new(
                "LBG353",
                "Scope endpoint owner must be partial",
                "Type '{0}' uses a Scope endpoint attribute and must be declared partial so the source generator can emit endpoint registration.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor OwnerCannotBeAbstract =
            new(
                "LBG354",
                "Scope endpoint owner cannot be abstract",
                "Type '{0}' uses a Scope endpoint attribute and cannot be abstract because generated endpoint registration requires a concrete owner instance.",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor ConflictingEndpointAttributes =
            new(
                "LBG355",
                "Conflicting Scope endpoint attributes",
                "Method '{0}' has multiple Scope endpoint attributes.",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class ScopeEndpointMethodCandidate
    {
        public ScopeEndpointMethodCandidate(IMethodSymbol method, string[] attributeNames)
        {
            Method = method;
            AttributeNames = attributeNames;
        }

        public IMethodSymbol Method { get; }
        public string[] AttributeNames { get; }
    }

    private sealed class ScopeEndpointBinding
    {
        private ScopeEndpointBinding(
            INamedTypeSymbol ownerType,
            ScopeEndpointOwnerKind ownerKind,
            ScopeEndpointKind kind,
            string methodName,
            string generatedIdentifier,
            string? requestDisplay,
            string? responseDisplay,
            string? eventDisplay,
            bool takesCancellationToken)
        {
            OwnerType = ownerType;
            OwnerKind = ownerKind;
            Kind = kind;
            MethodName = methodName;
            GeneratedIdentifier = generatedIdentifier;
            RequestDisplay = requestDisplay;
            ResponseDisplay = responseDisplay;
            EventDisplay = eventDisplay;
            TakesCancellationToken = takesCancellationToken;
        }

        public static ScopeEndpointBinding ForCall(
            INamedTypeSymbol ownerType,
            ScopeEndpointOwnerKind ownerKind,
            string methodName,
            string generatedIdentifier,
            string requestDisplay,
            string responseDisplay,
            bool takesCancellationToken)
        {
            return new ScopeEndpointBinding(
                ownerType,
                ownerKind,
                ScopeEndpointKind.Call,
                methodName,
                generatedIdentifier,
                requestDisplay,
                responseDisplay,
                null,
                takesCancellationToken);
        }

        public static ScopeEndpointBinding ForEvent(
            INamedTypeSymbol ownerType,
            ScopeEndpointOwnerKind ownerKind,
            string methodName,
            string generatedIdentifier,
            string eventDisplay)
        {
            return new ScopeEndpointBinding(
                ownerType,
                ownerKind,
                ScopeEndpointKind.Event,
                methodName,
                generatedIdentifier,
                null,
                null,
                eventDisplay,
                false);
        }

        public INamedTypeSymbol OwnerType { get; }
        public ScopeEndpointOwnerKind OwnerKind { get; }
        public ScopeEndpointKind Kind { get; }
        public string MethodName { get; }
        public string GeneratedIdentifier { get; }
        public string? RequestDisplay { get; }
        public string? ResponseDisplay { get; }
        public string? EventDisplay { get; }
        public bool TakesCancellationToken { get; }
    }

    private enum ScopeEndpointKind
    {
        Call,
        Event
    }

    private enum ScopeEndpointOwnerKind
    {
        Invalid = 0,
        Layer = 1,
        Service = 2,
        LayerContext = 3
    }
}
