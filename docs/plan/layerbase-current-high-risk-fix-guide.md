# layerbase-current-high-risk-fix-guide.md

## 1. 文档目标

本文档用于修复当前 `LayerBase` 最新代码中仍然存在的高危逻辑漏洞。

当前已确认修复较好的部分：

```text
1. EventStreamRuntime 已经从一维大稀疏数组改为二维缓存。
2. EventStreamSegmentPool.Return 已经先 Reset，再决定是否入池。
3. 含引用字段的 TEvent 已经通过 RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>() 触发清理。
4. ActorBehaviourEntry 已经加入 StreamUnregister。
5. ActorTypeMetaBuilder 已经生成强泛型注销委托。
6. TypedActorStorage 已经通过 entry.StreamUnregister 精确注销 handler。
```

当前仍需修复的高危点：

```text
P0：EventMetaData 在 LayerHub.Reset 后无法恢复。
P1：ActorWorld Dispose / Stopping 后旧引用仍可能 PostTo。
P1：EventStreamRuntime 旧 SearchKey 仍存在碰撞风险。
P2：Enable=false 与 EventStream Dispatch 的语义需要明确。
P3：ActorWorldRuntimeIndexAllocator 缺少 DEBUG 防御。
P3：EventStreamSegmentPool.Clear 没有遍历 Reset 池内 Segment。
```

---

## 2. 修改优先级

```text
第一批必须修：
1. EventMetaData Reset replay。
2. ActorWorld 状态防护。
3. 删除 SearchKey。

第二批建议修：
4. Enable=false 语义文档化或实现 enabledBySlot。
5. RuntimeIndexAllocator DEBUG 防御。
6. SegmentPool.Clear 清理补强。
```

---

# 3. P0：修复 EventMetaData Reset 后无法恢复

## 3.1 当前问题

当前 `EventMetaDataAutoRegister<TEvent>` 使用：

```text
s_initialized
```

它只保证第一次访问时触发 `TEvent.static ctor`。

问题流程：

```text
第一次读取 MoveEvent 元数据
→ RunClassConstructor(typeof(MoveEvent).TypeHandle)
→ MoveEvent.static ctor 注册 MoveEventMetaData
→ s_initialized = true

LayerHub.Reset()
→ EventMetaDataHandler.Clear()
→ 元数据表被清空

第二次读取 MoveEvent 元数据
→ s_initialized 已经是 true
→ 不再触发 static ctor
→ MoveEventMetaData 不会重新注册
→ ActorMailOptions / EventStreamOptions 回落默认配置
```

静态构造函数只能执行一次，不能作为 Reset 后的恢复机制。

---

## 3.2 修改文件

```text
LayerBase/Event/EventMetaData/EventMetaDataRegistry.cs
LayerBase.Generator/LayerBase.Generator/EventMetaDataGenerator.cs
```

---

## 3.3 修改 EventMetaDataAutoRegister<TEvent>

目标：把一次性初始化改成“静态构造触发一次 + 注册动作每次可 replay”。

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Event.EventMetaData;

/// <summary>
/// EventMetaData 自动注册器。
///
/// 作用：
/// 1. 第一次读取 TEvent 元数据时，强制触发 TEvent 的静态构造函数。
/// 2. TEvent 的静态构造函数通过 SetReplay 写入可重复执行的注册动作。
/// 3. 每次 EnsureInitialized 都执行 replay，保证 LayerHub.Reset 后元数据可以恢复。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
public static class EventMetaDataAutoRegister<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 是否已经触发过 TEvent 的静态构造函数。
    ///
    /// 注意：
    /// 这个字段只表示 static ctor 是否触发过，
    /// 不表示当前 EventMetaDataHandler 中一定存在 TEvent 元数据。
    /// </summary>
    private static bool s_classConstructorTriggered;

    /// <summary>
    /// 可重复执行的元数据注册动作。
    ///
    /// 作用：
    /// 由源生成器生成的 TEvent.static ctor 写入。
    /// 每次读取元数据前执行，用于恢复被 LayerHub.Reset 清空的注册表。
    /// </summary>
    private static Action? s_replay;

    /// <summary>
    /// 设置可重复执行的元数据注册动作。
    /// </summary>
    /// <param name="replay">
    /// 注册动作。
    /// 该动作内部应调用 EventMetaDataRegistry.RegisterMetaData&lt;TEvent&gt;(...)。
    /// </param>
    public static void SetReplay(Action replay)
    {
        s_replay = replay ?? throw new ArgumentNullException(nameof(replay));
    }

    /// <summary>
    /// 确保 TEvent 元数据已经注册。
    ///
    /// 逻辑：
    /// 1. 第一次调用时触发 TEvent.static ctor。
    /// 2. TEvent.static ctor 负责调用 SetReplay。
    /// 3. 每次调用都执行 s_replay，避免 Reset 后元数据丢失。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInitialized()
    {
        if (!s_classConstructorTriggered)
        {
            EnsureClassConstructorTriggeredSlow();
        }

        s_replay?.Invoke();
    }

    /// <summary>
    /// 慢路径：触发 TEvent 静态构造函数。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureClassConstructorTriggeredSlow()
    {
        if (s_classConstructorTriggered)
        {
            return;
        }

        RuntimeHelpers.RunClassConstructor(
            typeof(TEvent).TypeHandle);

        s_classConstructorTriggered = true;
    }
}
```

### 注意

`EventMetaDataAutoRegister<TEvent>` 必须是 `public`。

原因：

```text
EventMetaDataGenerator 生成的事件 partial 代码可能位于业务程序集或 Benchmark 程序集。
如果 EventMetaDataAutoRegister<TEvent> 是 internal，跨程序集生成代码无法访问。
```

---

## 3.4 修改 EventMetaDataGenerator 生成代码

当前生成：

```csharp
static MoveEvent()
{
    global::LayerBase.Event.EventMetaData.EventMetaDataRegistry.RegisterMetaData<MoveEvent>(
        new MoveEventMetaData());
}
```

改为生成：

```csharp
static MoveEvent()
{
    global::LayerBase.Event.EventMetaData.EventMetaDataAutoRegister<MoveEvent>.SetReplay(
        static () =>
        {
            global::LayerBase.Event.EventMetaData.EventMetaDataRegistry.RegisterMetaData<MoveEvent>(
                new MoveEventMetaData());
        });
}
```

生成器中替换静态构造体内的输出逻辑：

```csharp
// 参数说明：
// builder：生成源码用的 StringBuilder。
// staticCtorIndent：当前 static constructor 所在的缩进层级。
// eventTypeDisplay：事件类型的 fully-qualified name。
// metaDataDisplay：元数据类型的 fully-qualified name。

AppendIndent(builder, staticCtorIndent + 1);
builder.Append("global::LayerBase.Event.EventMetaData.EventMetaDataAutoRegister<")
       .Append(eventTypeDisplay)
       .AppendLine(">.SetReplay(");

AppendIndent(builder, staticCtorIndent + 2);
builder.AppendLine("static () =>");

AppendIndent(builder, staticCtorIndent + 2);
builder.AppendLine("{");

AppendIndent(builder, staticCtorIndent + 3);
builder.Append("global::LayerBase.Event.EventMetaData.EventMetaDataRegistry.RegisterMetaData<")
       .Append(eventTypeDisplay)
       .Append(">(new ")
       .Append(metaDataDisplay)
       .AppendLine("());");

AppendIndent(builder, staticCtorIndent + 2);
builder.AppendLine("});");
```

---

## 3.5 验收测试

新增测试：

```text
1. 定义 MoveEventMetaData，并设置非默认 SegmentCapacity / MaxRetainedSegments。
2. 第一次调用 ActorEventStreamPlanBuilder.Build<MoveEvent>()。
3. 检查读取到自定义配置。
4. 调用 LayerHub.Reset()。
5. 第二次调用 ActorEventStreamPlanBuilder.Build<MoveEvent>()。
6. 再次检查读取到自定义配置。
```

预期：

```text
Reset 前后配置一致。
不会回落到 EventStreamOptions.Default。
```

---

# 4. P1：ActorWorld Dispose / Stopping 后公开入口防护

## 4.1 当前问题

`ActorWorld.Dispose()` 会：

```text
1. 设置 _state = Disposed。
2. 解绑 EventStreamRuntime。
3. 清理 runtime 列表。
4. 归还 RuntimeIndex。
```

如果外部仍持有旧 `ActorWorld` 引用，并继续调用 `PostTo`，可能出现：

```text
worldA.Dispose()
worldB 创建并复用 worldA.RuntimeIndex
旧 worldA.PostTo(oldActorId, event)
通过 RuntimeIndex 找到 worldB 的 EventStreamCenter
```

这是跨 world 污染风险。

---

## 4.2 修改文件

```text
LayerBase/Actor/Storage/ActorWorld.cs
LayerBase/Actor/Storage/ActorWorld.Post.cs
LayerBase/Actor/Storage/ActorWorld.Create.cs
其他公开入口文件
```

---

## 4.3 新增状态检查方法

在 `ActorWorld` 中新增：

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    /// <summary>
    /// 当前 ActorWorld 是否允许执行公开操作。
    ///
    /// 允许状态：
    /// Created：LayerRuntime 构建中的 ActorWorld。
    /// Building：正在构建 Runtime。
    /// Running：正常运行。
    ///
    /// 禁止状态：
    /// Stopping：Runtime 正在停止。
    /// Disposed：ActorWorld 已释放，RuntimeIndex 可能已被复用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanUseWorldFast()
    {
        return _state is ActorWorldState.Created
            or ActorWorldState.Building
            or ActorWorldState.Running;
    }
}
```

---

## 4.4 修改 PostTo

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void PostTo<TEvent>(in ActorId actorId, in TEvent value)
    where TEvent : struct
{
    if (!CanUseWorldFast())
    {
        return;
    }

    EventStreamCenter<TEvent>? streamCenter =
        EventStreamRuntime<TEvent>.GetCenterUnchecked(
            RuntimeIndex,
            actorId.ArchetypeId);

    streamCenter?.Post(actorId, in value);
}
```

---

## 4.5 修改 PostToMany

避免每次循环重复状态检查，可以拆一个 unchecked 内部方法。

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void PostToUnchecked<TEvent>(in ActorId actorId, in TEvent value)
    where TEvent : struct
{
    EventStreamCenter<TEvent>? streamCenter =
        EventStreamRuntime<TEvent>.GetCenterUnchecked(
            RuntimeIndex,
            actorId.ArchetypeId);

    streamCenter?.Post(actorId, in value);
}
```

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void PostToMany<TEvent>(
    ReadOnlySpan<ActorId> actorIds,
    in TEvent value)
    where TEvent : struct
{
    if (!CanUseWorldFast())
    {
        return;
    }

    int length = actorIds.Length;
    int i = 0;
    int unrolledLength = length - (length % 8);

    for (; i < unrolledLength; i += 8)
    {
        PostToUnchecked(actorIds[i], in value);
        PostToUnchecked(actorIds[i + 1], in value);
        PostToUnchecked(actorIds[i + 2], in value);
        PostToUnchecked(actorIds[i + 3], in value);
        PostToUnchecked(actorIds[i + 4], in value);
        PostToUnchecked(actorIds[i + 5], in value);
        PostToUnchecked(actorIds[i + 6], in value);
        PostToUnchecked(actorIds[i + 7], in value);
    }

    for (; i < length; i++)
    {
        PostToUnchecked(actorIds[i], in value);
    }
}
```

参数说明：

```text
actorIds：
目标 ActorId 列表。

value：
要投递的事件值。

unrolledLength：
循环展开区域的长度。
这里按 8 个一组展开，用于减少循环分支开销。
```

---

## 4.6 修改 CreateActor

```csharp
public TActor CreateActor<TActor>(bool usePool = false)
    where TActor : class, IActor, new()
{
    if (!CanUseWorldFast())
    {
        throw new ObjectDisposedException(nameof(ActorWorld));
    }

    TActor actor = usePool
        ? RentActorFromPool<TActor>()
        : new TActor();

    // 后续逻辑保持原样
}
```

---

## 4.7 其他公开入口

需要检查并补充：

```text
DispatchNow
ImmediatelyAsk
PostCall
SetEnable
IsEnable
ReleaseProjectedActor
CreateProjectedActor / CreateActor 相关入口
```

原则：

```text
只要外部可以在 Dispose 后调用，并且可能访问 RuntimeIndex / storage / lifecycle，就应加状态防护。
```

---

## 4.8 验收测试

```text
1. 创建 worldA。
2. 创建 actorA，保存 actorA.Context.ActorId。
3. Dispose worldA。
4. 创建 worldB，使 RuntimeIndex 有机会复用。
5. 用 worldA.PostTo(actorAId, event)。
6. 验证 worldB 中没有 Actor 收到该事件。
```

---

# 5. P1：删除 EventStreamRuntime 旧 SearchKey

## 5.1 当前问题

当前 `GetOrCreateEventStreamRuntime` 仍使用：

```csharp
int searchKey = (RuntimeIndex << 20) | (archetypeId << 10) | eventTypeId;
```

风险：

```text
1. eventTypeId 超过 1023 后会污染 archetypeId 区间。
2. archetypeId 超过 1023 后会污染 runtimeIndex 区间。
3. runtimeIndex 过大可能 int 溢出。
4. SearchKey 是旧一维索引设计残留，不应继续作为唯一性判断。
```

---

## 5.2 修改文件

```text
LayerBase/Actor/EventStream/EventStreamRuntimeBase.cs
LayerBase/Actor/EventStream/EventStreamRuntime.cs
LayerBase/Actor/Storage/ActorWorld.cs
```

---

## 5.3 修改 EventStreamRuntimeBase

增加：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// EventStreamRuntime 基类。
///
/// 作用：
/// 提供类型擦除后的事件流运行时接口。
/// </summary>
internal abstract class EventStreamRuntimeBase : IEventStreamCenterRuntime
{
    /// <summary>
    /// 当前 EventStreamRuntime 所属 ActorWorld 的运行时编号。
    /// </summary>
    public abstract int RuntimeIndex { get; }

    /// <summary>
    /// 当前 EventStreamRuntime 所属 Actor archetype 编号。
    /// </summary>
    public abstract int ArchetypeId { get; }

    /// <summary>
    /// 当前事件类型 ID。
    /// </summary>
    public abstract int EventTypeId { get; }

    public abstract bool IsEmpty { get; }

    public abstract int Pump(int maxCount);

    public abstract void UnregisterHandler(int slotIndex);
}
```

---

## 5.4 修改 EventStreamRuntime<TEvent>

删除：

```text
SearchKey
MakeLegacySearchKey
```

增加：

```csharp
/// <summary>
/// 当前 EventStreamRuntime 所属 ActorWorld 的运行时编号。
/// </summary>
public override int RuntimeIndex => _runtimeIndex;

/// <summary>
/// 当前 EventStreamRuntime 所属 Actor archetype 编号。
/// </summary>
public override int ArchetypeId => _archetypeId;
```

---

## 5.5 修改 ActorWorld.GetOrCreateEventStreamRuntime

```csharp
internal EventStreamRuntime<TEvent> GetOrCreateEventStreamRuntime<TEvent>(
    ActorEventStreamPlan<TEvent> plan,
    int archetypeId = 0)
    where TEvent : struct
{
    foreach (IEventStreamCenterRuntime existing in _eventStreamRuntimes)
    {
        if (existing is EventStreamRuntime<TEvent> typedExisting &&
            typedExisting.RuntimeIndex == RuntimeIndex &&
            typedExisting.ArchetypeId == archetypeId)
        {
            return typedExisting;
        }
    }

    var runtime = new EventStreamRuntime<TEvent>(
        RuntimeIndex,
        archetypeId,
        plan.StreamOptions);

    _eventStreamRuntimes.Add(runtime);

    EventStreamRuntime<TEvent>.BindWorld(runtime);

    _eventStreamUnbinders.Add(() =>
    {
        EventStreamRuntime<TEvent>.UnbindWorld(
            RuntimeIndex,
            archetypeId);
    });

    return runtime;
}
```

---

## 5.6 验收测试

```text
1. 创建超过 1024 个 eventTypeId 的事件类型。
2. 创建超过 1024 个 archetype。
3. 确认 GetOrCreateEventStreamRuntime 不出现复用错误。
4. 确认不同 archetype 下的同一个 TEvent 不共用错误 center。
```

---

# 6. P2：明确 Enable=false 语义

## 6.1 当前状态

`EventStreamCenter.Dispatch` 当前只检查：

```text
1. slotIndex 是否越界。
2. generation 是否匹配。
3. handler 是否存在。
```

不检查 Actor 是否 Enabled。

---

## 6.2 推荐语义

建议当前阶段采用：

```text
Enable=false 只影响生命周期调度。
即：
不跑 Update / LateUpdate / FixedUpdate。
但仍然允许接收 EventStream 事件。
```

原因：

```text
1. 这样不需要修改 EventStreamCenter 热路径。
2. 禁用更新但仍可接收唤醒事件是合理业务模型。
3. 如果强行让 Enable=false 阻断所有事件，会使 Enable 语义过重。
```

---

## 6.3 需要补文档

在 Actor 文档中明确：

```text
Actor Enable=false：
1. 不参与 IUpdate / ILateUpdate / IFixedUpdate。
2. 不影响 PostTo / EventStream 事件接收。
3. 如果业务希望禁用后不处理事件，应在 handler 内自行检查状态，或后续使用 ActorPostPolicy 扩展。
```

---

## 6.4 如果未来要实现阻断事件

再考虑新增：

```text
EventStreamCenter<TEvent>._enabledBySlot
ActorBehaviourEntry.StreamSetEnabled
TypedActorStorage.SetEnable 中同步更新 EventStreamCenter
Dispatch 时检查 enabledBySlot
```

当前不建议立即修改。

---

# 7. P3：ActorWorldRuntimeIndexAllocator 增加 DEBUG 防御

## 7.1 修改文件

```text
LayerBase/Actor/Storage/ActorWorldRuntimeIndexAllocator.cs
```

---

## 7.2 目标代码

```csharp
namespace LayerBase.Actor;

internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;
    private static readonly Stack<int> s_free = new();

#if DEBUG
    /// <summary>
    /// DEBUG 下记录当前已租出的 runtimeIndex。
    ///
    /// 作用：
    /// 检测重复 Rent、重复 Return、Return 未租出的 index。
    /// </summary>
    private static readonly HashSet<int> s_rented = new();
#endif

    public static int Rent()
    {
        lock (s_free)
        {
            int index = s_free.Count > 0
                ? s_free.Pop()
                : s_nextIndex++;

#if DEBUG
            if (!s_rented.Add(index))
            {
                throw new InvalidOperationException(
                    $"ActorWorld runtime index {index} was rented twice.");
            }
#endif

            return index;
        }
    }

    public static void Return(int index)
    {
        lock (s_free)
        {
#if DEBUG
            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot return negative ActorWorld runtime index {index}.");
            }

            if (!s_rented.Remove(index))
            {
                throw new InvalidOperationException(
                    $"ActorWorld runtime index {index} was returned but is not currently rented.");
            }
#endif

            s_free.Push(index);
        }
    }
}
```

---

# 8. P3：EventStreamSegmentPool.Clear 清理补强

## 8.1 修改文件

```text
LayerBase/Actor/EventStream/EventStreamSegmentPool.cs
```

---

## 8.2 目标代码

```csharp
/// <summary>
/// 清空池中所有 Segment。
///
/// 作用：
/// 1. 断开池内 Segment 链表。
/// 2. 如果 TEvent 含引用字段，清理 Items 中可能残留的引用。
/// 3. 避免 Clear 后由池链表延长对象生命周期。
/// </summary>
public void Clear()
{
    EventStreamSegment<TEvent>? current = _first;

    while (current != null)
    {
        EventStreamSegment<TEvent>? next = current.Next;

        current.Reset(_clearItemsOnReturn);

        current = next;
    }

    _first = null;
    _count = 0;
}
```

---

# 9. 总体验收清单

## 9.1 Reset 后 metadata 恢复

```text
Build<MoveEvent>()
LayerHub.Reset()
Build<MoveEvent>()
```

预期：

```text
两次都读取到 MoveEventMetaData 自定义配置。
```

---

## 9.2 Dispose 后旧引用不能投递

```text
worldA.CreateActor()
worldA.Dispose()
worldB.CreateActor()
worldA.PostTo(oldActorId, event)
```

预期：

```text
worldB 不收到事件。
```

---

## 9.3 SearchKey 删除验证

```text
多个 archetype + 多个 event type 下，
GetOrCreateEventStreamRuntime 仍然返回正确 runtime。
```

---

## 9.4 性能回归

重点跑：

```text
Actor: PostTo + Pump ×10000
Full Pipeline ×10000
Actor Cold: New World + Create + Destroy ×1000
Pooled Actor Runtime: Rent + Return ×1000
```

预期：

```text
1. 不恢复 100MB 冷启动分配。
2. 不恢复 64KB Rent + Return 分配。
3. 主热路径无大额 GC。
4. EventMetaData 配置在 Reset 后仍然生效。
```

---

# 10. 最终结论

当前最需要优先修的是：

```text
EventMetaData replay action
ActorWorld state guard
删除 SearchKey
```

这三项属于真实逻辑风险，不是单纯性能优化。

`Enable=false` 可以先通过文档明确语义，不一定马上改热路径。

`RuntimeIndexAllocator DEBUG 防御` 和 `SegmentPool.Clear` 是健壮性补强，建议一并提交。
