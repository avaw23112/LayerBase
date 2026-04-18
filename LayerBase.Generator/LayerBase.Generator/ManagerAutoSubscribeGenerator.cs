using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class ManagerAutoSubscribeGenerator : IIncrementalGenerator
    {
        private const string SubscribeAttr = "LayerBase.Core.Event.SubscribeAttribute";
        private const string SubscribeAsyncAttr = "LayerBase.Core.Event.SubscribeAsyncAttribute";
        private const string SubscribeParallelAttr = "LayerBase.Core.Event.SubscribeParallelAttribute";
        private const string SubscribeDelayAttr = "LayerBase.Core.Event.SubscribeDelayAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax,
                    transform: static (ctx, _) => GetClassWithAttributes(ctx))
                .Where(static c => c is not null);

            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) =>
            {
                var compilation = source.Left;
                var classes = source.Right;

                foreach (var classSymbol in classes.OfType<INamedTypeSymbol>())
                {
                    GenerateAutoSubscribePartial(spc, compilation, classSymbol);
                }
            });
        }

        private static INamedTypeSymbol? GetClassWithAttributes(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            foreach (var member in classDeclaration.Members)
            {
                if (member.AttributeLists.Count > 0)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
                    return symbol as INamedTypeSymbol;
                }
            }
            return null;
        }

        private static void GenerateAutoSubscribePartial(SourceProductionContext spc, Compilation compilation, INamedTypeSymbol classSymbol)
        {
            var methods = classSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
            var properties = classSymbol.GetMembers().OfType<IPropertySymbol>().ToList();
            var fields = classSymbol.GetMembers().OfType<IFieldSymbol>().ToList();

            var bindings = new List<string>();
            bool hasAny = false;

            foreach (var method in methods)
            {
                foreach (var attr in method.GetAttributes())
                {
                    string? attrName = attr.AttributeClass?.ToDisplayString();
                    if (attrName == SubscribeAttr)
                    {
                        if (ValidateSyncMethod(spc, method))
                        {
                            var eventType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            bindings.Add($"layer.Subscribe<{eventType}>({method.Name});");
                            hasAny = true;
                        }
                    }
                    else if (attrName == SubscribeAsyncAttr)
                    {
                        if (ValidateAsyncMethod(spc, method))
                        {
                            var eventType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            bindings.Add($"layer.SubscribeAsync<{eventType}>({method.Name});");
                            hasAny = true;
                        }
                    }
                    else if (attrName == SubscribeParallelAttr)
                    {
                        if (ValidateSyncMethod(spc, method))
                        {
                            var eventType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            bindings.Add($"layer.SubscribeParallel<{eventType}>({method.Name});");
                            hasAny = true;
                        }
                    }
                }
            }

            foreach (var prop in properties)
            {
                if (prop.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == SubscribeDelayAttr))
                {
                    // Assume property is IDelayPublisher<T>
                    var type = prop.Type as INamedTypeSymbol;
                    if (type != null && type.IsGenericType && type.TypeArguments.Length == 1)
                    {
                        var eventType = type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        bindings.Add($"this.{prop.Name} = layer.SubscribeDelay<{eventType}>();");
                        hasAny = true;
                    }
                }
            }

            if (!hasAny) return;

            // Generate the partial class
            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {classSymbol.ContainingNamespace.ToDisplayString()}\n{{";
            var endNs = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}";
            
            var code = $@"// <auto-generated />
using LayerBase.Layers;
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;

{ns}
    partial class {classSymbol.Name} : IInternalLayerContext, IAutoSubscribe
    {{
        private int __routeIndex = -1;
        int IInternalLayerContext.LayerIndex {{ get => __routeIndex; set => __routeIndex = value; }}

        void IAutoSubscribe.AutoBind(Layer layer)
        {{
            {string.Join("\n            ", bindings)}
        }}
    }}
{endNs}";

            spc.AddSource($"{classSymbol.ToDisplayString().Replace(".", "_")}.AutoSubscribe.g.cs", SourceText.From(code, Encoding.UTF8));
        }

        private static bool ValidateSyncMethod(SourceProductionContext spc, IMethodSymbol method)
        {
            if (method.Parameters.Length != 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidParameterCount, method.Locations[0], method.Name));
                return false;
            }
            if (method.Parameters[0].RefKind != RefKind.In)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingInModifier, method.Parameters[0].Locations[0], method.Parameters[0].Name));
                return false;
            }
            // Check return type for EventHandledState
            if (method.ReturnType.Name != "EventHandledState" && method.ReturnType.SpecialType != SpecialType.System_Void)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidReturnTypeSync, method.Locations[0], method.Name));
                return false;
            }
            return true;
        }

        private static bool ValidateAsyncMethod(SourceProductionContext spc, IMethodSymbol method)
        {
            if (method.Parameters.Length != 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidParameterCount, method.Locations[0], method.Name));
                return false;
            }
            var returnType = method.ReturnType.ToDisplayString();
            if (!returnType.Contains("LBTask"))
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidReturnTypeAsync, method.Locations[0], method.Name));
                return false;
            }
            return true;
        }

        private static class Diagnostics
        {
            private const string Category = "ManagerAutoSubscribeGenerator";

            public static readonly DiagnosticDescriptor InvalidParameterCount = new DiagnosticDescriptor(
                "LBG201", "Invalid parameter count", "Event handler '{0}' must have exactly one parameter.", Category, DiagnosticSeverity.Error, true);

            public static readonly DiagnosticDescriptor MissingInModifier = new DiagnosticDescriptor(
                "LBG202", "Missing 'in' modifier", "Parameter '{0}' must have the 'in' modifier for synchronous event handlers.", Category, DiagnosticSeverity.Error, true);

            public static readonly DiagnosticDescriptor InvalidReturnTypeSync = new DiagnosticDescriptor(
                "LBG203", "Invalid return type", "Synchronous event handler '{0}' must return void or EventHandledState.", Category, DiagnosticSeverity.Error, true);

            public static readonly DiagnosticDescriptor InvalidReturnTypeAsync = new DiagnosticDescriptor(
                "LBG204", "Invalid return type", "Async event handler '{0}' must return LBTask.", Category, DiagnosticSeverity.Error, true);
        }
    }
}
