using System;
using System.Collections.Generic;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
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
		var top = new RecordingLayer<PlainEvent>(EventHandledState.Continue, e => e.Id);
		var middle = new RecordingLayer<PlainEvent>(EventHandledState.Continue, e => e.Id);
		var bottom = new RecordingLayer<PlainEvent>(EventHandledState.Continue, e => e.Id);

		LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

		LayerHub.Send(new PlainEvent(eventId));

		Assert.That(top.ReceivedIds, Is.EqualTo(new[] { eventId }));
		Assert.That(middle.ReceivedIds, Is.EqualTo(new[] { eventId }));
		Assert.That(bottom.ReceivedIds, Is.EqualTo(new[] { eventId }));
	}

    [Test]
    public void Direct_route_skips_layers_without_handlers()
	{
		const int eventId = 20;
		var first = new EmptyLayer();
		var middle = new EmptyLayer();
		var bottom = new RecordingLayer<PlainEvent>(EventHandledState.Continue, e => e.Id);

		LayerHub.CreateLayers().Push(first).Push(middle).Push(bottom).Build();

		first.SendDrop(new PlainEvent(eventId));

		Assert.That(bottom.ReceivedIds, Is.EqualTo(new[] { eventId }));
    }

	private sealed class RecordingLayer<TEvent> : Layer where TEvent : struct
	{
		private readonly EventHandledState _result;
		private readonly Func<TEvent, int> _idSelector;

		public RecordingLayer(EventHandledState result, Func<TEvent, int> idSelector)
		{
			_result = result;
			_idSelector = idSelector;
			Subscribe<TEvent>(Handle);
		}

		public List<int> ReceivedIds { get; } = new();

		private EventHandledState Handle(in TEvent evt)
		{
			ReceivedIds.Add(_idSelector(evt));
			return _result;
		}
	}

	private sealed class EmptyLayer : Layer { }

    public readonly struct PlainEvent
    {
        public PlainEvent(int id) => Id = id;
        public int Id { get; }
    }
}
