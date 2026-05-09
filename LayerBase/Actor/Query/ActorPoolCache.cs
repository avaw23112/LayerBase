namespace LayerBase.Actor;

internal static class ActorPoolCache<TActor>
    where TActor : class, IActor
{
    public static readonly ActorPool<TActor> Pool = new();
}
