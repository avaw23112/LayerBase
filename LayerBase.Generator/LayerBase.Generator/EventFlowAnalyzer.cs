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
        "事件 '{0}' 被订阅者: {1}",
        "Design",
        DiagnosticSeverity.Info,
        true,
        "显示此事件分发时的所有潜在订阅者。");

    private static readonly DiagnosticDescriptor DeprecatedRule = new(
        DeprecatedId,
        "Deprecated Propagation Semantics",
        "事件语义 '{0}' 已被删除。原 Bubble 场景请改用 Layer [Call] 或显式 Send，原 Drop 场景请改用显式 Send/Post 或 Delay。",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "硬删除 SendDrop 和 Bubble 语义以精简热路径。");

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

        // 获取事件类型 T (泛型参数)
        var eventType = methodSymbol.TypeArguments.FirstOrDefault();
        if (eventType == null)
        {
            // 处理非泛型调用，尝试从参数推导
            var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (firstArg != null)
            {
                var argType = context.SemanticModel.GetTypeInfo(firstArg.Expression).Type;
                if (argType != null) eventType = argType;
            }
        }

        if (eventType == null) return;

        // 🚀 核心：查找订阅者
        var subscribers = FindSubscribers(context.Compilation, eventType);
        if (subscribers.Count == 0) return;

        var subscriberNames = string.Join(" | ", subscribers.Select(s => s.DisplayName));

        // 生成诊断，并将目标位置关联
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
        var targetEventName = eventType.ToDisplayString();

        // 性能优化：不再遍历所有 TypeSymbol，而是先查语法树中有 [Subscribe] 的文件
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);

            // 寻找包含特性的方法
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in methods)
            {
                if (m.AttributeLists.Count == 0) continue;

                var methodSymbol = semanticModel.GetDeclaredSymbol(m);
                if (methodSymbol == null) continue;

                if (methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Subscribe") == true))
                {
                    var param = methodSymbol.Parameters.FirstOrDefault();
                    if (param != null && SymbolEqualityComparer.Default.Equals(param.Type, eventType))
                        result.Add(new SubscriberInfo(
                            $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
                            methodSymbol.Locations.FirstOrDefault()));
                }
            }

            // 同时兼容字段订阅 (Struct Handler)
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
                        // 检查是否是 IStructHandler<T>
                        var handlerInterface =
                            fieldSymbol.Type.AllInterfaces.FirstOrDefault(i =>
                                i.Name == "IStructHandler" && i.IsGenericType);
                        if (handlerInterface != null &&
                            SymbolEqualityComparer.Default.Equals(handlerInterface.TypeArguments[0], eventType))
                            result.Add(new SubscriberInfo(
                                $"[Struct] {fieldSymbol.ContainingType.Name}.{fieldSymbol.Name}",
                                fieldSymbol.Locations.FirstOrDefault()));
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