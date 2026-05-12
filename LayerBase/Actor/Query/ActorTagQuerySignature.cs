namespace LayerBase.Actor;

internal static class ActorTagQuerySignature<TTag>
    where TTag : struct, IActorTag
{
    public static readonly ActorTagSignature Value = new(new[]
    {
        ActorTagId<TTag>.Id
    });
}

internal static class ActorTagQuerySignature<TTag1, TTag2>
    where TTag1 : struct, IActorTag
    where TTag2 : struct, IActorTag
{
    public static readonly ActorTagSignature Value = new(new[]
    {
        ActorTagId<TTag1>.Id,
        ActorTagId<TTag2>.Id
    });
}