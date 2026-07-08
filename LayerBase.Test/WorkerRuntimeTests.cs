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
