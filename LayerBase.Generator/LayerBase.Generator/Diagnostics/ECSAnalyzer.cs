using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LayerBase.Generator.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ECSAnalyzer : DiagnosticAnalyzer
{
    private const string IComponentName = "LayerBase.Core.IComponent";
    private const string IActorEventName = "LayerBase.Core.IActorEvent";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ECS001_QueryMethodTypeMustBePartial,
            DiagnosticDescriptors.ECS002_QueryMethodMustBeStatic,
            DiagnosticDescriptors.ECS003_QueryMethodCannotBeGeneric,
            DiagnosticDescriptors.ECS004_QueryWithoutBringMustReturnVoid,
            DiagnosticDescriptors.ECS005_QueryWithBringMustReturnProjectResult,
            DiagnosticDescriptors.ECS006_BringMustDeclareEventType,
            DiagnosticDescriptors.ECS008_BringEventParamsAtEnd,
            DiagnosticDescriptors.ECS009_BringEventParamMustBeRef,
            DiagnosticDescriptors.ECS010_ComponentParamMustBeRefOrIn,
            DiagnosticDescriptors.ECS011_EntityParamAtMostOnce,
            DiagnosticDescriptors.ECS012_QueryInputMustAppearBeforeComponents,
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

        var queryAttribute = methodSymbol.GetAttributes()
                                         .FirstOrDefault(a => a.AttributeClass?.MetadataName == "QueryAttribute");

        if (queryAttribute == null)
        {
            return;
        }

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

        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS002_QueryMethodMustBeStatic,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        if (methodSymbol.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS003_QueryMethodCannotBeGeneric,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        var bringAttribute = methodSymbol.GetAttributes()
                                         .FirstOrDefault(a => a.AttributeClass?.MetadataName == "BringAttribute" ||
                                                              a.AttributeClass?.MetadataName.StartsWith(
                                                                  "BringAttribute`") == true);

        bool hasBring = bringAttribute != null;

        if (!hasBring && !methodSymbol.ReturnsVoid)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS004_QueryWithoutBringMustReturnVoid,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        if (hasBring)
        {
            bool returnsProjectResult = methodSymbol.ReturnType?.MetadataName == "ProjectResult" &&
                                        methodSymbol.ReturnType?.ContainingNamespace?.ToDisplayString() ==
                                        "LayerBase.ECS";

            if (!returnsProjectResult)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS005_QueryWithBringMustReturnProjectResult,
                    methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.Name));
            }

            if (bringAttribute!.ConstructorArguments.Length == 0 &&
                bringAttribute.AttributeClass?.TypeArguments.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ECS006_BringMustDeclareEventType,
                    bringAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
            }
        }

        AnalyzeParameters(context, methodSymbol, hasBring);
        AnalyzeEntryPoint(context, methodSymbol);
    }

    private static void AnalyzeEntryPoint(SymbolAnalysisContext context, IMethodSymbol methodSymbol)
    {
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
        IMethodSymbol methodSymbol,
        bool hasBring)
    {
        int entityCount = 0;
        bool componentStarted = false;
        bool foundBringEvent = false;

        foreach (var param in methodSymbol.Parameters)
        {
            bool isEntity = param.Type.MetadataName == "Entity" &&
                            param.Type.ContainingNamespace?.ToDisplayString() == "Arch.Core";

            if (isEntity)
            {
                componentStarted = true;
                entityCount++;

                if (entityCount > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS011_EntityParamAtMostOnce,
                        param.Locations.FirstOrDefault()));
                }

                continue;
            }

            if (hasBring && IsBringEventParam(param))
            {
                foundBringEvent = true;

                if (param.RefKind != RefKind.Ref)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS009_BringEventParamMustBeRef,
                        param.Locations.FirstOrDefault(),
                        param.Name));
                }

                continue;
            }

            bool isComponent = ImplementsInterface(param.Type, IComponentName);
            if (IsInputParameter(param))
            {
                if (componentStarted)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS012_QueryInputMustAppearBeforeComponents,
                        param.Locations.FirstOrDefault(),
                        param.Name));
                }

                continue;
            }

            if (isComponent)
            {
                componentStarted = true;

                if (param.RefKind != RefKind.Ref && param.RefKind != RefKind.In)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS010_ComponentParamMustBeRefOrIn,
                        param.Locations.FirstOrDefault(),
                        param.Name));
                }

                if (foundBringEvent)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ECS008_BringEventParamsAtEnd,
                        param.Locations.FirstOrDefault()));
                }

                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ECS013_ComponentMustImplementIComponent,
                param.Locations.FirstOrDefault(),
                param.Type.Name));
        }
    }

    private static bool IsInputParameter(IParameterSymbol param)
    {
        if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.Out)
        {
            return false;
        }

        if (!param.Type.IsValueType)
        {
            return false;
        }

        if (param.Type.MetadataName == "Entity" &&
            param.Type.ContainingNamespace?.ToDisplayString() == "Arch.Core")
        {
            return false;
        }

        return !ImplementsInterface(param.Type, IComponentName) &&
               !ImplementsInterface(param.Type, IActorEventName);
    }

    private static bool IsBringEventParam(IParameterSymbol param)
    {
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
