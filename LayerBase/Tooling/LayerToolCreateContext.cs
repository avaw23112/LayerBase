namespace LayerBase.Tooling;

public readonly struct LayerToolCreateContext
{
    public LayerToolCreateContext(LayerToolRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public LayerToolRegistry Registry { get; }

    public T Create<T>()
    {
        return Registry.Create<T>();
    }

    public TContract Create<TContract>(string key)
    {
        return Registry.Create<TContract>(key);
    }

    public T GetOrCreate<T>()
    {
        return Registry.GetOrCreate<T>();
    }

    public TContract GetOrCreate<TContract>(string key)
    {
        return Registry.GetOrCreate<TContract>(key);
    }
}
