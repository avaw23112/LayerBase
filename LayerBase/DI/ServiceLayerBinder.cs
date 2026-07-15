using System.Runtime.CompilerServices;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.DI;

/// <summary>
/// 服务对象与某个 LayerRuntime 的绑定信息。
///
/// 该对象保存 service / manager / handler 所属的 Layer、Runtime、EventCenter。
/// 扩展方法拿到它之后，可以直接访问对应运行时能力。
/// </summary>
internal sealed class ServiceLayerBinding
{
    /// <summary>
    /// 当前绑定版本。
    ///
    /// ServiceLayerBinder.Reset() 会递增全局版本号。
    /// 对象自身字段里的旧绑定无法被统一清空，
    /// 因此需要通过 Version 判断该绑定是否仍然有效。
    /// </summary>
    public readonly int Version;

    /// <summary>
    /// 当前对象所属 Runtime 的 ID。
    /// 用于多世界下识别对象绑定在哪个 Runtime。
    /// </summary>
    public readonly int RuntimeId;

    /// <summary>
    /// 当前对象所属 Layer 的索引。
    /// 用于 LayerIndex、诊断、订阅组织。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// 当前对象所属 Layer。
    /// Subscribe、OnEvent、GetService、Delay 等仍然通过 Layer 完成。
    /// </summary>
    public readonly Layer? Layer;

    /// <summary>
    /// 当前对象所属 Runtime。
    /// Post、SchedulePost 等需要访问 Scheduler、Timer、PolicyTable。
    /// </summary>
    public readonly LayerRuntime Runtime;

    /// <summary>
    /// 当前对象所属的 Scope。
    /// Service / Context 的 ECS 资源必须从这里取得，不能从 Runtime 兼容门面回退到 MainScope。
    /// </summary>
    public readonly ScopeRuntime OwnerScope;

    /// <summary>
    /// 当前 Runtime 的 EventCenter。
    /// Send 可以直接使用它，避免 Require 后再经过 Layer.Send。
    /// </summary>
    public readonly EventCenter EventCenter;

    /// <summary>
    /// 创建服务绑定信息。
    /// </summary>
    /// <param name="version">
    /// 当前 ServiceLayerBinder 的绑定版本号。
    /// 用于 Reset 后识别旧绑定。
    /// </param>
    /// <param name="runtimeId">
    /// 当前对象所属 Runtime 的 ID。
    /// </param>
    /// <param name="layerIndex">
    /// 当前对象所属 Layer 的索引。
    /// </param>
    /// <param name="layer">
    /// 当前对象所属 Layer。
    /// </param>
    /// <param name="runtime">
    /// 当前对象所属 Runtime。
    /// </param>
    public ServiceLayerBinding(
        int          version,
        int          runtimeId,
        int          layerIndex,
        Layer?       layer,
        LayerRuntime runtime,
        ScopeRuntime ownerScope)
    {
        Version = version;
        RuntimeId = runtimeId;
        LayerIndex = layerIndex;
        Layer = layer;
        Runtime = runtime;
        OwnerScope = ownerScope ?? throw new ArgumentNullException(nameof(ownerScope));
        EventCenter = ownerScope.EventCenter;
    }
}

/// <summary>
/// 服务对象与 LayerRuntime 的绑定器。
///
/// 设计目标：
/// 1. 支持多世界绑定。
/// 2. 优先读取对象自身的绑定槽位，避免热路径查表。
/// 3. 保留 ConditionalWeakTable 作为兜底，兼容未被源生成器增强的对象。
/// </summary>
internal static class ServiceLayerBinder
{
    /// <summary>
    /// 兜底绑定表。
    ///
    /// 只有对象没有实现 ILayerBindingAccessor 时才使用。
    /// key 是 service / manager / handler 实例。
    /// value 是该对象所属 Runtime 与 Layer 的绑定信息。
    /// </summary>
    private static ConditionalWeakTable<object, ServiceLayerBinding> s_bindingMap = new();

    /// <summary>
    /// 当前绑定版本号。
    ///
    /// Reset 会替换 ConditionalWeakTable。
    /// 但对象自身字段中的旧绑定无法被统一清空。
    /// 所以这里用版本号让旧绑定失效。
    /// </summary>
    private static int s_version;

    /// <summary>
    /// 重置绑定表。
    /// </summary>
    public static void Reset()
    {
        s_bindingMap = new ConditionalWeakTable<object, ServiceLayerBinding>();

        unchecked
        {
            s_version++;
        }
    }

    public static void Detach(object service)
    {
        if (service == null)
        {
            return;
        }

        if (service is ILayerBindingAccessor accessor)
        {
            accessor.__LayerBaseBinding = null;
        }

        s_bindingMap.Remove(service);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = -1;
        }
    }

    /// <summary>
    /// 把对象绑定到指定 Layer。
    /// </summary>
    /// <param name="service">
    /// 需要绑定的服务对象。
    /// </param>
    /// <param name="layer">
    /// service 所属的 Layer。
    /// </param>
    public static void Attach(object service, Layer layer)
    {
        AttachLayer(service, layer);
    }

    public static void AttachRuntime(object service, LayerRuntime runtime)
    {
        if (service == null)
        {
            return;
        }

        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var binding = new ServiceLayerBinding(
            version: s_version,
            runtimeId: runtime.Id,
            layerIndex: -1,
            layer: null,
            runtime: runtime,
            ownerScope: runtime.ScopeHost.MainScope);

        ApplyBinding(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = -1;
        }
    }

    public static void AttachLayer(object service, Layer layer)
    {
        if (service == null || layer == null)
        {
            return;
        }

        var runtime = layer.OwnerContext;

        if (runtime == null)
        {
            throw new InvalidOperationException("Layer is not attached to LayerRuntime.");
        }

        var binding = new ServiceLayerBinding(
            version: s_version,
            runtimeId: runtime.Id,
            layerIndex: layer.RouteIndex,
            layer: layer,
            runtime: runtime,
            ownerScope: runtime.ScopeHost.MainScope);

        ApplyBinding(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = layer.RouteIndex;
        }
    }

    public static void AttachScopeRuntime(object service, LayerRuntime runtime, ScopeRuntime ownerScope)
    {
        if (service == null)
        {
            return;
        }

        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        if (ownerScope == null)
        {
            throw new ArgumentNullException(nameof(ownerScope));
        }

        var binding = new ServiceLayerBinding(
            version: s_version,
            runtimeId: runtime.Id,
            layerIndex: -1,
            layer: null,
            runtime: runtime,
            ownerScope: ownerScope);

        ApplyBinding(service, binding);

        if (service is IInternalLayerContext internalContext)
        {
            internalContext.LayerIndex = -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceLayerBinding? GetBinding(object service)
    {
        if (service is ILayerBindingAccessor accessor &&
            accessor.__LayerBaseBinding is ServiceLayerBinding binding &&
            binding.Version == s_version)
        {
            return binding;
        }

        if (s_bindingMap.TryGetValue(service, out binding) &&
            binding.Version == s_version)
        {
            return binding;
        }

        return null;
    }

    private static void ApplyBinding(object service, ServiceLayerBinding binding)
    {
        if (service is ILayerBindingAccessor accessor)
        {
            accessor.__LayerBaseBinding = binding;
        }
        else
        {
            s_bindingMap.Remove(service);
            s_bindingMap.Add(service, binding);
        }
    }

    /// <summary>
    /// 获取对象的绑定信息。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 该对象所属 Runtime 与 Layer 的绑定信息。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceLayerBinding RequireBinding(object service)
    {
        if (service is ILayerBindingAccessor accessor &&
            accessor.__LayerBaseBinding is ServiceLayerBinding binding &&
            binding.Version == s_version)
        {
            return binding;
        }

        return RequireBindingSlow(service);
    }

    /// <summary>
    /// 慢路径绑定查找。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 该对象所属 Runtime 与 Layer 的绑定信息。
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ServiceLayerBinding RequireBindingSlow(object service)
    {
        if (s_bindingMap.TryGetValue(service, out var binding) &&
            binding.Version == s_version)
        {
            return binding;
        }

        throw new InvalidOperationException(
            $"Object {service.GetType().Name} is not attached to any Layer.");
    }

    /// <summary>
    /// 获取对象所属 Layer。
    /// 保留给现有冷路径 API 使用。
    /// </summary>
    /// <param name="service">
    /// 已绑定到 Layer 的对象。
    /// </param>
    /// <returns>
    /// 对象所属 Layer。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Layer Require(object service)
    {
        return RequireLayer(RequireBinding(service));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Layer RequireLayer(ServiceLayerBinding binding)
    {
        return binding.Layer ?? ThrowRuntimeOnlyLayerRequired();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Layer ThrowRuntimeOnlyLayerRequired()
    {
        throw new InvalidOperationException(
            "This service is bound to Runtime, not to a specific Layer. Layer-only API is unavailable.");
    }

    /// <summary>
    /// 获取对象所属 Layer 的索引。
    /// </summary>
    /// <param name="context">
    /// LayerContext 对象。
    /// </param>
    /// <returns>
    /// LayerIndex。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(ILayerContext context)
    {
        if (context is IInternalLayerContext internalContext &&
            internalContext.LayerIndex != -1)
        {
            return internalContext.LayerIndex;
        }

        return RequireBinding(context).LayerIndex;
    }
}

