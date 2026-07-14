using LayerBase;
using LayerBase.Actor;
using LayerBase.Actor.RuntimeCommands;
using LayerBase.Core.DataStruct;
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
using LayerBase.Scope.Queue;
using LayerBase.Scope.Resources;
using LayerBase.Snap;
using NUnit.Framework;
using Arch.Core;
using System.Reflection;
using System.Text.Json.Nodes;

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

public partial struct FrozenRegressionEvent
{
    public int Value;
}

public partial struct NeverPrewarmedFrozenEvent
{
    public int Value;
}

public partial struct FrozenFaultEvent
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
    public void Frozen_send_of_unregistered_event_must_not_create_bucket()
    {
        var center = new EventCenter();
        center.Freeze();

        int before = center.BucketCountForTest;

        Assert.Throws<EventCenterFrozenTypeException>(
            () => center.Send(new NeverPrewarmedFrozenEvent { Value = 1 }));

        Assert.That(center.BucketCountForTest, Is.EqualTo(before));
    }

    [Test]
    public void Prewarm_after_freeze_must_throw()
    {
        var center = new EventCenter();
        center.Freeze();

        Assert.Throws<InvalidOperationException>(
            () => center.PrewarmEvent<FrozenRegressionEvent>(
                new LayerPrewarmOptions(LayerPrewarmTargets.All)));
    }

    [Test]
    public void Handler_fault_after_freeze_must_not_rebuild_during_next_send()
    {
        var center = new EventCenter();
        int invokeCount = 0;

        center.Subscribe<FrozenFaultEvent>(0, (in FrozenFaultEvent _) =>
        {
            invokeCount++;
            throw new InvalidOperationException("expected");
        });

        center.PrewarmEvent<FrozenFaultEvent>(LayerPrewarmOptions.Default);
        center.Freeze();

        int rebuilds = center.GetRebuildCount<FrozenFaultEvent>();

        center.Send(new FrozenFaultEvent { Value = 1 });
        center.Send(new FrozenFaultEvent { Value = 2 });

        Assert.That(invokeCount, Is.EqualTo(1));
        Assert.That(center.GetRebuildCount<FrozenFaultEvent>(), Is.EqualTo(rebuilds));
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
    public void Runtime_dispose_must_unregister_even_when_user_runtime_stop_reports_error()
    {
        var throwingLayer = new EmptyRegressionLayer();
        throwingLayer.RegisterService(new ThrowingRuntimeStopService());
        LayerRuntime runtime = LayerHub.CreateLayers().Push(throwingLayer).Build();
        int runtimeId = runtime.Id;

        AggregateException error = Assert.Throws<AggregateException>(() => runtime.Dispose())!;
        Assert.That(error.Flatten().InnerExceptions, Has.One.Message.Contains("runtime stop failure"));

        LayerRuntime next = LayerHub.CreateLayers().Push(new EmptyRegressionLayer()).Build();

        Assert.That(next.Id, Is.EqualTo(runtimeId));
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
    [Category("Concurrency")]
    public void Ecs_work_queue_close_race_must_terminalize_every_accepted_batch()
    {
        const int attempts = 1000;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var queue = new EcsWorkQueue(ringCapacity: 1, overflowCapacity: 0);
            var batch = new EcsSubmissionBatch(1) { Sequence = 1 };
            var item = new TerminalCountingWorkItem();
            batch.Add(item);

            using var start = new ManualResetEventSlim(false);
            bool accepted = false;

            Task producer = Task.Run(() =>
            {
                start.Wait();
                accepted = queue.TryEnqueue(batch);
            });

            Task closer = Task.Run(() =>
            {
                start.Wait();
                queue.Close();
            });

            start.Set();

            Assert.That(Task.WaitAll(new[] { producer, closer }, TimeSpan.FromSeconds(2)), Is.True);

            List<EcsSubmissionBatch> pending = queue.DetachAll();
            foreach (EcsSubmissionBatch detached in pending)
            {
                detached.CancelPendingItems();
            }

            Assert.That(
                item.ExecuteCount + item.CancelCount,
                Is.EqualTo(accepted ? 1 : 0),
                $"Attempt {attempt}");
        }
    }

    [Test]
    [Category("Concurrency")]
    public void Ecs_queue_close_must_wait_for_inflight_producer_before_detach()
    {
        var queue = new EcsWorkQueue(ringCapacity: 1, overflowCapacity: 0);
        using var producerEntered = new ManualResetEventSlim(false);
        using var allowProducer = new ManualResetEventSlim(false);

        queue.AfterProducerAcceptedForTest = () =>
        {
            producerEntered.Set();
            allowProducer.Wait();
        };

        var batch = new EcsSubmissionBatch(1);
        batch.Add(new TerminalCountingWorkItem());

        Task<bool> producer = Task.Run(() => queue.TryEnqueue(batch));

        Assert.That(producerEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

        Task<List<EcsSubmissionBatch>> close = Task.Run(() =>
        {
            queue.Close();
            return queue.DetachAll();
        });

        Assert.That(close.Wait(TimeSpan.FromMilliseconds(100)), Is.False,
            "Close detached before inflight producer completed.");

        allowProducer.Set();

        Assert.That(producer.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(close.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(producer.Result, Is.True);
        Assert.That(close.Result, Has.Count.EqualTo(1));
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
    [Category("Concurrency")]
    public void Ecs_result_queue_close_race_must_apply_or_dispose_every_accepted_result()
    {
        const int attempts = 1000;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var queue = new EcsResultQueue(ringCapacity: 1, overflowCapacity: 0, batchCapacity: 1);
            var item = new TerminalCountingResultItem();
            using var start = new ManualResetEventSlim(false);
            bool accepted = false;

            Task producer = Task.Run(() =>
            {
                start.Wait();
                accepted = queue.Enqueue(item);
            });

            Task closer = Task.Run(() =>
            {
                start.Wait();
                queue.Close();
            });

            start.Set();

            Assert.That(Task.WaitAll(new[] { producer, closer }, TimeSpan.FromSeconds(2)), Is.True);

            queue.Clear();

            int terminalCount = item.ApplyCount + item.DisposeCount;
            Assert.That(terminalCount, Is.EqualTo(1), $"Attempt {attempt}");
            if (!accepted)
            {
                Assert.That(item.ApplyCount, Is.EqualTo(0), $"Attempt {attempt}");
                Assert.That(item.DisposeCount, Is.EqualTo(1), $"Attempt {attempt}");
            }
        }
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
    public void Submission_pool_must_not_retain_oversized_batch()
    {
        var pool = new EcsSubmissionBatchPool(
            initialBatchCapacity: 16,
            maxRetained: 8,
            maxRetainedItemCapacity: 256);

        EcsSubmissionBatch large = pool.Rent();
        large.EnsureCapacity(1_000_000);

        pool.Return(large);

        Assert.That(pool.Count, Is.EqualTo(0));

        EcsSubmissionBatch next = pool.Rent();
        Assert.That(next.Capacity, Is.LessThanOrEqualTo(256));
    }

    [Test]
    public void Result_pool_must_not_retain_oversized_batch()
    {
        var pool = new EcsResultBatchPool(
            initialCapacity: 16,
            maxRetained: 8,
            maxRetainedItemCapacity: 256);

        EcsResultBatch large = pool.Rent();
        large.EnsureCapacity(1_000_000);

        pool.Return(large);

        Assert.That(pool.Count, Is.EqualTo(0));

        EcsResultBatch next = pool.Rent();
        Assert.That(next.Capacity, Is.LessThanOrEqualTo(256));
    }

    [Test]
    public void Snap_decode_must_reject_input_over_character_limit()
    {
        var limits = new SnapDecodeLimits { MaxInputChars = 32 };
        string json = new string(' ', 33);

        Assert.Throws<SnapLimitExceededException>(() =>
            JsonSnapCodec.DecodeFromString(json, limits: limits));
    }

    [Test]
    public void Snap_decode_must_reject_too_many_sections()
    {
        var document = new SnapDocument
        {
            Sections = Enumerable.Range(0, 11)
                .ToDictionary(
                    i => $"S{i}",
                    i => new SnapSection
                    {
                        Key = $"S{i}",
                        Data = new JsonObject()
                    })
        };

        string json = JsonSnapCodec.EncodeToString(document);

        Assert.Throws<SnapLimitExceededException>(() =>
            JsonSnapCodec.DecodeFromString(json, limits: new SnapDecodeLimits { MaxSections = 10 }));
    }

    [Test]
    public void Snap_reader_must_reject_array_over_item_limit()
    {
        var data = new JsonObject
        {
            ["items"] = new JsonArray(1, 2, 3)
        };

        var reader = new SnapReader(data, version: 1, limits: new SnapDecodeLimits { MaxArrayItems = 2 });

        Assert.Throws<SnapLimitExceededException>(() => reader.ReadArray("items"));
    }

    [Test]
    public void Snap_reader_must_reject_string_over_length_limit()
    {
        var data = new JsonObject
        {
            ["name"] = new string('x', 5)
        };

        var reader = new SnapReader(data, version: 1, limits: new SnapDecodeLimits { MaxStringChars = 4 });

        Assert.Throws<SnapLimitExceededException>(() => reader.ReadString("name"));
    }

    [Test]
    public void EventCenter_direct_subscription_after_freeze_must_throw()
    {
        var center = new EventCenter();
        center.SubscribeNotify<FrozenRegressionEvent>(0, static (in FrozenRegressionEvent _) => { });

        center.Freeze();

        Assert.Throws<InvalidOperationException>(() =>
            center.SubscribeNotify<FrozenRegressionEvent>(0, static (in FrozenRegressionEvent _) => { }));
    }

    [Test]
    public void EventCenter_frozen_dispatch_must_not_rebuild_bucket()
    {
        var center = new EventCenter();
        center.SubscribeNotify<FrozenRegressionEvent>(0, static (in FrozenRegressionEvent _) => { });
        center.PrewarmEvent<FrozenRegressionEvent>(
            new LayerPrewarmOptions(LayerPrewarmTargets.All));
        center.Freeze();

        int rebuilds = center.GetRebuildCount<FrozenRegressionEvent>();

        for (int i = 0; i < 10000; i++)
        {
            center.Send(new FrozenRegressionEvent { Value = i });
        }

        Assert.That(center.GetRebuildCount<FrozenRegressionEvent>(), Is.EqualTo(rebuilds));
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
    public void Channel_close_and_drain_callback_runs_outside_lock()
    {
        var queue = new ClosableLockedRingQueue<int>(4);
        Assert.That(queue.TryEnqueue(1), Is.EqualTo(QueueEnqueueResult.Accepted));

        QueueEnqueueResult enqueueFromCallback = default;
        bool dequeueFromCallback = true;
        queue.CloseAndDrain(_ =>
        {
            enqueueFromCallback = queue.TryEnqueue(2);
            dequeueFromCallback = queue.TryDequeue(out _);
        });

        Assert.That(enqueueFromCallback, Is.EqualTo(QueueEnqueueResult.Closed));
        Assert.That(dequeueFromCallback, Is.False);
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

    [Test]
    public void Scope_service_lookup_must_not_use_static_slot_cache()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "LayerBase",
            "Scope",
            "ScopeServiceProvider.cs"));

        Assert.That(source, Does.Not.Contain("ScopeServiceSlotCache"));
        Assert.That(source, Does.Not.Contain("static int s_slot"));
    }

    [Test]
    public void Scope_service_lookup_must_not_share_slot_cache_between_runtimes()
    {
        using var first = new ScopeServiceProvider(new object[]
        {
            new FirstPaddingService(),
            new SharedContractService(1)
        });
        using var second = new ScopeServiceProvider(new object[]
        {
            new SharedContractService(2),
            new SecondPaddingService()
        });

        for (int i = 0; i < 10000; i++)
        {
            Assert.That(first.Get<ISharedContract>().Value, Is.EqualTo(1));
            Assert.That(second.Get<ISharedContract>().Value, Is.EqualTo(2));
        }
    }

    [Test]
    public void Scope_TryPost_without_dispatcher_must_reject_before_enqueue()
    {
        using var scope = new ScopeRuntime(
            ScopeDescriptors.Main,
            Array.Empty<IService>(),
            postDispatcher: null);

        scope.Start();

        bool accepted = scope.TryPost(new ScopePostMessage(
            eventId: 1,
            payload: new object()));

        Assert.That(accepted, Is.False);
        Assert.That(scope.PostInboxCount, Is.EqualTo(0));
    }

    [Test]
    public void Scope_service_provider_must_reject_ambiguous_interface_binding()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            new ScopeServiceProvider(new object[]
            {
                new FirstStorage(),
                new SecondStorage()
            }))!;

        Assert.That(error.Message, Does.Contain(nameof(IStorage)));
        Assert.That(error.Message, Does.Contain(nameof(FirstStorage)));
        Assert.That(error.Message, Does.Contain(nameof(SecondStorage)));
    }

    [Test]
    public void Scope_build_must_reject_out_of_range_service_slot()
    {
        var service = new FirstStorage();
        using var scope = new ScopeRuntime(
            ScopeDescriptors.Main,
            new IService[] { service });

        var plans = new[]
        {
            new ScopeServicePlan(
                serviceSlot: 10,
                serviceType: typeof(FirstStorage),
                instance: service,
                bindingInitializer: null,
                membership: new LayerMembership(0, 1))
        };

        Assert.Throws<InvalidOperationException>(() =>
            scope.UpdateServiceBindings(plans));
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

    private sealed partial class ThrowingRuntimeStopService : IService, IRuntimeStop
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ThrowingRuntimeStopService>(this);
        }

        public void RuntimeStop()
        {
            throw new InvalidOperationException("runtime stop failure");
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

    private interface ISharedContract
    {
        int Value { get; }
    }

    private sealed class SharedContractService : ISharedContract
    {
        public SharedContractService(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class FirstPaddingService
    {
    }

    private sealed class SecondPaddingService
    {
    }

    private interface IStorage
    {
    }

    private sealed class FirstStorage : IService, IStorage
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class SecondStorage : IService, IStorage
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class TerminalCountingWorkItem : IEcsWorkItem
    {
        public int ExecuteCount;
        public int CancelCount;

        public string DebugName => nameof(TerminalCountingWorkItem);

        public void Execute(World world, EcsResultQueue results)
        {
            Interlocked.Increment(ref ExecuteCount);
        }

        public void Cancel(Exception reason)
        {
            Interlocked.Increment(ref CancelCount);
        }
    }

    private sealed class TerminalCountingResultItem : IEcsResultItem, IDisposable
    {
        public int ApplyCount;
        public int DisposeCount;

        public string DebugName => nameof(TerminalCountingResultItem);

        public void Apply(LayerRuntime runtime)
        {
            Interlocked.Increment(ref ApplyCount);
        }

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }
}
