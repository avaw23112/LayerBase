using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventHandler;
using LayerBase.Event.EventMetaData;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;
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
	public void Bubble_stops_at_lower_priority_layer_when_handled_by_higher_priority()
	{
		// In the new system, order is ALWAYS registration order (0 -> 1 -> 2).
		// Higher priority (outer/registered first) handles it first.
		var higher = new RecordingLayer(
			EventHandledState.Handled,
			evt => Assert.That(evt.Id, Is.EqualTo(1))
		);
		var lower = new RecordingLayer(EventHandledState.Continue);

		LayerHub.CreateLayers().Push(higher).Push(lower).Build();

		// Event sent from lower, but higher is at index 0, so it gets it first.
		lower.SendBubble(new RoutingEvent(1));

		PumpTwice();

		Assert.That(higher.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(lower.ReceivedIds.Count, Is.EqualTo(0));
	}

	[Test]
	public void Pump_does_not_throw_when_a_queued_event_creates_a_new_queue_type()
	{
		var layer = new ReentrantPostingLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		LayerHub.Post(new QueuedRootEvent(7));

		Assert.That(() => LayerHub.Pump(0.02f), Throws.Nothing);
		CollectionAssert.AreEqual(new[] { 7 }, layer.RootIds);
		Assert.That(layer.FollowUpIds, Is.Empty);

		LayerHub.Pump(0.02f);

		CollectionAssert.AreEqual(new[] { 70 }, layer.FollowUpIds);
	}

	[Test]
	public void Broadcast_hits_all_layers_when_not_handled()
	{
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

		LayerHub.Send(new RoutingEvent(eventId));

		PumpTwice();

		Assert.That(middle.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(bottom.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(top.ReceivedIds.Count, Is.EqualTo(1));
	}

	[Test]
	public void Ordered_handlers_keep_registration_order_when_sync_and_async_are_mixed()
	{
		var order = new List<string>();
		var layer = new MixedOrderedLayer(order);

		LayerHub.CreateLayers().Push(layer).Build();
		LayerHub.Send(new RoutingEvent(42));

		PumpTwice();

		CollectionAssert.AreEqual(new[] { "sync-1", "async-2", "sync-3" }, order);
	}

	[Test]
	public void SubscribeParallel_dispatches_events_through_background_job_scheduler()
	{
		LayerHub.InitializeJobScheduler(workerCount: 2, queueCapacity: 256);

		var latch = new CountdownEvent(6);
		var layer = new ParallelRecordingLayer(latch);

		LayerHub.CreateLayers().Push(layer).Build();

		LayerHub.Send(new RoutingEvent(101));
		LayerHub.Send(new RoutingEvent(102));
		LayerHub.Send(new RoutingEvent(103));

		PumpTwice();

		Assert.That(latch.Wait(TimeSpan.FromSeconds(2)), Is.True);
		Assert.That(layer.DelegateHandledCount, Is.EqualTo(3));
		Assert.That(layer.HandlerHandledCount, Is.EqualTo(3));
	}

	[Test]
	public void Faulted_handler_is_disabled_and_reported_without_blocking_other_handlers()
	{
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

			LayerHub.Send(new RoutingEvent(401));
			PumpTwice();

			LayerHub.Send(new RoutingEvent(402));
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
		LayerHub.InitializeJobScheduler(workerCount: 2, queueCapacity: 256);

		var layer = new ParallelFaultIsolationLayer();
		int reportCount = 0;
		LayerEventErrorInfo? reportedError = null;
		var errorLatch = new CountdownEvent(1); // 显式等待错误报告

		Action<LayerEventErrorInfo> onError = info =>
		{
			if (info.EventFullName.Contains(nameof(RoutingEvent)))
			{
				reportedError = info;
				Interlocked.Increment(ref reportCount);
				if (errorLatch.CurrentCount > 0) errorLatch.Signal();
			}
		};

		LayerHub.OnLayerEventError += onError;
		try
		{
			LayerHub.CreateLayers().Push(layer).Build();

			LayerHub.Send(new RoutingEvent(501));
			LayerHub.Send(new RoutingEvent(502));
			PumpTwice();

			// 同时等待健康逻辑和错误报告
			Assert.That(layer.WaitHealthyHandled(TimeSpan.FromSeconds(2)), Is.True, "Healthy handlers timed out");
			Assert.That(errorLatch.Wait(TimeSpan.FromSeconds(2)), Is.True, "Error report timed out");
			
			Assert.That(layer.FailingCount, Is.EqualTo(1));
			Assert.That(layer.HealthyCount, Is.EqualTo(2));
			Assert.That(reportCount, Is.EqualTo(1));
			Assert.That(reportedError.HasValue, Is.True);
			Assert.That(reportedError!.Value.LayerFullName, Is.EqualTo(nameof(ParallelFaultIsolationLayer)));
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

	private sealed class ReentrantPostingLayer : Layer
	{
		public ReentrantPostingLayer()
		{
			Subscribe<QueuedRootEvent>(OnRoot);
			Subscribe<QueuedFollowUpEvent>(OnFollowUp);
		}

		public List<int> RootIds { get; } = new();
		public List<int> FollowUpIds { get; } = new();

		private EventHandledState OnRoot(in QueuedRootEvent evt)
		{
			RootIds.Add(evt.Id);
			LayerHub.Post(new QueuedFollowUpEvent(evt.Id * 10));
			return EventHandledState.Continue;
		}

		private EventHandledState OnFollowUp(in QueuedFollowUpEvent evt)
		{
			FollowUpIds.Add(evt.Id);
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

	private readonly struct QueuedRootEvent
	{
		public QueuedRootEvent(int id)
		{
			Id = id;
		}

		public int Id { get; }
	}

	private readonly struct QueuedFollowUpEvent
	{
		public QueuedFollowUpEvent(int id)
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
