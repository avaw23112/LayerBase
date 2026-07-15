using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase;

public static class LayerScopeExtensions
{
    public static ScopeRef<TScope> Scope<TScope>(this Layer layer)
        where TScope : IScopeDefinition
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));

        var runtime = layer.OwnerContext
                      ?? throw new InvalidOperationException("Layer not attached to LayerRuntime.");

        return runtime.GetScope<TScope>();
    }
}
