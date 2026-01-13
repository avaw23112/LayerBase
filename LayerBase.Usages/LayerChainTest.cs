using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.LayerChain;

GameLayer gameLayerHead = new GameLayer();
GameLayer gameLayerTail = new GameLayer();
LayerHub.CreateLayers()
        .Push(gameLayerHead)
        .Push(gameLayerTail);
gameLayerHead.Drop(new eventTest { i = 1 });
while (true)
{
	LayerHub.Pump();
}

internal struct eventTest
{
	public int i;
}

internal class GameLayer : Layer
{
	public GameLayer() 
	{
		Bind((eventTest @event) => TestEvent(@event));
		Bind(async (eventTest @event) => await TestEventAsync(@event));
	}
	public EventState TestEvent(eventTest eventTest)
	{
		Console.WriteLine(eventTest.i);
		return EventState.HandledAndContinue;
	}
	
	public async LBTask TestEventAsync(eventTest eventTest)
	{
		await LBTask.Delay(new TimeSpan(0,0,5));
		Console.WriteLine(eventTest.i);
	}
}