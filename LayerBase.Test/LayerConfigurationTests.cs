using System;
using System.Collections.Generic;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

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

		LayerHub.Send(new PlainEvent(eventId));

		PumpTwice();

		Assert.That(top.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(middle.ReceivedIds.Count, Is.EqualTo(1));
		Assert.That(bottom.ReceivedIds.Count, Is.EqualTo(1));
	}

    [Test]
    public void Direct_route_skips_layers_without_handlers()
	{
		var first = new EmptyLayer();
		var middle = new EmptyLayer();
		var bottom = new RecordingLayer<PlainEvent>(
			EventHandledState.Continue,
			e => e.Id,
			e => Assert.That(e.Id, Is.EqualTo(20))
		);

		LayerHub.CreateLayers().Push(first).Push(middle).Push(bottom).Build();

		first.SendDrop(new PlainEvent(20));

		PumpTwice();

		Assert.That(bottom.ReceivedIds, Is.EqualTo(new[] { 20 }));
    }

    private static void PumpTwice()
    {
        LayerHub.Pump(0.02f);
        LayerHub.Pump(0.02f);
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
			Subscribe<TEvent>(Handle);
		}

		public List<int> ReceivedIds { get; } = new();

		private EventHandledState Handle(in TEvent evt)
		{
			_assertion?.Invoke(evt);
			ReceivedIds.Add(_idSelector(evt));
			return _result;
		}
	}

	private sealed class EmptyLayer : Layer
	{
	}

    public readonly struct PlainEvent
    {
        public PlainEvent(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

}
