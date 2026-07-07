using System.Collections.Concurrent;
using System.Reflection;

namespace LayerBase.Layers;

/// <summary>
/// 全局 Layer 服务注册表。允许在 Layer 构建前注册自定义初始化逻辑。
/// 用于跨 Layer 的服务注入和自动配置场景。
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
}