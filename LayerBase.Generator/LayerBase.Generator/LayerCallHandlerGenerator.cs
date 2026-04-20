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
public sealed class LayerCallHandlerGenerator : IIncrementalGenerator
{
    private const string OwnerLayerAttributeName = "LayerBase.Layers.OwnerLayerAttribute";
    private const string LayerMetadataName = "LayerBase.Layers.Layer";
    private const string CallHandlerMetadataName = "LayerBase.Call.ILayerCallHandler`2";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var registrations = context.SyntaxProvider
                                   .ForAttributeWithMetadataName(
                                       OwnerLayerAttributeName,
                                       static (node, _) => node is ClassDeclarationSyntax,
                                       static (ctx,  _) => CreateRegistrations(ctx))
                                   .SelectMany(static (items, _) => items);

        var compilationAndRegistrations = context.CompilationProvider.Combine(registrations.Collect());

        context.RegisterSourceOutput(compilationAndRegistrations, static (spc, source) =>
        {
            var compilation = source.Left;
            var collected = source.Right;

            var layerSymbol = compilation.GetTypeByMetadataName(LayerMetadataName);
            var callHandlerSymbol = compilation.GetTypeByMetadataName(CallHandlerMetadataName);
            if (layerSymbol == null || callHandlerSymbol == null) return;

            var validBindings = new List<CallHandlerBinding>();
            foreach (var registration in collected)
            {
                var handlerType = registration.HandlerType;
                var targetLayer = registration.LayerType;
                var location = registration.Location ?? handlerType.Locations.FirstOrDefault();

                if (!InheritsFromLayer(targetLayer, layerSymbol))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustInheritLayer,
                        registration.Location ?? targetLayer.Locations.FirstOrDefault(),
                        targetLayer.ToDisplayString()));
                    continue;
                }

                if (!IsPartial(targetLayer))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.LayerMustBePartial,
                        registration.Location ?? targetLayer.Locations.FirstOrDefault(),
                        targetLayer.ToDisplayString()));
                    continue;
                }

                if (handlerType.IsAbstract)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.HandlerCannotBeAbstract, location,
                        handlerType.ToDisplayString()));
                    continue;
                }

                if (!HasAccessibleParameterlessConstructor(handlerType))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.HandlerNeedsPublicParameterlessConstructor,
                        location, handlerType.ToDisplayString()));
                    continue;
                }

                var implementations = GetCallHandlerInterfaces(handlerType, callHandlerSymbol).ToList();
                if (implementations.Count == 0) continue;

                foreach (var impl in implementations)
                    validBindings.Add(new CallHandlerBinding(handlerType, targetLayer, impl.RequestType, impl.ResponseType,
                        location));
            }

            var groupedByLayer = validBindings.GroupBy(r => r.LayerType, SymbolEqualityComparer.Default);
            foreach (var group in groupedByLayer)
            {
                if (group.Key is not INamedTypeSymbol layerKey) continue;

                var sourceText = GenerateLayerPartial(layerKey, group);
                if (string.IsNullOrEmpty(sourceText)) continue;

                spc.AddSource(CreateHintName(layerKey), SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private static ImmutableArray<CallHandlerRegistration> CreateRegistrations(GeneratorAttributeSyntaxContext context)
    {
        var handlerSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var builder = ImmutableArray.CreateBuilder<CallHandlerRegistration>();

        foreach (var attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length != 1) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol layerSymbol) continue;

            var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();
            builder.Add(new CallHandlerRegistration(handlerSymbol, layerSymbol, location));
        }

        return builder.ToImmutable();
    }

    private static IEnumerable<CallHandlerImplementation> GetCallHandlerInterfaces(INamedTypeSymbol handlerType,
                                                                                   INamedTypeSymbol callHandlerSymbol)
    {
        foreach (var iface in handlerType.AllInterfaces.OfType<INamedTypeSymbol>())
        {
            if (!SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, callHandlerSymbol)) continue;
            if (iface.TypeArguments.Length != 2) continue;

            yield return new CallHandlerImplementation(iface.TypeArguments[0], iface.TypeArguments[1]);
        }
    }

    private static bool InheritsFromLayer(INamedTypeSymbol target, INamedTypeSymbol layerSymbol)
    {
        for (var current = target; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, layerSymbol))
                return true;

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
                typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;

        return false;
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length != 0 || ctor.IsStatic) continue;

            if (ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                or Accessibility.ProtectedOrInternal) return true;
        }

        return false;
    }

    private static string GenerateLayerPartial(INamedTypeSymbol layerType, IEnumerable<CallHandlerBinding> bindings)
    {
        var handlers = (bindings ?? Enumerable.Empty<CallHandlerBinding>())
                       .Where(b => b?.HandlerType != null && b.RequestType != null && b.ResponseType != null)
                       .Distinct(CallHandlerBindingComparer.Instance)
                       .OrderBy(b => b.HandlerType.ToDisplayString())
                       .ThenBy(b => b.RequestType?.ToDisplayString())
                       .ThenBy(b => b.ResponseType?.ToDisplayString())
                       .ToList();

        if (handlers.Count == 0) return string.Empty;

        var layerDisplayName = layerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var layerIdentifier = layerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceSymbol = layerType.ContainingNamespace;
        var @namespace = namespaceSymbol is { IsGlobalNamespace: false }
            ? namespaceSymbol.ToDisplayString()
            : null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("// This file was generated by LayerCallHandlerGenerator.");
        builder.AppendLine("using LayerBase.Layers;");

        if (!string.IsNullOrEmpty(@namespace))
        {
            builder.Append("namespace ").Append(@namespace).AppendLine();
            builder.AppendLine("{");
        }

        builder.Append("partial class ").Append(layerIdentifier).AppendLine();
        builder.AppendLine("{");
        builder.Append("    static ").Append(layerType.Name).AppendLine("()");
        builder.AppendLine("    {");
        builder.Append("        LayerServiceRegistry.Register(typeof(").Append(layerDisplayName)
               .AppendLine("), static layerInstance =>");
        builder.AppendLine("        {");
        builder.Append("            var typedLayer = (").Append(layerDisplayName).AppendLine(")layerInstance;");

        foreach (var binding in handlers)
        {
            var handlerDisplay = binding.HandlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var requestDisplay = binding.RequestType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var responseDisplay = binding.ResponseType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            builder.Append("            typedLayer.RegisterCallHandler<").Append(requestDisplay).Append(", ")
                   .Append(responseDisplay).Append(">(new ").Append(handlerDisplay).AppendLine("());");
        }

        builder.AppendLine("        });");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        if (!string.IsNullOrEmpty(@namespace)) builder.AppendLine("}");

        return builder.ToString();
    }

    private static string CreateHintName(INamedTypeSymbol layerType)
    {
        var name = layerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = new StringBuilder(name.Length);
        foreach (var ch in name) sanitized.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return $"{sanitized}.LayerCallHandlers.g.cs";
    }

#pragma warning disable RS2008
    private static class Diagnostics
    {
        private const string Category = "LayerCallHandlerGenerator";

        public static readonly DiagnosticDescriptor LayerMustInheritLayer =
            new(
                "LBG202",
                "OwnerLayer target must derive from Layer",
                "Type '{0}' is not a Layer and cannot be used with OwnerLayerAttribute",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor LayerMustBePartial =
            new(
                "LBG203",
                "Layer must be partial",
                "Layer '{0}' must be declared as partial to allow generator to emit registrations",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor HandlerNeedsPublicParameterlessConstructor =
            new(
                "LBG204",
                "Call handler needs parameterless constructor",
                "Call handler '{0}' must have a public or internal parameterless constructor",
                Category,
                DiagnosticSeverity.Error,
                true);

        public static readonly DiagnosticDescriptor HandlerCannotBeAbstract =
            new(
                "LBG205",
                "Call handler cannot be abstract",
                "Call handler '{0}' cannot be abstract when used with OwnerLayerAttribute",
                Category,
                DiagnosticSeverity.Error,
                true);
    }
#pragma warning restore RS2008

    private sealed class CallHandlerRegistration
    {
        public CallHandlerRegistration(INamedTypeSymbol handlerType, INamedTypeSymbol layerType, Location? location)
        {
            HandlerType = handlerType;
            LayerType = layerType;
            Location = location;
        }

        public INamedTypeSymbol HandlerType { get; }
        public INamedTypeSymbol LayerType { get; }
        public Location? Location { get; }
    }

    private sealed class CallHandlerBinding
    {
        public CallHandlerBinding(INamedTypeSymbol handlerType, INamedTypeSymbol layerType, ITypeSymbol requestType,
                                  ITypeSymbol responseType, Location? location)
        {
            HandlerType = handlerType;
            LayerType = layerType;
            RequestType = requestType;
            ResponseType = responseType;
            Location = location;
        }

        public INamedTypeSymbol HandlerType { get; }
        public INamedTypeSymbol LayerType { get; }
        public ITypeSymbol RequestType { get; }
        public ITypeSymbol ResponseType { get; }
        public Location? Location { get; }
    }

    private readonly record struct CallHandlerImplementation(ITypeSymbol RequestType, ITypeSymbol ResponseType);

    private sealed class CallHandlerBindingComparer : IEqualityComparer<CallHandlerBinding>
    {
        public static readonly CallHandlerBindingComparer Instance = new();

        public bool Equals(CallHandlerBinding? x, CallHandlerBinding? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return SymbolEqualityComparer.Default.Equals(x.HandlerType, y.HandlerType)
                   && SymbolEqualityComparer.Default.Equals(x.RequestType, y.RequestType)
                   && SymbolEqualityComparer.Default.Equals(x.ResponseType, y.ResponseType);
        }

        public int GetHashCode(CallHandlerBinding obj)
        {
            var hash = SymbolEqualityComparer.Default.GetHashCode(obj.HandlerType);
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.RequestType);
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.ResponseType);
            return hash;
        }
    }
}
