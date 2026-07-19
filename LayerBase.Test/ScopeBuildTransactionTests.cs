using LayerBase;
using LayerBase.Async;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeBuildTransactionTests
{
    [Test]
    public void Worker_startup_uses_explicit_states_and_started_rollback_list()
    {
        string source = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "LayerBase", "Scope", "ScopeRuntimeHost.cs"));

        Assert.That(source, Does.Contain("switch (worker.StartState)"));
        Assert.That(source, Does.Contain("var started = new List<ScopeWorker>();"));
        Assert.That(source, Does.Contain("RollbackStartedWorkers(started"));
        Assert.That(source, Does.Not.Contain("StartState >="));
        Assert.That(source, Does.Not.Contain("StartState <="));
        Assert.That(source, Does.Not.Contain(">= ScopeWorkerStartState"));
        Assert.That(source, Does.Not.Contain("<= ScopeWorkerStartState"));
    }

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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "LayerBase")) &&
                Directory.Exists(Path.Combine(directory.FullName, "LayerBase.Test")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Could not find repository root.");
        return string.Empty;
    }
}
