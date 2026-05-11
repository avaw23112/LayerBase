namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public void PrewarmPool<TActor>(int count)
        where TActor : class, IActor, IPooledActor, new()
    {
        ActorPoolCache<TActor>.Pool.Prewarm(count);
    }

    public void SetPoolLimit<TActor>(int maxCount)
        where TActor : class, IActor, IPooledActor, new()
    {
        ActorPoolCache<TActor>.Pool.SetLimit(maxCount);
    }

    public ActorPoolStats GetPoolStats<TActor>()
        where TActor : class, IActor, IPooledActor, new()
    {
        return ActorPoolCache<TActor>.Pool.GetStats();
    }

    public void ClearPool<TActor>()
        where TActor : class, IActor, IPooledActor, new()
    {
        ActorPoolCache<TActor>.Pool.Clear();
    }

    public TActor CreatePooledActor<TActor>()
        where TActor : class, IActor, IPooledActor, new()
    {
        return CreateActor<TActor>(usePool: true);
    }
}
