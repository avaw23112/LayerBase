using LayerBase;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeBuildTransactionTests
{
    [Test]
    public void Worker_scope_has_synchronization_context_after_start()
    {
        LayerHub.Reset();

        using var runtime = new LayerRuntime(30100);

        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateWorkerPlan()
            },
            runtimeId: 30100,
            generation: 1);

        var workerScope = host.Scopes[1];

        var deadline = ShutdownDeadline.Start(
            TimeSpan.FromSeconds(15));

        host.StartWorkers(in deadline);

        Assert.That(
            workerScope.SynchronizationContext,
            Is.Not.Null);

        host.Dispose();
    }

    private static ScopeExecutionPlan CreateWorkerPlan()
    {
        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, "TestWorker", typeof(object)),
            ScopeOptions.Worker(tickRateHz: 1),
            null,
            null,
            ScopeLifecyclePlan.Empty);
    }
}
