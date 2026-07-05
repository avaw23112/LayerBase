namespace LayerBase.Actor;

internal delegate ActorCallColumnRuntime ActorCallColumnFactory(
    TypedStorageRuntime storage,
    object              invoker,
    ActorWorld          world);
