using LayerBase;

namespace LayerBase.Tooling;

public readonly struct LayerToolCreateContext
{
    private readonly LayerRuntime? _runtime;

    public LayerToolCreateContext(LayerToolRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _runtime = null;
    }

    public LayerToolCreateContext(LayerRuntime runtime, LayerToolRegistry registry)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public LayerToolRegistry Registry { get; }

    public LayerRuntime Runtime => _runtime ?? throw new InvalidOperationException("LayerToolCreateContext has no runtime.");

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

    public TService GetService<TService>() where TService : class
    {
        return Runtime.GetService<TService>();
    }

    public TFactory GetFactory<TFactory>() where TFactory : class
    {
        return Runtime.GetService<TFactory>();
    }
}
