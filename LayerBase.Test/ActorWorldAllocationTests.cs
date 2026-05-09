using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed partial class ActorWorldAllocationTests
{

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
