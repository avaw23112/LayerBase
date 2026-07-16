using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Tools;

public static class LayerToolExtensions
{
    public static LayerToolRegistry Tools(this Layer layer)
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));

        var registry = layer.OwnerContext?.Tools
                       ?? throw new InvalidOperationException("Layer not attached to LayerRuntime.");
        return registry.CreateView(layer.RouteIndex, ScopeDefinitionIds.Main);
    }

    public static LayerToolRegistry Tools(this IService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        var binding = ServiceLayerBinder.RequireBinding(service);
        return binding.RuntimeAccess.Tools.CreateView(binding.LayerIndex, binding.OwnerScope.ScopeId);
    }

    public static LayerToolRegistry Tools(this ILayerContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var binding = ServiceLayerBinder.RequireBinding(context);
        return binding.RuntimeAccess.Tools.CreateView(binding.LayerIndex, binding.OwnerScope.ScopeId);
    }
}
