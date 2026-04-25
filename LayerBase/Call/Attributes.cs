using LayerBase.Layers;

namespace LayerBase.Call;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CallAttribute : Attribute
{
}

public interface IAutoCallBinder
{
    void AutoBindCalls(Layer layer);
}

