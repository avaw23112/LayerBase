using System.Reflection;
using LayerBase.Core;

namespace EventsTest;

public class PooledChunkedOverwriteQueueTests
{
    [Test]
    public void Dispose_does_not_throw_with_internal_constructor_queue()
    {
        var queue = CreateQueue();

        Assert.DoesNotThrow(() => queue.Dispose());
        Assert.DoesNotThrow(() => queue.Dispose());
    }

    private static PooledChunkedOverwriteQueue<int> CreateQueue()
    {
        var ctor = typeof(PooledChunkedOverwriteQueue<int>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int), typeof(EventQueueOverflowStrategy) },
            modifiers: null);

        Assert.That(ctor, Is.Not.Null, "Expected internal constructor to exist.");
        return (PooledChunkedOverwriteQueue<int>)ctor!.Invoke(new object[] { 8, EventQueueOverflowStrategy.OverWrite });
    }
}
