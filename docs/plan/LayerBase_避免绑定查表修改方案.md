# LayerBase 避免绑定查表修改方案

## 修改目标

当前 `IService` / `ILayerContext` 扩展方法通过 `ServiceLayerBinder.RequireBinding(...)` 获取 `ServiceLayerBinding`。

本次修改目标：

1. 保留现有 `ServiceLayerBinding` 统一绑定模型。
2. 让源生成器为使用 `IService` / `ILayerContext` 的 partial 类自动补绑定槽位。
3. `ServiceLayerBinder` 优先从对象自身读取绑定，避免热路径进入 `ConditionalWeakTable`。
4. `ConditionalWeakTable` 只作为兼容兜底路径。
5. 源生成器只负责补结构，不参与 `Send`、`Post`、`SchedulePost` 等 API 生成。

---

## 1. 新增隐藏接口 `ILayerBindingAccessor`

### 文件位置

`LayerBase/DI/ServiceContracts.cs`

### 新增内容

```csharp
using System.ComponentModel;

namespace LayerBase.DI;

/// <summary>
/// 由 LayerBase 源生成器自动实现的隐藏绑定接口。
///
/// 作用：
/// 让 IService / ILayerContext 实例自身携带 Layer 绑定信息，
/// 避免 Send、Post、Subscribe 等高频扩展方法每次都进入 ConditionalWeakTable。
///
/// ConditionalWeakTable 是一种弱引用映射表，
/// 可以把额外数据挂到对象上，且不会阻止对象被 GC 回收。
/// 但它读取时仍然需要哈希查找，因此不适合作为热路径的默认方案。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ILayerBindingAccessor
{
    /// <summary>
    /// 当前对象的 Layer 绑定信息。
    ///
    /// 类型声明为 object?，是为了避免把 internal 的 ServiceLayerBinding
    /// 暴露给用户程序集。
    ///
    /// set：
    /// 由 ServiceLayerBinder.Attach 写入。
    ///
    /// get：
    /// 由 ServiceLayerBinder.RequireBinding 读取，
    /// 并在 LayerBase 内部转换为 ServiceLayerBinding。
    /// </summary>
    object? __LayerBaseBinding { get; set; }
}
```

---

## 2. 修改 `ServiceLayerBinding`

### 文件位置

`LayerBase/DI/ServiceContracts.cs`

### 修改内容

给 `ServiceLayerBinding` 增加 `Version` 字段，并在构造函数中写入。

```csharp
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
    ///
    /// 用于多世界下识别对象绑定在哪个 Runtime。
    /// </summary>
    public readonly int RuntimeId;

    /// <summary>
    /// 当前对象所属 Layer 的索引。
    ///
    /// 用于 LayerIndex、诊断、订阅组织。
    /// </summary>
    public readonly int LayerIndex;

    /// <summary>
    /// 当前对象所属 Layer。
    ///
    /// Subscribe、OnEvent、GetService、Delay 等 API 仍然通过 Layer 完成。
    /// </summary>
    public readonly Layer Layer;

    /// <summary>
    /// 当前对象所属 Runtime。
    ///
    /// Post、SchedulePost、Timer、PolicyTable 等能力从 Runtime 获取。
    /// </summary>
    public readonly LayerRuntime Runtime;

    /// <summary>
    /// 当前 Runtime 的 EventCenter。
    ///
    /// Send 热路径可以直接使用它，避免先经过 Layer.Send 转发。
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
        int version,
        int runtimeId,
        int layerIndex,
        Layer layer,
        LayerRuntime runtime)
    {
        Version = version;
        RuntimeId = runtimeId;
        LayerIndex = layerIndex;
        Layer = layer;
        Runtime = runtime;
        EventCenter = runtime.EventCenter;
    }
}
```

---

## 3. 修改 `ServiceLayerBinder`

### 文件位置

`LayerBase/DI/ServiceContracts.cs`

### 修改内容

`ServiceLayerBinder` 改成：

1. `Attach` 时优先写入 `ILayerBindingAccessor.__LayerBaseBinding`。
2. 对象没有实现 `ILayerBindingAccessor` 时，才写入 `ConditionalWeakTable`。
3. `RequireBinding` 时优先读取对象字段。
4. 字段读取失败时，才进入 `RequireBindingSlow`。
5. `Reset` 时替换兜底表，并递增版本号。

```csharp
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
    ///
    /// 作用：
    /// 1. 清空未实现 ILayerBindingAccessor 的对象绑定。
    /// 2. 让已经写入对象字段的旧绑定失效。
    /// </summary>
    public static void Reset()
    {
        s_bindingMap = new ConditionalWeakTable<object, ServiceLayerBinding>();

        unchecked
        {
            s_version++;
        }
    }

    /// <summary>
    /// 把对象绑定到指定 Layer。
    /// </summary>
    /// <param name="service">
    /// 需要绑定的服务对象或上下文对象。
    /// </param>
    /// <param name="layer">
    /// service 所属的 Layer。
    /// </param>
    public static void Attach(object service, Layer layer)
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
            runtime: runtime);

        if (service is ILayerBindingAccessor accessor)
        {
            // 快速路径：
            // 源生成器增强过的 IService / ILayerContext 会走这里。
            // 后续 Send/Post/Subscribe 可以直接从对象字段拿 binding。
            accessor.__LayerBaseBinding = binding;
        }
        else
        {
            // 兜底路径：
            // 兼容没有被源生成器增强的对象。
            s_bindingMap.Remove(service);
            s_bindingMap.Add(service, binding);
        }

        if (service is IInternalLayerContext internalContext)
        {
            // 保留原先 LayerIndex 的快速写入逻辑。
            // GetIndex 可以继续优先读取整数索引。
            internalContext.LayerIndex = layer.RouteIndex;
        }
    }

    /// <summary>
    /// 获取对象的绑定信息。
    ///
    /// 高频 API 都应该调用这个方法。
    /// 它会优先读取对象自身字段，失败后才查 ConditionalWeakTable。
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
    ///
    /// 作用：
    /// 把 ConditionalWeakTable 查找和异常创建放到非内联方法里，
    /// 让 RequireBinding 的快速路径更容易被 JIT 内联。
    ///
    /// JIT 是 .NET 的即时编译器。
    /// 内联是指编译器把一个小方法展开到调用点，减少方法调用开销。
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
    ///
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
        return RequireBinding(service).Layer;
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
```

---

## 4. 修改源生成器：为 `ILayerContext` 生成绑定槽位

### 输入示例

```csharp
public partial class PlayerManager : ILayerContext
{
}
```

### 生成结果

```csharp
using LayerBase.DI;

namespace UserNamespace;

/// <summary>
/// 由 LayerBase 源生成器生成的绑定增强代码。
///
/// 作用：
/// 1. 为 ILayerContext 对象补上 LayerIndex。
/// 2. 为 ILayerContext 对象补上 Layer 绑定槽位。
/// 3. 让 Send/Post/Subscribe 等扩展方法可以避开 ConditionalWeakTable。
/// </summary>
public partial class PlayerManager : IInternalLayerContext, ILayerBindingAccessor
{
    /// <summary>
    /// 当前对象所属 Layer 的索引。
    ///
    /// -1 表示对象尚未绑定到任何 Layer。
    /// </summary>
    public int LayerIndex { get; set; } = -1;

    /// <summary>
    /// 当前对象的 Layer 绑定信息。
    ///
    /// 实际类型是 LayerBase 内部的 ServiceLayerBinding。
    /// 这里声明为 object?，避免把内部实现类型暴露给用户代码。
    /// </summary>
    private object? __layerBaseBinding;

    /// <summary>
    /// ILayerBindingAccessor 的显式接口实现。
    ///
    /// 显式接口实现表示：
    /// 这个属性不会直接出现在 PlayerManager 的普通成员列表里，
    /// 只有把对象当成 ILayerBindingAccessor 使用时才能访问。
    /// </summary>
    object? ILayerBindingAccessor.__LayerBaseBinding
    {
        get => __layerBaseBinding;
        set => __layerBaseBinding = value;
    }
}
```

---

## 5. 修改源生成器：为 `IService` 生成绑定槽位

### 输入示例

```csharp
public partial class CombatService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
```

### 生成结果

```csharp
using LayerBase.DI;

namespace UserNamespace;

/// <summary>
/// 由 LayerBase 源生成器生成的 Service 绑定增强代码。
///
/// 作用：
/// 让 IService 实例自身携带 Layer 绑定信息，
/// 避免 ServiceExtensions 中的 Send/Post/GetService 每次查 ConditionalWeakTable。
/// </summary>
public partial class CombatService : ILayerBindingAccessor
{
    /// <summary>
    /// 当前 Service 的 Layer 绑定信息。
    ///
    /// 实际类型是 LayerBase 内部的 ServiceLayerBinding。
    /// 这里使用 object?，避免把内部实现类型暴露给用户代码。
    /// </summary>
    private object? __layerBaseBinding;

    /// <summary>
    /// ILayerBindingAccessor 的显式接口实现。
    ///
    /// get：
    /// ServiceLayerBinder.RequireBinding 会通过它读取绑定。
    ///
    /// set：
    /// ServiceLayerBinder.Attach 会通过它写入绑定。
    /// </summary>
    object? ILayerBindingAccessor.__LayerBaseBinding
    {
        get => __layerBaseBinding;
        set => __layerBaseBinding = value;
    }
}
```

---

## 6. 源生成器生成规则

```text
如果类型是 partial class，并且实现了 ILayerContext：
    生成 partial class : IInternalLayerContext, ILayerBindingAccessor
    生成 LayerIndex 属性
    生成 __layerBaseBinding 字段
    生成 ILayerBindingAccessor.__LayerBaseBinding 显式接口实现

如果类型是 partial class，并且实现了 IService：
    生成 partial class : ILayerBindingAccessor
    生成 __layerBaseBinding 字段
    生成 ILayerBindingAccessor.__LayerBaseBinding 显式接口实现

如果类型同时实现 ILayerContext 和 IService：
    只生成一份 __layerBaseBinding 字段
    只生成一份 ILayerBindingAccessor.__LayerBaseBinding 显式接口实现
    同时生成 IInternalLayerContext 和 LayerIndex

如果类型没有声明 partial：
    不生成绑定槽位
    报诊断提示
```

---

## 7. 新增诊断

### 诊断 ID

`LayerBase0001`

### 触发条件

类型实现了 `ILayerContext` 或 `IService`，但类型没有声明 `partial`。

### 诊断信息

```text
Type '{0}' implements ILayerContext or IService and must be declared partial to enable generated Layer binding fast path.
```

### 说明

源生成器不能直接修改用户写的类，只能生成同名 partial 类型片段。

如果用户类型不是 partial，生成器无法为它补字段和接口。

---

## 8. 扩展方法保持不变

`ServiceExtensions` 和 `LayerContextExtensions` 不需要继续膨胀。

只要继续统一调用 `ServiceLayerBinder.RequireBinding(...)`。

```csharp
public static class ServiceExtensions
{
    /// <summary>
    /// 获取 Service 的 Layer 绑定信息。
    ///
    /// ServiceLayerBinder.RequireBinding 会优先走对象自身绑定槽位，
    /// 只有对象没有被源生成器增强时才进入 ConditionalWeakTable。
    /// </summary>
    /// <param name="service">
    /// 当前 Service 对象。
    /// </param>
    /// <returns>
    /// 当前 Service 所属的 Layer/Runtime/EventCenter 绑定。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceLayerBinding GetBinding(this IService service)
    {
        return ServiceLayerBinder.RequireBinding(service);
    }
}
```

```csharp
public static class LayerContextExtensions
{
    /// <summary>
    /// 获取 ILayerContext 的 Layer 绑定信息。
    ///
    /// ServiceLayerBinder.RequireBinding 会优先走对象自身绑定槽位，
    /// 只有对象没有被源生成器增强时才进入 ConditionalWeakTable。
    /// </summary>
    /// <param name="context">
    /// 当前 LayerContext 对象。
    /// </param>
    /// <returns>
    /// 当前 LayerContext 所属的 Layer/Runtime/EventCenter 绑定。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ServiceLayerBinding GetBinding(this ILayerContext context)
    {
        return ServiceLayerBinder.RequireBinding(context);
    }
}
```

---

## 9. 修改后的调用路径

### 修改前

```text
Send/Post/Subscribe
    -> GetBinding
    -> ConditionalWeakTable.TryGetValue
    -> ServiceLayerBinding
    -> LayerRuntime / Layer / EventCenter
```

### 修改后

```text
Send/Post/Subscribe
    -> GetBinding
    -> ILayerBindingAccessor.__LayerBaseBinding
    -> ServiceLayerBinding
    -> LayerRuntime / Layer / EventCenter
```

### 未被源生成器增强的对象

```text
Send/Post/Subscribe
    -> GetBinding
    -> ILayerBindingAccessor 判断失败
    -> ConditionalWeakTable.TryGetValue
    -> ServiceLayerBinding
    -> LayerRuntime / Layer / EventCenter
```

---

## 10. 验收标准

1. 使用 `partial class Xxx : ILayerContext` 的类型会自动实现：
   - `IInternalLayerContext`
   - `ILayerBindingAccessor`
   - `LayerIndex`
   - `__LayerBaseBinding`

2. 使用 `partial class Xxx : IService` 的类型会自动实现：
   - `ILayerBindingAccessor`
   - `__LayerBaseBinding`

3. `ServiceLayerBinder.Attach(...)` 对源生成器增强对象不写入 `ConditionalWeakTable`。

4. `ServiceLayerBinder.RequireBinding(...)` 对源生成器增强对象不查 `ConditionalWeakTable`。

5. 没有被增强的旧对象仍可通过 `ConditionalWeakTable` 正常工作。

6. `Reset()` 后，旧对象字段中的绑定不会被误用。

7. `Send`、`Post`、`MarkDirty`、`PostLatest`、`PostCoalesced`、`SchedulePost`、`Delay`、`Subscribe`、`SubscribeFlow`、`SubscribeAsync`、`SubscribeParallel`、`OnEvent`、`GetService` 不需要各自改成源生成器生成逻辑。

---

## 11. 不做内容

1. 不新增 `LayerContextBase`。
2. 不新增 `LayerServiceBase`。
3. 不把 `ServiceLayerBinding` 改成公开业务 API。
4. 不让源生成器生成 `Send`、`Post`、`SchedulePost` 等扩展方法。
5. 不移除 `ConditionalWeakTable` 兜底路径。
