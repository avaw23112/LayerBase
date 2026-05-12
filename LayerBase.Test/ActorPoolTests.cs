using LayerBase.Actor;

namespace LayerBase.Test;

public readonly struct ActorPoolTag : IActorTag
{
}

[Tag<ActorPoolTag>]
internal sealed partial class PooledProbeActor : IPooledActor
{
    public static int RentCount { get; set; }
    public static int ReturnCount { get; set; }

    public long RecycleDeadlineTicks { get; set; }

    public int State { get; set; }

    public void OnRent()
    {
        RentCount++;
        RecycleDeadlineTicks = 0;
        State = 0;
    }

    public void OnReturn()
    {
        ReturnCount++;
        RecycleDeadlineTicks = 0;
        State = -1;
    }
}

[TestFixture]
public class ActorPoolTests
{
    [SetUp]
    public void SetUp()
    {
        PooledProbeActor.RentCount = 0;
        PooledProbeActor.ReturnCount = 0;
    }

    [Test]
    public void CreateActor_defaults_to_non_pooled_instances()
    {
        var world = new ActorWorld();

        PooledProbeActor first = world.CreateActor<PooledProbeActor>();
        Assert.That(PooledProbeActor.RentCount, Is.EqualTo(0));

        Assert.That(world.DestroyActor(first.GetActorId()), Is.True);

        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        PooledProbeActor second = world.CreateActor<PooledProbeActor>();

        Assert.That(ReferenceEquals(first, second), Is.False);
        Assert.That(PooledProbeActor.ReturnCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateActor_with_usePool_rents_and_returns_actor_instances()
    {
        var world = new ActorWorld();

        PooledProbeActor first = world.CreateActor<PooledProbeActor>(usePool: true);
        first.State = 42;

        Assert.That(PooledProbeActor.RentCount, Is.EqualTo(1));

        Assert.That(world.DestroyActor(first.GetActorId()), Is.True);

        var budget = new RuntimeFrameBudget(8, 0, 0);
        world.Pump(0f, 0f, false, ref budget);

        Assert.That(PooledProbeActor.ReturnCount, Is.EqualTo(1));
        Assert.That(first.State, Is.EqualTo(-1));

        PooledProbeActor second = world.CreateActor<PooledProbeActor>(usePool: true);

        Assert.That(PooledProbeActor.RentCount, Is.EqualTo(2));
        Assert.That(ReferenceEquals(first, second), Is.True);
        Assert.That(second.State, Is.EqualTo(0));
    }

    [Test]
    public void CreateActor_with_usePool_requires_ipooled_actor()
    {
        var world = new ActorWorld();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => world.CreateActor<ActorDamageOnly>(usePool: true))!;

        Assert.That(exception.Message, Does.Contain(nameof(IPooledActor)));
    }
}