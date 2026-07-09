namespace LayerBase.Tooling;

public interface ILayerToolFactory<out T>
{
    T Create(LayerToolCreateContext context, LayerToolEntry entry);
}
