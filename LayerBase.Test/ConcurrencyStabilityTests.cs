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
                    LayerHub.Send(new StressEvent(i));
                    LayerHub.Post(new StressEvent(i));
                }
            });
        }

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

    private class TestLayer : Layer { }
    private struct StressEvent { 
        public int Id;
        public StressEvent(int id) => Id = id;
    }
}
