using LayerBase.Modules;

namespace LayerBase.Tools;

public interface IGeneratedLayerToolProvider
{
    LayerToolContribution[] __GetLayerToolContributions();
}
