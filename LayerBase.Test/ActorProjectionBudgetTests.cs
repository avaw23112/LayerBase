using System.Reflection;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection;
using NUnit.Framework;

namespace LayerBase.Test;

public sealed class ActorProjectionBudgetTests
{
    [Test]
    public void Runtime_frame_budget_is_plain_work_item_budget()
    {
        Assert.That(typeof(RuntimeFrameBudget).IsByRefLike, Is.False);

        Assert.That(typeof(RuntimeFrameBudget).GetField("MaxWorkItems"), Is.Not.Null);
        Assert.That(typeof(RuntimeFrameBudget).GetField("UsedWorkItems"), Is.Not.Null);
        Assert.That(typeof(RuntimeFrameBudget).GetProperty("RemainingWorkItems"), Is.Not.Null);
        Assert.That(typeof(RuntimeFrameBudget).GetMethod("Consume", new[] { typeof(int) }), Is.Not.Null);
    }

    [Test]
    public void Main_scope_direct_projection_sink_and_sync_flag_are_removed()
    {
        Type? directSinkType = typeof(ProjectedActorEnsureResult)
            .Assembly
            .GetType("LayerBase.ECS.Projection.MainScopeProjectedActorCommandSink");

        Assert.That(directSinkType, Is.Null);
        Assert.That(typeof(IProjectedActorCommandSink).GetProperty("CompletesSynchronously"), Is.Null);
    }

    [Test]
    public void World_exposes_budgeted_projected_actor_sweep_entry()
    {
        MethodInfo? method = typeof(World).GetMethod(
            "SweepProjectedActors",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(RuntimeFrameBudget).MakeByRefType(), typeof(int) },
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(int)));
    }
}
