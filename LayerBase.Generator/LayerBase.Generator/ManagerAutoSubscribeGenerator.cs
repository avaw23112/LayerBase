using System;
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
        "Class '{0}' uses Subscribe attributes and must be declared as partial to allow source generation.",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor ConflictingSubscribeAttributes = new(
        "LBGS003",
        "Conflicting subscribe attributes",
        "Member '{0}' has multiple Subscribe attributes. A method can only have one subscription semantic.",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classMetaProvider = context.SyntaxProvider.CreateSyntaxProvider(
                                           static (node, _) => node is ClassDeclarationSyntax,
                                           static (ctx,  _) => GetClassMeta(ctx))
                                       .Where(static m => m != null)!;

        var combined = classMetaProvider.Collect();

        context.RegisterSourceOutput(combined, static (spc, metas) => GenerateMergedCode(spc, metas));
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
            // Only process members declared in THIS syntax node to avoid duplicates when collecting
            if (!member.DeclaringSyntaxReferences.Any(r => r.SyntaxTree == cds.SyntaxTree && cds.Span.Contains(r.Span)))
                continue;

            if (member is IMethodSymbol method)
            {
                var subscribeAttributes = method.GetAttributes()
                                                .Where(a => a.AttributeClass?.Name.StartsWith("Subscribe") == true)
                                                .ToList();

                if (subscribeAttributes.Count > 1)
                {
                    diagnostics.Add(new HandlerDiagnostic(
                        method.Name,
                        "Conflict",
                        string.Empty,
                        method.Locations.FirstOrDefault()));
                    continue;
                }

                foreach (var attr in subscribeAttributes)
                {
                    var attrName = attr.AttributeClass?.Name ?? "";
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
                    if (!attrName.Contains("Async") && !attrName.Contains("Delay") && !attrName.Contains("Parallel"))
                        ScanBody(ctx.SemanticModel, method, producedEvts);
                    handlers.Add(new HandlerInfo(method.Name, attrName, evtStr, producedEvts));
                }
            }
            else if (member is IPropertySymbol prop)
            {
                if (prop.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("SubscribeDelay") == true))
                {
                    if (!TryValidateDelayTarget(prop.Type, out var eventType, out var expectedSignature))
                        diagnostics.Add(new HandlerDiagnostic(
                            prop.Name,
                            "SubscribeDelayAttribute",
                            expectedSignature,
                            prop.Locations.FirstOrDefault()));
                    else
                        delayProps.Add($"{prop.Name}|{eventType}");
                }
            }
            else if (member is IFieldSymbol field)
            {
                if (field.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("SubscribeDelay") == true))
                {
                    if (!TryValidateDelayTarget(field.Type, out var eventType, out var expectedSignature))
                        diagnostics.Add(new HandlerDiagnostic(
                            field.Name,
                            "SubscribeDelayAttribute",
                            expectedSignature,
                            field.Locations.FirstOrDefault()));
                    else
                        delayProps.Add($"{field.Name}|{eventType}");
                }
            }
        }

        var isPartial = cds.Modifiers.Any(SyntaxKind.PartialKeyword);
        var implementsCtx = ImplementsInterfaceNamed(symbol, "ILayerContext");
        var implementsService = ImplementsInterfaceNamed(symbol, "IService");
        var emitsBindingAccessor = implementsCtx || implementsService;

        return new ClassMeta(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol.ToDisplayString(),
            implementsCtx, emitsBindingAccessor, handlers, delayProps, diagnostics, isPartial, cds.Identifier.GetLocation());
    }

    private static void GenerateMergedCode(SourceProductionContext spc, ImmutableArray<ClassMeta> metas)
    {
        var groups = metas.GroupBy(m => m.Display);

        foreach (var group in groups)
        {
            var first = group.First();
            var allHandlers = group.SelectMany(m => m.Handlers).ToList();
            var allDelayProps = group.SelectMany(m => m.DelayProps).Distinct().ToList();
            var allDiagnostics = group.SelectMany(m => m.Diagnostics).ToList();
            var isPartial = group.Any(m => m.IsPartial);

            if (allHandlers.Count == 0 && allDelayProps.Count == 0 && allDiagnostics.Count == 0)
                continue;

            foreach (var diagnostic in allDiagnostics)
            {
                if (diagnostic.AttributeName == "Conflict")
                    spc.ReportDiagnostic(Diagnostic.Create(
                        ConflictingSubscribeAttributes,
                        diagnostic.Location,
                        diagnostic.MethodName));
                else
                    spc.ReportDiagnostic(Diagnostic.Create(
                        InvalidSubscribeSignature,
                        diagnostic.Location,
                        diagnostic.MethodName,
                        diagnostic.AttributeName,
                        diagnostic.ExpectedSignature));
            }

            if (!isPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    ClassMustBePartial,
                    first.Location,
                    first.ClassName));
                continue;
            }

            if (allDiagnostics.Any(d => d.AttributeName == "Conflict")) continue;

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            if (first.Namespace != "<global namespace>")
            {
                sb.AppendLine($"namespace {first.Namespace}");
                sb.AppendLine("{");
            }

            var interfaces = new List<string>();
            if (first.ImplementsLayerContext) interfaces.Add("global::LayerBase.DI.IInternalLayerContext");
            if (first.EmitsBindingAccessor) interfaces.Add("global::LayerBase.DI.ILayerBindingAccessor");
            if (allHandlers.Count > 0 || allDelayProps.Count > 0) interfaces.Add("global::LayerBase.DI.IAutoSubscribe");
            var interfaceDecl = interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : "";

            sb.AppendLine($"    partial class {first.ClassName}{interfaceDecl}");
            sb.AppendLine("    {");

            if (first.ImplementsLayerContext)
            {
                sb.AppendLine("        private int __routeIndex = -1;");
                sb.AppendLine(
                    "        int global::LayerBase.DI.IInternalLayerContext.LayerIndex { get => __routeIndex; set => __routeIndex = value; }");
            }

            if (first.EmitsBindingAccessor)
            {
                if (first.ImplementsLayerContext)
                    sb.AppendLine();
                sb.AppendLine("        private object? __layerBaseBinding;");
                sb.AppendLine(
                    "        object? global::LayerBase.DI.ILayerBindingAccessor.__LayerBaseBinding { get => __layerBaseBinding; set => __layerBaseBinding = value; }");
            }

            if (allHandlers.Count > 0 || allDelayProps.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(
                    "        void global::LayerBase.DI.IAutoSubscribe.AutoBind(global::LayerBase.Layers.Layer layer)");
                sb.AppendLine("        {");
                foreach (var h in allHandlers)
                {
                    var reg = h.Attr.Contains("Async") ? "SubscribeAsync" :
                        h.Attr.Contains("Parallel")    ? "SubscribeParallel" :
                        h.Attr.Contains("Notify")      ? "SubscribeNotify" : 
                        h.Attr == "SubscribeFlow" || h.Attr == "SubscribeFlowAttribute" ? "SubscribeFlow" : "Subscribe";

                    sb.AppendLine($"            layer.{reg}<{h.Evt}>(this.{h.Name});");
                    sb.AppendLine($"            layer.RecordSubscribedEvent(typeof({h.Evt}));");

                    foreach (var produced in h.ProducedEvts)
                        sb.AppendLine($"            layer.RecordProducedEvent(typeof({produced}));");
                }

                foreach (var p in allDelayProps)
                {
                    var parts = p.Split('|');
                    sb.AppendLine($"            this.{parts[0]} = layer.SubscribeDelay<{parts[1]}>();");
                    sb.AppendLine($"            layer.RecordSubscribedEvent(typeof({parts[1]}));");
                }

                sb.AppendLine("        }");

                sb.AppendLine();
                sb.AppendLine(
                    "        global::System.Collections.Generic.IEnumerable<global::LayerBase.DI.EventDependency> global::LayerBase.DI.IAutoSubscribe.GetEventDependencies()");
                sb.AppendLine("        {");
                foreach (var h in allHandlers)
                foreach (var produced in h.ProducedEvts)
                    sb.AppendLine(
                        $"            yield return new global::LayerBase.DI.EventDependency(typeof({h.Evt}), typeof({produced}));");
                sb.AppendLine("            yield break;");
                sb.AppendLine("        }");

                sb.AppendLine();
                sb.AppendLine(
                    "        global::System.Collections.Generic.IEnumerable<global::System.Type> global::LayerBase.DI.IAutoSubscribe.GetSubscribedEvents()");
                sb.AppendLine("        {");
                foreach (var h in allHandlers) sb.AppendLine($"            yield return typeof({h.Evt});");
                foreach (var p in allDelayProps)
                {
                    var parts = p.Split('|');
                    sb.AppendLine($"            yield return typeof({parts[1]});");
                }

                sb.AppendLine("            yield break;");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            if (first.Namespace != "<global namespace>") sb.AppendLine("}");

            var hintName = first.Display.Replace(".", "_").Replace("::", "_").Replace("<", "_").Replace(">", "_");
            spc.AddSource($"{hintName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }

    private static bool ImplementsInterfaceNamed(INamedTypeSymbol symbol, string interfaceName)
    {
        return symbol.AllInterfaces.Any(i => i.Name == interfaceName);
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

        if (attrName == "Subscribe" || attrName == "SubscribeAttribute" || 
            attrName.Contains("Notify") || attrName.Contains("Parallel")) 
            return method.ReturnsVoid;

        return IsReturnType(method.ReturnType, "global::LayerBase.Core.Event.EventHandledState");
    }

    private static string GetExpectedSignature(string attrName)
    {
        if (attrName.Contains("Async")) return "LBTask Handle(TEvent value)";
        if (attrName == "Subscribe" || attrName == "SubscribeAttribute" || 
            attrName.Contains("Notify") || attrName.Contains("Parallel")) 
            return "void Handle(in TEvent value)";
        
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
        public ClassMeta(string       name,       string ns, string display, bool ctx, bool bindingAccessor, List<HandlerInfo> handlers,
                         List<string> delayProps, List<HandlerDiagnostic> diagnostics, bool isPartial, Location? location)
        {
            ClassName = name;
            Namespace = ns;
            Display = display;
            ImplementsLayerContext = ctx;
            EmitsBindingAccessor = bindingAccessor;
            Handlers = handlers;
            DelayProps = delayProps;
            Diagnostics = diagnostics;
            IsPartial = isPartial;
            Location = location;
        }

        public string ClassName { get; }
        public string Namespace { get; }
        public string Display { get; }
        public bool ImplementsLayerContext { get; }
        public bool EmitsBindingAccessor { get; }
        public List<HandlerInfo> Handlers { get; }
        public List<string> DelayProps { get; }
        public List<HandlerDiagnostic> Diagnostics { get; }
        public bool IsPartial { get; }
        public Location? Location { get; }
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
