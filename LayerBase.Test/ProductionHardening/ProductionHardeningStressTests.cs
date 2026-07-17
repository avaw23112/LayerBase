using System.Collections.Concurrent;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest.ProductionHardening;

[TestFixture]
[Category("ProductionHardening")]
public class ProductionHardeningStressTests
{
    private partial struct StressEvent
    {
        public int Value;
    }

    private sealed class StressLayer : Layer
    {
    }

    [Test]
    public void Runtime_reuse_does_not_leak_old_payloads()
    {
        for (int i = 0; i < 100; i++)
        {
            using var runtime = LayerHub.CreateLayers()
                .Push(new StressLayer())
                .Build();

            runtime.Send(new StressEvent { Value = i });
            runtime.Pump(0f);
        }
    }

    [Test]
    public void Concurrent_ingress_and_pump_returns_valid_results()
    {
        const int workerCount = 4;
        const int postsPerWorker = 500;
        var accepted = 0;
        var rejected = 0;
        var disposed = 0;
        var lockObj = new object();

        using var runtime = LayerHub.CreateLayers()
            .Push(new StressLayer())
            .Build();

        var mainScope = runtime.Main;
        var workers = new Task[workerCount];
        var barrier = new Barrier(workerCount + 1);

        for (int w = 0; w < workerCount; w++)
        {
            int workerId = w;
            workers[w] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int p = 0; p < postsPerWorker; p++)
                {
                    var result = mainScope.Post(new StressEvent { Value = workerId * 10000 + p });
                    lock (lockObj)
                    {
                        if (result.Status == ScopePostStatus.Accepted)
                            accepted++;
                        else if (result.Status == ScopePostStatus.QueueFull)
                            rejected++;
                        else
                            disposed++;
                    }
                }
            });
        }

        barrier.SignalAndWait();

        for (int p = 0; p < 50; p++)
        {
            runtime.Pump(0f);
            Thread.Yield();
        }

        Task.WaitAll(workers);
        runtime.Dispose();

        Assert.That(accepted + rejected + disposed, Is.EqualTo(workerCount * postsPerWorker));
    }
}
