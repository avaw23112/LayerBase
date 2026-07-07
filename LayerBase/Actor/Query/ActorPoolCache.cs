namespace LayerBase.Actor;

internal static class ActorPoolCache<TActor>
    where TActor : class, IActor, new()
{
    public static readonly ActorPool<TActor> Pool = new();
}