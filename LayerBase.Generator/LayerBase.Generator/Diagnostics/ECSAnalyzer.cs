using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator.Diagnostics;

/// <summary>
/// ECS Query/Bring 相关分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ECSAnalyzer : DiagnosticAnalyzer
{
    private const string QueryAttributeName = "LayerBase.ECS.QueryAttribute";
    private const string BringAttributeName = "LayerBase.ECS.BringAttribute";
    private const string EntryPointAttributeName = "LayerBase.ECS.EntryPointAttribute";
    private const string IComponentName = "LayerBase.Core.IComponent";
    private const string IActorEventName = "LayerBase.Core.IActorEvent";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ECS001_QueryMethodTypeMustBePartial,
            DiagnosticDescriptors.ECS003_QueryMethodCannotBeGeneric,
            DiagnosticDescriptors.ECS004_QueryWithoutBringMustReturnVoid,
            DiagnosticDescriptors.ECS005_QueryWithBringMustReturnProjectResult,
            DiagnosticDescriptors.ECS006_BringMustDeclareEventType,
            DiagnosticDescriptors.ECS008_BringEventParamsAtEnd,
            DiagnosticDescriptors.ECS009_BringEventParamMustBeRef,
            DiagnosticDescriptors.ECS010_ComponentParamMustBeRefOrIn,
            DiagnosticDescriptors.ECS011_EntityParamAtMostOnce,
            DiagnosticDescriptors.ECS013_ComponentMustImplementIComponent,
            DiagnosticDescriptors.ECS014_BringEventMustImplementIActorEvent,
            DiagnosticDescriptors.ECS020_QueryMethodMustStartWithOn,
            DiagnosticDescriptors.ECS024_EntryPointNameInvalid);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // 检查是否有 [Query] 属性
        var queryAttribute = methodSymbol.GetAttributes()
                                         .FirstOrDefault(a => a.AttributeClass?.MetadataName == "QueryAttribute");

        if (queryAttribute == null)
        {
            return;
        }

        // LB-ECS001: 包含 [Query] 方法的类型必须是 partial
        if (methodSymbol.ContainingType is INamedTypeSymbol containingType)
        {
            var classDecl = methodSymbol.DeclaringSyntaxReferences
                                        .FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;

            if (classDecl != null && !classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS001_QueryMethodTypeMustBePartial,
                    methodSymbol.Locations.FirstOrDefault(),
                    containingType.Name));
            }
        }

        // LB-ECS003: [Query] 方法不能是泛型
        if (methodSymbol.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS003_QueryMethodCannotBeGeneric,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        // 检查 [Bring] 属性
        var bringAttribute = methodSymbol.GetAttributes()
                                         .FirstOrDefault(a => a.AttributeClass?.MetadataName == "BringAttribute"
                                                              || a.AttributeClass?.MetadataName.StartsWith(
                                                                  "BringAttribute`") == true);

        bool hasBring = bringAttribute != null;

        // LB-ECS004: 无 [Bring] 的 [Query] 方法必须返回 void
        if (!hasBring && !methodSymbol.ReturnsVoid)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS004_QueryWithoutBringMustReturnVoid,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        // LB-ECS005: 有 [Bring] 的 [Query] 方法必须返回 ProjectResult
        if (hasBring)
        {
            bool returnsProjectResult = methodSymbol.ReturnType?.MetadataName == "ProjectResult"
                                        && methodSymbol.ReturnType?.ContainingNamespace?.ToDisplayString() ==
                                        "LayerBase.ECS";

            if (!returnsProjectResult)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS005_QueryWithBringMustReturnProjectResult,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name));
            }

            // LB-ECS006: [Bring] 必须声明至少一个事件类型
            if (bringAttribute!.ConstructorArguments.Length == 0
                && bringAttribute.AttributeClass?.TypeArguments.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS006_BringMustDeclareEventType,
                    bringAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
            }
        }

        // 分析方法参数
        AnalyzeParameters(context, methodSymbol, hasBring);

        // LB-ECS020: [Query] 方法必须以 On 开头或指定 [EntryPoint]
        var entryPointAttribute = methodSymbol.GetAttributes()
                                              .FirstOrDefault(a =>
                                                  a.AttributeClass?.MetadataName == "EntryPointAttribute");

        if (entryPointAttribute == null && !methodSymbol.Name.StartsWith("On"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS020_QueryMethodMustStartWithOn,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        // LB-ECS024: [EntryPoint] 名称必须是有效的 C# 方法名
        if (entryPointAttribute != null)
        {
            string? name = entryPointAttribute.ConstructorArguments[0].Value as string;
            if (string.IsNullOrWhiteSpace(name) || !IsValidIdentifier(name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS024_EntryPointNameInvalid,
                    entryPointAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    name ?? ""));
            }
        }
    }

    private static void AnalyzeParameters(
        SymbolAnalysisContext context,
        IMethodSymbol         methodSymbol,
        bool                  hasBring)
    {
        var parameters = methodSymbol.Parameters;
        int entityCount = 0;
        bool foundNonEntity = false;
        bool foundBringEvent = false;

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            bool isEntity = param.Type.MetadataName == "Entity"
                            && param.Type.ContainingNamespace?.ToDisplayString() == "Arch.Core";

            if (isEntity)
            {
                entityCount++;
                if (entityCount > 1)
                {
                    // LB-ECS011: Entity 参数最多出现一次
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS011_EntityParamAtMostOnce,
                        param.Locations.FirstOrDefault()));
                }

                continue;
            }

            foundNonEntity = true;

            // 检查是否是 Bring 事件参数
            if (hasBring && IsBringEventParam(param, methodSymbol))
            {
                foundBringEvent = true;

                // LB-ECS009: Bring 事件参数必须是 ref
                if (param.RefKind != RefKind.Ref)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS009_BringEventParamMustBeRef,
                        param.Locations.FirstOrDefault(),
                        param.Name));
                }
            }
            else
            {
                // 组件参数
                // LB-ECS010: 组件参数必须是 ref 或 in
                if (param.RefKind != RefKind.Ref && param.RefKind != RefKind.In)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS010_ComponentParamMustBeRefOrIn,
                        param.Locations.FirstOrDefault(),
                        param.Name));
                }

                // 如果已经遇到 Bring 事件参数，但当前是组件参数，说明顺序错误
                if (foundBringEvent)
                {
                    // LB-ECS008: Bring 事件参数必须在末尾
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS008_BringEventParamsAtEnd,
                        param.Locations.FirstOrDefault()));
                }

                // 检查组件类型是否实现 IComponent
                if (!ImplementsInterface(param.Type, IComponentName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS013_ComponentMustImplementIComponent,
                        param.Locations.FirstOrDefault(),
                        param.Type.Name));
                }
            }
        }
    }

    private static bool IsBringEventParam(IParameterSymbol param, IMethodSymbol method)
    {
        // 简化检查：如果参数类型实现 IActorEvent，则认为是 Bring 事件参数
        return ImplementsInterface(param.Type, IActorEventName);
    }

    private static bool ImplementsInterface(ITypeSymbol type, string interfaceName)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName);
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
            {
                return false;
            }
        }

        return true;
    }
}