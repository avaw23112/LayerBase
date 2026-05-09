namespace LayerBase.Actor;

internal delegate ActorEventColumnRuntime ActorEventColumnFactory(
    TypedStorageRuntime storage,
    object invoker,
    ActorWorld world,
    BehaviourType behaviourType);

internal delegate ActorCallColumnRuntime ActorCallColumnFactory(
    TypedStorageRuntime storage,
    object invoker,
    ActorWorld world);
