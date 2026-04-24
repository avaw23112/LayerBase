using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LayerBase.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class ManagerAutoSubscribeGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InvalidSubscribeSignature = new(
        "LBGS001",
        "Invalid subscribe member signature",
        "Member '{0}' uses [{1}] but must match '{2}'",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Subscribe-family attributes require delegate-compatible method signatures.");

    private static readonly DiagnosticDescriptor ClassMustBePartial = new(
        "LBGS002",
        "Class must be partial",
        "Class '{0}' uses [Subscribe] attributes and must be declared as partial to allow source generation.",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProvider = context.SyntaxProvider.CreateSyntaxProvider(
                                       static (node, _) => node is ClassDeclarationSyntax,
                                       static (ctx, _) => GetClassMeta(ctx))
                                   .Where(static m => m != null)!;

        context.RegisterSourceOutput(classProvider, static (spc, meta) => GenerateCode(spc, meta!));
    }

    private static ClassMeta? GetClassMeta(GeneratorSyntaxContext ctx)
    {
        var cds = (ClassDeclarationSyntax)ctx.Node;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(cds);
        if (symbol == null) return null;

        var handlers = new List<HandlerInfo>();
        var diagnostics = new List<HandlerDiagnostic>();
        var delayProps = new List<string>();

        foreach (var member in symbol.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                foreach (var attr in method.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.Name ?? "";
                    if (attrName.StartsWith("Subscribe"))
                    {
                        if (!TryValidateHandlerSignature(method, attrName, out var expectedSignature))
                        {
                            diagnostics.Add(new HandlerDiagnostic(
                                method.Name,
                                attrName,
                                expectedSignature,
                                method.Locations.FirstOrDefault()));
                            continue;
                        }

                        var evtParam = method.Parameters[0];
                        var evtStr = evtParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        var producedEvts = new List<string>();
                        if (!attrName.Contains("Async") && !attrName.Contains("Delay"))
                            ScanBody(ctx.SemanticModel, method, producedEvts);
                        handlers.Add(new HandlerInfo(method.Name, attrName, evtStr, producedEvts));
                    }
                }
            }
            else if (member is IPropertySymbol prop)
            {
                if (prop.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("SubscribeDelay") == true))
                {
                    if (!TryValidateDelayTarget(prop.Type, out var eventType, out var expectedSignature))
                    {
                        diagnostics.Add(new HandlerDiagnostic(
                            prop.Name,
                            "SubscribeDelayAttribute",
                            expectedSignature,
                            prop.Locations.FirstOrDefault()));
                    }
                    else
                    {
                        delayProps.Add($"{prop.Name}|{eventType}");
                    }
                }
            }
            else if (member is IFieldSymbol field)
            {
                if (field.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("SubscribeDelay") == true))
                {
                    if (!TryValidateDelayTarget(field.Type, out var eventType, out var expectedSignature))
                    {
                        diagnostics.Add(new HandlerDiagnostic(
                            field.Name,
                            "SubscribeDelayAttribute",
                            expectedSignature,
                            field.Locations.FirstOrDefault()));
                    }
                    else
                    {
                        delayProps.Add($"{field.Name}|{eventType}");
                    }
                }
            }
        }

        if (handlers.Count == 0 && delayProps.Count == 0 && diagnostics.Count == 0)
            return null;

        if (!cds.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            diagnostics.Add(new HandlerDiagnostic(
                symbol.Name,
                "PartialCheck",
                "partial class",
                cds.Identifier.GetLocation()));
        }

        var implementsCtx = symbol.AllInterfaces.Any(i => i.Name.Contains("ILayerContext"));

        return new ClassMeta(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol.ToDisplayString(),
            implementsCtx, handlers, delayProps, diagnostics);
    }

    private static void GenerateCode(SourceProductionContext spc, ClassMeta meta)
    {
        foreach (var diagnostic in meta.Diagnostics)
        {
            if (diagnostic.AttributeName == "PartialCheck")
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    ClassMustBePartial,
                    diagnostic.Location,
                    diagnostic.MethodName));
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    InvalidSubscribeSignature,
                    diagnostic.Location,
                    diagnostic.MethodName,
                    diagnostic.AttributeName,
                    diagnostic.ExpectedSignature));
            }
        }

        if (meta.Diagnostics.Any(d => d.AttributeName == "PartialCheck")) return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        if (meta.Namespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {meta.Namespace}");
            sb.AppendLine("{");
        }

        var interfaces = new List<string>();
        if (meta.ImplementsLayerContext) interfaces.Add("global::LayerBase.DI.IInternalLayerContext");
        if (meta.Handlers.Count > 0 || meta.DelayProps.Count > 0) interfaces.Add("global::LayerBase.DI.IAutoSubscribe");
        var interfaceDecl = interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : "";

        sb.AppendLine($"    partial class {meta.ClassName}{interfaceDecl}");
        sb.AppendLine("    {");

        if (meta.ImplementsLayerContext)
        {
            sb.AppendLine("        private int __routeIndex = -1;");
            sb.AppendLine(
                "        int global::LayerBase.DI.IInternalLayerContext.LayerIndex { get => __routeIndex; set => __routeIndex = value; }");
        }

        if (meta.Handlers.Count > 0 || meta.DelayProps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(
                "        void global::LayerBase.DI.IAutoSubscribe.AutoBind(global::LayerBase.Layers.Layer layer)");
            sb.AppendLine("        {");
            foreach (var h in meta.Handlers)
            {
                var reg = h.Attr.Contains("Async") ? "SubscribeAsync" :
                    h.Attr.Contains("Parallel")    ? "SubscribeParallel" : 
                    h.Attr.Contains("Notify")      ? "SubscribeNotify" : "Subscribe";

                sb.AppendLine($"            layer.{reg}<{h.Evt}>(this.{h.Name});");
                sb.AppendLine($"            layer.SubscribedEvents.Add(typeof({h.Evt}));");
                
                foreach (var produced in h.ProducedEvts)
                {
                    sb.AppendLine($"            layer.ProducedEvents.Add(typeof({produced}));");
                }
            }

            foreach (var p in meta.DelayProps)
            {
                var parts = p.Split('|');
                sb.AppendLine($"            this.{parts[0]} = layer.SubscribeDelay<{parts[1]}>();");
                sb.AppendLine($"            layer.SubscribedEvents.Add(typeof({parts[1]}));");
            }

            sb.AppendLine("        }");

            sb.AppendLine();
            sb.AppendLine(
                "        global::System.Collections.Generic.IEnumerable<global::LayerBase.DI.EventDependency> global::LayerBase.DI.IAutoSubscribe.GetEventDependencies()");
            sb.AppendLine("        {");
            foreach (var h in meta.Handlers)
            foreach (var produced in h.ProducedEvts)
                sb.AppendLine($"            yield return new global::LayerBase.DI.EventDependency(typeof({h.Evt}), typeof({produced}));");
            sb.AppendLine("            yield break;");
            sb.AppendLine("        }");

            sb.AppendLine();
            sb.AppendLine(
                "        global::System.Collections.Generic.IEnumerable<global::System.Type> global::LayerBase.DI.IAutoSubscribe.GetSubscribedEvents()");
            sb.AppendLine("        {");
            foreach (var h in meta.Handlers) sb.AppendLine($"            yield return typeof({h.Evt});");
            foreach (var p in meta.DelayProps)
            {
                var parts = p.Split('|');
                sb.AppendLine($"            yield return typeof({parts[1]});");
            }

            sb.AppendLine("            yield break;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        if (meta.Namespace != "<global namespace>") sb.AppendLine("}");

        spc.AddSource($"{meta.Display.Replace(".", "_")}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool TryValidateHandlerSignature(IMethodSymbol method, string attrName, out string expectedSignature)
    {
        expectedSignature = GetExpectedSignature(attrName);

        if (method.Parameters.Length != 1) return false;

        if (attrName.Contains("Async"))
        {
            if (method.Parameters[0].RefKind != RefKind.None) return false;
            return IsReturnType(method.ReturnType, "global::LayerBase.Async.LBTask");
        }

        if (method.Parameters[0].RefKind != RefKind.In) return false;

        if (attrName.Contains("Notify")) return method.ReturnsVoid;

        return IsReturnType(method.ReturnType, "global::LayerBase.Core.Event.EventHandledState");
    }

    private static string GetExpectedSignature(string attrName)
    {
        if (attrName.Contains("Async")) return "LBTask Handle(TEvent value)";
        if (attrName.Contains("Notify")) return "void Handle(in TEvent value)";
        return "EventHandledState Handle(in TEvent value)";
    }

    private static bool IsReturnType(ITypeSymbol type, string fullyQualifiedName)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == fullyQualifiedName;
    }

    private static bool TryValidateDelayTarget(ITypeSymbol type, out string eventType, out string expectedSignature)
    {
        expectedSignature = "IDelayPublisher<TEvent>";
        eventType = string.Empty;

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType) return false;

        if (namedType.Name != "IDelayPublisher" || namedType.TypeArguments.Length != 1) return false;

        eventType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return true;
    }

    private static void ScanBody(SemanticModel model, IMethodSymbol handler, List<string> producedEvts)
    {
        var syntax = handler.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
        var body = (SyntaxNode?)syntax?.Body ?? syntax?.ExpressionBody;
        if (body == null) return;
        foreach (var inv in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            if (inv.Expression.ToString().Contains("Send"))
            {
                var info = model.GetSymbolInfo(inv);
                var sym = info.Symbol as IMethodSymbol ??
                          info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                if (sym?.IsGenericMethod == true && sym.Name.StartsWith("Send"))
                {
                    var target = sym.TypeArguments.FirstOrDefault()
                                    ?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (target != null) producedEvts.Add(target);
                }
            }
    }

    private class ClassMeta
    {
        public ClassMeta(string       name, string ns, string display, bool ctx, List<HandlerInfo> handlers,
                         List<string> delayProps, List<HandlerDiagnostic> diagnostics)
        {
            ClassName = name;
            Namespace = ns;
            Display = display;
            ImplementsLayerContext = ctx;
            Handlers = handlers;
            DelayProps = delayProps;
            Diagnostics = diagnostics;
        }

        public string ClassName { get; }
        public string Namespace { get; }
        public string Display { get; }
        public bool ImplementsLayerContext { get; }
        public List<HandlerInfo> Handlers { get; }
        public List<string> DelayProps { get; }
        public List<HandlerDiagnostic> Diagnostics { get; }
    }

    private class HandlerInfo
    {
        public HandlerInfo(string n, string a, string e, List<string> producedEvts)
        {
            Name = n;
            Attr = a;
            Evt = e;
            ProducedEvts = producedEvts;
        }

        public string Name { get; }
        public string Attr { get; }
        public string Evt { get; }
        public List<string> ProducedEvts { get; }
    }

    private class HandlerDiagnostic
    {
        public HandlerDiagnostic(string methodName, string attributeName, string expectedSignature, Location? location)
        {
            MethodName = methodName;
            AttributeName = attributeName;
            ExpectedSignature = expectedSignature;
            Location = location;
        }

        public string MethodName { get; }
        public string AttributeName { get; }
        public string ExpectedSignature { get; }
        public Location? Location { get; }
    }
}
