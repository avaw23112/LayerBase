using LayerBase.Core.Event;
using LayerBase.Layers;

namespace LayerBase.Usage;

public struct UIEvent
{
    public string Text;
}

public partial class TopLayer : Layer
{
    [Subscribe]
    private EventHandledState OnUI(in UIEvent e)
    {
        Console.WriteLine($"[TopLayer] Received: {e.Text}");
        return EventHandledState.Continue;
    }
}

public partial class MiddleLayer : Layer
{
    [Subscribe]
    private EventHandledState OnUI(in UIEvent e)
    {
        Console.WriteLine($"[MiddleLayer] Received: {e.Text}");
        return EventHandledState.Handled;
    }
}

public partial class BottomLayer : Layer
{
    [Subscribe]
    private EventHandledState OnUI(in UIEvent e)
    {
        Console.WriteLine($"[BottomLayer] Received: {e.Text}");
        return EventHandledState.Continue;
    }
}

public static class PropagationUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Propagation Usage ---");
        LayerHub.Reset();

        var top = new TopLayer();
        var mid = new MiddleLayer();
        var bot = new BottomLayer();

        LayerHub.CreateLayers()
                .Push(top)
                .Push(mid)
                .Push(bot)
                .Build();

        Console.WriteLine("\nSending Global:");
        LayerHub.Send(new UIEvent { Text = "Global Hello" });

        Console.WriteLine("\nSending Bubble (from Bot):");
        bot.SendBubble(new UIEvent { Text = "Bubble Up" });

        Console.WriteLine("\nSending Drop (from Top):");
        top.SendDrop(new UIEvent { Text = "Drop Down" });
    }
}