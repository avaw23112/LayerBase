using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework.Legacy;

namespace EventsTest;

public class EventPipelineTests
{
	[SetUp]
	public void SetUp()
	{
		LayerHub.Reset();
	}


	[Test]
	public void Bubble_stops_at_current_layer_when_handled()
	{
		OpenGate<RoutingEvent>();

		var upper = new RecordingLayer(EventHandledState.Continue);
		var lower = new RecordingLayer(
			EventHandledState.Handled,
			evt => Assert.That(evt.Id, Is.EqualTo(1))
		);

		LayerHub.CreateLayers().Push(upper).Push(lower).Build();

		lower.Bubble(new RoutingEvent(1));

		PumpTwice();

		Assert.That(upper.ReceivedIds.Count, Is.EqualTo(0));
		Assert.That(lower.ReceivedIds.Count, Is.EqualTo(1));
	}

	[Test]
	public void Broadcast_hits_all_layers_when_not_handled()
	{
		OpenGate<RoutingEvent>();

		const int eventId = 2;
		var top = new RecordingLayer(
			EventHandledState.Continue,
			evt => Assert.That(evt.Id, Is.EqualTo(eventId))
		);
		var middle = new RecordingLayer(
			EventHandledState.Continue,
			evt => Assert.That(evt.Id, Is.EqualTo(eventId))
		);
		var bottom = new RecordingLayer(
			EventHandledState.Continue,
			evt => Assert.That(evt.Id, Is.EqualTo(eventId))
		);

		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		middle.BroadCast(new RoutingEvent(eventId));

		PumpTwice();

		Assert.That(middle.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(bottom.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(top.ReceivedIds.Count, Is.EqualTo(1));
	}

	[Test]
	public void Ordered_handlers_keep_registration_order_when_sync_and_async_are_mixed()
	{
		OpenGate<RoutingEvent>();
		var order = new List<string>();
		var layer = new MixedOrderedLayer(order);

		LayerHub.CreateLayers().Push(layer).Build();
		layer.BroadCast(new RoutingEvent(42));

		PumpTwice();

		CollectionAssert.AreEqual(new[] { "sync-1", "async-2", "sync-3" }, order);
	}

	[Test]
	public void SubscribeParallel_dispatches_events_through_background_job_scheduler()
	{
		OpenGate<RoutingEvent>();
		LayerHub.InitializeJobScheduler(workerCount: 2, queueCapacity: 256);

		var latch = new CountdownEvent(6);
		var layer = new ParallelRecordingLayer(latch);

		LayerHub.CreateLayers().Push(layer).Build();

		layer.BroadCast(new RoutingEvent(101));
		layer.BroadCast(new RoutingEvent(102));
		layer.BroadCast(new RoutingEvent(103));

		PumpTwice();

		Assert.That(latch.Wait(TimeSpan.FromSeconds(2)), Is.True);
		Assert.That(layer.DelegateHandledCount, Is.EqualTo(3));
		Assert.That(layer.HandlerHandledCount, Is.EqualTo(3));
	}

	[Test]
	public void Faulted_handler_is_disabled_and_reported_without_blocking_other_handlers()
	{
		OpenGate<RoutingEvent>();
		var layer = new FaultIsolationLayer();
		int reportCount = 0;
		LayerEventErrorInfo? reportedError = null;

		Action<LayerEventErrorInfo> onError = info =>
		{
			if (info.EventFullName.Contains(nameof(RoutingEvent)))
			{
				reportedError = info;
				Interlocked.Increment(ref reportCount);
			}
		};

		LayerHub.OnLayerEventError += onError;
		try
		{
			LayerHub.CreateLayers().Push(layer).Build();

			layer.BroadCast(new RoutingEvent(401));
			PumpTwice();

			layer.BroadCast(new RoutingEvent(402));
			PumpTwice();

			Assert.That(layer.FailingCount, Is.EqualTo(1));
			Assert.That(layer.HealthyCount, Is.EqualTo(2));
			Assert.That(reportCount, Is.EqualTo(1));
			Assert.That(reportedError.HasValue, Is.True);
			Assert.That(reportedError!.Value.LayerFullName, Is.EqualTo(nameof(FaultIsolationLayer)));
			Assert.That(reportedError!.Value.HandlerFullName, Does.Contain(nameof(FaultIsolationLayer)));
			Assert.That(reportedError!.Value.EventFullName, Does.Contain(nameof(RoutingEvent)));
		}
		finally
		{
			LayerHub.OnLayerEventError -= onError;
		}
	}

	[Test]
	public void Faulted_parallel_handler_is_disabled_and_reported_once()
	{
		OpenGate<RoutingEvent>();
		LayerHub.InitializeJobScheduler(workerCount: 2, queueCapacity: 256);

		var layer = new ParallelFaultIsolationLayer();
		int reportCount = 0;
		LayerEventErrorInfo? reportedError = null;
		Action<LayerEventErrorInfo> onError = info =>
		{
			if (info.EventFullName.Contains(nameof(RoutingEvent)))
			{
				reportedError = info;
				Interlocked.Increment(ref reportCount);
			}
		};

		LayerHub.OnLayerEventError += onError;
		try
		{
			LayerHub.CreateLayers().Push(layer).Build();

			layer.BroadCast(new RoutingEvent(501));
			layer.BroadCast(new RoutingEvent(502));
			PumpTwice();

			Assert.That(layer.WaitHealthyHandled(TimeSpan.FromSeconds(2)), Is.True);
			Assert.That(layer.FailingCount, Is.EqualTo(1));
			Assert.That(layer.HealthyCount, Is.EqualTo(2));
			Assert.That(reportCount, Is.EqualTo(1));
			Assert.That(reportedError.HasValue, Is.True);
			Assert.That(reportedError!.Value.LayerFullName, Is.EqualTo(nameof(ParallelFaultIsolationLayer)));
			Assert.That(reportedError!.Value.HandlerFullName, Does.Contain(nameof(ParallelFaultIsolationLayer)));
			Assert.That(reportedError!.Value.EventFullName, Does.Contain(nameof(RoutingEvent)));
		}
		finally
		{
			LayerHub.OnLayerEventError -= onError;
		}
	}

	private static void PumpTwice()
	{
		PumpOnce();
		PumpOnce();
	}

	private static void PumpOnce()
	{
		LayerHub.Pump(0.02f);
		// LayerHub.Reset() clears TimerSchedulers, so tick the event-specific scheduler directly to flush any
		// frequency-gated invocations that were queued during BroadCast/Bubble.
		EventMetaData<RoutingEvent>.TimerScheduler.Tick(0.02);
	}

	private static void OpenGate<T>() where T : struct
	{
		// Event meta-data uses a per-event-type TimerScheduler; open its gate so pooled events can pump immediately.
		EventMetaData<T>.TimerScheduler.SetFrequency(0.001);
		EventMetaData<T>.TimerScheduler.Tick(0.01);
	}

	private sealed class RecordingLayer : Layer
	{
		private readonly EventHandledState _result;
		private readonly Action<RoutingEvent>? _assertion;

		public RecordingLayer(EventHandledState result, Action<RoutingEvent>? assertion = null)
		{
			_result = result;
			_assertion = assertion;
			Subscribe<RoutingEvent>(Handle);
		}

		public List<int> ReceivedIds { get; } = new();

		private EventHandledState Handle(in RoutingEvent evt)
		{
			_assertion?.Invoke(evt);
			ReceivedIds.Add(evt.Id);
			return _result;
		}
	}

	private sealed class MixedOrderedLayer : Layer
	{
		private readonly List<string> _order;

		public MixedOrderedLayer(List<string> order)
		{
			_order = order;
			Subscribe<RoutingEvent>(OnFirstSync);
			SubscribeAsync<RoutingEvent>(OnAsync);
			Subscribe<RoutingEvent>(OnSecondSync);
		}

		private EventHandledState OnFirstSync(in RoutingEvent evt)
		{
			_order.Add("sync-1");
			return EventHandledState.Continue;
		}

		private LBTask OnAsync(RoutingEvent evt)
		{
			_order.Add("async-2");
			return LBTask.CompletedTask;
		}

		private EventHandledState OnSecondSync(in RoutingEvent evt)
		{
			_order.Add("sync-3");
			return EventHandledState.Continue;
		}
	}

	private sealed class ParallelRecordingLayer : Layer
	{
		private readonly CountdownEvent _latch;
		private readonly ParallelHandler _parallelHandler;
		private int _delegateHandledCount;

		public ParallelRecordingLayer(CountdownEvent latch)
		{
			_latch = latch;
			_parallelHandler = new ParallelHandler(_latch);
			SubscribeParallel<RoutingEvent>(_parallelHandler);
			SubscribeParallel<RoutingEvent>(OnParallelDelegate);
		}

		public int DelegateHandledCount => Volatile.Read(ref _delegateHandledCount);
		public int HandlerHandledCount => _parallelHandler.HandledCount;

		private EventHandledState OnParallelDelegate(in RoutingEvent evt)
		{
			Interlocked.Increment(ref _delegateHandledCount);
			_latch.Signal();
			return EventHandledState.Continue;
		}
	}

	private sealed class FaultIsolationLayer : Layer
	{
		private int _failingCount;
		private int _healthyCount;

		public FaultIsolationLayer()
		{
			Subscribe<RoutingEvent>(OnFailing);
			Subscribe<RoutingEvent>(OnHealthy);
		}

		public int FailingCount => Volatile.Read(ref _failingCount);
		public int HealthyCount => Volatile.Read(ref _healthyCount);

		private EventHandledState OnFailing(in RoutingEvent evt)
		{
			Interlocked.Increment(ref _failingCount);
			throw new InvalidOperationException("fault from ordered handler");
		}

		private EventHandledState OnHealthy(in RoutingEvent evt)
		{
			Interlocked.Increment(ref _healthyCount);
			return EventHandledState.Continue;
		}
	}

	private sealed class ParallelFaultIsolationLayer : Layer
	{
		private readonly CountdownEvent _healthyLatch = new(2);
		private int _failingCount;
		private int _healthyCount;

		public ParallelFaultIsolationLayer()
		{
			SubscribeParallel<RoutingEvent>(OnFailing);
			SubscribeParallel<RoutingEvent>(OnHealthy);
		}

		public int FailingCount => Volatile.Read(ref _failingCount);
		public int HealthyCount => Volatile.Read(ref _healthyCount);

		public bool WaitHealthyHandled(TimeSpan timeout)
		{
			return _healthyLatch.Wait(timeout);
		}

		private EventHandledState OnFailing(in RoutingEvent evt)
		{
			Interlocked.Increment(ref _failingCount);
			throw new InvalidOperationException("fault from parallel handler");
		}

		private EventHandledState OnHealthy(in RoutingEvent evt)
		{
			Interlocked.Increment(ref _healthyCount);
			_healthyLatch.Signal();
			return EventHandledState.Continue;
		}
	}

	private sealed class ParallelHandler : IEventHandler<RoutingEvent>
	{
		private readonly CountdownEvent _latch;
		private int _handledCount;

		public ParallelHandler(CountdownEvent latch)
		{
			_latch = latch;
		}

		public int HandledCount => Volatile.Read(ref _handledCount);

		public void Deal(in RoutingEvent @event)
		{
			Interlocked.Increment(ref _handledCount);
			_latch.Signal();
		}
	}

	public partial struct RoutingEvent
	{
		public RoutingEvent(int id)
		{
			Id = id;
		}

		public int Id { get; }
	}
	
	public class RoutingEventMeta : EventMetaData<RoutingEvent>
	{
		private static readonly EventCategoryToken s_category = EventCatalogue.Path("routing").GetToken();
		public override EventCategoryToken Category => s_category;
	}
}
