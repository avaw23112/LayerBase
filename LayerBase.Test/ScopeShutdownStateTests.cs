using LayerBase;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeShutdownStateTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        LayerBase.Event.EventMetaData.EventMetaDataHandler.Clear();
    }

    [Test]
    public void ApplyFaultPolicy_does_not_throw_ObjectDisposedException_after_shutdown()
    {
        using var runtime = new LayerRuntime(9991);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[] { ScopeExecutionPlan.CreateMain() },
            runtimeId: 9991,
            generation: 1);

        var record = new ScopeFaultRecord(
            9991, 1, ScopeDefinitionIds.Main,
            ScopeFaultPhase.WorkerLoop,
            new InvalidOperationException("test fault"));

        host.Dispose();

        Assert.DoesNotThrow(() => host.ApplyFaultPolicy(in record));
    }

    [Test]
    public void Worker_dispose_control_exception_is_reported_as_fault_not_silently_lost()
    {
        Exception? capturedFaultException = null;

        using var runtime = new LayerRuntime(9992);

        var disposeInvokers = new LifecycleInvoker[]
        {
            () => throw new InvalidOperationException("dispose failed")
        };

        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)
        };

        var lifecyclePlan = new ScopeLifecyclePlan(
            layers,
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            Array.Empty<UpdateInvoker>(),
            Array.Empty<FixedUpdateInvoker>(),
            Array.Empty<LifecycleInvoker>(),
            disposeInvokers);

        var workerPlan = new ScopeExecutionPlan(
            new ScopeDescriptor(777, "TestWorker", typeof(TestWorkerScope)),
            ScopeOptions.Worker(tickRateHz: 10),
            lifecyclePlan: lifecyclePlan);

        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[] { ScopeExecutionPlan.CreateMain(), workerPlan },
            runtimeId: 9992,
            generation: 1);

        runtime.Faulted += info =>
        {
            capturedFaultException = info.Record.Exception;
        };

        var deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(15));
        host.StartWorkers(in deadline);

        host.Dispose();

        Assert.That(capturedFaultException, Is.Not.Null,
            "An exception should have been reported via runtime.Faulted");
        Assert.That(capturedFaultException, Is.TypeOf<InvalidOperationException>());
        Assert.That(capturedFaultException!.Message, Does.Contain("dispose failed"));
    }

    private sealed class TestWorkerScope : IScopeDefinition
    {
        public ScopeOptions Options => ScopeOptions.Worker(tickRateHz: 10);
    }
}
