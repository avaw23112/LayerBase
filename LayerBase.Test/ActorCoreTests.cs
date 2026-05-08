using System.Reflection;
using LayerBase.Actor;
using LayerBase.Core.Event;

namespace LayerBase.Test;

public struct ActorCoreEventA
{
}

public struct ActorCoreEventB
{
}

[TestFixture]
public class ActorCoreTests
{
    [Test]
    public void IActor_is_a_marker_interface()
    {
        MemberInfo[] declaredMembers = typeof(IActor).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.That(declaredMembers, Is.Empty);
    }

    [Test]
    public void IGeneratedActorMeta_is_public()
    {
        Assert.That(typeof(IGeneratedActorMeta).IsPublic, Is.True);
    }

    [Test]
    public void ActorContext_constructor_is_internal()
    {
        ConstructorInfo? constructor = typeof(ActorContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(ActorWorld), typeof(ActorId) },
            modifiers: null);

        Assert.That(constructor, Is.Not.Null);
        Assert.That(constructor!.IsAssembly, Is.True);
    }

    [Test]
    public void ActorGeneratedAccess_throws_for_non_generated_actor()
    {
        var actor = new PlainActor();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ActorGeneratedAccess.RequireGenerated(actor))!;

        Assert.That(exception.Message, Does.Contain(nameof(PlainActor)));
    }

    [Test]
    public void ActorTypeMetaBuilder_collects_behaviours_and_sorts_signature()
    {
        var builder = new ActorTypeMetaBuilder();

        builder.AddBehaviour<BuilderActor, ActorCoreEventB>(static (BuilderActor _, in ActorCoreEventB _) => { });
        builder.AddBehaviour<BuilderActor, ActorCoreEventA>(static (BuilderActor _, in ActorCoreEventA _) => { });

        ActorTypeMeta<BuilderActor> meta = builder.Build<BuilderActor>();

        Assert.That(meta.Behaviours, Has.Length.EqualTo(2));
        Assert.That(meta.Signature.EventTypeIds.ToArray(), Is.Ordered.Ascending);
        Assert.That(meta.Behaviours.Select(static entry => entry.EventTypeId).ToArray(), Is.EqualTo(meta.Signature.EventTypeIds.ToArray()));
    }

    [Test]
    public void BehaviourSignature_contains_all_across_multiple_mask_words()
    {
        var full = new BehaviourSignature(new[] { 1, 64, 130 });
        var subset = new BehaviourSignature(new[] { 64, 130 });
        var missing = new BehaviourSignature(new[] { 2, 130 });

        Assert.That(full.ContainsAll(subset), Is.True);
        Assert.That(full.ContainsAll(missing), Is.False);
    }

    [Test]
    public void ActorTypeMetaCache_builds_only_once_per_actor_type()
    {
        var actor = new CachedActor();

        ActorTypeMeta<CachedActor> first = ActorTypeMetaCache.GetOrBuild<CachedActor>(actor);
        ActorTypeMeta<CachedActor> second = ActorTypeMetaCache.GetOrBuild<CachedActor>(actor);

        Assert.That(first, Is.SameAs(second));
        Assert.That(actor.BuildCount, Is.EqualTo(1));
    }

    private sealed class PlainActor : IActor
    {
    }

    private sealed class BuilderActor : IActor
    {
    }

    private sealed class CachedActor : IActor, IGeneratedActorMeta
    {
        public int BuildCount { get; private set; }

        public void __BuildActorMeta(ActorTypeMetaBuilder builder)
        {
            BuildCount++;
            builder.AddBehaviour<CachedActor, ActorCoreEventA>(static (CachedActor _, in ActorCoreEventA _) => { });
        }

        public ActorId GetId()
        {
            return default;
        }

        public void ActorInit(ActorContext context)
        {
        }

        public PostResult Post<TEvent>(in TEvent value)
            where TEvent : struct
        {
            return PostResult.Success;
        }

        public PostResult TryPost<TEvent>(in TEvent value)
            where TEvent : struct
        {
            return PostResult.Success;
        }
    }
}
