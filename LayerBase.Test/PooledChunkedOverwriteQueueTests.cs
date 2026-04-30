using System.Buffers;
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

    [Test]
    public void Draining_to_empty_reuses_last_segment_until_dispose()
    {
        var pool = new TrackingArrayPool<int>();
        var queue = CreateQueue(pool, 8);
        var enqueue = typeof(PooledChunkedOverwriteQueue<int>).GetMethod(
            "EnqueueOverwrite",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var tryDequeue = typeof(PooledChunkedOverwriteQueue<int>).GetMethod(
            "TryDequeue",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(enqueue, Is.Not.Null);
        Assert.That(tryDequeue, Is.Not.Null);

        for (var i = 0; i < 5; i++)
        {
            enqueue!.Invoke(queue, new object[] { i });
            var args = new object?[] { null };
            var dequeued = (bool)tryDequeue!.Invoke(queue, args)!;
            Assert.That(dequeued, Is.True);
            Assert.That(args[0], Is.EqualTo(i));
        }

        Assert.That(pool.RentCount, Is.EqualTo(1),
            "An empty queue should retain and reuse its final segment instead of losing it each drain cycle.");

        queue.Dispose();

        Assert.That(pool.ReturnCount, Is.EqualTo(1),
            "The retained empty segment should still be returned when the queue is disposed.");
    }

    private static PooledChunkedOverwriteQueue<int> CreateQueue()
    {
        var ctor = typeof(PooledChunkedOverwriteQueue<int>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(EventQueueOverflowStrategy) },
            null);

        Assert.That(ctor, Is.Not.Null, "Expected internal constructor to exist.");
        return (PooledChunkedOverwriteQueue<int>)ctor!.Invoke(new object[] { 8, EventQueueOverflowStrategy.OverWrite });
    }

    private static PooledChunkedOverwriteQueue<int> CreateQueue(ArrayPool<int> pool, int chunkSize)
    {
        var ctor = typeof(PooledChunkedOverwriteQueue<int>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(int),
                typeof(int),
                typeof(ArrayPool<int>),
                typeof(bool),
                typeof(bool),
                typeof(EventQueueOverflowStrategy)
            },
            null);

        Assert.That(ctor, Is.Not.Null, "Expected internal full constructor to exist.");
        return (PooledChunkedOverwriteQueue<int>)ctor!.Invoke(new object[]
        {
            chunkSize,
            0,
            pool,
            false,
            false,
            EventQueueOverflowStrategy.OverWrite
        });
    }

    private sealed class TrackingArrayPool<T> : ArrayPool<T>
    {
        public int RentCount;
        public int ReturnCount;

        public override T[] Rent(int minimumLength)
        {
            Interlocked.Increment(ref RentCount);
            return new T[minimumLength];
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (clearArray) Array.Clear(array, 0, array.Length);
            Interlocked.Increment(ref ReturnCount);
        }
    }
}
