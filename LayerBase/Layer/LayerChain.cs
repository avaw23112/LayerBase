using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;

namespace LayerBase.Layers;

internal sealed class LayerChain
{
    private readonly ResponsibilityChain responsibilityChain;
    private bool _built;

    internal LayerChain(ResponsibilityChain chain)
    {
        responsibilityChain = chain;
    }

    internal ResponsibilityChain Chain => responsibilityChain;

    internal void AddNode(Node node)
    {
        responsibilityChain.AddLast(node);
        if (_built)
        {
            AssignEventBus();
        }
    }

    internal void Build(int slabSize = 512, bool releaseMode = false)
    {
        AssignEventBus();
        foreach (var node in responsibilityChain)
        {
            (node as Layer)?.Build();
        }

        _built = true;
    }

    internal void SetLogTracing(Action<string>? logger = null, int logQueueCapacity = 256)
    {
        // Event path tracing was removed from the hot path. This method remains as a no-op
        // so older builder calls do not break at compile time.
    }

    internal void Pump()
    {
        foreach (var node in responsibilityChain)
        {
            (node as Layer)?.Pump();
        }
    }

    internal void PrintLog()
    {
    }

    private void AssignEventBus()
    {
    }
}
