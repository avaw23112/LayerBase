using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class EventCenterScopeMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Each_scope_owns_independent_event_center()
    {
        using var runtime = new LayerRuntime(1701);
        using var host = CreateHost(runtime);

        Assert.That(host.Scopes[1].EventCenter, Is.Not.SameAs(host.MainScope.EventCenter));
        Assert.That(host.Scopes[1].PostScheduler, Is.Null);
    }

    [Test]
    public void Custom_scope_does_not_fallback_to_main_event_center()
    {
        using var runtime = new LayerRuntime(1702);
        using var host = CreateHost(runtime);
        var service = new ScopedEventService();
        var mainCount = 0;
        var scopedCount = 0;

        host.MainScope.EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => mainCount++);
        host.Scopes[1].EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => scopedCount++);
        ServiceLayerBinder.AttachScopeRuntime(service, runtime, host.Scopes[1]);

        service.Send(new ScopedEvent());

        Assert.That(mainCount, Is.EqualTo(0));
        Assert.That(scopedCount, Is.EqualTo(1));
    }

    [Test]
    public void Same_event_type_in_different_scopes_is_isolated()
    {
        using var runtime = new LayerRuntime(1703);
        using var host = CreateHost(runtime);
        var mainCount = 0;
        var scopedCount = 0;

        host.MainScope.EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => mainCount++);
        host.Scopes[1].EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => scopedCount++);

        host.MainScope.EventCenter.Send(new ScopedEvent());
        host.Scopes[1].EventCenter.Send(new ScopedEvent());

        Assert.That(mainCount, Is.EqualTo(1));
        Assert.That(scopedCount, Is.EqualTo(1));
    }

    [Test]
    public void Scope_post_dispatches_through_owner_event_center()
    {
        using var runtime = new LayerRuntime(1704);
        using var host = CreateHost(runtime);
        var service = new ScopedEventService();
        var mainCount = 0;
        var scopedCount = 0;
        var scoped = host.Scopes[1];

        host.MainScope.EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => mainCount++);
        scoped.EventCenter.SubscribeNotify<ScopedEvent>(0, (in ScopedEvent _) => scopedCount++);
        scoped.InitializeOrUpdateScheduler(
            PostSchedulerOptions.Default,
            new EventBuildPolicyTable(),
            new[] { PostTypePlan.Default(EventTypeId<ScopedEvent>.Id, BackpressurePolicy.RejectNew) });
        ServiceLayerBinder.AttachScopeRuntime(service, runtime, scoped);

        Assert.That(service.Post(new ScopedEvent()).IsSuccess, Is.True);
        scoped.PostScheduler!.Pump();

        Assert.That(mainCount, Is.EqualTo(0));
        Assert.That(scopedCount, Is.EqualTo(1));
    }

    [Test]
    public void Layer_manual_subscription_uses_original_push_layer_index_order()
    {
        var trace = new List<string>();
        var first = new OrderedLayer("first", trace);
        var second = new OrderedLayer("second", trace);
        var runtime = LayerHub.CreateLayers()
                              .Push(first)
                              .Push(second)
                              .Build();

        first.SubscribeFlow<OrderedEvent>(first.Handle);
        second.SubscribeFlow<OrderedEvent>(second.Handle);

        first.Send(new OrderedEvent());

        Assert.That(trace, Is.EqualTo(new[] { "first:0", "second:1" }));
    }

    [Test]
    public void No_scope_subscription_plan_or_handler_range_is_retained()
    {
        var forbiddenTypes = typeof(LayerRuntime).Assembly
            .GetTypes()
            .Select(static type => type.Name)
            .Where(static name =>
                name.Contains("ScopeSubscriptionPlan", StringComparison.Ordinal) ||
                name.Contains("HandlerRange", StringComparison.Ordinal) ||
                name.Contains("ObjectSlotDispatch", StringComparison.Ordinal))
            .ToArray();

        Assert.That(forbiddenTypes, Is.Empty);
    }

    private static ScopeRuntimeHost CreateHost(LayerRuntime runtime)
    {
        return ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(ScopedEventScope.ScopeId, nameof(ScopedEventScope), typeof(ScopedEventScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);
    }

    private sealed class ScopedEventService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private readonly struct ScopedEvent
    {
    }

    private readonly struct ScopedEventScope : IScopeDefinition
    {
        public const int ScopeId = 17;
    }

    private readonly struct OrderedEvent
    {
    }

    private sealed class OrderedLayer : Layer
    {
        private readonly string _name;
        private readonly List<string> _trace;

        public OrderedLayer(string name, List<string> trace)
        {
            _name = name;
            _trace = trace;
        }

        public EventHandledState Handle(in OrderedEvent value)
        {
            _trace.Add($"{_name}:{RouteIndex}");
            return EventHandledState.Continue;
        }
    }
}
