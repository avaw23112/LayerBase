using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Core.EventCatalogue;
using LayerBase.LayerHub;
using LayerBase.Layers;
using LayerBase.Layers.LayerMetaData;

namespace EventsTest;

public class LayerConfigurationTests
{
    [SetUp]
    public void Reset()
    {
        LayerHub.Reset();
    }
    
	[Test]
	public void Broadcast_without_metadata_still_reaches_all_layers()
	{
		OpenGate<PlainEvent>();

		const int eventId = 10;
		var top = new RecordingLayer<PlainEvent>(
			EventHandledState.Continue,
			e => e.Id,
			e => Assert.That(e.Id, Is.EqualTo(eventId))
		);
		var middle = new RecordingLayer<PlainEvent>(
			EventHandledState.Continue,
			e => e.Id,
			e => Assert.That(e.Id, Is.EqualTo(eventId))
		);
		var bottom = new RecordingLayer<PlainEvent>(
			EventHandledState.Continue,
			e => e.Id,
			e => Assert.That(e.Id, Is.EqualTo(eventId))
		);

		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		middle.BroadCast(new PlainEvent(eventId));

		PumpTwice();

		Assert.That(top.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(middle.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(bottom.ReceivedIds.Count, Is.EqualTo(1));
	}

	[Test]
	public void Layer_strategy_throw_blocks_handling_and_forwarding()
	{
		OpenGate<StrategyEvent>();
		LayerMetaData<RecordingLayer<StrategyEvent>>.SetDispatchStrategy(EventCategoryToken.Empty, LayerDispatchStrategy.Throw);
		try
		{
			var first = new RecordingLayer<StrategyEvent>(EventHandledState.Continue, e => e.Id);
			var second = new RecordingLayer<StrategyEvent>(EventHandledState.Continue, e => e.Id);

			LayerHub.CreateLayers().Push(first).Push(second).Build();

			first.Drop(new StrategyEvent(1));

			PumpTwice();

			Assert.That(first.ReceivedIds, Is.Empty);
			Assert.That(second.ReceivedIds, Is.Empty);
		}
		finally
		{
			LayerMetaData<RecordingLayer<StrategyEvent>>.SetDispatchStrategy(EventCategoryToken.Empty, LayerDispatchStrategy.None);
		}
    }

    [Test]
    public void Layer_strategy_ignore_skips_current_layer_but_forwards()
	{
		OpenGate<StrategyEvent>();
		LayerMetaData<RecordingLayer<StrategyEvent>>.SetDispatchStrategy(EventCategoryToken.Empty, LayerDispatchStrategy.Ignore);
		try
		{
			var first = new RecordingLayer<StrategyEvent>(EventHandledState.Continue, e => e.Id);
			var second = new RecordingLayer<StrategyEvent>(
				EventHandledState.Continue,
				e => e.Id,
				e => Assert.That(e.Id, Is.EqualTo(2))
			);

			LayerHub.CreateLayers().Push(first).Push(second).Build();

			first.Drop(new StrategyEvent(2));

			PumpTwice();

			Assert.That(first.ReceivedIds, Is.Empty);
			Assert.That(second.ReceivedIds, Is.Empty);
		}
		finally
		{
			LayerMetaData<RecordingLayer<StrategyEvent>>.SetDispatchStrategy(EventCategoryToken.Empty, LayerDispatchStrategy.None);
		}
    }

    private static void PumpTwice()
    {
        LayerHub.Pump(0.02f);
        LayerHub.Pump(0.02f);
    }

    private static void OpenGate<T>() where T : struct
    {
        EventMetaData<T>.TimerScheduler.SetFrequency(0.001);
        EventMetaData<T>.TimerScheduler.Tick(0.01);
    }

	private sealed class RecordingLayer<TEvent> : Layer where TEvent : struct
	{
		private readonly EventHandledState _result;
		private readonly Func<TEvent, int> _idSelector;
		private readonly Action<TEvent>? _assertion;

		public RecordingLayer(EventHandledState result, Func<TEvent, int> idSelector, Action<TEvent>? assertion = null)
		{
			_result = result;
			_idSelector = idSelector;
			_assertion = assertion;
			Bind<TEvent>(Handle);
		}

		public List<int> ReceivedIds { get; } = new();

		private EventHandledState Handle(in TEvent evt)
		{
			_assertion?.Invoke(evt);
			ReceivedIds.Add(_idSelector(evt));
			return _result;
		}
	}

    public readonly struct PlainEvent
    {
        public PlainEvent(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public readonly struct StrategyEvent
    {
        public StrategyEvent(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
