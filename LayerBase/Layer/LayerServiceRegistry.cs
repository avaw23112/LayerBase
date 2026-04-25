using System.Collections.Concurrent;
using System.Reflection;

namespace LayerBase.Layers;

/// <summary>
///     Stores per-layer DI registration actions that are filled by the source generator.
/// </summary>
public static class LayerServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, Action<Layer>> s_registrations = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_initMethods = new();

    public static void Register(Type layerType, Action<Layer> registrar)
    {
        if (layerType == null) throw new ArgumentNullException(nameof(layerType));
        if (registrar == null) throw new ArgumentNullException(nameof(registrar));

        s_registrations.AddOrUpdate(layerType, registrar, (_, existing) => existing + registrar);
    }

    public static void Reset()
    {
        s_registrations.Clear();
        s_initMethods.Clear();
    }

    internal static void Apply(Layer layer)
    {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        
        var type = layer.GetType();
        
        // 1. Call source-generated init methods found via reflection
        var initMethod = s_initMethods.GetOrAdd(type, static t => 
            t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
             .FirstOrDefault(m => m.GetCustomAttribute<SourceGeneratedServiceInitAttribute>() != null));

        initMethod?.Invoke(null, new object[] { layer });

        // 2. Call manually registered actions
        if (s_registrations.TryGetValue(type, out var registrar)) registrar(layer);
    }
}
