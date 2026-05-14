namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        if (!CanUseWorldFast())
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }

        TActor actor = usePool
            ? RentActorFromPool<TActor>()
            : new TActor();

        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);
        ActorTypeMeta<TActor> meta = ActorTypeMetaCache.GetOrBuild<TActor>(generated);

        var key = new ActorArchetypeKey(
            typeof(TActor),
            meta.Signature,
            new ActorTagSignature(meta.TagIds),
            new ActorGroupSignature(meta.GroupIds));

        BehaviourArchetype archetype = GetOrCreateArchetype(key);
        TypedActorStorage<TActor> storage = archetype.GetOrCreateStorage(meta, this);

        int slotIndex = storage.AllocateSlot(actor, usePool);
        ActorId actorId = new(
            archetypeId: archetype.ArchetypeId,
            slotIndex: slotIndex,
            generation: storage.GetGeneration(slotIndex));

        generated.ActorInit(new ActorContext(this, actorId));
        storage.RegisterStreamHandlers(actor, actorId, slotIndex, this);
        storage.RegisterLifecycleInterfaces(actor, actorId, slotIndex, this);
        return actor;
    }

    private static TActor RentActorFromPool<TActor>()
        where TActor : class, IActor, new()
    {
        if (!typeof(IPooledActor).IsAssignableFrom(typeof(TActor)))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} must implement IPooledActor when usePool is true.");
        }

        return ActorPoolCache<TActor>.Pool.Rent();
    }
}
