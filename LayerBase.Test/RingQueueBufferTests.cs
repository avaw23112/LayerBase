using LayerBase.Actor;

namespace LayerBase.Test;

[TestFixture]
public sealed class RingQueueBufferTests
{
    [Test]
    public void Rent_after_release_should_reuse_buffer_id()
    {
        var buffer = new RingQueueBuffer<TestEvent>();

        int firstId = buffer.Rent(initialCapacity: 4);
        buffer.Release(firstId);

        int secondId = buffer.Rent(initialCapacity: 4);

        Assert.That(secondId, Is.EqualTo(firstId));
    }

    [Test]
    public void Rent_after_release_should_preserve_capacity_when_enough()
    {
        var buffer = new RingQueueBuffer<TestEvent>();

        int firstId = buffer.Rent(initialCapacity: 8);
        int firstCapacity = buffer.GetCapacity(firstId);
        buffer.Release(firstId);

        int secondId = buffer.Rent(initialCapacity: 4);
        int secondCapacity = buffer.GetCapacity(secondId);

        Assert.That(secondId, Is.EqualTo(firstId));
        Assert.That(secondCapacity, Is.EqualTo(firstCapacity));
    }

    [Test]
    public void Rent_after_release_should_grow_reused_buffer_when_needed()
    {
        var buffer = new RingQueueBuffer<TestEvent>();

        int firstId = buffer.Rent(initialCapacity: 4);
        buffer.Release(firstId);

        int secondId = buffer.Rent(initialCapacity: 8);

        Assert.That(secondId, Is.EqualTo(firstId));
        Assert.That(buffer.GetCapacity(secondId), Is.EqualTo(8));
    }

    private readonly struct TestEvent
    {
    }
}