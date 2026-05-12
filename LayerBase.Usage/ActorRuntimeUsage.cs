using LayerBase.Actor;

namespace LayerBase.Usage;

public struct UsageActorEvent
{
    public int Value;

    public UsageActorEvent(int value)
    {
        Value = value;
    }
}

public sealed partial class UsageActor : IActor
{
    public int LastValue { get; private set; }

    [ActorBehaviour]
    private void OnEvent(in UsageActorEvent value)
    {
        LastValue = value.Value;
    }
}

public static class ActorRuntimeUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Actor Runtime Usage Verification ---");

        var world = new ActorWorld();
        UsageActor actor = world.CreateActor<UsageActor>();

        actor.PostInside(new UsageActorEvent(42));

        var budget = new RuntimeFrameBudget(maxEvents: 8, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);

        Console.WriteLine($"Actor runtime processed value: {actor.LastValue}");
    }
}