# layerbase-high-risk-fixes.md

## 1. 目标

本文档用于修复当前 LayerBase 代码中剩余的高危逻辑漏洞。

范围：

```text
1. EventMetaData 在 LayerHub.Reset 后无法自动恢复。
2. EventStreamRuntime 仍保留旧 SearchKey 比较逻辑。
3. ActorWorld Dispose / Stopping 后旧引用仍可能 PostTo。
4. Enable=false 是否阻断 EventStream Dispatch 的语义缺口。
5. ActorWorldRuntimeIndexAllocator 缺少 DEBUG 防御。
6. EventStreamSegmentPool.Clear 没有重置池内 Segment。
```

本文档只处理逻辑正确性，不推翻当前 EventStream + ProjectedRef 架构。

---

## 2. 当前已确认较健康的部分

当前代码已经完成以下关键修复：

```text
1. EventStreamRuntime 已从一维巨大稀疏数组改为 s_byRuntime[runtimeIndex][archetypeId]。
2. EventStreamSegmentPool.Return 已经先 Reset，再判断是否保留。
3. EventStreamSegment.Reset 已支持 clearItems，用于清理包含引用字段的事件。
4. ActorBehaviourEntry 已增加 StreamUnregister。
5. ActorTypeMetaBuilder.AddBehaviour 已生成强泛型注销委托。
6. TypedActorStorage.UnregisterStreamHandlers 已走 entry.StreamUnregister，不再遍历全部 runtime。
```

剩余问题主要集中在“静态缓存生命周期一致性”和“world 生命周期隔离”。

---

## 3. P0：修复 EventMetaData Reset 后无法恢复

### 3.1 问题

当前 `EventMetaDataRegistry.GetActorMailOptions<TEvent>()` 会调用 `EventMetaDataAutoRegister<TEvent>.EnsureInitialized()`。

当前模型的问题是：

```text
第一次 Build<TEvent>()
→ RuntimeHelpers.RunClassConstructor(typeof(TEvent).TypeHandle)
→ TEvent.static ctor 注册 EventMetaData
→ EventMetaDataAutoRegister<TEvent>.s_initialized = true

LayerHub.Reset()
→ EventMetaDataHandler.Clear()
→ EventMetaData 表被清空

第二次 Build<TEvent>()
→ EnsureInitialized 看到 s_initialized = true
→ 直接 return
→ 不会重新注册 EventMetaData
→ EventStreamOptions 回落到默认值
```

根因：

```text
静态构造函数只会执行一次。
LayerHub.Reset 会清空注册表。
bool s_initialized 不能表达“当前注册表里是否还有该元数据”。
```

### 3.2 修改文件

```text
LayerBase/Event/EventMetaData/EventMetaDataRegistry.cs
LayerBase.Generator/LayerBase.Generator/EventMetaDataGenerator.cs
```

### 3.3 修改 EventMetaDataAutoRegister<TEvent>

将 `bool s_initialized` 改为：

```text
1. s_classConstructorTriggered：只表示是否触发过 TEvent 静态构造函数。
2. s_replay：保存可重复执行的注册动作。
3. EnsureInitialized：每次读取元数据前都 replay 一次。
```

目标代码：

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Event.EventMetaData;

/// <summary>
/// EventMetaData 自动注册器。
///
/// 作用：
/// 1. 第一次读取元数据时触发 TEvent 的静态构造函数。
/// 2. 静态构造函数负责设置可重复执行的注册动作。
/// 3. 每次读取元数据前都 replay 一次注册动作，确保 LayerHub.Reset 后能恢复元数据。
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
    /// 它不代表 EventMetaData 当前已经存在，因为注册表可能被 LayerHub.Reset 清空。
    /// </summary>
    private static bool s_classConstructorTriggered;

    /// <summary>
    /// 可重复执行的元数据注册动作。
    ///
    /// 由源生成器写入 TEvent.static ctor 中设置。
    /// </summary>
    private static Action? s_replay;

    /// <summary>
    /// 设置可重复执行的注册动作。
    /// </summary>
    /// <param name="replay">
    /// 由源生成器生成的注册动作。
    /// 该动作内部应调用 EventMetaDataRegistry.RegisterMetaData&lt;TEvent&gt;(...)。
    /// </param>
    public static void SetReplay(Action replay)
    {
        s_replay = replay ?? throw new ArgumentNullException(nameof(replay));
    }

    /// <summary>
    /// 确保 TEvent 的元数据已注册。
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

注意：

```text
EventMetaDataAutoRegister<TEvent> 必须是 public。
```

原因：

```text
EventMetaDataGenerator 生成的代码可能位于业务程序集或 Benchmark 程序集。
如果 EventMetaDataAutoRegister<TEvent> 是 internal，生成代码跨程序集无法访问。
```

### 3.4 修改 EventMetaDataGenerator 输出

当前生成目标类似：

```csharp
static MoveEvent()
{
    EventMetaDataRegistry.RegisterMetaData<MoveEvent>(
        new MoveEventMetaData());
}
```

改成：

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

生成器中可替换为：

```csharp
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

### 3.5 验收测试

```text
1. 第一次 ActorEventStreamPlanBuilder.Build<MoveEvent>() 能读取 MoveEventMetaData。
2. 调用 LayerHub.Reset()。
3. 再次 ActorEventStreamPlanBuilder.Build<MoveEvent>() 仍能读取 MoveEventMetaData。
4. SegmentCapacity / MaxRetainedSegments 不回落到默认值。
```

---

## 4. P1：删除 EventStreamRuntime 旧 SearchKey 逻辑

### 4.1 问题

`EventStreamRuntime<TEvent>` 已经改为：

```text
s_byRuntime[runtimeIndex][archetypeId]
```

但 `ActorWorld.GetOrCreateEventStreamRuntime<TEvent>()` 仍然用旧 key：

```csharp
int searchKey = (RuntimeIndex << 20) | (archetypeId << 10) | eventTypeId;
```

风险：

```text
1. eventTypeId 超过 1023 后可能污染 archetypeId 位区间。
2. archetypeId 超过 1023 后可能污染 runtimeIndex 位区间。
3. runtimeIndex 足够大时可能 int 溢出。
4. SearchKey 已经不是必要设计，继续保留会制造隐性碰撞。
```

### 4.2 修改文件

```text
LayerBase/Actor/EventStream/EventStreamRuntimeBase.cs
LayerBase/Actor/EventStream/EventStreamRuntime.cs
LayerBase/Actor/Storage/ActorWorld.cs
```

### 4.3 修改 EventStreamRuntimeBase

补充：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// EventStreamRuntime 非泛型基类。
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
    /// 当前 EventStreamRuntime 对应的事件类型 ID。
    /// </summary>
    public abstract int EventTypeId { get; }

    public abstract bool IsEmpty { get; }

    public abstract int Pump(int maxCount);

    public abstract void UnregisterHandler(int slotIndex);
}
```

如果当前基类已有 `EventTypeId`、`IsEmpty`、`Pump`、`UnregisterHandler`，只补 `RuntimeIndex` 和 `ArchetypeId`。

### 4.4 修改 EventStreamRuntime<TEvent>

删除：

```text
SearchKey
MakeLegacySearchKey
```

增加：

```csharp
public override int RuntimeIndex => _runtimeIndex;

public override int ArchetypeId => _archetypeId;
```

### 4.5 修改 ActorWorld.GetOrCreateEventStreamRuntime

目标代码：

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

### 4.6 验收测试

```text
1. 创建多个 archetype，注册相同 TEvent。
2. 确认它们的 EventStreamCenter 不互相污染。
3. 构造 eventTypeId > 1023 的场景。
4. 构造 archetypeId > 1023 的场景。
5. 不出现 key 碰撞。
```

---

## 5. P1：ActorWorld 公开入口增加状态防护

### 5.1 问题

`ActorWorld.Dispose()` 会：

```text
1. 执行 EventStream Unbind。
2. 清空 runtime 列表。
3. 归还 RuntimeIndex。
```

如果外部仍持有旧 `ActorWorld` 引用并继续 `PostTo`，而 `RuntimeIndex` 已被新 `ActorWorld` 复用，就可能误投递到新 world。

### 5.2 修改文件

```text
LayerBase/Actor/Storage/ActorWorld.Post.cs
LayerBase/Actor/Storage/ActorWorld.Create.cs
其他公开入口所在文件
```

### 5.3 新增状态检查方法

```csharp
/// <summary>
/// 当前 ActorWorld 是否允许执行公开操作。
/// </summary>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool CanUseWorldFast()
{
    return _state is ActorWorldState.Created
        or ActorWorldState.Building
        or ActorWorldState.Running;
}
```

### 5.4 修改 PostTo

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

### 5.5 修改 PostToMany

为了避免循环内重复检查 world 状态，可以新增内部 unchecked 方法：

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

然后：

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

### 5.6 修改 CreateActor

```csharp
public TActor CreateActor<TActor>(bool usePool = false)
    where TActor : class, IActor, new()
{
    if (!CanUseWorldFast())
    {
        throw new ObjectDisposedException(nameof(ActorWorld));
    }

    // 原逻辑保持不变
}
```

### 5.7 验收测试

```text
1. 创建 worldA。
2. worldA 创建 actorA。
3. Dispose worldA。
4. 创建 worldB，使 RuntimeIndex 复用。
5. 用旧 worldA.PostTo(actorA.Id, event)。
6. actorB 不应收到事件。
```

---

## 6. P2：明确 Enable=false 是否阻断 EventStream Dispatch

### 6.1 当前问题

`EventStreamCenter.Dispatch` 当前只检查：

```text
slotIndex 越界
generation 匹配
handler 是否存在
```

它不检查 Actor 是否启用。

如果你的语义是：

```text
Enable=false 后 Actor 不应该接收普通事件
```

则当前存在逻辑漏洞。

如果你的语义是：

```text
Enable=false 只停止 Update/LateUpdate/FixedUpdate，但仍允许接收事件
```

则当前逻辑可以保留，但必须写入文档。

### 6.2 推荐语义

建议定义为：

```text
Enable=false：
不参与生命周期 Update/LateUpdate/FixedUpdate；
是否接收事件由 ActorPostPolicy 或 EventMetaData 决定。
```

原因：

```text
1. 某些 Actor 禁用渲染/更新后，仍可能需要接收唤醒事件。
2. 强行禁用所有事件会导致业务语义过重。
3. 当前 EventStreamCenter 不存 Enabled 表，保持轻量。
```

### 6.3 如果要阻断事件

需要修改：

```text
EventStreamCenter<TEvent>
ActorBehaviourEntry
ActorTypeMetaBuilder
TypedActorStorage.SetEnable
```

方案：

```text
1. EventStreamCenter 增加 bool[] _enabledBySlot。
2. RegisterHandler 写入 enabled 状态。
3. SetEnable 时通过 entry.StreamSetEnabled 更新 EventStreamCenter。
4. Dispatch 时检查 _enabledBySlot[slotIndex]。
```

不建议立刻做，除非业务语义明确要求禁用后不收事件。

---

## 7. P3：ActorWorldRuntimeIndexAllocator 增加 DEBUG 防御

### 7.1 问题

当前 allocator 直接：

```text
Rent：从 free 栈 pop 或 s_nextIndex++
Return：直接 push
```

缺少 DEBUG 检查：

```text
1. 是否重复 Return。
2. 是否 Return 负数。
3. 是否 Return 未租出的 index。
```

### 7.2 修改文件

```text
LayerBase/Actor/Storage/ActorWorldRuntimeIndexAllocator.cs
```

### 7.3 目标代码

```csharp
namespace LayerBase.Actor;

internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;
    private static readonly Stack<int> s_free = new();

#if DEBUG
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

## 8. P3：EventStreamSegmentPool.Clear 重置池内 Segment

### 8.1 问题

当前 `Return` 已经安全：

```text
先 Reset，再决定是否入池。
```

但 `Clear` 只断开 `_first`，建议让它也重置池中 Segment。

### 8.2 修改文件

```text
LayerBase/Actor/EventStream/EventStreamSegmentPool.cs
```

### 8.3 目标代码

```csharp
/// <summary>
/// 清空池中所有 Segment。
///
/// 作用：
/// 1. 断开池内链表。
/// 2. 如果 TEvent 含引用字段，清理 Items 中残留的引用。
/// 3. 避免 Clear 后仍通过链表上的旧引用延长对象生命周期。
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

## 9. 总体验收清单

### 9.1 EventMetaData Reset 测试

```text
1. Build<MoveEvent>()。
2. 检查读取到自定义 SegmentCapacity。
3. LayerHub.Reset()。
4. 再次 Build<MoveEvent>()。
5. 检查仍读取到自定义 SegmentCapacity。
```

### 9.2 EventStreamRuntime 唯一性测试

```text
1. 同一个 TEvent。
2. 创建多个 archetype。
3. 各自注册 handler。
4. 各自 PostTo。
5. 确认不会互相收到事件。
```

### 9.3 Dispose 后误投递测试

```text
1. worldA 创建 actorA。
2. Dispose worldA。
3. worldB 创建 actorB。
4. 用旧 worldA.PostTo(actorA.Id, event)。
5. actorB 不应收到事件。
```

### 9.4 SegmentPool 引用清理测试

```text
1. 创建含引用字段的 struct event。
2. 投递大量事件。
3. Pump 完成。
4. Clear pool。
5. 确认旧引用不会被池长期持有。
```

### 9.5 Benchmark 复测

重点观察：

```text
Actor Cold: New World + Create + Destroy ×1000
Pooled Actor Runtime: Rent + Return ×1000
Actor: PostTo + Pump ×10000
Full Pipeline ×10000
```

预期：

```text
1. 主热路径保持 0 大额 GC。
2. Actor Cold 不回到 100MB。
3. Rent + Return 不回到 64KB。
4. EventMetaData 配置在 Reset 后仍然生效。
```

---

## 10. 最终建议

优先级：

```text
P0：EventMetaData Reset 后无法恢复。
P1：ActorWorld Dispose 后旧引用仍可 PostTo。
P1：删除 SearchKey 旧逻辑。
P2：明确 Enable=false 的事件接收语义。
P3：补 RuntimeIndexAllocator DEBUG 防御。
P3：补 SegmentPool.Clear 清理。
```

当前最大风险不是性能，而是静态状态生命周期一致性。
