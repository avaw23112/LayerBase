using LayerBase;
using LayerBase.Layers;
using LayerBase.Core.Event;
namespace Usage;
public class PropagationGameLayer : Layer { }
public class PropagationUsage {
    public struct UIEvent { public string Text; }
    public static void Run()
 {
        var rt = LayerHub.CreateLayers()
                         .Push(new PropagationGameLayer())
                         .Push(new PropagationGameLayer())
                         .Build();
        rt.Send(new UIEvent { Text = "Global Hello" });
    }
}