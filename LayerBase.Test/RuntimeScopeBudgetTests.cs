using LayerBase;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Scope;
using LayerBase.Worker;
using NUnit.Framework;

namespace LayerBase.Test;

public partial struct BudgetTestEventA
{
    public int Value;
}

public class BudgetTestMetaA : EventMetaData<BudgetTestEventA>
{
}

public partial struct BudgetTestEventB
{
    public int Value;
}

public class BudgetTestMetaB : EventMetaData<BudgetTestEventB>
{
}

[TestFixture]
public class RuntimeScopeBudgetTests
{
    private static PostScheduler CreateScheduler(PostSchedulerOptions options, EventCenter eventCenter)
    {
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int idA = EventTypeId<BudgetTestEventA>.Id;
        int idB = EventTypeId<BudgetTestEventB>.Id;

        policyTable.SetMetaData(idA, new BudgetTestMetaA());
        policyTable.SetMetaData(idB, new BudgetTestMetaB());

        var scheduler = new PostScheduler(0, eventCenter, options, policyTable);
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(idA, PostDeliveryMode.Normal, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
            new PostTypePlan(idB, PostDeliveryMode.Normal, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
        });

        return scheduler;
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Zero_budget_unlimited_processes_all_normal_events()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, hasPostLimit: false);
        var stats = scheduler.Pump(ref budget);

        Assert.That(stats.ProcessedCount, Is.EqualTo(10));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void Budget_limits_normal_events_processed()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 3, hasPostLimit: true);
        var stats = scheduler.Pump(ref budget);

        Assert.That(stats.ProcessedCount, Is.EqualTo(3));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void Pump_with_RemainingPostCount_tracks_remaining_budget()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 8, hasPostLimit: true);

        var stats1 = scheduler.Pump(ref budget);
        Assert.That(stats1.ProcessedCount, Is.EqualTo(8));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void Multiple_events_same_pump_with_budget()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 100, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler = CreateScheduler(options, eventCenter);

        for (int i = 0; i < 10; i++)
            scheduler.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 5, hasPostLimit: true);
        var stats = scheduler.Pump(ref budget);

        Assert.That(stats.ProcessedCount, Is.EqualTo(5));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void Two_pumps_with_shared_budget_consume_from_same_pool()
    {
        var eventCenter = new EventCenter();
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        using var scheduler1 = CreateScheduler(options, eventCenter);
        using var scheduler2 = CreateScheduler(options, new EventCenter());

        for (int i = 0; i < 5; i++)
            scheduler1.TryPost(new BudgetTestEventA { Value = i });
        for (int i = 0; i < 5; i++)
            scheduler2.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 7, hasPostLimit: true);

        var stats1 = scheduler1.Pump(ref budget);
        Assert.That(stats1.ProcessedCount, Is.EqualTo(5));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(2));

        var stats2 = scheduler2.Pump(ref budget);
        Assert.That(stats2.ProcessedCount, Is.EqualTo(2));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void Zero_budget_at_constructor_means_unlimited()
    {
        var budget = new RuntimeFrameBudget(0, 0, 0);
        Assert.That(budget.HasPostLimit, Is.False);
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    [Test]
    public void StartingScopeIndex_defaults_to_zero()
    {
        var budget = new RuntimeFrameBudget(0, 0, 0);
        Assert.That(budget.StartingScopeIndex, Is.EqualTo(0));
    }

    [Test]
    public void Inline_scope_pump_consumes_budget_work_items()
    {
        using var runtime = new LayerRuntime(9303);
        var inlinePlan = new ScopeExecutionPlan(
            new ScopeDescriptor(1, "InlineScope", typeof(MainScope)),
            ScopeOptions.Inline);
        using var scope = new ScopeRuntime(
            inlinePlan,
            runtimeId: 9303,
            generation: 1,
            runtime.WorkerExecutor,
            CreateNoopCallbacks(runtime));

        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int id = EventTypeId<BudgetTestEventA>.Id;
        policyTable.SetMetaData(id, new BudgetTestMetaA());

        scope.InitializeOrUpdateScheduler(options, policyTable, new[]
        {
            new PostTypePlan(id, PostDeliveryMode.Normal, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
        });

        for (int i = 0; i < 3; i++)
            scope.PostScheduler!.TryPost(new BudgetTestEventA { Value = i });

        var budget = new RuntimeFrameBudget(0, 0, 0, hasPostLimit: false);
        scope.PumpScopeResources(0.016f, ref budget, CompletionExceptionPolicy.Throw, null);

        Assert.That(budget.UsedWorkItems, Is.EqualTo(3));
        Assert.That(budget.RemainingPostCount, Is.EqualTo(0));
    }

    private static ScopeRuntimeCallbacks CreateNoopCallbacks(LayerRuntime runtime)
    {
        return new ScopeRuntimeCallbacks(
            static (in ScopeFaultRecord _) => { },
            static _ => { },
            runtime.ReportLayerEventError,
            runtime.DisposeScopeServices);
    }
}
