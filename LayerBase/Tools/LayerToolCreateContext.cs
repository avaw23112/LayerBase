namespace LayerBase.Tools;

public readonly struct LayerToolCreateContext
{
    internal LayerToolCreateContext(int runtimeId, int generation, LayerToolRegistry registry)
    {
        RuntimeId = runtimeId;
        Generation = generation;
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public int RuntimeId { get; }

    public int Generation { get; }

    public LayerToolRegistry Registry { get; }
}

public delegate object LayerToolFactoryInvoker(in LayerToolCreateContext context);
