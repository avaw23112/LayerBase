using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Worker;

namespace EventsTest;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerCoordinatorRaceTests
{
    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Cancel_does_not_reuse_handle_before_physical_completion()
    {
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var service = new BlockingWorkerService(entered, release);
        var layer = new BlockingWorkerLayer();

        layer.RegisterService(service);

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        using var cancellation = new CancellationTokenSource();

        WorkerHandle first = service.Run(cancellation.Token);

        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        cancellation.Cancel();

        WorkerHandle second = service.Run(CancellationToken.None);

        Assert.That(second, Is.Not.EqualTo(first));

        Assert.That(
            first.Index == second.Index &&
            first.Version == second.Version,
            Is.False);

        release.Set();

        Assert.That(
            SpinUntil(() =>
            {
                runtime.Pump(0f);

                return runtime.WorkerJobs.GetState(first) ==
                       WorkerState.Cancelled;
            }),
            Is.True);
    }

    [Test]
    public void Blocking_job_enters_running_before_physical_completion()
    {
        using var entered = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);

        var service = new BlockingWorkerService(entered, release);
        var layer = new BlockingWorkerLayer();
        layer.RegisterService(service);

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        WorkerHandle handle = service.Run(CancellationToken.None);

        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        Assert.That(
            SpinUntil(() =>
            {
                runtime.Pump(0f);
                return runtime.WorkerJobs.GetState(handle) == WorkerState.Running;
            }),
            Is.True);

        Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(1));

        release.Set();

        Assert.That(
            SpinUntil(() =>
            {
                runtime.Pump(0f);
                return runtime.WorkerJobs.GetState(handle) == WorkerState.Completed;
            }),
            Is.True);

        Assert.That(runtime.WorkerJobs.RunningCount, Is.EqualTo(0));
    }

    [Test]
    [Category("ProductionSoak")]
    public void Completion_and_cancel_produce_one_terminal_state()
    {
        var service = new FastWorkerService();
        var layer = new FastWorkerLayer();

        layer.RegisterService(service);

        using LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        for (int i = 0; i < 10_000; i++)
        {
            using var cancellation = new CancellationTokenSource();

            WorkerHandle handle = service.Run(cancellation.Token);

            cancellation.Cancel();

            Assert.That(
                SpinUntil(() =>
                {
                    runtime.Pump(0f);

                    WorkerState state = runtime.WorkerJobs.GetState(handle);

                    return state is
                        WorkerState.Completed or
                        WorkerState.Cancelled or
                        WorkerState.Failed;
                }),
                Is.True);
        }

        Assert.That(runtime.WorkerJobs.ActiveCount, Is.EqualTo(0));
    }

    private static bool SpinUntil(Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return true;

            Thread.Yield();
        }

        return false;
    }

    private sealed class BlockingWorkerLayer : Layer
    {
    }

    private sealed class FastWorkerLayer : Layer
    {
    }

    private sealed class BlockingWorkerService : IService
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;

        public BlockingWorkerService(
            ManualResetEventSlim entered,
            ManualResetEventSlim release)
        {
            _entered = entered;
            _release = release;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle Run(CancellationToken token)
        {
            var job = new BlockingJob(_entered, _release);

            int input = 1;

            return this.WorkerJobs().Run<BlockingJob, int, WorkerRaceResult>(
                in job,
                in input,
                cancellationToken: token);
        }
    }

    private sealed class FastWorkerService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle Run(CancellationToken token)
        {
            var job = new FastJob();
            int input = 1;

            return this.WorkerJobs().Run<FastJob, int, WorkerRaceResult>(
                in job,
                in input,
                cancellationToken: token);
        }
    }

    private readonly struct BlockingJob : IWorkerEventJob<int, WorkerRaceResult>
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;

        public BlockingJob(
            ManualResetEventSlim entered,
            ManualResetEventSlim release)
        {
            _entered = entered;
            _release = release;
        }

        public WorkerRaceResult Execute(
            in int input,
            in WorkerJobContext context)
        {
            _entered.Set();
            _release.Wait();

            return new WorkerRaceResult(input);
        }
    }

    private readonly struct FastJob : IWorkerEventJob<int, WorkerRaceResult>
    {
        public WorkerRaceResult Execute(
            in int input,
            in WorkerJobContext context)
        {
            return new WorkerRaceResult(input);
        }
    }

    private readonly struct WorkerRaceResult
    {
        public WorkerRaceResult(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
