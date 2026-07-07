using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.Delay;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using NUnit.Framework;

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
}
