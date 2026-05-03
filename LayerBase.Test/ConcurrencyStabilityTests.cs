using LayerBase;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class ConcurrencyStabilityTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void MultiThreaded_Send_Post_StressTest()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        var threadCount = 8;
        var iterations = 1000;
        var tasks = new Task[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    LayerHub.PostFromAnyThread(new StressEvent(i));
                }
            });
        }
        LayerHub.Pump(0.16f);
        Assert.DoesNotThrow(() => Task.WaitAll(tasks), "Stress test should not cause deadlocks or crashes");
    }

    [Test]
    public void Send_And_Reset_Interleaved_Test()
    {
        var run = true;
        var task = Task.Run(() =>
        {
            while (run)
            {
                LayerHub.Send(new StressEvent(1));
                Thread.Yield();
            }
        });

        for (int i = 0; i < 50; i++)
        {
            LayerHub.Reset();
            var layer = new TestLayer();
            LayerHub.CreateLayers().Push(layer).Build();
            Thread.Sleep(10);
        }

        run = false;
        task.Wait();
    }

    [Test]
    public void Send_While_Subscribe_Rebuilds_Does_Not_Observe_Mutating_Handler_Lists()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        var run = true;
        Exception? senderError = null;
        Exception? subscriberError = null;
        var received = 0;

        var sender = Task.Run(() =>
        {
            try
            {
                while (Volatile.Read(ref run))
                {
                    LayerHub.Send(new StressEvent(1));
                    Thread.Yield();
                }
            }
            catch (Exception ex)
            {
                senderError = ex;
            }
        });

        var subscriber = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 512; i++)
                {
                    layer.SubscribeNotify<StressEvent>((in StressEvent _) => Interlocked.Increment(ref received));
                    LayerHub.Send(new StressEvent(i));
                }
            }
            catch (Exception ex)
            {
                subscriberError = ex;
            }
            finally
            {
                Volatile.Write(ref run, false);
            }
        });

        Assert.DoesNotThrow(() => Task.WaitAll(sender, subscriber));
        Assert.That(senderError, Is.Null);
        Assert.That(subscriberError, Is.Null);
        Assert.That(received, Is.GreaterThan(0));
    }

    [Test]
    public void Layer_dispose_is_idempotent_under_concurrent_calls()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        layer.SubscribeNotify<StressEvent>((in StressEvent _) => { });

        var tasks = Enumerable.Range(0, 8)
                              .Select(_ => Task.Run(() => layer.Dispose()))
                              .ToArray();

        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    private class TestLayer : Layer { }
    private struct StressEvent { 
        public int Id;
        public StressEvent(int id) => Id = id;
    }
}
