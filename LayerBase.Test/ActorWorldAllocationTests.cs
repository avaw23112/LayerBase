using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed partial class ActorWorldAllocationTests
{
    [Test]
    public void ActorWorld_post_pump_should_not_allocate_after_warmup()
    {
        const int iterations = 200_000;

        var world = new ActorWorld();
        AllocationProbeActor actor = world.CreateActor<AllocationProbeActor>();

        actor.Post(new AllocationProbeEvent());
        Pump(world);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++)
        {
            actor.Post(new AllocationProbeEvent());
            Pump(world);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0));
    }

    [Test]
    public void Default_mail_options_should_keep_empty_mailbox_buffer()
    {
        Assert.That(ActorMailOptions.Default.ReleaseWhenEmpty, Is.False);
    }

    [Test]
    public void Memory_saving_mail_options_should_release_empty_mailbox_buffer()
    {
        Assert.That(ActorMailOptions.MemorySaving.ReleaseWhenEmpty, Is.True);
    }

    [Test]
    public void ActorRef_post_should_not_allocate_after_warmup()
    {
        const int iterations = 200_000;

        var world = new ActorWorld();
        AllocationProbeActor actor = world.CreateActor<AllocationProbeActor>();
        ActorRef<AllocationProbeActor> actorRef = world.GetActorRef<AllocationProbeActor>(actor.GetActorId());

        actorRef.Post(new AllocationProbeEvent());
        Pump(world);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++)
        {
            actorRef.Post(new AllocationProbeEvent());
            Pump(world);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0));
    }

    [Test]
    public void Query_postall_should_not_allocate_after_warmup()
    {
        const int iterations = 10_000;

        var world = new ActorWorld();
        world.CreateActor<AllocationProbeActor>();
        world.CreateActor<AllocationProbeActor>();
        ActorQueryResult query = world.QueryActor<AllocationProbeEvent>();

        query.PostAll(new AllocationProbeEvent());
        Pump(world);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < iterations; i++)
        {
            query.PostAll(new AllocationProbeEvent());
            Pump(world);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.EqualTo(0));
    }

    private static void Pump(ActorWorld world)
    {
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);
    }

    private readonly struct AllocationProbeEvent
    {
    }

    private sealed partial class AllocationProbeActor : IActor
    {
        [ActorBehaviour]
        private void OnEvent(in AllocationProbeEvent value)
        {
        }
    }
}
