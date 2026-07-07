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
