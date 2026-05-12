namespace LayerBase.Actor;

internal static class ActorGroupQuerySignature<TGroup>
    where TGroup : struct, IActorGroup
{
    public static readonly ActorGroupSignature Value = new(new[]
    {
        ActorGroupId<TGroup>.Id
    });
}

internal static class ActorGroupQuerySignature<TGroup1, TGroup2>
    where TGroup1 : struct, IActorGroup
    where TGroup2 : struct, IActorGroup
{
    public static readonly ActorGroupSignature Value = new(new[]
    {
        ActorGroupId<TGroup1>.Id,
        ActorGroupId<TGroup2>.Id
    });
}