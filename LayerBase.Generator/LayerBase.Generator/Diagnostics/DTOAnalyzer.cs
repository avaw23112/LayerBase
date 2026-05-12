using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator.Diagnostics;

/// <summary>
/// DTO 类型分析器。
/// 检查 DTO 标记接口的正确使用。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DTOAnalyzer : DiagnosticAnalyzer
{
    private const string IComponentName = "LayerBase.Core.IComponent";
    private const string IActorEventName = "LayerBase.Core.IActorEvent";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.DTO006_CannotImplementBothComponentAndEvent);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        // LB-DTO006: 类型不能同时实现 IComponent 和 IActorEvent
        bool implementsIComponent = typeSymbol.AllInterfaces
            .Any(i => i.MetadataName == "IComponent");
        bool implementsIActorEvent = typeSymbol.AllInterfaces
            .Any(i => i.MetadataName == "IActorEvent");

        if (implementsIComponent && implementsIActorEvent)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DTO006_CannotImplementBothComponentAndEvent,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.Name));
        }
    }
}
