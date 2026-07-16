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
    public void Send_Uses_Explicit_Runtime_While_Hub_Rebuilds()
    {
        var runtime = LayerHub.CreateLayers().Push(new TestLayer()).Build();
        var run = true;
        var task = Task.Run(() =>
        {
            while (run)
            {
                runtime.Send(new StressEvent(1));
                Thread.Yield();
            }
        });

        for (int i = 0; i < 50; i++)
        {
            var layer = new TestLayer();
            var rebuilt = LayerHub.CreateLayers().Push(layer).Build();
            rebuilt.Dispose();
            Thread.Sleep(10);
        }

        run = false;
        task.Wait();
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

    private class TestLayer : Layer
    {
    }

    private struct StressEvent
    {
        public int Id;
        public StressEvent(int id) => Id = id;
    }
}
