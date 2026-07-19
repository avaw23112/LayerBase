using LayerBase;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public class InlineScopeFairnessTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Inline_scopes_use_fair_round_robin_across_frames()
    {
        using var runtime = new LayerRuntime(9901);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(1, "ScopeA", typeof(MainScope)),
                    ScopeOptions.Inline),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(2, "ScopeB", typeof(MainScope)),
                    ScopeOptions.Inline)
            },
            runtimeId: 9901,
            generation: 1);

        var scopeA = host.Scopes[1];
        var scopeB = host.Scopes[2];

        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        var policyTable = new EventBuildPolicyTable(options.DefaultBackpressure);
        int id = EventTypeId<BudgetTestEventA>.Id;
        policyTable.SetMetaData(id, new BudgetTestMetaA());

        scopeA.InitializeOrUpdateScheduler(options, policyTable, new[]
        {
            new PostTypePlan(id, PostDeliveryMode.Normal, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
        });

        var policyTableB = new EventBuildPolicyTable(options.DefaultBackpressure);
        policyTableB.SetMetaData(id, new BudgetTestMetaA());
        scopeB.InitializeOrUpdateScheduler(options, policyTableB, new[]
        {
            new PostTypePlan(id, PostDeliveryMode.Normal, BackpressurePolicy.DropOldest, 0, options.DefaultBackpressure),
        });

        scopeA.PostScheduler!.TryPost(new BudgetTestEventA { Value = 1 });
        scopeB.PostScheduler!.TryPost(new BudgetTestEventA { Value = 1 });

        var budget1 = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 1, hasPostLimit: true);
        host.PumpInlineScopes(0.016f, ref budget1, CompletionExceptionPolicy.Throw, null);

        Assert.That(scopeA.PostScheduler.HasPendingWork, Is.False,
            "Scope A should have consumed its event in frame 1");
        Assert.That(scopeB.PostScheduler.HasPendingWork, Is.True,
            "Scope B should still have a pending event after frame 1");

        scopeA.PostScheduler.TryPost(new BudgetTestEventA { Value = 2 });

        var budget2 = new RuntimeFrameBudget(0, 0, 0, remainingPostCount: 1, hasPostLimit: true);
        host.PumpInlineScopes(0.016f, ref budget2, CompletionExceptionPolicy.Throw, null);

        Assert.That(scopeB.PostScheduler.HasPendingWork, Is.False,
            "Scope B should have consumed its event in frame 2 (fair round-robin)");
        Assert.That(scopeA.PostScheduler.HasPendingWork, Is.True,
            "Scope A should still have its new event pending after frame 2");
    }
}
