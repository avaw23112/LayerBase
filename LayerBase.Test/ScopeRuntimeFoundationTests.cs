using Arch.Core;
using LayerBase.Async;
using LayerBase.Core.DataStruct;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Flow;
using LayerBase.ECS.Runtime;
using LayerBase.Layers;
using LayerBase.Scope;
using ActorBehaviourAttribute = LayerBase.Actor.ActorBehaviourAttribute;
using IPooledActor = LayerBase.Actor.IPooledActor;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeRuntimeFoundationTests
{
    [Test]
    public void LocalRingQueue_should_keep_fixed_capacity_and_wrap()
    {
        var queue = new LocalRingQueue<int>(2);

        Assert.That(queue.TryEnqueue(1), Is.True);
        Assert.That(queue.TryEnqueue(2), Is.True);
        Assert.That(queue.TryEnqueue(3), Is.False);
        Assert.That(queue.Capacity, Is.EqualTo(2));
        Assert.That(queue.Count, Is.EqualTo(2));

        Assert.That(queue.TryDequeue(out int first), Is.True);
        Assert.That(first, Is.EqualTo(1));
        Assert.That(queue.TryEnqueue(3), Is.True);

        Assert.That(queue.TryDequeue(out int second), Is.True);
        Assert.That(second, Is.EqualTo(2));
        Assert.That(queue.TryDequeue(out int third), Is.True);
        Assert.That(third, Is.EqualTo(3));
        Assert.That(queue.TryDequeue(out _), Is.False);
    }

    [Test]
    public void LockedBoundedRingQueue_should_reject_when_full_and_clear_references()
    {
        var queue = new LockedBoundedRingQueue<object>(1);
        var payload = new object();

        Assert.That(queue.TryEnqueue(payload), Is.True);
        Assert.That(queue.TryEnqueue(new object()), Is.False);
        Assert.That(queue.Count, Is.EqualTo(1));

        queue.Clear();

        Assert.That(queue.Count, Is.EqualTo(0));
        Assert.That(queue.TryDequeue(out _), Is.False);
        Assert.That(queue.TryEnqueue(new object()), Is.True);
    }

    [Test]
    public void ScopeOptionsAttribute_should_expose_runtime_policy()
    {
        var attribute = new ScopeOptionsAttribute(
            threading: ScopeThreadingMode.Worker,
            clock: ScopeClockMode.FixedRate,
            tickRateHz: 60,
            stopPolicy: ScopeStopPolicy.Drop);

        Assert.That(attribute.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(attribute.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(attribute.TickRateHz, Is.EqualTo(60));
        Assert.That(attribute.StopPolicy, Is.EqualTo(ScopeStopPolicy.Drop));
    }

    [Test]
    public void ScopeOptionsAttribute_should_reject_negative_tick_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScopeOptionsAttribute(tickRateHz: -1));
    }

    [Test]
    public void ScopeDescriptor_should_reject_negative_tick_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ScopeDescriptor(
            scopeId: 1,
            name: "CombatScope",
            threading: ScopeThreadingMode.Inline,
            clock: ScopeClockMode.EngineDriven,
            tickRateHz: -1,
            stopPolicy: ScopeStopPolicy.Drain));
    }

    [Test]
    public void ScopeRuntime_should_bind_services_and_own_independent_runtime_resources()
    {
        var service = new ScopeProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 3,
                name: "CombatScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();
        Entity entity = runtime.EcsWorld.Create(new ScopeProbeComponent { Value = 7 });

        Assert.That(runtime.ScopeId, Is.EqualTo(3));
        Assert.That(runtime.Services[0], Is.SameAs(service));
        Assert.That(service.OwnerScope, Is.SameAs(runtime));
        Assert.That(service.ServiceId, Is.EqualTo(0));
        Assert.That(service.InitializeCount, Is.EqualTo(1));
        Assert.That(runtime.EventCenter, Is.Not.Null);
        Assert.That(runtime.EcsWorld.IsAlive(entity), Is.True);
    }

    [Test]
    public void IService_ecs_access_should_resolve_owner_scope_resources()
    {
        var service = new ScopeProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 4,
                name: "EcsScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        Assert.That(service.ECSWorld(), Is.SameAs(runtime.EcsWorld));
        Assert.That(service.ECSQueryRegistry(), Is.SameAs(runtime.EcsQueryRegistry));
    }

    [Test]
    public void IService_query_should_execute_against_owner_scope_world()
    {
        var service = new ScopeProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 5,
                name: "QueryScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();
        Entity entity = runtime.EcsWorld.Create(new ScopeProbeComponent { Value = 7 });
        var job = new ScopeIncrementJob(5);

        service.Query<ScopeProbeComponent>().ForEach(ref job);

        Assert.That(runtime.EcsWorld.Get<ScopeProbeComponent>(entity).Value, Is.EqualTo(12));
    }

    [Test]
    public void IService_actor_and_projected_actor_outbox_should_resolve_owner_scope()
    {
        ScopeProjectedActor.Reset();
        var service = new ScopeActorProjectionService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 18,
                name: "ActorProjectionScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();

        Assert.That(service.Actors(), Is.SameAs(runtime.Actors));

        service.PostProjectedEvent(9);
        Assert.That(ScopeProjectedActor.Received, Is.Empty);

        runtime.Pump(0.016f);

        Assert.That(ScopeProjectedActor.Received, Has.Count.EqualTo(1));
        Assert.That(ScopeProjectedActor.Received[0].Value, Is.EqualTo(9));
        Assert.That(runtime.EcsWorld.GetProjectionMeta(service.Entity).ActorId.IsValid, Is.True);
    }

    [Test]
    public void ScopeRuntime_post_should_defer_dispatch_until_pump_inside_scope_execution()
    {
        int dispatchCount = 0;
        int dispatchScopeId = -1;
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            Array.Empty<IService>(),
            postDispatcher: (scope, message) =>
            {
                dispatchCount++;
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(message.EventId, Is.EqualTo(12));
                Assert.That(message.Payload, Is.EqualTo("payload"));
                Assert.That(scope.ScopeId, Is.EqualTo(ScopeDescriptors.Main.ScopeId));
            });

        Assert.That(runtime.PostInboxKind, Is.EqualTo(ScopeInboxKind.Local));
        Assert.That(runtime.TryPost(new ScopePostMessage(12, "payload")), Is.True);
        Assert.That(dispatchCount, Is.EqualTo(0));

        runtime.Pump(0.016f);

        Assert.That(dispatchCount, Is.EqualTo(1));
        Assert.That(dispatchScopeId, Is.EqualTo(ScopeDescriptors.Main.ScopeId));
    }

    [Test]
    public void ScopeRuntime_worker_scope_should_use_locked_bounded_post_queue()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 5,
                name: "CombatScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 60,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            new ScopeRuntimeOptions(postQueueCapacity: 2));

        Assert.That(runtime.PostInboxKind, Is.EqualTo(ScopeInboxKind.Locked));
        Assert.That(runtime.TryPost(new ScopePostMessage(1, "a")), Is.True);
        Assert.That(runtime.TryPost(new ScopePostMessage(2, "b")), Is.True);
        Assert.That(runtime.TryPost(new ScopePostMessage(3, "c")), Is.False);
        Assert.That(runtime.PostInboxCount, Is.EqualTo(2));
    }

    [Test]
    public void ScopeRuntime_worker_scope_should_initialize_services_on_worker_thread()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var service = new WorkerProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 8,
                name: "WorkerInitScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();

        Assert.That(service.Initialized.Wait(TimeSpan.FromSeconds(2)), Is.True);
        runtime.Stop();

        Assert.That(service.InitializeThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(service.InitializeScopeId, Is.EqualTo(8));
        Assert.That(service.OwnerScope, Is.SameAs(runtime));
    }

    [Test]
    public void ScopeRuntime_worker_scope_should_process_posts_without_external_pump()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var dispatched = new ManualResetEventSlim();
        int dispatchThreadId = -1;
        int dispatchScopeId = -1;

        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 9,
                name: "WorkerPostScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            postDispatcher: (_, message) =>
            {
                dispatchThreadId = Thread.CurrentThread.ManagedThreadId;
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(message.EventId, Is.EqualTo(77));
                Assert.That(message.Payload, Is.EqualTo("worker"));
                dispatched.Set();
            });

        runtime.Start();
        Assert.That(runtime.TryPost(new ScopePostMessage(77, "worker")), Is.True);

        Assert.That(dispatched.Wait(TimeSpan.FromSeconds(2)), Is.True);
        runtime.Stop();

        Assert.That(dispatchThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(dispatchScopeId, Is.EqualTo(9));
    }

    [Test]
    public void ScopeRuntime_worker_fixed_rate_scope_should_update_on_worker_thread()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var service = new WorkerUpdateProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 19,
                name: "WorkerFixedRateScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();

        Assert.That(service.Updated.Wait(TimeSpan.FromSeconds(2)), Is.True);
        runtime.Stop();

        Assert.That(service.UpdateThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(service.UpdateScopeId, Is.EqualTo(19));
    }

    [Test]
    public void ScopeRuntime_worker_realtime_scope_should_update_on_worker_thread()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var service = new WorkerUpdateProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 20,
                name: "WorkerRealtimeScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.Realtime,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();

        Assert.That(service.Updated.Wait(TimeSpan.FromSeconds(2)), Is.True);
        runtime.Stop();

        Assert.That(service.UpdateThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(service.UpdateScopeId, Is.EqualTo(20));
    }

    [Test]
    public void ScopeRuntime_worker_manual_scope_should_only_pump_when_requested()
    {
        using var dispatched = new ManualResetEventSlim();
        int dispatchScopeId = -1;
        int dispatchThreadId = -1;
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;

        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 21,
                name: "WorkerManualScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.Manual,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            postDispatcher: (_, message) =>
            {
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                dispatchThreadId = Thread.CurrentThread.ManagedThreadId;
                Assert.That(message.EventId, Is.EqualTo(88));
                dispatched.Set();
            });

        runtime.Start();
        Assert.That(runtime.TryPost(new ScopePostMessage(88, "manual")), Is.True);
        Assert.That(dispatched.Wait(TimeSpan.FromMilliseconds(80)), Is.False);

        runtime.Pump(0.016f);

        Assert.That(dispatched.Wait(TimeSpan.FromSeconds(2)), Is.True);
        runtime.Stop();

        Assert.That(dispatchScopeId, Is.EqualTo(21));
        Assert.That(dispatchThreadId, Is.Not.EqualTo(callerThreadId));
    }

    [Test]
    public void ScopeRuntime_stop_should_reject_new_pending_work()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 10,
                name: "StoppedScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drop),
            Array.Empty<IService>());

        runtime.Start();
        runtime.Stop();

        Assert.That(runtime.TryPost(new ScopePostMessage(1, "stopped")), Is.False);
        Assert.That(runtime.TryEnqueueContinuation(() => { }), Is.False);
    }

    [Test]
    public void ScopeRuntime_worker_scope_should_dispose_services_on_worker_thread_when_stopped()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        var service = new WorkerLifecycleProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 13,
                name: "WorkerStopScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        runtime.Start();
        Assert.That(service.Initialized.Wait(TimeSpan.FromSeconds(2)), Is.True);

        runtime.Stop();

        Assert.That(service.Disposed.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(service.DisposeThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(service.DisposeScopeId, Is.EqualTo(13));
    }

    [Test]
    public void ScopeRuntime_drop_stop_policy_should_clear_pending_work_without_dispatching()
    {
        int dispatchCount = 0;
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 6,
                name: "DropScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drop),
            Array.Empty<IService>(),
            postDispatcher: (_, _) => dispatchCount++);

        Assert.That(runtime.TryPost(new ScopePostMessage(1, "pending")), Is.True);
        runtime.Stop();

        Assert.That(dispatchCount, Is.EqualTo(0));
        Assert.That(runtime.PostInboxCount, Is.EqualTo(0));
    }

    [Test]
    public void ScopeRuntime_continuation_should_run_inside_owner_scope()
    {
        int scopeId = -1;
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 7,
                name: "ContinuationScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());

        Assert.That(runtime.TryEnqueueContinuation(() => scopeId = ScopeExecution.Current.ScopeId), Is.True);
        runtime.Pump(0.016f);

        Assert.That(scopeId, Is.EqualTo(7));
    }

    [Test]
    public void ScopeRuntime_continuation_queue_should_be_locked_for_cross_scope_completion()
    {
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 11,
                name: "ContinuationReturnScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>());

        Assert.That(runtime.PostInboxKind, Is.EqualTo(ScopeInboxKind.Local));
        Assert.That(runtime.ContinuationInboxKind, Is.EqualTo(ScopeInboxKind.Locked));
    }

    [Test]
    public void ScopeRuntime_should_restore_lbtask_continuations_inside_owner_scope()
    {
        var service = new ScopeTaskProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 17,
                name: "TaskScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        int pumpThreadId = Thread.CurrentThread.ManagedThreadId;

        runtime.Start();
        runtime.Pump(0.016f);

        Assert.That(service.Completed, Is.False);

        runtime.Pump(0.016f);

        Assert.That(service.Completed, Is.True);
        Assert.That(service.ContinuationScopeId, Is.EqualTo(17));
        Assert.That(service.ContinuationThreadId, Is.EqualTo(pumpThreadId));
    }

    [Test]
    public void WorkerScope_should_restore_lbtask_continuations_on_worker_thread()
    {
        var service = new ScopeTaskProbeService();
        using var runtime = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 18,
                name: "WorkerTaskScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            new IService[] { service });

        int testThreadId = Thread.CurrentThread.ManagedThreadId;

        runtime.Start();

        Assert.That(service.CompletedEvent.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(service.ContinuationScopeId, Is.EqualTo(18));
        Assert.That(service.ContinuationThreadId, Is.Not.EqualTo(testThreadId));
    }

    [Test]
    public void ScopeRuntimePlanner_should_group_unscoped_services_into_main_scope()
    {
        var service = new PlannerMainService();

        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { service });

        Assert.That(plans.Count, Is.EqualTo(1));
        Assert.That(plans[0].Descriptor.ScopeId, Is.EqualTo(ScopeDescriptors.Main.ScopeId));
        Assert.That(plans[0].Descriptor.Name, Is.EqualTo(ScopeDescriptors.Main.Name));
        Assert.That(plans[0].Services, Is.EqualTo(new IService[] { service }));
    }

    [Test]
    public void ScopeRuntimePlanner_should_group_scoped_services_by_scope_attribute()
    {
        var main = new PlannerMainService();
        var firstCombat = new PlannerCombatService();
        var secondCombat = new PlannerCombatService();

        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(
            new IService[] { main, firstCombat, secondCombat });

        Assert.That(plans.Count, Is.EqualTo(2));
        Assert.That(plans[0].Descriptor.ScopeId, Is.EqualTo(0));
        Assert.That(plans[0].Services, Is.EqualTo(new IService[] { main }));

        Assert.That(plans[1].ScopeType, Is.EqualTo(typeof(PlannerCombatScope)));
        Assert.That(plans[1].Descriptor.ScopeId, Is.EqualTo(1));
        Assert.That(plans[1].Descriptor.Name, Is.EqualTo(nameof(PlannerCombatScope)));
        Assert.That(plans[1].Descriptor.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(plans[1].Descriptor.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(plans[1].Descriptor.TickRateHz, Is.EqualTo(30));
        Assert.That(plans[1].Descriptor.StopPolicy, Is.EqualTo(ScopeStopPolicy.Drop));
        Assert.That(plans[1].Services, Is.EqualTo(new IService[] { firstCombat, secondCombat }));
    }

    [Test]
    public void ScopeRuntimePlanner_should_use_resolver_scope_options_without_reflection()
    {
        var service = new PlannerResolverOnlyService();
        var descriptor = new ScopeDescriptor(
            scopeId: 42,
            name: nameof(PlannerResolverOnlyScope),
            threading: ScopeThreadingMode.Worker,
            clock: ScopeClockMode.FixedRate,
            tickRateHz: 60,
            stopPolicy: ScopeStopPolicy.Drain);

        ScopeRuntimeServiceScopeResolver resolver = (Type serviceType, out ScopeRuntimeServiceScopeInfo scopeInfo) =>
        {
            if (serviceType == typeof(PlannerResolverOnlyService))
            {
                scopeInfo = new ScopeRuntimeServiceScopeInfo(typeof(PlannerResolverOnlyScope), descriptor);
                return true;
            }

            scopeInfo = default;
            return false;
        };

        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { service }, resolver);

        Assert.That(plans.Count, Is.EqualTo(2));
        Assert.That(plans[1].ScopeType, Is.EqualTo(typeof(PlannerResolverOnlyScope)));
        Assert.That(plans[1].Descriptor.ScopeId, Is.EqualTo(42));
        Assert.That(plans[1].Descriptor.Name, Is.EqualTo(nameof(PlannerResolverOnlyScope)));
        Assert.That(plans[1].Descriptor.Threading, Is.EqualTo(ScopeThreadingMode.Worker));
        Assert.That(plans[1].Descriptor.Clock, Is.EqualTo(ScopeClockMode.FixedRate));
        Assert.That(plans[1].Descriptor.TickRateHz, Is.EqualTo(60));
        Assert.That(plans[1].Services, Is.EqualTo(new IService[] { service }));
    }

    [Test]
    public void ScopeRuntimePlanner_should_reject_scope_attribute_without_scope_options()
    {
        var service = new PlannerMissingScopeOptionsService();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ScopeRuntimePlanner.Build(new IService[] { service }))!;

        Assert.That(exception.Message, Does.Contain(nameof(PlannerMissingScopeOptions)));
    }

    [Test]
    public void ScopeRuntimeHost_should_create_scopes_and_routes_from_runtime_plans()
    {
        var main = new HostMainService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        using var host = ScopeRuntimeHost.Create(plans);

        Assert.That(host.Scopes.Count, Is.EqualTo(2));
        Assert.That(host.Routes.Count, Is.EqualTo(2));
        Assert.That(host.Routes.TryGetScope(0, out ScopeRuntime mainScope), Is.True);
        Assert.That(host.Routes.TryGetScope(1, out ScopeRuntime uiScope), Is.True);
        Assert.That(mainScope.Services, Is.EqualTo(new IService[] { main }));
        Assert.That(uiScope.Services, Is.EqualTo(new IService[] { ui }));
        Assert.That(main.OwnerScope, Is.SameAs(mainScope));
        Assert.That(ui.OwnerScope, Is.SameAs(uiScope));
        Assert.That(uiScope.Descriptor.Name, Is.EqualTo(nameof(HostUiScope)));
    }

    [Test]
    public void ScopeRuntimeHost_start_pump_stop_should_drive_inline_scopes_in_scope_execution()
    {
        var main = new HostMainService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        using var host = ScopeRuntimeHost.Create(plans);

        host.Start();
        host.Pump(0.016f);
        host.Stop();

        Assert.That(main.InitializeScopeId, Is.EqualTo(0));
        Assert.That(ui.InitializeScopeId, Is.EqualTo(1));
        Assert.That(main.UpdateScopeId, Is.EqualTo(0));
        Assert.That(ui.UpdateScopeId, Is.EqualTo(1));
        Assert.That(main.DisposeScopeId, Is.EqualTo(0));
        Assert.That(ui.DisposeScopeId, Is.EqualTo(1));
    }

    [Test]
    public void ScopeRuntimeHost_should_route_post_without_exposing_route_table()
    {
        var main = new HostMainService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        int dispatchScopeId = -1;
        int dispatchEventId = -1;
        object? dispatchPayload = null;
        using var host = ScopeRuntimeHost.Create(
            plans,
            postDispatcher: (scope, message) =>
            {
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(scope.ScopeId, Is.EqualTo(1));
                dispatchEventId = message.EventId;
                dispatchPayload = message.Payload;
            });

        Assert.That(host.TryGetScope(1, out ScopeRuntime targetScope), Is.True);
        Assert.That(targetScope.ScopeId, Is.EqualTo(1));
        Assert.That(host.TryPost(1, new ScopePostMessage(123, "ui")), Is.True);
        Assert.That(dispatchScopeId, Is.EqualTo(-1));

        host.Pump(0.016f);

        Assert.That(dispatchScopeId, Is.EqualTo(1));
        Assert.That(dispatchEventId, Is.EqualTo(123));
        Assert.That(dispatchPayload, Is.EqualTo("ui"));
    }

    [Test]
    public void ScopeRuntimeHost_should_provide_scope_ref_for_calls()
    {
        var main = new HostMainService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        int handlerScopeId = -1;
        int continuationScopeId = -1;
        int result = 0;
        using var host = ScopeRuntimeHost.Create(
            plans,
            callDispatcher: (_, message) =>
            {
                handlerScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(message.CallId, Is.EqualTo(7));
                Assert.That(message.Payload, Is.EqualTo(41));
                ((ScopePromise<int>)message.Promise).SetResult((int)message.Payload + 1);
            });

        host.Start();
        ScopePromise<int> promise;
        using (ScopeExecution.Enter(host.Scopes[0]))
        {
            promise = host.GetScopeRef<HostUiScope>(1).Call<int>(7, 41);
            promise.OnCompleted(() =>
            {
                continuationScopeId = ScopeExecution.Current.ScopeId;
                result = promise.GetResult();
            });
        }

        Assert.That(promise.IsCompleted, Is.False);
        host.Pump(0.016f);

        Assert.That(handlerScopeId, Is.EqualTo(1));
        Assert.That(continuationScopeId, Is.EqualTo(-1));

        host.Pump(0.016f);

        Assert.That(continuationScopeId, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ScopeRuntimeHost_should_bind_routes_so_services_can_get_scope_refs_from_owner_scope()
    {
        var main = new HostRoutingService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        int dispatchScopeId = -1;
        int dispatchEventId = -1;
        object? dispatchPayload = null;
        using var host = ScopeRuntimeHost.Create(
            plans,
            postDispatcher: (scope, message) =>
            {
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(scope.ScopeId, Is.EqualTo(1));
                dispatchEventId = message.EventId;
                dispatchPayload = message.Payload;
            });

        Assert.That(host.GetScopeRef<HostUiScope>().TargetScopeId, Is.EqualTo(1));
        host.Start();
        host.Pump(0.016f);

        Assert.That(main.PostAccepted, Is.True);
        Assert.That(dispatchScopeId, Is.EqualTo(1));
        Assert.That(dispatchEventId, Is.EqualTo(321));
        Assert.That(dispatchPayload, Is.EqualTo("from-service"));
    }

    [Test]
    public void ScopeRuntime_should_bind_generated_scope_service_binding()
    {
        var main = new HostGeneratedBindingRoutingService();
        var ui = new HostUiService();
        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, ui });

        int dispatchScopeId = -1;
        int dispatchEventId = -1;
        object? dispatchPayload = null;
        using var host = ScopeRuntimeHost.Create(
            plans,
            postDispatcher: (scope, message) =>
            {
                dispatchScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(scope.ScopeId, Is.EqualTo(1));
                dispatchEventId = message.EventId;
                dispatchPayload = message.Payload;
            });

        host.Start();
        host.Pump(0.016f);

        Assert.That(main.OwnerScopeId, Is.EqualTo(0));
        Assert.That(main.BoundServiceId, Is.EqualTo(0));
        Assert.That(main.PostAccepted, Is.True);
        Assert.That(dispatchScopeId, Is.EqualTo(1));
        Assert.That(dispatchEventId, Is.EqualTo(654));
        Assert.That(dispatchPayload, Is.EqualTo("from-generated-binding"));
    }

    [Test]
    public void LayerRuntime_should_create_scope_host_and_drive_scoped_services()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            Assert.That(runtime.ScopeHost, Is.Not.Null);
            Assert.That(runtime.ScopeHost!.Scopes.Count, Is.EqualTo(2));
            Assert.That(layer.ScopedService.InitializeScopeId, Is.EqualTo(1));

            runtime.Pump(0.016f);

            Assert.That(layer.ScopedService.UpdateScopeId, Is.EqualTo(1));
            Assert.That(layer.ScopedService.OwnerScope, Is.SameAs(runtime.ScopeHost.Scopes[1]));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_scoped_service_query_should_use_owner_scope_ecs_world()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            Assert.That(layer.ScopedService.ECSWorld(), Is.SameAs(runtime.ScopeHost!.Scopes[1].EcsWorld));
            Assert.That(layer.ScopedService.ECSWorld(), Is.Not.SameAs(runtime.EcsWorld));

            Entity scopedEntity = layer.ScopedService.ECSWorld().Create(new ScopeProbeComponent { Value = 3 });
            Entity mainEntity = runtime.EcsWorld.Create(new ScopeProbeComponent { Value = 30 });

            runtime.Pump(0.016f);

            Assert.That(layer.ScopedService.ECSWorld().Get<ScopeProbeComponent>(scopedEntity).Value, Is.EqualTo(8));
            Assert.That(runtime.EcsWorld.Get<ScopeProbeComponent>(mainEntity).Value, Is.EqualTo(30));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_scoped_service_auto_subscribe_should_bind_to_owner_scope_event_center()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedEventLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            runtime.EventCenter.Send(new RuntimeScopedSignalEvent(10));
            Assert.That(layer.EventService.Total, Is.EqualTo(0));

            runtime.ScopeHost!.Scopes[1].EventCenter.Send(new RuntimeScopedSignalEvent(3));
            Assert.That(layer.EventService.Total, Is.EqualTo(3));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_scoped_interface_event_handler_should_bind_to_owner_scope_event_center()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedInterfaceEventLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            runtime.EventCenter.Send(new RuntimeScopedSignalEvent(10));
            Assert.That(layer.EventService.Total, Is.EqualTo(0));

            runtime.ScopeHost!.Scopes[1].EventCenter.Send(new RuntimeScopedSignalEvent(4));
            Assert.That(layer.EventService.Total, Is.EqualTo(4));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_scoped_service_post_should_use_owner_scope_post_scheduler()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedEventLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            layer.EventService.PostScopedSignal(5);
            runtime.Scheduler.Pump();
            Assert.That(layer.EventService.Total, Is.EqualTo(0));

            runtime.ScopeHost!.Scopes[1].PostScheduler.Pump();
            Assert.That(layer.EventService.Total, Is.EqualTo(5));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_scoped_service_schedule_post_should_use_owner_scope_timer()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeScopedEventLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            layer.EventService.ScheduleScopedSignal(9, 0.05f);

            runtime.Pump(0.016f);
            Assert.That(layer.EventService.Total, Is.EqualTo(0));

            runtime.Pump(0.1f);

            Assert.That(layer.EventService.Total, Is.EqualTo(9));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_should_use_generated_scope_host_factory_dispatchers()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeGeneratedDispatchLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            ScopeRef<RuntimeGeneratedDispatchScope> scopeRef = runtime.ScopeHost!.GetScopeRef<RuntimeGeneratedDispatchScope>();
            Assert.That(scopeRef.Post(new RuntimeGeneratedDispatchEvent(6)), Is.True);

            LBTask<RuntimeGeneratedDispatchResult> callTask =
                scopeRef.Call(new RuntimeGeneratedDispatchCall(4));

            runtime.Pump(0.016f);

            Assert.That(layer.Service.EventTotal, Is.EqualTo(6));
            Assert.That(callTask.GetAwaiter().IsCompleted, Is.True);
            Assert.That(callTask.GetAwaiter().GetResult().Value, Is.EqualTo(9));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void LayerRuntime_generated_scope_call_lbtask_should_resume_inside_origin_scope()
    {
        LayerHub.Reset();
        try
        {
            var layer = new RuntimeGeneratedDispatchLayer();
            LayerRuntime runtime = LayerHub.CreateLayers()
                                           .Push(layer)
                                           .Build();

            int scopeId = runtime.ScopeHost!.Scopes[1].ScopeId;

            layer.Service.RequestAwaitSelfCall(8);
            runtime.Pump(0.016f);
            runtime.Pump(0.016f);

            Assert.That(layer.Service.AwaitAfterScopeId, Is.EqualTo(-1));

            runtime.Pump(0.016f);

            Assert.That(layer.Service.AwaitBeforeScopeId, Is.EqualTo(scopeId));
            Assert.That(layer.Service.AwaitAfterScopeId, Is.EqualTo(scopeId));
            Assert.That(layer.Service.AwaitResult, Is.EqualTo(13));
        }
        finally
        {
            LayerHub.Reset();
        }
    }

    [Test]
    public void ScopeRouteTable_should_route_post_to_target_scope_queue()
    {
        int targetDispatchCount = 0;
        using var main = new ScopeRuntime(ScopeDescriptors.Main, Array.Empty<IService>());
        using var combat = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1,
                name: "CombatScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            postDispatcher: (scope, message) =>
            {
                targetDispatchCount++;
                Assert.That(scope.ScopeId, Is.EqualTo(1));
                Assert.That(message.EventId, Is.EqualTo(99));
                Assert.That(message.Payload, Is.EqualTo("spawn"));
            });
        using var routes = new ScopeRouteTable(new[] { main, combat });

        Assert.That(routes.TryPost(1, new ScopePostMessage(99, "spawn")), Is.True);
        Assert.That(targetDispatchCount, Is.EqualTo(0));

        combat.Pump(0.016f);

        Assert.That(targetDispatchCount, Is.EqualTo(1));
    }

    [Test]
    public void ScopeRef_call_should_execute_on_target_and_continue_on_origin_scope()
    {
        int handlerScopeId = -1;
        int continuationScopeId = -1;
        int result = 0;

        using var main = new ScopeRuntime(ScopeDescriptors.Main, Array.Empty<IService>());
        using var combat = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 1,
                name: "CombatScope",
                threading: ScopeThreadingMode.Inline,
                clock: ScopeClockMode.EngineDriven,
                tickRateHz: 0,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            callDispatcher: (_, message) =>
            {
                handlerScopeId = ScopeExecution.Current.ScopeId;
                Assert.That(message.CallId, Is.EqualTo(42));
                Assert.That(message.Payload, Is.EqualTo(21));
                ((ScopePromise<int>)message.Promise).SetResult(42);
            });
        using var routes = new ScopeRouteTable(new[] { main, combat });

        ScopePromise<int> promise;
        using (ScopeExecution.Enter(main))
        {
            promise = routes.GetScopeRef<CombatScopeMarker>(1).Call<int>(42, 21);
            promise.OnCompleted(() =>
            {
                continuationScopeId = ScopeExecution.Current.ScopeId;
                result = promise.GetResult();
            });
        }

        Assert.That(promise.IsCompleted, Is.False);
        combat.Pump(0.016f);

        Assert.That(handlerScopeId, Is.EqualTo(1));
        Assert.That(continuationScopeId, Is.EqualTo(-1));

        main.Pump(0.016f);

        Assert.That(continuationScopeId, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ScopeRef_call_to_worker_scope_should_execute_on_worker_and_continue_on_origin_scope()
    {
        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        using var handlerRan = new ManualResetEventSlim();
        int handlerThreadId = -1;
        int handlerScopeId = -1;
        int continuationScopeId = -1;
        int result = 0;

        using var main = new ScopeRuntime(ScopeDescriptors.Main, Array.Empty<IService>());
        using var combat = new ScopeRuntime(
            new ScopeDescriptor(
                scopeId: 12,
                name: "WorkerCallScope",
                threading: ScopeThreadingMode.Worker,
                clock: ScopeClockMode.FixedRate,
                tickRateHz: 120,
                stopPolicy: ScopeStopPolicy.Drain),
            Array.Empty<IService>(),
            callDispatcher: (_, message) =>
            {
                handlerThreadId = Thread.CurrentThread.ManagedThreadId;
                handlerScopeId = ScopeExecution.Current.ScopeId;
                ((ScopePromise<int>)message.Promise).SetResult((int)message.Payload + 1);
                handlerRan.Set();
            });
        using var routes = new ScopeRouteTable(new[] { main, combat });

        combat.Start();
        ScopePromise<int> promise;
        using (ScopeExecution.Enter(main))
        {
            promise = routes.GetScopeRef<CombatScopeMarker>(12).Call<int>(7, 41);
            promise.OnCompleted(() =>
            {
                continuationScopeId = ScopeExecution.Current.ScopeId;
                result = promise.GetResult();
            });
        }

        Assert.That(handlerRan.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(handlerThreadId, Is.Not.EqualTo(callerThreadId));
        Assert.That(handlerScopeId, Is.EqualTo(12));
        Assert.That(continuationScopeId, Is.EqualTo(-1));

        main.Pump(0.016f);

        Assert.That(continuationScopeId, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ScopeRef_call_to_missing_scope_should_complete_with_exception()
    {
        using var main = new ScopeRuntime(ScopeDescriptors.Main, Array.Empty<IService>());
        using var routes = new ScopeRouteTable(new[] { main });

        ScopePromise<int> promise = routes.GetScopeRef<CombatScopeMarker>(4).Call<int>(1, "missing");

        Assert.That(promise.IsCompleted, Is.True);
        Assert.Throws<InvalidOperationException>(() => promise.GetResult());
    }

    private sealed class ScopeProbeService : IService, IInitializable, IServiceScopeBinding
    {
        public ScopeRuntime? OwnerScope { get; private set; }
        public int ServiceId { get; private set; } = -1;
        public int InitializeCount { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            InitializeCount++;
        }

        void IServiceScopeBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            OwnerScope = ownerScope;
            ServiceId = serviceId;
        }
    }

    private sealed class WorkerProbeService : IService, IInitializable, IServiceScopeBinding, IDisposable
    {
        public ManualResetEventSlim Initialized { get; } = new();
        public ScopeRuntime? OwnerScope { get; private set; }
        public int InitializeThreadId { get; private set; } = -1;
        public int InitializeScopeId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            InitializeThreadId = Thread.CurrentThread.ManagedThreadId;
            InitializeScopeId = ScopeExecution.Current.ScopeId;
            Initialized.Set();
        }

        public void Dispose()
        {
            Initialized.Dispose();
        }

        void IServiceScopeBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            OwnerScope = ownerScope;
        }
    }

    private sealed class WorkerLifecycleProbeService : IService, IInitializable, IDisposable
    {
        public ManualResetEventSlim Initialized { get; } = new();
        public ManualResetEventSlim Disposed { get; } = new();
        public int DisposeThreadId { get; private set; } = -1;
        public int DisposeScopeId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            Initialized.Set();
        }

        public void Dispose()
        {
            DisposeThreadId = Thread.CurrentThread.ManagedThreadId;
            DisposeScopeId = ScopeExecution.Current.ScopeId;
            Disposed.Set();
        }
    }

    private sealed class WorkerUpdateProbeService : IService, IUpdate, IDisposable
    {
        public ManualResetEventSlim Updated { get; } = new();
        public int UpdateThreadId { get; private set; } = -1;
        public int UpdateScopeId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            UpdateThreadId = Thread.CurrentThread.ManagedThreadId;
            UpdateScopeId = ScopeExecution.Current.ScopeId;
            Updated.Set();
        }

        public void Dispose()
        {
            Updated.Dispose();
        }
    }

    private sealed class ScopeTaskProbeService : IService, IUpdate
    {
        private bool _started;

        public bool Completed { get; private set; }

        public ManualResetEventSlim CompletedEvent { get; } = new();

        public int ContinuationScopeId { get; private set; } = -1;

        public int ContinuationThreadId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            RunAsync().Forget();
        }

        private async LBTask RunAsync()
        {
            await LBTask.NextFrame();
            ContinuationScopeId = ScopeExecution.Current.ScopeId;
            ContinuationThreadId = Thread.CurrentThread.ManagedThreadId;
            Completed = true;
            CompletedEvent.Set();
        }
    }

    private sealed class PlannerMainService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    [ScopeOptions(
        threading: ScopeThreadingMode.Worker,
        clock: ScopeClockMode.FixedRate,
        tickRateHz: 30,
        stopPolicy: ScopeStopPolicy.Drop)]
    private sealed class PlannerCombatScope
    {
    }

    [Scope<PlannerCombatScope>]
    private sealed class PlannerCombatService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class PlannerResolverOnlyScope
    {
    }

    [Scope<PlannerResolverOnlyScope>]
    private sealed class PlannerResolverOnlyService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class PlannerMissingScopeOptions
    {
    }

    [Scope<PlannerMissingScopeOptions>]
    private sealed class PlannerMissingScopeOptionsService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    [ScopeOptions(
        threading: ScopeThreadingMode.Inline,
        clock: ScopeClockMode.EngineDriven,
        tickRateHz: 0,
        stopPolicy: ScopeStopPolicy.Drain)]
    private sealed class HostUiScope
    {
    }

    private sealed class HostMainService : HostLifecycleService
    {
    }

    [Scope<HostUiScope>]
    private sealed class HostUiService : HostLifecycleService
    {
    }

    private sealed class HostRoutingService : IService, IUpdate, IServiceScopeBinding
    {
        private ScopeRuntime? _ownerScope;
        private bool _posted;

        public bool PostAccepted { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            if (_posted)
            {
                return;
            }

            _posted = true;
            PostAccepted = _ownerScope!.GetScopeRef<HostUiScope>().TryPost(321, "from-service");
        }

        void IServiceScopeBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            _ownerScope = ownerScope;
        }
    }

    private sealed class HostGeneratedBindingRoutingService : IService, IUpdate, IGeneratedScopeServiceBinding
    {
        private ScopeRuntime? _ownerScope;
        private bool _posted;

        public int OwnerScopeId => _ownerScope!.ScopeId;

        public int BoundServiceId { get; private set; } = -1;

        public bool PostAccepted { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Update()
        {
            if (_posted)
            {
                return;
            }

            _posted = true;
            PostAccepted = _ownerScope!.GetScopeRef<HostUiScope>().TryPost(654, "from-generated-binding");
        }

        void IGeneratedScopeServiceBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            _ownerScope = ownerScope;
            BoundServiceId = serviceId;
        }
    }

    private sealed class RuntimeScopedLayer : Layer
    {
        public RuntimeScopedLayer()
        {
            ScopedService = new RuntimeScopedService();
            RegisterService(typeof(RuntimeScopedService), ScopedService);
        }

        public RuntimeScopedService ScopedService { get; }
    }

    [ScopeOptions]
    private sealed class RuntimeScopedCombatScope
    {
    }

    [Scope<RuntimeScopedCombatScope>]
    private sealed class RuntimeScopedService : IService, IInitializable, IUpdate, IServiceScopeBinding
    {
        public int InitializeScopeId { get; private set; } = -1;

        public int UpdateScopeId { get; private set; } = -1;

        public ScopeRuntime? OwnerScope { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            InitializeScopeId = ScopeExecution.Current.ScopeId;
        }

        public void Update()
        {
            UpdateScopeId = ScopeExecution.Current.ScopeId;
            var job = new ScopeIncrementJob(5);
            this.Query<ScopeProbeComponent>().ForEach(ref job);
        }

        void IServiceScopeBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            OwnerScope = ownerScope;
        }
    }

    private abstract class HostLifecycleService : IService, IInitializable, IUpdate, IDisposable, IServiceScopeBinding
    {
        public ScopeRuntime? OwnerScope { get; private set; }
        public int InitializeScopeId { get; private set; } = -1;
        public int UpdateScopeId { get; private set; } = -1;
        public int DisposeScopeId { get; private set; } = -1;

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Initialize()
        {
            InitializeScopeId = ScopeExecution.Current.ScopeId;
        }

        public void Update()
        {
            UpdateScopeId = ScopeExecution.Current.ScopeId;
        }

        public void Dispose()
        {
            DisposeScopeId = ScopeExecution.Current.ScopeId;
        }

        void IServiceScopeBinding.BindScope(ScopeRuntime ownerScope, int serviceId)
        {
            OwnerScope = ownerScope;
        }
    }

    private struct ScopeProbeComponent : IComponent
    {
        public int Value;
    }

    private readonly struct ScopeIncrementJob : IQueryJob<ScopeProbeComponent>
    {
        private readonly int _amount;

        public ScopeIncrementJob(int amount)
        {
            _amount = amount;
        }

        public void Execute(Entity entity, ref ScopeProbeComponent component)
        {
            component.Value += _amount;
        }
    }

    private sealed class CombatScopeMarker
    {
    }
}

internal sealed class ScopeActorProjectionService : IService
{
    public Entity Entity { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void PostProjectedEvent(int value)
    {
        Entity = this.ECSWorld().Create(
            new ScopeProjectedComponent { Value = value },
            new ProjectedActorRef());
        this.ECSWorld().WithProjectedActor<ScopeProjectedActor>(
            Entity,
            keepAliveSeconds: 0.5f);

        this.Query<ScopeProjectedComponent>()
            .Bring<ScopeProjectedEvent>()
            .ForEach(static (
                in Entity _,
                ref ScopeProjectedComponent component,
                ref ScopeProjectedEvent output) =>
            {
                output = new ScopeProjectedEvent(component.Value);
            })
            .Post();
    }
}

internal struct ScopeProjectedComponent : IComponent
{
    public int Value;
}

internal readonly struct ScopeProjectedEvent : IActorEvent
{
    public ScopeProjectedEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

internal sealed partial class ScopeProjectedActor : IPooledActor
{
    public static List<ScopeProjectedEvent> Received { get; } = new();

    public static void Reset()
    {
        Received.Clear();
    }

    [ActorBehaviour]
    private void OnProjected(in ScopeProjectedEvent value)
    {
        Received.Add(value);
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

internal sealed class RuntimeScopedEventLayer : Layer
{
    public RuntimeScopedEventLayer()
    {
        EventService = new RuntimeScopedEventService();
        RegisterService(typeof(RuntimeScopedEventService), EventService);
    }

    public RuntimeScopedEventService EventService { get; }
}

internal sealed class RuntimeScopedInterfaceEventLayer : Layer
{
    public RuntimeScopedInterfaceEventLayer()
    {
        EventService = new RuntimeScopedInterfaceEventService();
        RegisterService(typeof(RuntimeScopedInterfaceEventService), EventService);
    }

    public RuntimeScopedInterfaceEventService EventService { get; }
}

[ScopeOptions]
internal sealed partial class RuntimeScopedEventScope
{
}

internal readonly struct RuntimeScopedSignalEvent
{
    public RuntimeScopedSignalEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[Scope<RuntimeScopedEventScope>]
internal sealed partial class RuntimeScopedEventService : IService
{
    public int Total { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void PostScopedSignal(int value)
    {
        this.Post(new RuntimeScopedSignalEvent(value));
    }

    public TimerHandle ScheduleScopedSignal(int value, float delaySeconds)
    {
        return this.SchedulePost(new RuntimeScopedSignalEvent(value), delaySeconds);
    }

    [Subscribe]
    public void OnSignal(in RuntimeScopedSignalEvent value)
    {
        Total += value.Value;
    }
}

[Scope<RuntimeScopedEventScope>]
internal sealed class RuntimeScopedInterfaceEventService : IService, IEventHandler<RuntimeScopedSignalEvent>
{
    public int Total { get; private set; }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Deal(in RuntimeScopedSignalEvent value)
    {
        Total += value.Value;
    }
}

public sealed class RuntimeGeneratedDispatchLayer : Layer
{
    public RuntimeGeneratedDispatchLayer()
    {
        Service = new RuntimeGeneratedDispatchService();
        RegisterService(typeof(RuntimeGeneratedDispatchService), Service);
    }

    public RuntimeGeneratedDispatchService Service { get; }
}

[ScopeOptions]
public sealed partial class RuntimeGeneratedDispatchScope
{
}

[ScopeEvent<RuntimeGeneratedDispatchScope>]
public readonly struct RuntimeGeneratedDispatchEvent
{
    public RuntimeGeneratedDispatchEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public readonly struct RuntimeGeneratedDispatchResult
{
    public RuntimeGeneratedDispatchResult(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[ScopeCall<RuntimeGeneratedDispatchScope, RuntimeGeneratedDispatchResult>]
public readonly struct RuntimeGeneratedDispatchCall
{
    public RuntimeGeneratedDispatchCall(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[Scope<RuntimeGeneratedDispatchScope>]
public sealed partial class RuntimeGeneratedDispatchService : IService, IUpdate
{
    private bool _awaitRequested;
    private int _awaitValue;

    public int EventTotal { get; private set; }

    public int AwaitBeforeScopeId { get; private set; } = -1;

    public int AwaitAfterScopeId { get; private set; } = -1;

    public int AwaitResult { get; private set; } = -1;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void RequestAwaitSelfCall(int value)
    {
        _awaitValue = value;
        _awaitRequested = true;
        AwaitBeforeScopeId = -1;
        AwaitAfterScopeId = -1;
        AwaitResult = -1;
    }

    public void Update()
    {
        if (!_awaitRequested)
        {
            return;
        }

        _awaitRequested = false;
        _ = AwaitSelfCallAsync(_awaitValue);
    }

    private async LBTask AwaitSelfCallAsync(int value)
    {
        AwaitBeforeScopeId = ScopeExecution.Current.ScopeId;
        RuntimeGeneratedDispatchResult result = await Scope<RuntimeGeneratedDispatchScope>()
            .Call(new RuntimeGeneratedDispatchCall(value));
        AwaitAfterScopeId = ScopeExecution.Current.ScopeId;
        AwaitResult = result.Value;
    }

    [ScopeEvent]
    private void OnDispatchEvent(RuntimeGeneratedDispatchEvent message)
    {
        EventTotal += message.Value;
    }

    [ScopeCall]
    private RuntimeGeneratedDispatchResult OnDispatchCall(RuntimeGeneratedDispatchCall call)
    {
        return new RuntimeGeneratedDispatchResult(call.Value + 5);
    }
}
