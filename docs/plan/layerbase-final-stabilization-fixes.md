# layerbase-final-stabilization-fixes.md

## 1. 目标

本文档用于完成 LayerBase 当前阶段的最后一批稳定性修复。

当前框架主体已经可以暂时成型，剩余问题主要是运行时边界防护，而不是架构方向问题。

本次只修：

```text
1. ActorWorld 剩余公开入口状态防护。
2. ActorWorldRuntimeIndexAllocator 的线程安全和 DEBUG 校验。
3. DelayPost / DelayAsk 在 Stopping 状态下的行为。
4. 对应回归测试。
```

不再继续推翻：

```text
EventStream
ProjectedRef
EventMetaData replay
StreamUnregister
ActorBehaviour handler cache
PostTo 主热路径
```

---

## 2. 当前已经完成的部分

已确认完成：

```text
1. EventMetaData 使用 replay action，LayerHub.Reset 后可以恢复元数据。
2. EventStreamRuntime 删除旧 SearchKey，改成 RuntimeIndex + ArchetypeId 显式比较。
3. PostTo / PostToMany 已经接入 CanUseWorldFast。
4. CreateActor 已经接入 CanUseWorldFast。
5. EventStreamSegmentPool.Clear 已经遍历 Reset 池内 Segment。
6. Enable=false 语义已明确：不影响 EventStream 收事件，只跳过生命周期更新。
```

剩余重点：

```text
1. DispatchNow / ImmediatelyAsk / DestroyActor / IsAlive / IsEnable / SetEnable。
2. ActorWorldRuntimeIndexAllocator。
3. DelayPost / DelayAsk 的 Stopping 语义。
```

---

## 3. 修复 ActorWorld.Dispatch.cs

### 3.1 问题

`DispatchNow` 在 `ActorWorld.Dispose()` 后仍可能通过旧 world 引用访问 `_archetypes`。

### 3.2 目标代码

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public DispatchResult DispatchNow<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        // CanUseWorldFast：
        // 检查当前 ActorWorld 是否还处于 Created / Building / Running。
        // 如果已经 Stopping 或 Disposed，就不能再访问 archetype / storage。
        if (!CanUseWorldFast())
        {
            return DispatchResult.Failure(
                DispatchFailureKind.ActorNotFound,
                "ActorWorld is not running.");
        }

        // actorId.ArchetypeId：
        // Actor 所属 archetype 编号。
        // 使用 uint 转换同时拦截负数和越界值。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.InvalidActorId,
                "Invalid ActorId.ArchetypeId.");
        }

        // 转交给 archetype。
        // archetype 内部继续检查 slotIndex、generation、PendingDestroy。
        return _archetypes[actorId.ArchetypeId]
            .DispatchNow(actorId, in value);
    }
}
```

---

## 4. 修复 ActorWorld.Call.cs

### 4.1 问题

`ImmediatelyAsk` 在 world dispose 后仍可能访问旧 storage。

### 4.2 目标代码

```csharp
using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // cancellationToken：
        // 调用方传入的取消令牌。
        // 如果已经取消，直接返回 canceled task，避免进入 ActorWorld。
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        // CanUseWorldFast：
        // 防止 Stopping / Disposed 后继续访问旧 storage。
        // 这里返回 Disposed，方便上层区分生命周期失败。
        if (!CanUseWorldFast())
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.Disposed);
        }

        // actorId.ArchetypeId：
        // 检查目标 Actor 所属 archetype 是否存在。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.InvalidActorId);
        }

        // 转交给 archetype 处理具体 call 行为。
        return _archetypes[actorId.ArchetypeId]
            .ImmediatelyAsk<TRequest, TResponse>(
                actorId,
                in request,
                cancellationToken);
    }

    public LBTask<TResponse> Call<TActor, TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        // Call 是类型提示入口。
        // 真正状态检查由 Ask / ImmediatelyAsk 这类最终入口负责。
        return Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

---

## 5. 修复 ActorWorld.Destroy.cs

### 5.1 问题

`DestroyActor` 和 `IsAlive` 在 world dispose 后仍可能读取旧 storage。

### 5.2 目标代码

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool DestroyActor(ActorId actorId)
    {
        // Stopping / Disposed 后禁止销毁 Actor。
        if (!CanUseWorldFast())
        {
            return false;
        }

        // actorId.ArchetypeId：
        // 检查 Actor 所属 archetype 是否存在。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        bool marked = _archetypes[actorId.ArchetypeId]
            .MarkPendingDestroy(actorId);

        if (marked)
        {
            _pendingDestroyCount++;
        }

        return marked;
    }

    public bool IsAlive(ActorId actorId)
    {
        // world 不可用时，外部观察结果统一视为 false。
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId]
            .IsAlive(actorId);
    }

    private void SweepPendingDestroy()
    {
        if (_pendingDestroyCount <= 0)
        {
            return;
        }

        foreach (BehaviourArchetype archetype in _archetypes)
        {
            archetype.SweepPendingDestroy(this);
        }

        _pendingDestroyCount = 0;
    }
}
```

---

## 6. 修复 ActorWorld.Lifecycle.cs

### 6.1 问题

`IsEnable` 和 `SetEnable` 在 world dispose 后仍可能访问旧 storage。

### 6.2 目标代码

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool IsEnable(ActorId actorId)
    {
        // world 不可用时，统一视为未启用。
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId]
            .IsEnable(actorId);
    }

    public bool SetEnable(ActorId actorId, bool enable)
    {
        // world 不可用时，禁止修改 enable 状态。
        if (!CanUseWorldFast())
        {
            return false;
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId]
            .SetEnable(actorId, enable);
    }
}
```

---

## 7. 修复 ActorWorld.Delay.cs

### 7.1 推荐语义

`Stopping` 后不允许新增 `DelayPost / DelayAsk`。

理由：

```text
1. RuntimeStop 会清空 DelayScheduler。
2. Stopping 表示运行时正在停止。
3. 继续调度延迟任务容易出现静默丢弃或边界触发。
```

### 7.2 目标代码

```csharp
using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public DelayPostHandle DelayPost<TEvent>(
        ActorId actorId,
        in TEvent value,
        float delaySeconds)
        where TEvent : struct
    {
        // 检查 world 是否允许新增延迟任务。
        EnsureDelayAvailable();

        return DelayScheduler.Schedule(
            new DelayPostTask<TEvent>(this, actorId, in value),
            delaySeconds);
    }

    public LBTask<TResponse> DelayAsk<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        float delaySeconds,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        EnsureDelayAvailable();

        // source：
        // 用于在延迟任务触发后把结果写回给调用方。
        var source = new LBTaskCompletionSource<TResponse>();

        DelayScheduler.Schedule(
            new DelayAskTask<TRequest, TResponse>(
                this,
                actorId,
                in request,
                cancellationToken,
                source),
            delaySeconds);

        return source.Task;
    }

    private void EnsureDelayAvailable()
    {
        // CanUseWorldFast：
        // Created / Building / Running 允许。
        // Stopping / Disposed 拒绝。
        if (!CanUseWorldFast())
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }
    }
}
```

---

## 8. 修复 ActorWorldRuntimeIndexAllocator.cs

### 8.1 问题

当前风险：

```text
1. Stack<int> 并发 Push / Pop 不安全。
2. s_nextIndex++ 并发不安全。
3. DEBUG 下 s_rented 声明了但未使用。
4. 重复 Return 无法被发现。
5. Return 未租出的 index 无法被发现。
```

### 8.2 目标代码

```csharp
namespace LayerBase.Actor;

internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;

    /// <summary>
    /// 可复用 runtimeIndex 栈。
    ///
    /// 作用：
    /// ActorWorld.Dispose 后归还 RuntimeIndex，
    /// 后续新 ActorWorld 可以复用该编号。
    /// </summary>
    private static readonly Stack<int> s_free = new();

#if DEBUG
    /// <summary>
    /// DEBUG 下记录当前已租出的 runtimeIndex。
    ///
    /// 作用：
    /// 1. 检查重复 Rent。
    /// 2. 检查重复 Return。
    /// 3. 检查 Return 未租出的 index。
    /// </summary>
    private static readonly HashSet<int> s_rented = new();
#endif

    public static int Rent()
    {
        // s_free 和 s_nextIndex 都是静态共享状态。
        // 必须加锁，避免并发创建 ActorWorld 时出现重复 index。
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
        // Return 和 Rent 必须用同一把锁。
        // 否则 Stack<int> 的内部状态可能被并发写坏。
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

## 9. 回归测试建议

修改文件：

```text
LayerBase.Test/HighRiskFixRegressionTests.cs
```

### 9.1 DispatchNow after Dispose

```csharp
[Test]
public void Dispatch_now_returns_failure_after_dispose()
{
    var world = new ActorWorld();
    HighRiskActor actor = world.CreateActor<HighRiskActor>();
    ActorId actorId = actor.GetActorId();

    world.Dispose();

    DispatchResult result = world.DispatchNow(
        actorId,
        new HighRiskActorEvent(4));

    Assert.That(result.IsSuccess, Is.False);
    Assert.That(result.FailureKind, Is.EqualTo(DispatchFailureKind.ActorNotFound));
    Assert.That(HighRiskTrace.Entries, Is.Empty);
}
```

---

### 9.2 ImmediatelyAsk after Dispose

```csharp
[Test]
public void Immediately_ask_returns_disposed_failure_after_dispose()
{
    var world = new ActorWorld();
    HighRiskActor actor = world.CreateActor<HighRiskActor>();
    ActorId actorId = actor.GetActorId();

    world.Dispose();

    ActorCallException? error = Assert.Throws<ActorCallException>(() =>
        world.ImmediatelyAsk<HighRiskCallRequest, HighRiskCallResponse>(
                actorId,
                new HighRiskCallRequest(5))
            .GetAwaiter()
            .GetResult());

    Assert.That(error!.FailureKind, Is.EqualTo(ActorCallFailureKind.Disposed));
    Assert.That(HighRiskTrace.Entries, Is.Empty);
}
```

---

### 9.3 Destroy / Enable after Dispose

```csharp
[Test]
public void Enable_and_destroy_queries_return_false_after_dispose()
{
    var world = new ActorWorld();
    HighRiskActor actor = world.CreateActor<HighRiskActor>();
    ActorId actorId = actor.GetActorId();

    world.Dispose();

    Assert.That(world.IsEnable(actorId), Is.False);
    Assert.That(world.SetEnable(actorId, false), Is.False);
    Assert.That(world.IsAlive(actorId), Is.False);
    Assert.That(world.DestroyActor(actorId), Is.False);
}
```

---

## 10. 验收清单

修复后跑：

```text
LayerBase.Test/HighRiskFixRegressionTests.cs
```

重点通过：

```text
1. Event_metadata_replays_after_layerhub_reset
2. Event_stream_runtime_is_unique_per_archetype
3. Disposed_world_does_not_post_into_reused_runtime_index
4. Dispatch_now_returns_failure_after_dispose
5. Immediately_ask_returns_disposed_failure_after_dispose
6. Delay_post_throws_after_dispose
7. Enable_and_destroy_queries_return_false_after_dispose
8. Disabled_actor_still_receives_events_while_lifecycle_updates_are_skipped
9. Event_stream_segment_pool_clear_resets_retained_segments
```

再跑 benchmark：

```text
Actor: PostTo + Pump ×10000
Full Pipeline ×10000
Actor Cold: New World + Create + Destroy ×1000
Pooled Actor Runtime: Rent + Return ×1000
```

期望：

```text
1. 主热路径仍然 0 大额 GC。
2. Actor Cold 不回到 100MB。
3. Rent + Return 不回到 64KB。
4. EventMetaData Reset 后配置仍然生效。
```

---

## 11. 最终状态

完成本文档后，LayerBase 可以进入：

```text
Prototype Stable
```

下一阶段建议转向：

```text
1. README 心智模型。
2. Benchmark 报告。
3. 最小 Demo。
4. API 命名整理。
5. Unity 集成样例。
```
