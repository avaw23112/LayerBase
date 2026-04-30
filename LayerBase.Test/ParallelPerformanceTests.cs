using System.Diagnostics;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class ParallelPerformanceTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        LayerHub.InitializeJobScheduler(4);
    }

    [Test]
    public void ParallelHandlers_Should_Run_Concurrently_And_Not_Block_MainThread()
    {
        var layer = new ParallelLayer();
        var processedCount = 0;
        var countdown = new CountdownEvent(100);

        layer.SubscribeParallel((in WorkEvent e) =>
        {
            Thread.Sleep(5);
            Interlocked.Increment(ref processedCount);
            if (!countdown.IsSet) countdown.Signal();
        });

        LayerHub.CreateLayers().Push(layer).Build();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++) LayerHub.Send(new WorkEvent());
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500));

        var finished = countdown.Wait(5000);
        Assert.That(finished, Is.True, $"Parallel tasks timed out. Processed: {processedCount}/100");
    }

    [Test]
    public void ParallelHandlers_Fault_Isolation_Test()
    {
        var layer = new ParallelLayer();
        var errorOccurred = new ManualResetEventSlim(false);

        LayerHub.OnLayerEventInfo += info =>
        {
            if (info.Type == LayerEventInfoType.Error) errorOccurred.Set();
        };

        layer.SubscribeParallel((in FaultEvent e) => throw new Exception("Parallel fault"));

        LayerHub.CreateLayers().Push(layer).Build();
        LayerHub.Send(new FaultEvent());

        // Wait for the signal instead of arbitrary sleep
        var reported = errorOccurred.Wait(2000);
        Assert.That(reported, Is.True, "Parallel fault signal was never received.");
    }

    [Test]
    public void Disposed_parallel_subscription_drops_queued_events()
    {
        LayerHub.InitializeJobScheduler(1);

        var layer = new ParallelLayer();
        var firstStarted = new ManualResetEventSlim(false);
        var releaseFirst = new ManualResetEventSlim(false);
        var firstFinished = new ManualResetEventSlim(false);
        var handled = 0;

        layer.SubscribeParallel((in WorkEvent e) =>
        {
            var current = Interlocked.Increment(ref handled);
            if (current != 1) return;

            firstStarted.Set();
            try
            {
                Assert.That(releaseFirst.Wait(2000), Is.True, "Timed out waiting to release first handler.");
            }
            finally
            {
                firstFinished.Set();
            }
        });

        LayerHub.CreateLayers().Push(layer).Build();

        LayerHub.Send(new WorkEvent());
        Assert.That(firstStarted.Wait(1000), Is.True, "First parallel handler did not start.");

        LayerHub.Send(new WorkEvent());
        layer.Dispose();
        releaseFirst.Set();

        Assert.That(firstFinished.Wait(1000), Is.True, "First parallel handler did not finish.");
        Thread.Sleep(100);
        Assert.That(Volatile.Read(ref handled), Is.EqualTo(1));
    }

    private class ParallelLayer : Layer
    {
    }

    public struct WorkEvent
    {
    }

    public struct FaultEvent
    {
    }
}

