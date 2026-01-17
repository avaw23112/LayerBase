using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

GameLayer gameLayerHead = new GameLayer();
GameLayer gameLayerTail = new GameLayer();
LayerHub.CreateLayers()
        .Push(gameLayerHead)
        .Push(gameLayerTail)
        .SetLogTracing(s => Console.WriteLine(s))
        .Build();
        
gameLayerHead.Drop(new eventTest { i = 1 });
while (true)
{
	LayerHub.Pump(0.2f);
}

internal struct eventTest
{
	public int i;
}

internal class GameLayer : Layer
{
	public GameLayer() 
	{
		Bind((in Event<eventTest> @event) => TestEvent(@event));
		Bind(async ( Event<eventTest> @event) => await TestEventAsync(@event));
	}
	public EventHandledState TestEvent(Event<eventTest> eventTest)
	{
		Console.WriteLine(eventTest.Value.i);
		return EventHandledState.HandledAndContinue;
	}
	
	public async LBTask TestEventAsync(Event<eventTest> eventTest)
	{
		await LBTask.Delay(new TimeSpan(0,0,5));
		Console.WriteLine(eventTest.Value.i);
	}
}


