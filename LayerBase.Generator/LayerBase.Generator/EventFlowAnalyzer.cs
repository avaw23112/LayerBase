using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventFlowAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "EVT001";
    public const string DeprecatedId = "EVT002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Event Flow Insight",
        "Event '{0}' is subscribed at: {1}",
        "Design",
        DiagnosticSeverity.Info,
        true,
        "Shows all potential subscribers when this event is dispatched.");

    private static readonly DiagnosticDescriptor DeprecatedRule = new(
        DeprecatedId,
        "Deprecated Propagation Semantics",
        "Event semantic '{0}' has been removed. Use Layer [Call] or explicit Send for Bubble scenarios, and explicit Send/Post or Delay for Drop scenarios.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Hard delete SendDrop and Bubble semantics to streamline hot path.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule, DeprecatedRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var methodName = "";
        if (invocation.Expression is MemberAccessExpressionSyntax maes)
            methodName = maes.Name.Identifier.Text;
        else if (invocation.Expression is IdentifierNameSyntax ins)
            methodName = ins.Identifier.Text;

        if (IsDeprecatedMethod(methodName))
        {
            context.ReportDiagnostic(Diagnostic.Create(DeprecatedRule, invocation.GetLocation(), methodName));
            return;
        }

        if (!IsDispatchMethod(methodName)) return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
        if (methodSymbol == null) return;

        var eventType = methodSymbol.TypeArguments.FirstOrDefault();
        if (eventType == null)
        {
            var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (firstArg != null)
            {
                var argType = context.SemanticModel.GetTypeInfo(firstArg.Expression).Type;
                if (argType != null) eventType = argType;
            }
        }
        if (eventType == null) return;

        var subscribers = FindSubscribers(context.Compilation, eventType);
        if (subscribers.Count == 0) return;

        var subscriberNames = string.Join(" | ", subscribers.Select(s => s.DisplayName));
        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            subscribers.Select(s => s.Location).Where(l => l != null && l.IsInSource),
            eventType.Name,
            subscriberNames);

        context.ReportDiagnostic(diagnostic);
    }

    private bool IsDispatchMethod(string name)
    {
        return name == "Send" || name == "Post" || name.Contains("SendGlobal") ||
               name.Contains("SendLocal");
    }

    private bool IsDeprecatedMethod(string name)
    {
        return name.Contains("SendBubble") || name.Contains("SendDrop") ||
               name.Contains("PostBubble") || name.Contains("PostDrop") ||
               name.Contains("DelayBubble") || name.Contains("DelayDrop");
    }

    private List<SubscriberInfo> FindSubscribers(Compilation compilation, ITypeSymbol eventType)
    {
        var result = new List<SubscriberInfo>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in methods)
            {
                if (m.AttributeLists.Count == 0) continue;
                var methodSymbol = semanticModel.GetDeclaredSymbol(m);
                if (methodSymbol == null) continue;
                var attributes = methodSymbol.GetAttributes();
                if (attributes.Any(a => a.AttributeClass?.Name.Contains("Subscribe") == true))
                {
                    var param = methodSymbol.Parameters.FirstOrDefault();
                    if (param != null && SymbolEqualityComparer.Default.Equals(param.Type, eventType))
                    {
                        var tag = attributes.Any(a => a.AttributeClass?.Name.Contains("NotifySafe") == true) ? "[NotifySafe] " : 
                                  attributes.Any(a => a.AttributeClass?.Name.Contains("Notify") == true) ? "[Notify] " : "";
                        result.Add(new SubscriberInfo(
                            $"{tag}{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
                            methodSymbol.Locations.FirstOrDefault()!));
                    }
                }
            }
            var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var f in fields)
            {
                if (f.AttributeLists.Count == 0) continue;
                foreach (var variable in f.Declaration.Variables)
                {
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol == null) continue;
                    if (fieldSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Subscribe") == true))
                    {
                        var handlerInterface = fieldSymbol.Type.AllInterfaces.FirstOrDefault(i =>
                                i.Name == "IStructHandler" && i.IsGenericType);
                        if (handlerInterface != null &&
                            SymbolEqualityComparer.Default.Equals(handlerInterface.TypeArguments[0], eventType))
                            result.Add(new SubscriberInfo(
                                $"[Struct] {fieldSymbol.ContainingType.Name}.{fieldSymbol.Name}",
                                fieldSymbol.Locations.FirstOrDefault()!));
                    }
                }
            }
        }
        return result;
    }

    private struct SubscriberInfo
    {
        public readonly string DisplayName;
        public readonly Location Location;
        public SubscriberInfo(string name, Location loc)
        {
            DisplayName = name;
            Location = loc;
        }
    }
}

