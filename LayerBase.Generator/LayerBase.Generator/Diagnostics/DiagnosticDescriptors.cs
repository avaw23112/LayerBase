using Microsoft.CodeAnalysis;

namespace LayerBase.Generator.Diagnostics;

/// <summary>
/// LayerBase 诊断描述符。
/// </summary>
public static class DiagnosticDescriptors
{
    private const string ECS = "ECS";
    private const string Blueprint = "Blueprint";
    private const string DTO = "DTO";

    // ECS 诊断
    public static readonly DiagnosticDescriptor ECS001_QueryMethodTypeMustBePartial = new(
        DiagnosticIds.ECS001,
        "Query method type must be partial",
        "Type '{0}' containing [Query] method must be partial",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS002_QueryMethodMustBeStatic = new(
        DiagnosticIds.ECS002,
        "Query method must be static",
        "[Query] method '{0}' must be static",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS003_QueryMethodCannotBeGeneric = new(
        DiagnosticIds.ECS003,
        "Query method cannot be generic",
        "[Query] method '{0}' cannot be generic",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS004_QueryWithoutBringMustReturnVoid = new(
        DiagnosticIds.ECS004,
        "Query without Bring must return void",
        "[Query] method '{0}' without [Bring] must return void",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS005_QueryWithBringMustReturnProjectResult = new(
        DiagnosticIds.ECS005,
        "Query with Bring must return ProjectResult",
        "[Query] method '{0}' with [Bring] must return ProjectResult",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS006_BringMustDeclareEventType = new(
        DiagnosticIds.ECS006,
        "Bring must declare at least one event type",
        "[Bring] must declare at least one event type",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS008_BringEventParamsAtEnd = new(
        DiagnosticIds.ECS008,
        "Bring event parameters must be at end",
        "[Bring] event parameters must appear at the end of the method parameter list and match the [Bring] event type order",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS009_BringEventParamMustBeRef = new(
        DiagnosticIds.ECS009,
        "Bring event parameter must be ref",
        "Bring event parameter '{0}' must be ref",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS010_ComponentParamMustBeRefOrIn = new(
        DiagnosticIds.ECS010,
        "Component parameter must be ref or in",
        "ECS component parameter '{0}' must be ref or in",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS011_EntityParamAtMostOnce = new(
        DiagnosticIds.ECS011,
        "Entity parameter can appear at most once",
        "Entity parameter can appear at most once",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS012_QueryInputMustAppearBeforeComponents = new(
        DiagnosticIds.ECS012,
        "Query input parameters must appear before ECS components",
        "Query input parameter '{0}' must appear before Entity/component parameters",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS013_ComponentMustImplementIComponent = new(
        DiagnosticIds.ECS013,
        "Component must implement IComponent",
        "Query component type '{0}' must implement IComponent",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS014_BringEventMustImplementIActorEvent = new(
        DiagnosticIds.ECS014,
        "Bring event must implement IActorEvent",
        "Bring event type '{0}' must implement IActorEvent",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS020_QueryMethodMustStartWithOn = new(
        DiagnosticIds.ECS020,
        "Query method must start with On or specify EntryPoint",
        "[Query] method '{0}' must start with 'On' or specify [EntryPoint(\"Name\")]",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ECS024_EntryPointNameInvalid = new(
        DiagnosticIds.ECS024,
        "EntryPoint name is not a valid C# method name",
        "[EntryPoint] name '{0}' is not a valid C# method name",
        ECS,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Blueprint 诊断
    public static readonly DiagnosticDescriptor BP001_BundleMustBeClass = new(
        DiagnosticIds.BP001,
        "Bundle must be class",
        "[LayerBundle] type '{0}' must be a class",
        Blueprint,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BP002_BundleMustImplementIBundle = new(
        DiagnosticIds.BP002,
        "Bundle must implement IBundle",
        "[LayerBundle] type '{0}' must implement IBundle",
        Blueprint,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BP003_BundleMustHaveParameterlessConstructor = new(
        DiagnosticIds.BP003,
        "Bundle must have public parameterless constructor",
        "[LayerBundle] type '{0}' must have a public parameterless constructor",
        Blueprint,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BP004_BlueprintMustBeClass = new(
        DiagnosticIds.BP004,
        "Blueprint must be class",
        "[LayerBlueprint] type '{0}' must be a class",
        Blueprint,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BP005_BlueprintMustImplementIEntityBlueprint = new(
        DiagnosticIds.BP005,
        "Blueprint must implement IEntityBlueprint",
        "[LayerBlueprint] type '{0}' must implement IEntityBlueprint",
        Blueprint,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // DTO 诊断
    public static readonly DiagnosticDescriptor DTO001_ComponentMustImplementIComponent = new(
        DiagnosticIds.DTO001,
        "Component must implement IComponent",
        "ECS component type '{0}' must implement IComponent",
        DTO,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DTO002_BringEventMustImplementIActorEvent = new(
        DiagnosticIds.DTO002,
        "Bring event must implement IActorEvent",
        "Bring event type '{0}' must implement IActorEvent",
        DTO,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DTO006_CannotImplementBothComponentAndEvent = new(
        DiagnosticIds.DTO006,
        "Cannot implement both IComponent and IActorEvent",
        "Type '{0}' cannot implement both IComponent and IActorEvent",
        DTO,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
