namespace LayerBase.Actor;

internal readonly struct ActorLifecycleMethodMeta
{
    public readonly ActorLifecyclePhase Phase;
    public readonly TickTier Tier;
    public readonly int TickPhase;
    public readonly ActorLifecycleMethodInvoker Invoker;

    public ActorLifecycleMethodMeta(
        ActorLifecyclePhase        phase,
        TickTier                   tier,
        int                        tickPhase,
        ActorLifecycleMethodInvoker invoker)
    {
        Phase = phase;
        Tier = tier;
        TickPhase = tickPhase;
        Invoker = invoker;
    }
}
