using Microsoft.CodeAnalysis;

namespace LayerBase.Generator;

internal static class ActorBehaviourDiagnostics
{
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "LBACTOR001",
        title: "Actor type must be partial",
        messageFormat: "ActorBehaviour type '{0}' must be declared partial",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClassMustImplementActor = new(
        id: "LBACTOR002",
        title: "Actor type must implement IActor",
        messageFormat: "ActorBehaviour type '{0}' must implement LayerBase.Actor.IActor",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodMustBeInstance = new(
        id: "LBACTOR003",
        title: "ActorBehaviour method cannot be static",
        messageFormat: "ActorBehaviour method '{0}' cannot be static",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodMustReturnVoid = new(
        id: "LBACTOR004",
        title: "ActorBehaviour method must return void",
        messageFormat: "ActorBehaviour method '{0}' must return void",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodMustHaveSingleParameter = new(
        id: "LBACTOR005",
        title: "ActorBehaviour method must have a single parameter",
        messageFormat: "ActorBehaviour method '{0}' must have exactly one parameter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParameterMustBeInStructEvent = new(
        id: "LBACTOR006",
        title: "ActorBehaviour parameter must be in TEvent",
        messageFormat: "ActorBehaviour method '{0}' parameter must be declared as 'in TEvent'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EventTypeMustBeStruct = new(
        id: "LBACTOR007",
        title: "ActorBehaviour event type must be a struct",
        messageFormat: "ActorBehaviour method '{0}' event type '{1}' must be a struct",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateEventType = new(
        id: "LBACTOR008",
        title: "ActorBehaviour event type must be unique per actor",
        messageFormat: "Actor type '{0}' already defines an ActorBehaviour for event '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ManualGeneratedMetaImplementation = new(
        id: "LBACTOR009",
        title: "Do not manually implement IGeneratedActorMeta",
        messageFormat: "Actor type '{0}' should not manually implement LayerBase.Actor.IGeneratedActorMeta",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CallMethodCannotBeGeneric = new(
        id: "LBACTOR201",
        title: "ActorCallBehaviour method cannot be generic",
        messageFormat: "ActorCallBehaviour method '{0}' cannot be generic",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CallMethodMustReturnLBTask = new(
        id: "LBACTOR202",
        title: "ActorCallBehaviour method must return LBTask<TResponse>",
        messageFormat: "ActorCallBehaviour method '{0}' must return LayerBase.Async.LBTask<TResponse>",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CallMethodMustHaveRequestAndCancellationToken = new(
        id: "LBACTOR203",
        title: "ActorCallBehaviour method signature is invalid",
        messageFormat:
        "ActorCallBehaviour method '{0}' must have parameters '(in TRequest request, CancellationToken cancellationToken)'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CallRequestTypeMustBeStruct = new(
        id: "LBACTOR204",
        title: "ActorCallBehaviour request type must be a struct",
        messageFormat: "ActorCallBehaviour method '{0}' request type '{1}' must be a struct",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CallResponseTypeMustBeStruct = new(
        id: "LBACTOR205",
        title: "ActorCallBehaviour response type must be a struct",
        messageFormat: "ActorCallBehaviour method '{0}' response type '{1}' must be a struct",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateCallRoute = new(
        id: "LBACTOR206",
        title: "ActorCallBehaviour request/response pair must be unique per actor",
        messageFormat: "Actor type '{0}' already defines an ActorCallBehaviour for request '{1}' and response '{2}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}