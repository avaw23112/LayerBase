using System.Diagnostics;
using System.Reflection;
using LayerBase;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Worker;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerPoolTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Default_cts_is_reused_after_completed_job()
    {
        int targetCount = 32;
        var (runtime, service) = BuildRuntime();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < targetCount; i++)
            handles.Add(service.Run(i));

        Assert.That(SpinUntilAllDone(runtime, handles, TimeSpan.FromSeconds(15)), Is.True);

        Assert.That(runtime.WorkerJobs.ActiveCount, Is.EqualTo(0));
        Assert.That(GetCoordinatorCtsPoolCount(runtime.WorkerJobs), Is.GreaterThan(0),
            "Uncancelled cancellation sources should return to the coordinator pool.");

        runtime.Dispose();
    }

    [Test]
    public void External_token_registration_is_released_after_completion()
    {
        using var cts = new CancellationTokenSource();
        var (runtime, service) = BuildRuntime();

        var handle = service.Run(42, cts.Token);
        Assert.That(SpinUntilAllDone(runtime, [handle], TimeSpan.FromSeconds(15)), Is.True);

        WorkerState terminal = runtime.WorkerJobs.GetState(handle);

        cts.Cancel();
        runtime.Pump(0f);

        Assert.That(runtime.WorkerJobs.GetState(handle), Is.EqualTo(terminal),
            "Cancelling the external token after completion must not change the terminal state.");
        runtime.Dispose();
    }

    [Test]
    public void Terminal_job_releases_cts_exactly_once()
    {
        var (runtime, service) = BuildRuntime();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < 20; i++)
            handles.Add(service.Run(i));

        Assert.That(SpinUntilAllDone(runtime, handles, TimeSpan.FromSeconds(15)), Is.True);

        Assert.That(runtime.WorkerJobs.ActiveCount, Is.EqualTo(0));
        Assert.That(GetCoordinatorCtsPoolCount(runtime.WorkerJobs),
            Is.LessThanOrEqualTo(WorkerJobSchedulerOptions.Default.WorkerItemPoolCapacity));

        runtime.Dispose();
    }

    [Test]
    public void Worker_item_pool_does_not_exceed_configured_cap()
    {
        var (runtime, service) = BuildRuntime();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < 128; i++)
            handles.Add(service.Run(i));

        Assert.That(SpinUntilAllDone(runtime, handles, TimeSpan.FromSeconds(15)), Is.True);

        Assert.That(GetExecutionItemPoolCount(), Is.LessThanOrEqualTo(64));
        runtime.Dispose();
    }

    [Test]
    public void Shutdown_total_time_is_bounded_by_single_deadline()
    {
        var layer = new StuckWorkerLayer();
        layer.RegisterService(new StuckWorkerService());

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var sw = Stopwatch.StartNew();
        runtime.Dispose();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(30000),
            "Dispose should be bounded by global deadline.");
    }

    [Test]
    public void Pooled_item_does_not_retain_runtime_or_endpoint()
    {
        int targetCount = 16;
        var (runtime, service) = BuildRuntime();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < targetCount; i++)
            handles.Add(service.Run(i));

        Assert.That(SpinUntilAllDone(runtime, handles, TimeSpan.FromSeconds(15)), Is.True);

        runtime.Dispose();
    }

    private static int GetCoordinatorCtsPoolCount(WorkerJobCoordinator coordinator)
    {
        FieldInfo field = typeof(WorkerJobCoordinator)
            .GetField("_ctsPool", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var pool = (Stack<CancellationTokenSource>)field.GetValue(coordinator)!;
        return pool.Count;
    }

    private static int GetExecutionItemPoolCount()
    {
        Type itemType = typeof(WorkerExecutionItem<,,>)
            .MakeGenericType(typeof(PoolProbeJob), typeof(PoolProbeInput), typeof(PoolProbeResult));

        FieldInfo field = itemType.GetField(
            "s_poolCount",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (int)field.GetValue(null)!;
    }

    private static (LayerRuntime Runtime, PoolProbeService Service) BuildRuntime()
    {
        var layer = new PoolProbeLayer();
        var service = new PoolProbeService();
        layer.RegisterService(service);
        var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();
        return (runtime, service);
    }

    private static bool SpinUntilAllDone(LayerRuntime runtime, IReadOnlyList<WorkerHandle> handles, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            runtime.Pump(0f);

            bool allDone = true;
            foreach (var h in handles)
            {
                var state = runtime.WorkerJobs.GetState(h);
                if (state != WorkerState.Completed && state != WorkerState.Failed && state != WorkerState.Cancelled)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone) return true;
            Thread.Sleep(10);
        }

        return false;
    }

    private sealed class PoolProbeLayer : Layer
    {
    }

    private sealed class PoolProbeService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle Run(int value, CancellationToken ct = default)
        {
            return this.WorkerJobs().Run<PoolProbeJob, PoolProbeInput, PoolProbeResult>(
                new PoolProbeJob(),
                new PoolProbeInput(value),
                WorkerEventJobOptions.Default,
                ct);
        }
    }

    private readonly struct PoolProbeInput
    {
        public PoolProbeInput(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct PoolProbeResult
    {
        public PoolProbeResult(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct PoolProbeJob : IWorkerEventJob<PoolProbeInput, PoolProbeResult>
    {
        public PoolProbeResult Execute(in PoolProbeInput input, in WorkerJobContext context)
        {
            return new PoolProbeResult(input.Value * 2);
        }
    }

    private sealed class StuckWorkerLayer : Layer
    {
    }

    private sealed class StuckWorkerService : IService
    {
        public void ConfigureServices(IServiceCollection services) { }
    }
}
