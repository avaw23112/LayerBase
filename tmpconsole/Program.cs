using System.Collections.Generic;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.LayerHub;
using LayerBase.Layers;

LayerHub.Reset();

EventMetaData<PlainEvent>.TimerScheduler.SetFrequency(0.001);
EventMetaData<PlainEvent>.TimerScheduler.Tick(0.01);

var top = new RecordingLayer();
var middle = new RecordingLayer();
var bottom = new RecordingLayer();

LayerHub.CreateLayers().Push(top).Push(middle).Push(bottom).Build();

middle.BroadCast(new PlainEvent(10));

LayerHub.Pump(0.02f);
LayerHub.Pump(0.02f);

Console.WriteLine($"top:{string.Join(",", top.ReceivedIds)}");
Console.WriteLine($"middle:{string.Join(",", middle.ReceivedIds)}");
Console.WriteLine($"bottom:{string.Join(",", bottom.ReceivedIds)}");

public sealed class RecordingLayer : Layer
{
    public List<int> ReceivedIds { get; } = new();

    public RecordingLayer()
    {
        Bind<PlainEvent>(Handle);
    }

    private EventHandledState Handle(in PlainEvent evt)
    {
        ReceivedIds.Add(evt.Id);
        return EventHandledState.Continue;
    }
}

public readonly struct PlainEvent
{
    public PlainEvent(int id) => Id = id;
    public int Id { get; }
}
