using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Event.EventMetaData;
using LayerBase.LayerHub;
using LayerBase.Layers;

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

		Assert.That(upper.ReceivedIds.Count,Is.EqualTo(0));
		Assert.That(lower.ReceivedIds.Count,Is.EqualTo(1));
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
			Bind<RoutingEvent>(Handle);
		}

		public List<int> ReceivedIds { get; } = new();

		private EventHandledState Handle(in RoutingEvent evt)
		{
			_assertion?.Invoke(evt);
			ReceivedIds.Add(evt.Id);
			return _result;
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
