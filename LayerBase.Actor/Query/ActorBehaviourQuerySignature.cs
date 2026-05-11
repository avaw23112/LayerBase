using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal static class ActorBehaviourQuerySignature<TEvent>
    where TEvent : struct
{
    public static readonly BehaviourSignature Value = new(new[]
    {
        EventTypeId<TEvent>.Id
    });
}

internal static class ActorBehaviourQuerySignature<TEvent1, TEvent2>
    where TEvent1 : struct
    where TEvent2 : struct
{
    public static readonly BehaviourSignature Value = new(
        ActorSignatureUtility.Merge(
            new[] { EventTypeId<TEvent1>.Id },
            new[] { EventTypeId<TEvent2>.Id }));
}
