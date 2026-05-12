namespace LayerBase.Actor;

internal readonly struct ActorLifecycleHandle
{
    public static ActorLifecycleHandle Invalid => new(-1, 0);

    public readonly int Index;
    public readonly int Version;

    public ActorLifecycleHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public bool IsValid => Index >= 0;
}