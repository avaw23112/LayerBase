using LayerBase;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Runtime.Submission;
using LayerBase.Event.Delay;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Scope.Resources;
using NUnit.Framework;
using Arch.Core;
using System.Reflection;

namespace EventsTest;

public partial struct DropNewestMaxPendingEvent
{
    public int Value;
}

public partial struct QueuedReferencePayloadEvent
{
    public object? Value;
}

public partial struct ParallelRegressionEvent
{
    public int Value;
}

public partial struct InitDelayRegressionEvent
{
    public int Value;
}

public partial struct DelayOverflowRegressionEvent
{
    public int Value;
}

public partial struct LatestThrowingPayloadEvent
{
    public object? Value;
}

public class DropNewestMaxPendingEventMetaData : EventMetaData<DropNewestMaxPendingEvent>
{
    public override EventPostPolicy? PostPolicy =>
        new(PostDeliveryMode.Normal, BackpressurePolicy.DropNewest, maxPending: 2);
}

[TestFixture]
public partial class RuntimeSafetyRegressionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void PostScheduler_Dispose_releases_payloads_left_in_normal_queues()
    {
        var weak = CreateQueuedPayloadAndDispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(weak.IsAlive, Is.False);
    }

    private static WeakReference CreateQueuedPayloadAndDispose()
    {
        var options = new PostSchedulerOptions(readyCapacity: 8,
            nextCapacity: 8,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);
        var center = new EventCenter();
        var scheduler = new PostScheduler(251, center, options, new EventBuildPolicyTable(options.DefaultBackpressure));
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(EventTypeId<QueuedReferencePayloadEvent>.Id,
                PostDeliveryMode.Normal,
                options.DefaultBackpressure,
                maxPending: 0,
                options.DefaultBackpressure)
        });

        object payload = new byte[1024 * 1024];
        var weak = new WeakReference(payload);
        scheduler.TryPost(new QueuedReferencePayloadEvent { Value = payload });
        payload = null!;
        scheduler.Dispose();
        return weak;
    }

    [Test]
    public void Reset_disposes_existing_layer_scoped_services()
    {
        DisposableProbeService.DisposeCount = 0;

        var layer = new DisposableProbeLayer();
        layer.RegisterService(new DisposableProbeRegistrar());
        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        Assert.That(DisposableProbeService.DisposeCount, Is.EqualTo(0));

        LayerHub.Reset();

        Assert.That(DisposableProbeService.DisposeCount, Is.EqualTo(1),
            "Reset should dispose the previous layer chain before clearing global state.");
    }

    [Test]
    public void Reset_disposes_all_tracked_runtimes_before_reusing_runtime_ids()
    {
        DisposableProbeService.DisposeCount = 0;

        var first = new DisposableProbeLayer();
        first.RegisterService(new DisposableProbeRegistrar());
        LayerHub.CreateLayers().Push(first).Build();

        var second = new DisposableProbeLayer();
        second.RegisterService(new DisposableProbeRegistrar());
        LayerHub.CreateLayers().Push(second).Build();

        LayerHub.Reset();

        Assert.That(DisposableProbeService.DisposeCount, Is.EqualTo(2));
    }

    [Test]
    public void LayersBuilder_rejects_repeated_build_and_push_after_build()
    {
        var builder = LayerHub.CreateLayers().Push(new EmptyRegressionLayer());
        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.Push(new EmptyRegressionLayer()));
    }

    [Test]
    public void Latest_flush_releases_payload_when_handler_throws()
    {
        var weak = CreateLatestPayloadAndThrowDuringFlush();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(weak.IsAlive, Is.False);
    }

    private static WeakReference CreateLatestPayloadAndThrowDuringFlush()
    {
        var options = new PostSchedulerOptions(readyCapacity: 8,
            nextCapacity: 8,
            maxEventsPerPump: 16,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);
        var center = new EventCenter();
        center.SubscribeNotify<LatestThrowingPayloadEvent>(0, (in LatestThrowingPayloadEvent _) =>
            throw new InvalidOperationException("expected"));

        var scheduler = new PostScheduler(252, center, options, new EventBuildPolicyTable(options.DefaultBackpressure));
        scheduler.BuildPlans(new[]
        {
            new PostTypePlan(EventTypeId<LatestThrowingPayloadEvent>.Id,
                PostDeliveryMode.Latest,
                options.DefaultBackpressure,
                maxPending: 0,
                options.DefaultBackpressure)
        });

        object payload = new byte[1024 * 1024];
        var weak = new WeakReference(payload);
        scheduler.TryPost(new LatestThrowingPayloadEvent { Value = payload });
        payload = null!;

        Assert.Throws<InvalidOperationException>(() => scheduler.Pump());
        scheduler.Dispose();
        return weak;
    }

    [Test]
    public void DropNewest_does_not_consume_MaxPending_capacity_for_dropped_events()
    {
        EventMetaDataRegistry.RegisterMetaData<DropNewestMaxPendingEvent>(new DropNewestMaxPendingEventMetaData());

        var runtime = LayerHub.CreateLayers()
                              .Push(new EmptyRegressionLayer())
                              .SetPostOptions(new PostSchedulerOptions(readyCapacity: 1,
                                  nextCapacity: 1,
                                  maxEventsPerPump: 0,
                                  maxMillisecondsPerPump: 0,
                                  maxWavesPerPump: 1,
                                  timeCheckInterval: 64,
                                  defaultBackpressure: BackpressurePolicy.RejectNew))
                              .Build();

        var received = new List<int>();
        runtime.EventCenter.SubscribeNotify<DropNewestMaxPendingEvent>(0,
            (in DropNewestMaxPendingEvent e) => received.Add(e.Value));

        Assert.That(runtime.Scheduler.TryPost(new DropNewestMaxPendingEvent { Value = 1 }).IsSuccess, Is.True);
        Assert.That(runtime.Scheduler.TryPost(new DropNewestMaxPendingEvent { Value = 2 }).IsSuccess, Is.True);
        Assert.That(runtime.Scheduler.TryPost(new DropNewestMaxPendingEvent { Value = 3 }).IsSuccess, Is.True);

        runtime.Scheduler.Pump();

        Assert.That(received, Is.EqualTo(new[] { 1 }));
        Assert.That(runtime.Scheduler.TryPost(new DropNewestMaxPendingEvent { Value = 4 }).IsSuccess, Is.True);
    }

    [Test]
    public void Parallel_subscription_is_dispatchable_immediately_after_subscribe()
    {
        using var received = new ManualResetEventSlim(false);
        var center = new EventCenter();
        center.SubscribeParallel<ParallelRegressionEvent>(0, (in ParallelRegressionEvent _) => received.Set(),
            (_, _, _, ex) => throw ex);

        center.Send(new ParallelRegressionEvent { Value = 1 });

        Assert.That(received.Wait(TimeSpan.FromSeconds(2)), Is.True);
    }

    [Test]
    public void Long_timer_cancel_does_not_promote_reused_slot_from_stale_heap_entry()
    {
        var scheduler = new TimeScheduler<int>(new TimeSchedulerOptions(
            tickDurationSeconds: 1,
            wheelSize: 4,
            initialTimerCapacity: 1,
            longTimerThresholdSeconds: 4,
            maxExpiredPerTick: 16,
            maxPromotePerTick: 16,
            defaultRepeatMode: TimerRepeatMode.Once,
            defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed));
        var sink = new RecordingTimerSink();

        var first = scheduler.Schedule(1, delaySeconds: 10);
        Assert.That(scheduler.Cancel(first), Is.True);
        scheduler.Schedule(2, delaySeconds: 20);

        scheduler.Tick(8.1f, sink);
        Assert.That(sink.Values, Is.Empty);

        scheduler.Tick(12.1f, sink);
        Assert.That(sink.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void TimeScheduler_expiration_cap_requeues_remaining_timers_to_next_tick()
    {
        var scheduler = new TimeScheduler<int>(new TimeSchedulerOptions(
            tickDurationSeconds: 1,
            wheelSize: 4,
            initialTimerCapacity: 2,
            longTimerThresholdSeconds: 4,
            maxExpiredPerTick: 1,
            maxPromotePerTick: 16,
            defaultRepeatMode: TimerRepeatMode.Once,
            defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed));
        var sink = new RecordingTimerSink();

        scheduler.Schedule(1, delaySeconds: 1);
        scheduler.Schedule(2, delaySeconds: 1);

        scheduler.Tick(1.1f, sink);
        Assert.That(sink.Values.Count, Is.EqualTo(1));

        scheduler.Tick(1.1f, sink);
        Assert.That(sink.Values.OrderBy(static x => x), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void TimeScheduler_normalizes_non_positive_repeat_interval_to_one_tick()
    {
        var scheduler = new TimeScheduler<int>(new TimeSchedulerOptions(
            tickDurationSeconds: 1,
            wheelSize: 4,
            initialTimerCapacity: 1,
            longTimerThresholdSeconds: 4,
            maxExpiredPerTick: 16,
            maxPromotePerTick: 16,
            defaultRepeatMode: TimerRepeatMode.FixedDelay,
            defaultCatchUpPolicy: TimerCatchUpPolicy.SkipMissed));
        var sink = new RecordingTimerSink();

        scheduler.Schedule(7, delaySeconds: -1, repeatCount: 2, intervalSeconds: 0);

        scheduler.Tick(1.1f, sink);
        scheduler.Tick(1.1f, sink);
        scheduler.Tick(1.1f, sink);

        Assert.That(sink.Values, Is.EqualTo(new[] { 7, 7, 7 }));
    }

    [Test]
    public void Delay_expiration_cap_requeues_remaining_entries_instead_of_losing_them()
    {
        var first = new EmptyRegressionLayer();
        var second = new EmptyRegressionLayer();
        LayerHub.CreateLayers()
                .Push(first)
                .Push(second)
                .SetDelayOptions(new DelayBufferOptions(tickDurationSeconds: 0.1f,
                    wheelSize: 4,
                    initialCapacity: 4,
                    maxExpiredPerTick: 1))
                .Build();

        var firstPublisher = first.SubscribeDelay<DelayOverflowRegressionEvent>();
        var secondPublisher = second.SubscribeDelay<DelayOverflowRegressionEvent>();
        firstPublisher.Publish(new DelayOverflowRegressionEvent { Value = 1 }, 0.1f);
        secondPublisher.Publish(new DelayOverflowRegressionEvent { Value = 2 }, 0.1f);

        LayerHub.Pump(0.1f);
        Assert.That(firstPublisher.HasValue || secondPublisher.HasValue, Is.True);

        LayerHub.Pump(0.1f);
        Assert.That(firstPublisher.HasValue, Is.False);
        Assert.That(secondPublisher.HasValue, Is.False);
    }

    [Test]
    public void Service_initialize_can_publish_delay_after_runtime_managers_are_ready()
    {
        var layer = new InitDelayLayer();
        layer.RegisterService(new InitDelayService(layer));

        LayerHub.CreateLayers().Push(layer).Build();

        var publisher = layer.SubscribeDelay<InitDelayRegressionEvent>();
        Assert.That(publisher.TryGet(out var value), Is.True);
        Assert.That(value.Value, Is.EqualTo(42));
    }

    [Test]
    public void Disposing_one_runtime_does_not_clear_another_runtime_delay_publishers()
    {
        var first = new EmptyRegressionLayer();
        var second = new EmptyRegressionLayer();
        var firstRuntime = LayerHub.CreateLayers().Push(first).Build();
        LayerHub.CreateLayers().Push(second).Build();

        var firstPublisher = first.SubscribeDelay<DelayOverflowRegressionEvent>();
        var secondPublisher = second.SubscribeDelay<DelayOverflowRegressionEvent>();
        firstPublisher.Publish(new DelayOverflowRegressionEvent { Value = 1 }, 10);
        secondPublisher.Publish(new DelayOverflowRegressionEvent { Value = 2 }, 10);

        firstRuntime.Dispose();

        Assert.That(secondPublisher.TryGet(out var value), Is.True);
        Assert.That(value.Value, Is.EqualTo(2));
    }

    [Test]
    public void DelayPublisher_rejects_publish_after_owning_runtime_is_disposed()
    {
        var layer = new EmptyRegressionLayer();
        var runtime = LayerHub.CreateLayers().Push(layer).Build();
        var publisher = layer.SubscribeDelay<DelayOverflowRegressionEvent>();

        runtime.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            publisher.Publish(new DelayOverflowRegressionEvent { Value = 1 }, 1));
    }

    [Test]
    public void Actor_event_inbox_rejects_enqueue_after_runtime_closes_actor_inboxes()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new EmptyRegressionLayer())
                              .Build();
        int payload = runtime.ActorPayloads.Store((Action<ActorWorld>)(_ => { }));

        runtime.CloseActorInboxes();

        bool accepted = runtime.EnqueueActorEvent(new ActorCommandEnvelope(
            ActorCommandKind.Post,
            ActorId.Invalid,
            routeId: 0,
            payload));

        Assert.That(accepted, Is.False);
        Assert.That(runtime.ActorPayloads.Count, Is.EqualTo(0));
    }

    [Test]
    public void Actor_payload_storage_rejects_type_mismatch_with_clear_error()
    {
        var store = new ActorCommandPayloadStorage();
        int handle = store.Store("payload");

        var error = Assert.Throws<InvalidOperationException>(() => store.Retrieve<int>(handle));

        Assert.That(error!.Message, Does.Contain("Payload handle"));
        Assert.That(error.Message, Does.Contain(typeof(string).FullName!));
        Assert.That(error.Message, Does.Contain(typeof(int).FullName!));
        Assert.That(store.Retrieve<string>(handle), Is.EqualTo("payload"));
    }

    [Test]
    public void Actor_payload_storage_must_not_reuse_live_handle_after_counter_wrap()
    {
        var store = new ActorCommandPayloadStorage();
        int firstHandle = store.Store("first");
        SetPayloadNextHandle(store, firstHandle - 1);

        int secondHandle = store.Store("second");

        Assert.That(secondHandle, Is.Not.EqualTo(firstHandle));
        Assert.That(store.Retrieve<string>(firstHandle), Is.EqualTo("first"));
        Assert.That(store.Retrieve<string>(secondHandle), Is.EqualTo("second"));
        Assert.That(store.Count, Is.EqualTo(2));
    }

    [Test]
    public void Actor_lifecycle_inbox_must_bound_overflow()
    {
        var inbox = new ActorLifecycleInbox(1);

        for (int i = 0; i < 16; i++)
        {
            Assert.That(inbox.TryEnqueue(default), Is.EqualTo(ControlEnqueueResult.AcceptedFast));
        }

        Assert.That(inbox.TryEnqueue(default), Is.EqualTo(ControlEnqueueResult.AcceptedOverflow));

        for (int i = 1; i < 16; i++)
        {
            Assert.That(inbox.TryEnqueue(default), Is.EqualTo(ControlEnqueueResult.AcceptedOverflow));
        }

        ControlEnqueueResult rejected = inbox.TryEnqueue(default);
        Assert.That(rejected, Is.EqualTo(ControlEnqueueResult.Failed));
        Assert.That(inbox.Count, Is.EqualTo(32));
        Assert.That(inbox.IsFull, Is.True);
    }

    [Test]
    public void Ecs_work_queue_must_bound_overflow_and_reject_after_close()
    {
        var queue = new EcsWorkQueue(ringCapacity: 1, overflowCapacity: 1);

        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 1 }), Is.True);
        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 2 }), Is.True);
        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 3 }), Is.False);
        Assert.That(queue.Count, Is.EqualTo(2));

        queue.Close();
        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 4 }), Is.False);
        Assert.That(queue.Count, Is.EqualTo(2));
    }

    [Test]
    public void Ecs_work_queue_completed_sequence_must_be_terminal_and_monotonic()
    {
        var queue = new EcsWorkQueue(ringCapacity: 1, overflowCapacity: 1);
        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 1 }), Is.True);
        Assert.That(queue.TryDequeue(out EcsSubmissionBatch? batch), Is.True);
        queue.MarkCompleted(batch!.Sequence);

        queue.Clear();
        Assert.That(queue.CompletedSequence, Is.EqualTo(1));

        Assert.That(queue.TryEnqueue(new EcsSubmissionBatch(1) { Sequence = 2 }), Is.True);
        List<EcsSubmissionBatch> detached = queue.DetachAll();
        Assert.That(detached, Has.Count.EqualTo(1));
        Assert.That(queue.CompletedSequence, Is.EqualTo(2));
        Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void Ecs_result_queue_must_bound_overflow_and_dispose_rejected_items()
    {
        var queue = new EcsResultQueue(ringCapacity: 1, overflowCapacity: 0, batchCapacity: 1);
        var accepted = new DisposableResultItem();
        var rejected = new DisposableResultItem();
        var closed = new DisposableResultItem();

        Assert.That(queue.Enqueue(accepted), Is.True);
        Assert.That(queue.Enqueue(rejected), Is.False);
        Assert.That(rejected.DisposeCount, Is.EqualTo(1));

        queue.Close();
        Assert.That(queue.Enqueue(closed), Is.False);
        Assert.That(closed.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Ecs_submission_batch_pool_must_bound_retained_batches()
    {
        var pool = new EcsSubmissionBatchPool(initialBatchCapacity: 1, maxRetained: 1);
        var first = new EcsSubmissionBatch(1);
        var second = new EcsSubmissionBatch(1);

        pool.Return(first);
        pool.Return(second);

        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public void Ecs_result_batch_pool_must_bound_retained_batches()
    {
        var pool = new EcsResultBatchPool(initialCapacity: 1, maxRetained: 1);
        var first = new EcsResultBatch(1);
        var second = new EcsResultBatch(1);

        pool.Return(first);
        pool.Return(second);

        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public void Actor_command_enqueue_must_use_channel_close_as_authority()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "LayerBase",
            "Application",
            "LayerRuntime.ActorCommands.cs"));

        AssertMethodGroupDoesNotContainDisposedPrecheck(source, "internal bool EnqueueActorEvent");
    }

    [Test]
    public void Scope_composition_must_be_one_shot_after_finalize()
    {
        using var runtime = new ScopeRuntime(
            ScopeDescriptors.Main,
            Array.Empty<IService>());

        runtime.SetContexts(Array.Empty<ILayerContext>());

        Assert.Throws<InvalidOperationException>(() => runtime.FinalizeScopeBuild());
        Assert.Throws<InvalidOperationException>(() => runtime.SetContexts(Array.Empty<ILayerContext>()));
        Assert.Throws<InvalidOperationException>(() => runtime.SetResourcePlan(ScopeResourcePlan.Empty));
        Assert.Throws<InvalidOperationException>(() => runtime.UpdateServiceBindings(Array.Empty<ScopeServicePlan>()));
    }

    private static void AssertMethodGroupDoesNotContainDisposedPrecheck(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        int end = source.IndexOf("internal bool IsOwnerThreadForActorWorld", StringComparison.Ordinal);

        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        string methodGroup = source.Substring(start, end - start);

        Assert.That(methodGroup, Does.Not.Contain("_disposed"));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LayerBase.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return string.Empty;
    }

    private static void SetPayloadNextHandle(ActorCommandPayloadStorage store, int value)
    {
        var field = typeof(ActorCommandPayloadStorage).GetField(
            "_nextHandle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field!.SetValue(store, value);
    }

    private sealed class EmptyRegressionLayer : Layer
    {
    }

    private sealed class DisposableProbeLayer : Layer
    {
    }

    private sealed partial class DisposableProbeRegistrar : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DisposableProbeService, DisposableProbeService>();
        }
    }

    private sealed class DisposableProbeService : IDisposable
    {
        public static int DisposeCount;

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }

    private sealed class InitDelayLayer : Layer
    {
    }

    private sealed partial class InitDelayService : IService, IInitializable
    {
        private readonly Layer _layer;

        public InitDelayService(Layer layer)
        {
            _layer = layer;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<InitDelayService>(this);
        }

        public void Initialize()
        {
            _layer.SubscribeDelay<InitDelayRegressionEvent>()
                  .Publish(new InitDelayRegressionEvent { Value = 42 }, 10);
        }
    }

    private sealed class RecordingTimerSink : IExpiredTimerSink<int>
    {
        public List<int> Values { get; } = new();

        public bool TryAcceptExpired(in int payload, TimerHandle handle)
        {
            Values.Add(payload);
            return true;
        }
    }

    private sealed class DisposableResultItem : IEcsResultItem, IDisposable
    {
        public int DisposeCount { get; private set; }

        public string DebugName => nameof(DisposableResultItem);

        public void Apply(LayerRuntime runtime)
        {
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
