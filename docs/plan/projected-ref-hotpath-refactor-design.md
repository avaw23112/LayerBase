# ProjectedRef Hot Path Refactor Design

## 1. 目标

本文档专门定义 `ProjectedActorRef` 的改造方案。

目标：

```text
ECS Query / Projection Post 热路径
不再把 ProjectedActorMeta 当作主要 ActorId 读取入口，
而是直接读取 ProjectedActorRef.ActorId。
```

`ProjectedActorMeta` 继续保留，但它只负责 Projection 生命周期数据：

```text
ActorTypeId
ReleasePolicy
KeepAliveTicks
ActiveListIndex
ProjectedActorState
```

`ProjectedActorRef` 负责热路径缓存：

```text
Entity -> ActorId
```

最终目标链路：

```text
ProjectionExecutor
→ ref ProjectedActorRef actorRef
→ ActorId actorId = actorRef.ActorId
→ batch.Add(actorId, event) / actorWorld.PostTo(actorId, event)
```

---

## 2. 当前真实代码状态

### 2.1 ProjectedActorRef 已存在

当前 `LayerBase/ECS/Projection/ProjectedActorRef.cs` 已有公开组件：

```csharp
public struct ProjectedActorRef
{
    public ActorId ActorId;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    public ProjectedActorRef(ActorId actorId)
    {
        ActorId = actorId;
    }
}
```

当前定位是：

```text
公开 ActorId 缓存组件。
业务 ECS Query 可以直接拿 ActorId。
避免每帧通过 Entity 反查 ProjectedActorMeta。
```

该方向正确，但模板层尚未完整使用它作为热路径主入口。

---

### 2.2 BindingUtility 已同步写 Ref

当前 `ProjectedActorBindingUtility.Bind` 已经同时写入：

```text
ProjectedActorMeta
ProjectedActorRef
```

现有逻辑：

```csharp
meta.BindActor(actorId);
UpsertRef(world, entity, actorId);
```

`Clear` 也已经同时清理：

```csharp
meta.ClearActor();
UpsertRef(world, entity, ActorId.Invalid);
```

这说明当前项目已经有 `Meta + Ref` 一致性维护入口。

---

### 2.3 Meta 是 Chunk 内部投影状态

当前 `Chunk.Projection.cs` 里，每个 chunk 内部有：

```csharp
internal ProjectedActorMeta[] ProjectedActors { get; private set; }
```

并提供：

```csharp
internal ref ProjectedActorMeta ProjectionAt(int row)
internal ref ProjectedActorMeta FirstProjection()
```

这说明 `ProjectedActorMeta` 不是普通 ECS 组件，而是 Chunk 旁路存储。

它适合保存 Projection 内部状态，但不适合作为业务 Query 和 Post 热路径的长期入口。

---

### 2.4 ProjectionExecutor.tt 仍以 Meta 为主

当前 `ProjectionExecutor.tt` 的 CollectPostChunk 主要路径是：

```csharp
ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();

ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);

ActorId actorId = meta.ActorId;

if (!actorId.IsValid)
{
    actorId = ProjectedActorBinding.EnsureProjectedActor(...);
}
else
{
    ProjectedActorBinding.TouchProjectedActor(...);
    actorId = meta.ActorId;
}

batch.Add(actorId, in output);
```

问题：

```text
ProjectedActorRef 虽然存在，但 ProjectionExecutor 热路径没有使用它。
ActorId 热路径仍来自 ProjectedActorMeta。
```

---

## 3. 设计原则

### 3.1 Meta 和 Ref 职责分离

`ProjectedActorMeta`：

```text
内部状态
生命周期管理
active list index
release policy
keep alive
actor type id
```

`ProjectedActorRef`：

```text
公开热路径缓存
只负责 ActorId
用于 Query / Post / Batch
```

### 3.2 Ref 是热路径真源

在 Post / Query 热路径中，默认读取：

```csharp
actorRef.ActorId
```

只有以下情况才读 `ProjectedActorMeta`：

```text
1. actorRef.ActorId 无效，需要创建 projected actor。
2. actorRef.ActorId 失效，需要 touch 时发现 actor 已不存在。
3. Sweep / release / unbind 等生命周期路径。
```

### 3.3 所有 Meta.ActorId 写入必须同步 Ref

只允许通过统一工具修改绑定关系：

```text
Bind
Clear
EnsureInvalidRef
MarkProjectable
```

禁止业务代码直接写：

```csharp
meta.ActorId = xxx;
```

---

## 4. ProjectedActorRef 结构调整

当前结构已经足够轻量，不建议增加 `HasActor` 或 `Version` 字段。

原因：

```text
ActorId 已经包含有效性判断。
ActorId.Generation 已经能防止 slot 复用误投。
额外 bool 可能导致结构体填充变大。
额外 version 会增加 Query 热路径读取成本。
```

建议保留最小结构，并补充静态属性和方法：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 的公开 ActorId 缓存组件。
///
/// 作用：
/// 1. 让业务 ECS Query 直接拿到 ActorId。
/// 2. 避免每帧通过 Entity 反查 ProjectedActorMeta。
/// 3. 不暴露 internal ProjectedActorMeta。
/// 4. 作为 Projection Post 热路径的 ActorId 来源。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    /// ActorId 是 ActorWorld 中定位 Actor 的轻量句柄。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// 无效 ProjectedActorRef。
    /// </summary>
    public static ProjectedActorRef Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ProjectedActorRef(ActorId.Invalid);
    }

    /// <summary>
    /// 当前 ActorId 是否有效。
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    /// <summary>
    /// 构造 ProjectedActorRef。
    /// </summary>
    /// <param name="actorId">
    /// 当前 Entity 对应的 ActorId。
    /// </param>
    public ProjectedActorRef(
        ActorId actorId)
    {
        ActorId = actorId;
    }

    /// <summary>
    /// 写入新的 ActorId。
    /// </summary>
    /// <param name="actorId">
    /// 新绑定的 ActorId。
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(
        ActorId actorId)
    {
        ActorId = actorId;
    }

    /// <summary>
    /// 清空 ActorId。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        ActorId = ActorId.Invalid;
    }
}
```

---

## 5. BindingUtility 改造

### 5.1 保留当前 Entity 版本 Bind / Clear

用于非热路径：

```text
ActiveProjectedActorList.Sweep
外部 Bind / Clear
只有 Entity + meta，没有 ref ProjectedActorRef 的路径
```

保留现有方法：

```csharp
Bind(World world, Entity entity, ref ProjectedActorMeta meta, ActorId actorId)
Clear(World world, Entity entity, ref ProjectedActorMeta meta)
```

---

### 5.2 新增 ref 版本 Bind / Clear

用于 ProjectionExecutor 热路径。

避免在已经拿到 `ref ProjectedActorRef` 的情况下再调用：

```csharp
world.Set(entity, actorRef)
```

新增：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 绑定工具。
/// 作用：统一维护 ProjectedActorMeta 和 ProjectedActorRef 的一致性。
/// </summary>
internal static class ProjectedActorBindingUtility
{
    /// <summary>
    /// 绑定 Projected Actor。
    /// </summary>
    /// <param name="meta">
    /// ProjectedActorMeta 引用。
    /// </param>
    /// <param name="actorRef">
    /// ProjectedActorRef 引用。
    /// </param>
    /// <param name="actorId">
    /// 新绑定的 ActorId。
    /// </param>
    /// <remarks>
    /// 作用：
    /// 用于模板热路径。
    /// 当模板已经拿到 ref ProjectedActorRef 时，不需要再 world.Set。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind(
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        ActorId actorId)
    {
        meta.BindActor(actorId);
        actorRef.Bind(actorId);
    }

    /// <summary>
    /// 清理 Projected Actor 绑定。
    /// </summary>
    /// <param name="meta">
    /// ProjectedActorMeta 引用。
    /// </param>
    /// <param name="actorRef">
    /// ProjectedActorRef 引用。
    /// </param>
    /// <remarks>
    /// 作用：
    /// 用于模板热路径。
    /// 当 actor 已经失效时，同时清理 meta 和 ref。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef)
    {
        meta.ClearActor();
        actorRef.Clear();
    }

    /// <summary>
    /// 确保 Entity 上存在 ProjectedActorRef。
    /// </summary>
    /// <param name="world">
    /// ECS World。
    /// </param>
    /// <param name="entity">
    /// 目标 Entity。
    /// </param>
    /// <remarks>
    /// 作用：
    /// ProjectedActorRef 必须在 Entity 被标记为可投影时就添加。
    /// 否则 Query 如果要求 ProjectedActorRef 组件，会跳过尚未创建 Actor 的可投影实体。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInvalidRef(
        World world,
        Entity entity)
    {
        if (world.Has<ProjectedActorRef>(entity))
        {
            return;
        }

        world.Add(
            entity,
            ProjectedActorRef.Invalid);
    }
}
```

---

## 6. MarkProjectable 同步要求

### 6.1 当前风险

如果 `ProjectedActorRef` 只在 Bind 时 Add，那么尚未创建 actor 的 Entity 没有 `ProjectedActorRef` 组件。

这样模板如果改成 Query `ProjectedActorRef`，会出现问题：

```text
可投影但尚未创建 Actor 的 Entity
因为没有 ProjectedActorRef 组件
被 Query 跳过
永远不会进入 EnsureProjectedActor
```

### 6.2 要求

当 Entity 被标记为 Projectable 时，必须立即添加：

```csharp
ProjectedActorRef.Invalid
```

也就是说：

```text
MarkProjected
→ meta.MarkProjected(...)
→ EnsureInvalidRef(world, entity)
```

新增统一 API：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorMarkUtility
{
    /// <summary>
    /// 将 Entity 标记为可投影 Actor。
    /// </summary>
    /// <param name="world">
    /// ECS World。
    /// </param>
    /// <param name="entity">
    /// 目标 Entity。
    /// </param>
    /// <param name="meta">
    /// ProjectedActorMeta 引用。
    /// </param>
    /// <param name="actorTypeId">
    /// Projected Actor 类型 ID。
    /// </param>
    /// <param name="keepAliveTicks">
    /// 保活时间。
    /// </param>
    /// <param name="releasePolicy">
    /// 释放策略。
    /// </param>
    /// <remarks>
    /// 作用：
    /// 保证所有可投影 Entity 都拥有 ProjectedActorRef 组件。
    /// 这样 ProjectionExecutor 可以把 ProjectedActorRef 作为 Query 热路径输入。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkProjected(
        World world,
        Entity entity,
        ref ProjectedActorMeta meta,
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        meta.MarkProjected(
            actorTypeId,
            keepAliveTicks,
            releasePolicy);

        ProjectedActorBindingUtility.EnsureInvalidRef(
            world,
            entity);
    }
}
```

迁移要求：

```text
所有直接调用 meta.MarkProjected 的地方，
统一替换为 ProjectedActorMarkUtility.MarkProjected。
```

---

## 7. ProjectedActorBinding 改造

### 7.1 EnsureProjectedActor 增加 ref ProjectedActorRef 版本

当前：

```csharp
EnsureProjectedActor(
    World world,
    ActorWorld actorWorld,
    Entity entity,
    ref ProjectedActorMeta meta,
    long nowTicks)
```

新增热路径版本：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    /// <summary>
    /// 确保 Entity 拥有 Projected Actor。
    /// </summary>
    /// <param name="world">
    /// ECS World。
    /// </param>
    /// <param name="actorWorld">
    /// ActorWorld。
    /// </param>
    /// <param name="entity">
    /// 当前 Entity。
    /// </param>
    /// <param name="meta">
    /// 当前 Entity 的 ProjectedActorMeta。
    /// </param>
    /// <param name="actorRef">
    /// 当前 Entity 的 ProjectedActorRef。
    /// </param>
    /// <param name="nowTicks">
    /// 当前时间戳。
    /// </param>
    /// <returns>
    /// 有效 ActorId，或 ActorId.Invalid。
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        ActorWorld actorWorld,
        Entity entity,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(
                actorWorld,
                meta.ActorTypeId);

        if (!handle.IsValid)
        {
            actorRef.Clear();
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                meta.KeepAliveTicks);

        ProjectedActorBindingUtility.Bind(
            ref meta,
            ref actorRef,
            handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }
}
```

保留旧版本给非模板路径使用，但内部可以继续走 `BindingUtility.Bind(world, entity, ...)`。

---

### 7.2 TouchProjectedActor 增加 ref ProjectedActorRef 版本

当前 `TouchProjectedActor` 如果 actor 不存在，只会：

```csharp
meta.ClearActor();
```

需要同步清理 `ProjectedActorRef`。

新增：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    /// <summary>
    /// 刷新 Projected Actor 保活时间。
    /// </summary>
    /// <param name="actorWorld">
    /// ActorWorld。
    /// </param>
    /// <param name="meta">
    /// ProjectedActorMeta。
    /// </param>
    /// <param name="actorRef">
    /// ProjectedActorRef。
    /// </param>
    /// <param name="nowTicks">
    /// 当前时间戳。
    /// </param>
    /// <returns>
    /// true：actor 仍然有效。
    /// false：actor 已失效，meta/ref 已清理。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TouchProjectedActor(
        ActorWorld actorWorld,
        ref ProjectedActorMeta meta,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            ProjectedActorBindingUtility.Clear(
                ref meta,
                ref actorRef);

            return false;
        }

        if (!actorWorld.TryGetPooledActor(
                actorId,
                out IPooledActor pooledActor))
        {
            ProjectedActorBindingUtility.Clear(
                ref meta,
                ref actorRef);

            return false;
        }

        pooledActor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                meta.KeepAliveTicks);

        return true;
    }
}
```

---

## 8. ProjectionExecutor.tt 改造

### 8.1 Query 前提

所有 Projection Post 模板必须确保 Query 包含：

```text
ProjectedActorRef
```

原因：

```text
模板热路径需要 ref ProjectedActorRef。
可投影 Entity 在 MarkProjected 时已经会添加 ProjectedActorRef.Invalid。
```

如果当前 Query 构建没有自动包含 `ProjectedActorRef`，需要在 Projection 扩展入口补齐。

---

### 8.2 CollectPostChunk 单事件版本目标

当前模板：

```csharp
ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
```

目标：

```csharp
ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
ref ProjectedActorRef firstActorRef = ref chunk.GetFirst<ProjectedActorRef>();
ref Entity firstEntity = ref chunk.Entities.DangerousGetReference();
```

`chunk.GetFirst<ProjectedActorRef>()` 需要替换为项目当前 Chunk 获取组件首引用的真实 API。

---

### 8.3 单事件 CollectPostChunk 目标代码

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void CollectPostChunk<TEvent>(
    World world,
    ActorWorld actorWorld,
    ref Chunk chunk,
    ProjectionPredicate? predicate,
    ProjectionForEach<TEvent> forEach,
    long nowTicks,
    ref ProjectionBatchBuffer<TEvent> batch)
    where TEvent : struct
{
    ref ProjectedActorMeta firstMeta =
        ref chunk.FirstProjection();

    ref ProjectedActorRef firstActorRef =
        ref chunk.GetFirst<ProjectedActorRef>();

    ref Entity firstEntity =
        ref chunk.Entities.DangerousGetReference();

    int count =
        chunk.Count;

    for (int row = 0; row < count; row++)
    {
        Entity entity =
            Unsafe.Add(
                ref firstEntity,
                row);

        if (predicate != null && !predicate(in entity))
        {
            continue;
        }

        ref ProjectedActorRef actorRef =
            ref Unsafe.Add(
                ref firstActorRef,
                row);

        ActorId actorId =
            actorRef.ActorId;

        ref ProjectedActorMeta meta =
            ref Unsafe.Add(
                ref firstMeta,
                row);

        if (!actorId.IsValid)
        {
            actorId =
                ProjectedActorBinding.EnsureProjectedActor(
                    world,
                    actorWorld,
                    entity,
                    ref meta,
                    ref actorRef,
                    nowTicks);

            if (!actorId.IsValid)
            {
                continue;
            }
        }
        else
        {
            bool alive =
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    ref actorRef,
                    nowTicks);

            if (!alive)
            {
                continue;
            }

            actorId =
                actorRef.ActorId;
        }

        TEvent output =
            default;

        forEach(
            in entity,
            ref output);

        batch.Add(
            actorId,
            in output);
    }
}
```

注意：

```text
ActorId 来自 actorRef。
meta 只用于 Ensure/Touch 生命周期。
Ensure/Touch 必须同步 actorRef。
```

---

### 8.4 多组件模板同步改造

所有以下模板位置都要同步改：

```text
ProjectionExecutor0
ProjectionExecutor0_NE
ProjectionExecutorC
ProjectionExecutorC_NE
ProjectionExecutor Post<TJob>
ProjectionExecutor CollectPostJobChunk
ProjectionExecutor TouchChunk
```

替换规则：

```text
ActorId actorId = meta.ActorId;
```

替换为：

```text
ActorId actorId = actorRef.ActorId;
```

Ensure 规则：

```text
EnsureProjectedActor(world, actorWorld, entity, ref meta, nowTicks)
```

替换为：

```text
EnsureProjectedActor(world, actorWorld, entity, ref meta, ref actorRef, nowTicks)
```

Touch 规则：

```text
TouchProjectedActor(actorWorld, ref meta, nowTicks)
```

替换为：

```text
TouchProjectedActor(actorWorld, ref meta, ref actorRef, nowTicks)
```

---

## 9. TouchChunk 改造

TouchChunk 不投递事件，但同样要同步 Ref。

目标代码：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void TouchChunk(
    World world,
    ActorWorld actorWorld,
    ref Chunk chunk,
    ProjectionPredicate? predicate,
    long nowTicks)
{
    ref ProjectedActorMeta firstMeta =
        ref chunk.FirstProjection();

    ref ProjectedActorRef firstActorRef =
        ref chunk.GetFirst<ProjectedActorRef>();

    ref Entity firstEntity =
        ref chunk.Entities.DangerousGetReference();

    int count =
        chunk.Count;

    for (int row = 0; row < count; row++)
    {
        Entity entity =
            Unsafe.Add(
                ref firstEntity,
                row);

        if (predicate != null && !predicate(in entity))
        {
            continue;
        }

        ref ProjectedActorMeta meta =
            ref Unsafe.Add(
                ref firstMeta,
                row);

        ref ProjectedActorRef actorRef =
            ref Unsafe.Add(
                ref firstActorRef,
                row);

        if (!actorRef.ActorId.IsValid)
        {
            _ = ProjectedActorBinding.EnsureProjectedActor(
                world,
                actorWorld,
                entity,
                ref meta,
                ref actorRef,
                nowTicks);
        }
        else
        {
            _ = ProjectedActorBinding.TouchProjectedActor(
                actorWorld,
                ref meta,
                ref actorRef,
                nowTicks);
        }
    }
}
```

---

## 10. ActiveProjectedActorList 同步要求

当前 `ActiveProjectedActorList.Sweep` 已经在失效、超时、释放时调用：

```csharp
ProjectedActorBindingUtility.Clear(world, entity, ref meta);
```

该方法会 `UpsertRef(world, entity, ActorId.Invalid)`。

保留该逻辑。

额外要求：

```text
1. RemoveDeadAt 不需要处理 ref，因为 Entity 已不存在或 meta 不可获取。
2. RemoveAt 只负责 active list swap-back 和 ActiveListIndex。
3. 所有 release 分支都必须先 Clear meta/ref，再 RemoveAt。
```

---

## 11. ProjectedActorRef 组件生命周期

### 11.1 创建

当 Entity 被标记为 Projectable 时：

```text
Add ProjectedActorRef.Invalid
```

### 11.2 绑定

当 Actor 被创建成功时：

```text
meta.ActorId = actorId
actorRef.ActorId = actorId
meta.State = Active
```

### 11.3 Touch 失败

当 ActorWorld 找不到 pooled actor：

```text
meta.ClearActor()
actorRef.Clear()
```

### 11.4 Sweep 回收

当 actor 超过 keep alive：

```text
actorWorld.ReleaseProjectedActor(...)
ProjectedActorBindingUtility.Clear(world, entity, ref meta)
RemoveAt(...)
```

### 11.5 Entity 删除

如果 Entity 已不存在：

```text
ActiveProjectedActorList.RemoveDeadAt
```

无需额外清 ref，因为组件随 Entity 一起消失。

---

## 12. ProjectionBatchBuffer 配合

ProjectedRef 改造后，batch 收集阶段应只传 ActorId：

```csharp
batch.Add(
    actorRef.ActorId,
    in output);
```

如果已经切换到 EventStream 后端，也可以将模板最终改成：

```csharp
actorWorld.PostTo(
    actorRef.ActorId,
    in output);
```

或者保留 batch：

```csharp
batch.PostTo(actorWorld);
```

推荐顺序：

```text
第一阶段：
ProjectedRef cached + 现有 batch。

第二阶段：
ProjectedRef cached + PostBatch/EventStream batch。
```

---

## 13. Benchmark 设计

新增 benchmark：

```text
Projection: Meta ActorId Read ×1000
Projection: ProjectedActorRef ActorId Read ×1000
Projection: Entity → ActorId Lookup ×1000
Projection: Cached ProjectedActorRef Valid Count ×1000
FullPipeline: Meta ActorId Path ×1000
FullPipeline: ProjectedRef ActorId Path ×1000
```

目标：

```text
ProjectedActorRef 读取路径低于 Entity → ProjectionMeta 反查路径。
FullPipeline 使用 ProjectedRef 后低于旧路径。
Hot path 保持 0 allocation。
```

---

## 14. 单元测试

### 14.1 Bind 同步

```text
Given Entity 已有 ProjectedActorMeta 和 ProjectedActorRef
When Bind actorId
Then meta.ActorId == actorId
And actorRef.ActorId == actorId
```

### 14.2 Clear 同步

```text
Given Entity 已绑定 actor
When Clear
Then meta.ActorId == ActorId.Invalid
And actorRef.ActorId == ActorId.Invalid
```

### 14.3 MarkProjected 添加 Ref

```text
Given Entity 被 MarkProjected
When Entity 尚未创建 Actor
Then Entity 拥有 ProjectedActorRef
And ProjectedActorRef.ActorId == ActorId.Invalid
```

### 14.4 EnsureProjectedActor 同步 Ref

```text
Given actorRef invalid
When EnsureProjectedActor 成功
Then actorRef.ActorId 有效
And meta.ActorId == actorRef.ActorId
```

### 14.5 Touch 失败同步清理

```text
Given actorRef.ActorId 有效但 ActorWorld 找不到 pooled actor
When TouchProjectedActor
Then meta.ActorId invalid
And actorRef.ActorId invalid
```

### 14.6 Sweep 同步清理

```text
Given Projected Actor 超时
When SweepProjectedActors
Then ProjectedActorRef.ActorId invalid
And active list 移除该 Entity
```

---

## 15. 文件修改清单

### 15.1 修改

```text
LayerBase/ECS/Projection/ProjectedActorRef.cs
LayerBase/ECS/Projection/ProjectedActorBindingUtility.cs
LayerBase/ECS/Projection/ProjectedActorBinding.cs
LayerBase/ECS/Projection/ActiveProjectedActorList.cs
LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt
```

### 15.2 新增

```text
LayerBase/ECS/Projection/ProjectedActorMarkUtility.cs
```

### 15.3 测试

```text
LayerBase.Test/ProjectedActorProjectionTests.cs
LayerBase.BenchMark/EcsActorBenchmarks.cs
```

---

## 16. 提交顺序

### Commit 1：ProjectedActorRef API 补强

```text
ProjectedActorRef.Invalid
ProjectedActorRef.Bind
ProjectedActorRef.Clear
```

### Commit 2：BindingUtility ref overload

```text
Bind(ref meta, ref actorRef, actorId)
Clear(ref meta, ref actorRef)
EnsureInvalidRef(world, entity)
```

### Commit 3：MarkProjected 统一入口

```text
ProjectedActorMarkUtility.MarkProjected
替换所有 meta.MarkProjected 直接调用
保证 ProjectedActorRef.Invalid 在 projectable 阶段存在
```

### Commit 4：ProjectedActorBinding overload

```text
EnsureProjectedActor(... ref meta, ref actorRef ...)
TouchProjectedActor(... ref meta, ref actorRef ...)
```

### Commit 5：ProjectionExecutor.tt 改造

```text
模板增加 ProjectedActorRef 组件读取
ActorId 来源从 meta.ActorId 改为 actorRef.ActorId
Ensure/Touch 改为 ref actorRef overload
```

### Commit 6：Benchmark 和测试

```text
ProjectedRef cached benchmark
FullPipeline 对照 benchmark
Bind/Clear/Ensure/Touch/Sweep 同步测试
```

---

## 17. 验收标准

```text
1. 所有 ProjectedActorMeta.ActorId 变化都会同步 ProjectedActorRef.ActorId。
2. 所有可投影 Entity 在 MarkProjected 后都拥有 ProjectedActorRef。
3. ProjectionExecutor Post 热路径从 actorRef.ActorId 读取 ActorId。
4. ProjectionExecutor 仅在 Ensure/Touch 时读取 ProjectedActorMeta。
5. ActiveProjectedActorList.Sweep 释放时会清理 ProjectedActorRef。
6. Entity 删除不会留下 active list 错误索引。
7. Benchmark 中 ProjectedRef 路径低于旧 Projection Lookup 路径。
8. FullPipeline 保持 0 allocation。
```

---

## 18. 最终结构

```text
ProjectedActorMeta：
内部生命周期状态。

ProjectedActorRef：
公开 ActorId 缓存组件。
Projection/Post 热路径 ActorId 来源。

ProjectedActorBindingUtility：
统一同步 meta/ref。

ProjectionExecutor：
优先读 actorRef.ActorId。
meta 只参与 Ensure/Touch。

ActiveProjectedActorList：
负责超时释放和失效清理。
```
