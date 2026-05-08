namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public TActor CreateActor<TActor>()
        where TActor : class, IActor, new()
    {
        TActor actor = new TActor();
        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);
        ActorTypeMeta<TActor> meta = ActorTypeMetaCache.GetOrBuild<TActor>(generated);

        BehaviourArchetype archetype = GetOrCreateArchetype(meta.Signature);
        TypedActorStorage<TActor> storage = archetype.GetOrCreateStorage(meta, this);

        int slotIndex = storage.AllocateSlot(actor);
        ActorId actorId = new(
            archetypeId: archetype.ArchetypeId,
            typeStorageIndex: storage.TypeStorageIndex,
            slotIndex: slotIndex,
            generation: storage.GetGeneration(slotIndex));

        generated.ActorInit(new ActorContext(this, actorId));
        return actor;
    }
}
