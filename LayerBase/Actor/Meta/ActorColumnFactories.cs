namespace LayerBase.Actor;

internal delegate ActorEventColumnRuntime ActorEventColumnFactory(
    TypedStorageRuntime storage,
    object invoker,
    ActorWorld world);

internal delegate ActorCallColumnRuntime ActorCallColumnFactory(
    TypedStorageRuntime storage,
    object invoker,
    ActorWorld world);
