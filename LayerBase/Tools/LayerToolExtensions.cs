using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Tools;

public static class LayerToolExtensions
{
    public static LayerToolRegistry Tools(this Layer layer)
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));

        return layer.OwnerContext?.Tools
               ?? throw new InvalidOperationException("Layer not attached to LayerRuntime.");
    }

    public static LayerToolRegistry Tools(this IService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        return ServiceLayerBinder.RequireBinding(service).RuntimeAccess.Tools;
    }

    public static LayerToolRegistry Tools(this ILayerContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        return ServiceLayerBinder.RequireBinding(context).RuntimeAccess.Tools;
    }
}
