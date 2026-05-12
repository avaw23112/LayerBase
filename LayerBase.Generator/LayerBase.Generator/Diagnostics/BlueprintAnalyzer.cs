using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator.Diagnostics;

/// <summary>
/// Bundle/Blueprint 相关分析器。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlueprintAnalyzer : DiagnosticAnalyzer
{
    private const string LayerBundleAttributeName = "LayerBase.ECS.LayerBundleAttribute";
    private const string LayerBlueprintAttributeName = "LayerBase.ECS.LayerBlueprintAttribute";
    private const string IBundleName = "LayerBase.ECS.IBundle";
    private const string IEntityBlueprintName = "LayerBase.ECS.IEntityBlueprint";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.BP001_BundleMustBeClass,
            DiagnosticDescriptors.BP002_BundleMustImplementIBundle,
            DiagnosticDescriptors.BP003_BundleMustHaveParameterlessConstructor,
            DiagnosticDescriptors.BP004_BlueprintMustBeClass,
            DiagnosticDescriptors.BP005_BlueprintMustImplementIEntityBlueprint);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedType)
        {
            return;
        }

        // 检查 [LayerBundle] 属性
        var bundleAttribute = namedType.GetAttributes()
                                       .FirstOrDefault(a => a.AttributeClass?.MetadataName == "LayerBundleAttribute");

        if (bundleAttribute != null)
        {
            AnalyzeBundle(context, namedType, bundleAttribute);
        }

        // 检查 [LayerBlueprint] 属性
        var blueprintAttribute = namedType.GetAttributes()
                                          .FirstOrDefault(a =>
                                              a.AttributeClass?.MetadataName == "LayerBlueprintAttribute");

        if (blueprintAttribute != null)
        {
            AnalyzeBlueprint(context, namedType, blueprintAttribute);
        }
    }

    private static void AnalyzeBundle(
        SymbolAnalysisContext context,
        INamedTypeSymbol      namedType,
        AttributeData         bundleAttribute)
    {
        // LB-BP001: [LayerBundle] 类型必须是 class
        if (namedType.TypeKind != TypeKind.Class)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.BP001_BundleMustBeClass,
                bundleAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                namedType.Name));
            return;
        }

        // LB-BP002: [LayerBundle] 类型必须实现 IBundle
        if (!namedType.AllInterfaces.Any(i => i.ToDisplayString() == IBundleName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.BP002_BundleMustImplementIBundle,
                bundleAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                namedType.Name));
        }

        // LB-BP003: [LayerBundle] 类型必须有 public 无参构造函数
        if (!HasPublicParameterlessConstructor(namedType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.BP003_BundleMustHaveParameterlessConstructor,
                bundleAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                namedType.Name));
        }
    }

    private static void AnalyzeBlueprint(
        SymbolAnalysisContext context,
        INamedTypeSymbol      namedType,
        AttributeData         blueprintAttribute)
    {
        // LB-BP004: [LayerBlueprint] 类型必须是 class
        if (namedType.TypeKind != TypeKind.Class)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.BP004_BlueprintMustBeClass,
                blueprintAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                namedType.Name));
            return;
        }

        // LB-BP005: [LayerBlueprint] 类型必须实现 IEntityBlueprint
        if (!namedType.AllInterfaces.Any(i => i.ToDisplayString() == IEntityBlueprintName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.BP005_BlueprintMustImplementIEntityBlueprint,
                blueprintAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                namedType.Name));
        }
    }

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol namedType)
    {
        return namedType.Constructors.Any(c =>
            c.Parameters.Length == 0
            && c.DeclaredAccessibility == Accessibility.Public);
    }
}