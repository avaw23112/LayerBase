using LayerBase.Core;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Worker;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class WorkerRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        WorkerResultService.Received.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void WorkerEventJob_PostsResultEventOnPump()
    {
        LayerRuntime runtime = LayerHub.CreateLayers()
                                       .Push(new WorkerTestLayer())
                                       .Build();

        WorkerHandle handle = runtime.Worker.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
            new WorkerRuntimeAddJob(),
            new WorkerRuntimeAddInput(2, 5));

        SpinWait.SpinUntil(
            () => runtime.Worker.GetState(handle) == WorkerState.Completed,
            TimeSpan.FromSeconds(2));

        runtime.Pump(0.016f);

        Assert.That(runtime.Worker.GetState(handle), Is.EqualTo(WorkerState.Completed));
        Assert.That(WorkerResultService.Received, Is.EqualTo(new[] { 7 }));
    }

    [Test]
    public void Worker_dispose_must_cancel_every_accepted_pending_job()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 1,
            new WorkerOptions(stateCapacity: 16, jobQueueCapacity: 16, eventQueueCapacity: 16));
        var handles = new List<WorkerHandle>();

        for (int i = 0; i < 8; i++)
        {
            handles.Add(runtime.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
                new WorkerRuntimeAddJob(),
                new WorkerRuntimeAddInput(i, i)));
        }

        runtime.Dispose();

        foreach (var handle in handles)
        {
            Assert.That(runtime.GetState(handle), Is.EqualTo(WorkerState.Cancelled));
        }
    }

    [Test]
    public void Worker_job_queue_must_never_exceed_capacity()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 1,
            new WorkerOptions(stateCapacity: 16, jobQueueCapacity: 4, eventQueueCapacity: 16));

        for (int i = 0; i < 10; i++)
        {
            _ = runtime.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
                new WorkerRuntimeAddJob(),
                new WorkerRuntimeAddInput(i, i));
        }

        Assert.That(runtime.JobQueueCountForTest, Is.LessThanOrEqualTo(4));
    }

    [Test]
    public void Worker_state_storage_must_remain_bounded_after_100k_jobs()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 1,
            new WorkerOptions(stateCapacity: 32, jobQueueCapacity: 32, eventQueueCapacity: 32));

        for (int i = 0; i < 100_000; i++)
        {
            _ = runtime.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
                new WorkerRuntimeAddJob(),
                new WorkerRuntimeAddInput(i, i));
        }

        Assert.That(runtime.StateStorageCapacityForTest, Is.EqualTo(32));
        Assert.That(runtime.JobQueueCountForTest, Is.LessThanOrEqualTo(32));
    }

    [Test]
    public void Worker_event_queue_must_never_exceed_capacity()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 1,
            new WorkerOptions(stateCapacity: 16, jobQueueCapacity: 16, eventQueueCapacity: 1));

        runtime.Start();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < 8; i++)
        {
            handles.Add(runtime.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
                new WorkerRuntimeAddJob(),
                new WorkerRuntimeAddInput(i, i)));
        }

        SpinWait.SpinUntil(
            () => handles.All(handle => runtime.GetState(handle) != WorkerState.Pending &&
                                        runtime.GetState(handle) != WorkerState.Running),
            TimeSpan.FromSeconds(2));

        Assert.That(runtime.EventQueueCountForTest, Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void Concurrent_start_must_not_create_duplicate_threads()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 2,
            new WorkerOptions(stateCapacity: 16, jobQueueCapacity: 16, eventQueueCapacity: 16));

        Parallel.For(0, 32, _ => runtime.Start());

        Assert.That(runtime.CreatedThreadCountForTest, Is.EqualTo(2));
    }

    [Test]
    public void Accepted_equals_completed_plus_failed_plus_cancelled()
    {
        using var runtime = new WorkerRuntime(
            workerCount: 1,
            new WorkerOptions(stateCapacity: 32, jobQueueCapacity: 32, eventQueueCapacity: 32));

        runtime.Start();

        var handles = new List<WorkerHandle>();
        for (int i = 0; i < 16; i++)
        {
            handles.Add(runtime.RunEventJob<WorkerRuntimeAddJob, WorkerRuntimeAddInput, WorkerRuntimeAddResult>(
                new WorkerRuntimeAddJob(),
                new WorkerRuntimeAddInput(i, i)));
        }

        SpinWait.SpinUntil(
            () => handles.All(handle => runtime.GetState(handle) != WorkerState.Pending &&
                                        runtime.GetState(handle) != WorkerState.Running),
            TimeSpan.FromSeconds(2));

        int accepted = handles.Count(handle => !handle.IsInvalid);
        int terminal = handles.Count(handle =>
        {
            WorkerState state = runtime.GetState(handle);
            return state is WorkerState.Completed or WorkerState.Failed or WorkerState.Cancelled;
        });

        Assert.That(terminal, Is.EqualTo(accepted));
    }

}

internal readonly struct WorkerRuntimeAddInput
{
    public WorkerRuntimeAddInput(int a, int b)
    {
        A = a;
        B = b;
    }

    public int A { get; }

    public int B { get; }
}

internal readonly struct WorkerRuntimeAddResult : ILayerDto
{
    public WorkerRuntimeAddResult(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

internal readonly struct WorkerRuntimeAddJob : IWorkerEventJob<WorkerRuntimeAddInput, WorkerRuntimeAddResult>
{
    public WorkerRuntimeAddResult Execute(in WorkerRuntimeAddInput input)
    {
        return new WorkerRuntimeAddResult(input.A + input.B);
    }
}

internal sealed partial class WorkerResultService : IService
{
    public static List<int> Received { get; } = new();

    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    internal void OnResult(in WorkerRuntimeAddResult result)
    {
        Received.Add(result.Value);
    }
}

internal sealed partial class WorkerTestLayer : Layer
{
    [Mount] private WorkerResultService _service;
}
