using System.Reflection;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Scope;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopePostTimerDelayMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Each_scope_has_independent_post_scheduler_timer_and_delay_manager()
    {
        using var runtime = new LayerRuntime(1801);
        using var host = CreateHost(runtime);

        InitializeScopeResources(host.MainScope, EventTypeId<ScopePostEvent>.Id);
        InitializeScopeResources(host.Scopes[1], EventTypeId<ScopePostEvent>.Id);

        Assert.That(host.Scopes[1].PostScheduler, Is.Not.SameAs(host.MainScope.PostScheduler));
        Assert.That(host.Scopes[1].Timer, Is.Not.SameAs(host.MainScope.Timer));
        Assert.That(host.Scopes[1].DelayManager, Is.Not.SameAs(host.MainScope.DelayManager));
    }

    [Test]
    public void Custom_scope_service_delay_uses_owner_scope_delay_manager()
    {
        using var runtime = new LayerRuntime(1802);
        using var host = CreateHost(runtime);
        var service = new ScopeDelayService();
        var main = host.MainScope;
        var custom = host.Scopes[1];

        InitializeScopeResources(main, EventTypeId<ScopeDelayEvent>.Id);
        InitializeScopeResources(custom, EventTypeId<ScopeDelayEvent>.Id);
        ServiceLayerBinder.AttachScopeRuntime(service, runtime, custom);

        service.Delay(new ScopeDelayEvent(7), 1.0f);

        Assert.That(custom.SubscribeDelay<ScopeDelayEvent>().TryGet(out var scoped), Is.True);
        Assert.That(scoped.Value, Is.EqualTo(7));
        Assert.That(main.SubscribeDelay<ScopeDelayEvent>().HasValue, Is.False);
    }

    [Test]
    public void Custom_scope_delay_expires_from_owner_scope_tick()
    {
        using var runtime = new LayerRuntime(1803);
        using var host = CreateHost(runtime);
        var service = new ScopeDelayService();
        var custom = host.Scopes[1];

        InitializeScopeResources(custom, EventTypeId<ScopeDelayEvent>.Id);
        ServiceLayerBinder.AttachScopeRuntime(service, runtime, custom);

        service.Delay(new ScopeDelayEvent(11), 0.05f);
        var publisher = custom.SubscribeDelay<ScopeDelayEvent>();

        Assert.That(publisher.HasValue, Is.True);

        custom.PumpScopeResources(0.1f);

        Assert.That(publisher.HasValue, Is.False);
    }

    [Test]
    public void Timer_fires_into_owner_scope_post_scheduler()
    {
        using var runtime = new LayerRuntime(1804);
        using var host = CreateHost(runtime);
        var service = new ScopeDelayService();
        var mainCount = 0;
        var customCount = 0;
        var main = host.MainScope;
        var custom = host.Scopes[1];

        InitializeScopeResources(main, EventTypeId<TimerLocalEvent>.Id);
        InitializeScopeResources(custom, EventTypeId<TimerLocalEvent>.Id);
        main.EventCenter.SubscribeNotify<TimerLocalEvent>(0, (in TimerLocalEvent _) => mainCount++);
        custom.EventCenter.SubscribeNotify<TimerLocalEvent>(0, (in TimerLocalEvent _) => customCount++);
        ServiceLayerBinder.AttachScopeRuntime(service, runtime, custom);

        service.SchedulePost(new TimerLocalEvent(3), 0.01f);
        custom.TickTimer(0.02f);
        custom.PostScheduler!.Pump();

        Assert.That(mainCount, Is.EqualTo(0));
        Assert.That(customCount, Is.EqualTo(1));
    }

    [Test]
    public void Latest_state_is_isolated_per_scope()
    {
        using var runtime = new LayerRuntime(1805);
        using var host = CreateHost(runtime);
        var mainValues = new List<int>();
        var customValues = new List<int>();
        var main = host.MainScope;
        var custom = host.Scopes[1];
        var latestPolicy = new EventPostPolicy(
            PostDeliveryMode.Latest,
            BackpressurePolicy.RejectNew,
            maxPending: 1);

        InitializeScopeResources(main, EventTypeId<LatestLocalEvent>.Id);
        InitializeScopeResources(custom, EventTypeId<LatestLocalEvent>.Id);
        main.EventCenter.SubscribeNotify<LatestLocalEvent>(0, (in LatestLocalEvent value) => mainValues.Add(value.Value));
        custom.EventCenter.SubscribeNotify<LatestLocalEvent>(0, (in LatestLocalEvent value) => customValues.Add(value.Value));

        main.PostScheduler!.TryPost(new LatestLocalEvent(1));
        main.PostScheduler.TryPost(new LatestLocalEvent(2));
        custom.PostScheduler!.TryPost(new LatestLocalEvent(10));
        custom.PostScheduler.TryPost(new LatestLocalEvent(20));

        main.PostScheduler.Pump();
        main.PostScheduler.Pump();
        custom.PostScheduler.Pump();
        custom.PostScheduler.Pump();

        Assert.That(mainValues, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(customValues, Is.EqualTo(new[] { 10, 20 }));
    }

    [Test]
    public void Post_scheduler_has_no_cross_thread_ingress_api()
    {
        var members = typeof(PostScheduler)
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Select(static member => member.Name)
            .ToArray();

        Assert.That(members, Has.No.Contains("CrossThreadIngress"));
        Assert.That(members, Has.No.Contains("PostIngressQueue"));
        Assert.That(members, Has.No.Contains("PostFromAnyThread"));
        Assert.That(members, Has.No.Contains("TryPostFromAnyThread"));
    }

    [Test]
    public void Scope_post_endpoint_type_does_not_exist()
    {
        var forbidden = typeof(ScopeRuntime).Assembly
            .GetTypes()
            .Where(static type => type.Name.Contains("ScopePostEndpoint", StringComparison.Ordinal))
            .ToArray();

        Assert.That(forbidden, Is.Empty);
    }

    private static ScopeRuntimeHost CreateHost(LayerRuntime runtime)
    {
        return ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(ScopePostTimerDelayScope.ScopeId, nameof(ScopePostTimerDelayScope), typeof(ScopePostTimerDelayScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);
    }

    private static void InitializeScopeResources(ScopeRuntime scope, params int[] eventTypeIds)
    {
        var plans = eventTypeIds
            .Distinct()
            .Select(static eventTypeId => PostTypePlan.Default(eventTypeId, BackpressurePolicy.RejectNew))
            .ToArray();

        scope.InitializeOrUpdateScheduler(PostSchedulerOptions.Default, new EventBuildPolicyTable(), plans);
        scope.InitializeTimer(TimeSchedulerOptions.Default);
        scope.InitializeDelay(DelayBufferOptions.Default);
        scope.RunRuntimeStartOnOwnerThread();
    }

    private sealed class ScopeDelayService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class ScopePostTimerDelayScope : IScopeDefinition
    {
        public const int ScopeId = 18;
        public ScopeOptions Options => ScopeOptions.Inline;
        
    }

    private readonly struct ScopePostEvent
    {
    }

    private readonly struct ScopeDelayEvent
    {
        public ScopeDelayEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct TimerLocalEvent
    {
        public TimerLocalEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private readonly struct LatestLocalEvent
    {
        public LatestLocalEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
