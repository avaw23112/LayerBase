using System.Diagnostics;
using LayerBase;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Worker;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerShutdownTests
{
    private static readonly ManualResetEventSlim JobStarted = new(false);

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
        JobStarted.Reset();
    }

    [Test]
    public void Worker_scheduler_dispose_is_bounded_when_job_ignores_cancellation()
    {
        var layer = new ShutdownProbeLayer();
        var service = new ShutdownProbeService();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        WorkerHandle handle = service.RunBlocking();
        Assert.That(handle.IsValid, Is.True);

        JobStarted.Wait(TimeSpan.FromSeconds(5));

        var sw = Stopwatch.StartNew();
        runtime.Dispose();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(30_000),
            "Dispose should not block indefinitely; bounded join should timeout and continue.");
    }

    [Test]
    public void Normal_worker_shutdown_releases_all_threads()
    {
        var layer = new ShutdownProbeLayer();
        var service = new ShutdownProbeService();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < 10; i++)
        {
            handles.Add(service.RunNormal(i));
        }

        bool allDone = SpinUntil(() =>
        {
            foreach (var h in handles)
            {
                var state = runtime.WorkerJobs.GetState(h);
                if (state != WorkerState.Completed && state != WorkerState.Failed && state != WorkerState.Cancelled)
                    return false;
            }
            return true;
        }, TimeSpan.FromSeconds(15));

        Assert.That(allDone, Is.True, "All jobs should complete normally.");
        runtime.Dispose();
    }

    private static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    private sealed class ShutdownProbeLayer : Layer
    {
    }

    private sealed class ShutdownProbeService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle RunBlocking()
        {
            return this.WorkerJobs().Run<BlockingJob, BlockingInput, BlockingResult>(
                new BlockingJob(),
                new BlockingInput());
        }

        public WorkerHandle RunNormal(int value)
        {
            return this.WorkerJobs().Run<NormalJob, NormalInput, NormalResult>(
                new NormalJob(),
                new NormalInput(value));
        }
    }

    private readonly struct BlockingInput
    {
    }

    private readonly struct BlockingResult
    {
    }

    private readonly struct BlockingJob : IWorkerEventJob<BlockingInput, BlockingResult>
    {
        public BlockingResult Execute(in BlockingInput input, in WorkerJobContext context)
        {
            JobStarted.Set();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }

    private readonly struct NormalInput
    {
        public NormalInput(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct NormalResult
    {
        public NormalResult(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct NormalJob : IWorkerEventJob<NormalInput, NormalResult>
    {
        public NormalResult Execute(in NormalInput input, in WorkerJobContext context)
        {
            return new NormalResult(input.Value);
        }
    }
}
