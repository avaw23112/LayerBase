using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;

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

        var runtime = LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

        runtime.Send(new PlainEvent(eventId));

        Assert.That(top.ReceivedIds, Is.EqualTo(new[] { eventId }));
        Assert.That(middle.ReceivedIds, Is.EqualTo(new[] { eventId }));
        Assert.That(bottom.ReceivedIds, Is.EqualTo(new[] { eventId }));
    }


    private sealed class RecordingLayer<TEvent> : Layer where TEvent : struct
    {
        private readonly Func<TEvent, int> _idSelector;
        private readonly EventHandledState _result;

        public RecordingLayer(EventHandledState result, Func<TEvent, int> idSelector)
        {
            _result = result;
            _idSelector = idSelector;
            SubscribeFlow<TEvent>(Handle);
        }

        public List<int> ReceivedIds { get; } = new();

        private EventHandledState Handle(in TEvent evt)
        {
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
