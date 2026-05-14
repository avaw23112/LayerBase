# ProjectedActorRef Hot Path Code Modification Execution

## 1. 执行目标

本文件用于指导 agent 对 `ProjectedActorRef` 热路径进行代码修改。

目标：

```text
Projection Query 生成阶段强制带上 ProjectedActorRef。
ProjectionExecutor 模板不再通过 chunk.Has<ProjectedActorRef>() 兜底。
ProjectionExecutor 热路径不再逐行读取 ProjectedActorMeta。
ProjectedActorRef 从 ActorId 缓存升级为 Projection 热路径缓存。
ProjectedActorMeta 只保留为内部生命周期和 Sweep 状态。
```

最终热路径：

```text
ProjectionWorldExtensions.Query
→ QueryDescription.WithAll<ProjectedActorRef>()
→ ProjectionExecutor
→ ref ProjectedActorRef actorRef
→ ActorId actorId = actorRef.ActorId
→ Ensure / Touch 正常路径只读 actorRef
→ batch.Add(actorId, event)
```

---

## 2. 修改范围

本次只修改 `ProjectedActorRef` 热路径相关代码。

不修改：

```text
EventStream 邮箱重构
ActorBehaviour 生成器
Actor 邮箱后端
batch.Post 结构
ActorWorld Pump 结构
```

需要修改的文件：

```text
LayerBase/ECS/Projection/ProjectedActorRef.cs
LayerBase/ECS/Projection/ProjectedActorMarkUtility.cs
LayerBase/ECS/Projection/ProjectedActorBindingUtility.cs
LayerBase/ECS/Projection/ProjectedActorBinding.cs
LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt
LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt
```

---

## 3. 修改 ProjectedActorRef.cs

把 `ProjectedActorRef` 从单纯 `ActorId` 缓存升级为 Projection 热路径缓存。

目标代码：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 的热路径缓存组件。
///
/// 作用：
/// 1. 作为普通 ECS 组件参与 Projection Query。
/// 2. 保存 ProjectionExecutor 热路径所需的 ActorId。
/// 3. 保存冷路径创建 Actor 所需的 ActorTypeId。
/// 4. 保存 Touch 时刷新回收时间所需的 KeepAliveTicks。
/// 5. 避免 ProjectionExecutor 每行读取 ProjectedActorMeta。
/// </summary>
public struct ProjectedActorRef
{
    /// <summary>
    /// 当前 Entity 绑定的 ActorId。
    ///
    /// 参数作用：
    /// ProjectionExecutor 通过它直接定位目标 Actor。
    /// 如果 ActorId 无效，则会尝试创建新的 Projected Actor。
    /// </summary>
    public ActorId ActorId;

    /// <summary>
    /// Projected Actor 类型 ID。
    ///
    /// 参数作用：
    /// ActorId 无效时，EnsureProjectedActor 使用该字段创建正确类型的 Actor。
    ///
    /// 注意：
    /// 该字段是 internal，业务层不应依赖它。
    /// </summary>
    internal int ActorTypeId;

    /// <summary>
    /// Projected Actor 保活时间。
    ///
    /// 参数作用：
    /// TouchProjectedActor 使用该字段刷新 IPooledActor.RecycleDeadlineTicks。
    /// </summary>
    internal long KeepAliveTicks;

    /// <summary>
    /// Projected Actor 释放策略。
    ///
    /// 参数作用：
    /// 与 ProjectedActorMeta.ReleasePolicy 保持一致。
    /// 当前热路径一般不直接读取它，但需要保留配置同步能力。
    /// </summary>
    internal ProjectedActorReleasePolicy ReleasePolicy;

    /// <summary>
    /// 当前 ActorId 是否有效。
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActorId.IsValid;
    }

    /// <summary>
    /// 构造未绑定但可投影的 ProjectedActorRef。
    /// </summary>
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">Projected Actor 保活时间。如果传入负数，则修正为 0。</param>
    /// <param name="releasePolicy">Projected Actor 释放策略。</param>
    public ProjectedActorRef(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = ActorId.Invalid;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
    }

    /// <summary>
    /// 构造已绑定 ActorId 的 ProjectedActorRef。
    /// </summary>
    /// <param name="actorId">已绑定的 ActorId。</param>
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">保活时间。</param>
    /// <param name="releasePolicy">释放策略。</param>
    public ProjectedActorRef(
        ActorId actorId,
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        ActorId = actorId;
        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
    }

    /// <summary>
    /// 创建未绑定但可投影的 ProjectedActorRef。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ProjectedActorRef CreateProjectable(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        return new ProjectedActorRef(
            actorTypeId,
            keepAliveTicks,
            releasePolicy);
    }

    /// <summary>
    /// 绑定 ActorId。
    /// </summary>
    /// <param name="actorId">新绑定的 ActorId。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bind(
        ActorId actorId)
    {
        ActorId = actorId;
    }

    /// <summary>
    /// 清空 ActorId。
    ///
    /// 作用：
    /// 只清空 ActorId，不清空 ActorTypeId、KeepAliveTicks、ReleasePolicy。
    /// 因为 Entity 仍然是可投影实体，后续可以再次创建 Actor。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearActor()
    {
        ActorId = ActorId.Invalid;
    }
}
```

---

## 4. 修改 ProjectedActorMarkUtility.cs

目标：

```text
MarkProjected 时不仅写 meta，还要写完整 ProjectedActorRef。
```

替换为：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;

namespace LayerBase.ECS.Projection;

/// <summary>
/// Projected Actor 标记工具。
/// 作用：统一标记 Entity 为可投影，并保证 ProjectedActorRef 热路径组件存在。
/// </summary>
internal static class ProjectedActorMarkUtility
{
    /// <summary>
    /// 将 Entity 标记为可投影 Actor。
    /// </summary>
    /// <param name="world">ECS World。</param>
    /// <param name="entity">目标 Entity。</param>
    /// <param name="meta">ProjectedActorMeta 引用。</param>
    /// <param name="actorTypeId">Projected Actor 类型 ID。</param>
    /// <param name="keepAliveTicks">保活时间。</param>
    /// <param name="releasePolicy">释放策略。</param>
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

        ProjectedActorRef actorRef =
            ProjectedActorRef.CreateProjectable(
                actorTypeId,
                keepAliveTicks,
                releasePolicy);

        if (world.Has<ProjectedActorRef>(entity))
        {
            world.Set(entity, actorRef);
        }
        else
        {
            world.Add(entity, actorRef);
        }
    }
}
```

---

## 5. 修改 ProjectedActorBindingUtility.cs

### 5.1 修改热路径 Clear

把：

```csharp
actorRef.Clear();
```

改为：

```csharp
actorRef.ClearActor();
```

目标代码：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static void Clear(
    ref ProjectedActorMeta meta,
    ref ProjectedActorRef actorRef)
{
    meta.ClearActor();
    actorRef.ClearActor();
}
```

### 5.2 修改 UpsertRef

当前 `UpsertRef` 只接收 `ActorId`，会丢失 `ActorTypeId / KeepAliveTicks / ReleasePolicy`。

替换为：

```csharp
/// <summary>
/// 插入或更新 ProjectedActorRef。
/// </summary>
/// <param name="world">ECS World。</param>
/// <param name="entity">目标 Entity。</param>
/// <param name="meta">当前 ProjectedActorMeta。</param>
/// <param name="actorId">要写入的 ActorId。</param>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void UpsertRef(
    World world,
    Entity entity,
    in ProjectedActorMeta meta,
    ActorId actorId)
{
    var actorRef = new ProjectedActorRef(
        actorId,
        meta.ActorTypeId,
        meta.KeepAliveTicks,
        meta.ReleasePolicy);

    if (world.Has<ProjectedActorRef>(entity))
    {
        world.Set(entity, actorRef);
    }
    else
    {
        world.Add(entity, actorRef);
    }
}
```

把调用处改为：

```csharp
UpsertRef(world, entity, in meta, actorId);
```

### 5.3 修改 World 版本 Clear

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static void Clear(
    World world,
    Entity entity,
    ref ProjectedActorMeta meta)
{
    meta.ClearActor();

    UpsertRef(
        world,
        entity,
        in meta,
        ActorId.Invalid);
}
```

### 5.4 EnsureInvalidRef 处理

`EnsureInvalidRef(world, entity)` 不再适合作为主路径，因为它没有 `ActorTypeId / KeepAliveTicks / ReleasePolicy`。

处理方式：

```text
1. 保留方法，仅作为兼容兜底。
2. 不允许 MarkProjected 主路径调用它。
3. MarkProjected 必须写完整 ProjectedActorRef。
```

---

## 6. 修改 ProjectedActorBinding.cs

### 6.1 新增不读 meta 的 EnsureProjectedActor

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
    /// <param name="world">ECS World。</param>
    /// <param name="actorWorld">ActorWorld。</param>
    /// <param name="entity">当前 Entity。</param>
    /// <param name="actorRef">ProjectedActorRef 热路径缓存。</param>
    /// <param name="nowTicks">当前时间戳。</param>
    /// <returns>有效 ActorId，或 ActorId.Invalid。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        ActorWorld actorWorld,
        Entity entity,
        ref ProjectedActorRef actorRef,
        long nowTicks)
    {
        ProjectedActorHandle handle =
            ProjectedActorTypeRegistry.CreateActorByTypeId(
                actorWorld,
                actorRef.ActorTypeId);

        if (!handle.IsValid)
        {
            actorRef.ClearActor();
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                actorRef.KeepAliveTicks);

        actorRef.Bind(handle.ActorId);

        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        meta.BindActor(handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }
}
```

### 6.2 新增不读 meta 的 TouchProjectedActor

```csharp
/// <summary>
/// 刷新 Projected Actor 保活时间。
/// </summary>
/// <param name="world">ECS World。</param>
/// <param name="actorWorld">ActorWorld。</param>
/// <param name="entity">当前 Entity。</param>
/// <param name="actorRef">ProjectedActorRef 热路径缓存。</param>
/// <param name="nowTicks">当前时间戳。</param>
/// <returns>true 表示 actor 仍然有效；false 表示 actor 已失效。</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool TouchProjectedActor(
    World world,
    ActorWorld actorWorld,
    Entity entity,
    ref ProjectedActorRef actorRef,
    long nowTicks)
{
    ActorId actorId = actorRef.ActorId;

    if (!actorId.IsValid)
    {
        ClearByEntity(world, entity, ref actorRef);
        return false;
    }

    if (!actorWorld.TryGetPooledActor(
            actorId,
            out IPooledActor pooledActor))
    {
        ClearByEntity(world, entity, ref actorRef);
        return false;
    }

    pooledActor.RecycleDeadlineTicks =
        ProjectedActorTime.BuildDeadline(
            nowTicks,
            actorRef.KeepAliveTicks);

    return true;
}
```

### 6.3 新增 ClearByEntity

```csharp
/// <summary>
/// 根据 Entity 清理 meta/ref。
/// </summary>
/// <param name="world">ECS World。</param>
/// <param name="entity">当前 Entity。</param>
/// <param name="actorRef">ProjectedActorRef。</param>
[MethodImpl(MethodImplOptions.NoInlining)]
private static void ClearByEntity(
    World world,
    Entity entity,
    ref ProjectedActorRef actorRef)
{
    actorRef.ClearActor();

    ref ProjectedActorMeta meta =
        ref world.GetProjectionMeta(entity);

    meta.ClearActor();
}
```

旧的 `EnsureProjectedActor(... ref ProjectedActorMeta ...)` 和 `TouchProjectedActor(... ref ProjectedActorMeta ...)` 可以暂时保留，避免一次性破坏非模板调用。

---

## 7. 修改 ProjectionWorldExtensions.tt

所有 Projection Query 默认必须包含：

```csharp
description.WithAll<ProjectedActorRef>();
```

目标模板：

```csharp
<#@ template language="C#" #>
<#@ output extension=".cs" #>
<#@ include file="Helpers.ttinclude" #>
#nullable enable
using Arch.Core;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

public static class ProjectionWorldExtensions
{
    public static ProjectionQueryFlow0 Query(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // Projection 热路径必须组件。
        // 作用：保证进入 ProjectionExecutor 的 chunk 必然拥有 ActorId 缓存。
        description.WithAll<ProjectedActorRef>();

        return new ProjectionQueryFlow0(
            world,
            world.Query(in description));
    }

<#
    for (var c = 1; c <= ComponentAmount; c++)
    {
#>
<#
        var compG = CG(c);
#>
    public static ProjectionQueryFlow<#= c #><<#= compG #>> Query<<#= compG #>>(
        this World world)
    {
        QueryDescription description = new QueryDescription();

        // ProjectedActorRef：
        // 必须作为 Projection Query 的基础组件。
        description.WithAll<ProjectedActorRef>();

        // 用户业务组件。
        description.WithAll<<#= compG #>>();

        return new ProjectionQueryFlow<#= c #><<#= compG #>>(
            world,
            world.Query(in description));
    }

<#
    }
#>
}
```

---

## 8. 修改 ProjectionExecutor.tt

### 8.1 全局删除

删除所有：

```csharp
if (!chunk.Has<ProjectedActorRef>())
{
    return;
}
```

删除所有：

```csharp
ref ProjectedActorMeta firstMeta = ref chunk.FirstProjection();
```

删除所有：

```csharp
ref ProjectedActorMeta meta = ref Unsafe.Add(ref firstMeta, row);
```

### 8.2 全局替换 Ensure 调用

从：

```csharp
ProjectedActorBinding.EnsureProjectedActor(
    world,
    actorWorld,
    entity,
    ref meta,
    ref actorRef,
    nowTicks)
```

改为：

```csharp
ProjectedActorBinding.EnsureProjectedActor(
    world,
    actorWorld,
    entity,
    ref actorRef,
    nowTicks)
```

### 8.3 全局替换 Touch 调用

从：

```csharp
ProjectedActorBinding.TouchProjectedActor(
    actorWorld,
    ref meta,
    ref actorRef,
    nowTicks)
```

改为：

```csharp
ProjectedActorBinding.TouchProjectedActor(
    world,
    actorWorld,
    entity,
    ref actorRef,
    nowTicks)
```

---

## 9. ProjectionExecutor.tt 单事件目标形态

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
    ref ProjectedActorRef firstActorRef =
        ref chunk.GetFirst<ProjectedActorRef>();

    ref Entity firstEntity =
        ref chunk.Entities.DangerousGetReference();

    int count = chunk.Count;

    for (int row = 0; row < count; row++)
    {
        Entity entity = Unsafe.Add(ref firstEntity, row);

        if (predicate != null && !predicate(in entity))
        {
            continue;
        }

        ref ProjectedActorRef actorRef =
            ref Unsafe.Add(ref firstActorRef, row);

        ActorId actorId = actorRef.ActorId;

        if (!actorId.IsValid)
        {
            actorId =
                ProjectedActorBinding.EnsureProjectedActor(
                    world,
                    actorWorld,
                    entity,
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
                    world,
                    actorWorld,
                    entity,
                    ref actorRef,
                    nowTicks);

            if (!alive)
            {
                continue;
            }

            actorId = actorRef.ActorId;
        }

        TEvent output = default;

        forEach(
            in entity,
            ref output);

        batch.Add(
            actorId,
            in output);
    }
}
```

---

## 10. TouchChunk 目标形态

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void TouchChunk(
    World world,
    ActorWorld actorWorld,
    ref Chunk chunk,
    ProjectionPredicate? predicate,
    long nowTicks)
{
    ref ProjectedActorRef firstActorRef =
        ref chunk.GetFirst<ProjectedActorRef>();

    ref Entity firstEntity =
        ref chunk.Entities.DangerousGetReference();

    int count = chunk.Count;

    for (int row = 0; row < count; row++)
    {
        Entity entity = Unsafe.Add(ref firstEntity, row);

        if (predicate != null && !predicate(in entity))
        {
            continue;
        }

        ref ProjectedActorRef actorRef =
            ref Unsafe.Add(ref firstActorRef, row);

        if (!actorRef.ActorId.IsValid)
        {
            _ = ProjectedActorBinding.EnsureProjectedActor(
                world,
                actorWorld,
                entity,
                ref actorRef,
                nowTicks);
        }
        else
        {
            _ = ProjectedActorBinding.TouchProjectedActor(
                world,
                actorWorld,
                entity,
                ref actorRef,
                nowTicks);
        }
    }
}
```

---

## 11. 多事件 / Job 模板同步规则

所有以下生成块都应用相同规则：

```text
ProjectionExecutor0
ProjectionExecutor0_2E 到 EventAmount 上限
ProjectionExecutorC
ProjectionExecutorC.Post<TEvent>
ProjectionExecutorC.Post<TEvent0,TJob>
ProjectionExecutorC_2E 到 EventAmount 上限
CollectPostChunk
CollectPostJobChunk
TouchChunk
```

统一要求：

```text
只读取 ProjectedActorRef。
不读取 ProjectedActorMeta。
Ensure / Touch 通过 entity + ref actorRef 在冷路径中按需同步 meta。
```

---

## 12. 生成后检查

重新生成 `.cs` 后，在生成文件中搜索。

### 12.1 不应再出现

```text
chunk.Has<ProjectedActorRef>()
ref ProjectedActorMeta firstMeta
ref ProjectedActorMeta meta = ref Unsafe.Add
ref meta, ref actorRef
TouchProjectedActor(actorWorld, ref meta
```

### 12.2 允许出现

```text
world.GetProjectionMeta(entity)
```

但只允许出现在：

```text
ProjectedActorBinding.cs
ProjectedActorBindingUtility.cs
ActiveProjectedActorList.cs
```

不允许出现在：

```text
ProjectionExecutor.Generated.cs
```

---

## 13. 测试要求

### 13.1 MarkProjected 测试

```text
Given Entity 无 ProjectedActorRef
When WithProjectedActor<TActor>(entity)
Then Entity 拥有 ProjectedActorRef
And actorRef.ActorId == ActorId.Invalid
And actorRef.ActorTypeId == ActorType<TActor>.Id
And actorRef.KeepAliveTicks > 0
```

### 13.2 EnsureProjectedActor 测试

```text
Given actorRef.ActorId invalid
When ProjectionExecutor 执行 Post
Then EnsureProjectedActor 创建 Actor
And actorRef.ActorId valid
And meta.ActorId == actorRef.ActorId
```

### 13.3 Touch 测试

```text
Given actorRef.ActorId valid
When TouchProjectedActor
Then pooledActor.RecycleDeadlineTicks 被刷新
And ProjectionExecutor 模板不读取 ProjectedActorMeta
```

### 13.4 失效清理测试

```text
Given actorRef.ActorId 指向已销毁 Actor
When TouchProjectedActor
Then actorRef.ActorId invalid
And meta.ActorId invalid
```

---

## 14. Benchmark 要求

新增或更新：

```text
Projection: ProjectedRef Query ×1000
Projection: ProjectedRef Touch ×1000
FullPipeline: ProjectedRef HotPath ×1000
```

对比旧路径：

```text
Projection: Entity → ActorId Lookup ×1000
Projection: Meta ActorId Path ×1000
FullPipeline: Meta Path ×1000
```

目标：

```text
ProjectionExecutor.Generated.cs 不再读取 ProjectedActorMeta。
FullPipeline 不高于旧路径。
ProjectedRef Query 保持 0 allocation。
```

---

## 15. 提交顺序

```text
Commit 1：
修改 ProjectedActorRef.cs，加入 ActorTypeId / KeepAliveTicks / ReleasePolicy。

Commit 2：
修改 ProjectedActorMarkUtility.cs，MarkProjected 时写完整 ProjectedActorRef。

Commit 3：
修改 ProjectedActorBindingUtility.cs，Clear 只清 ActorId，UpsertRef 保留配置字段。

Commit 4：
修改 ProjectedActorBinding.cs，新增不读 meta 的 Ensure / Touch。

Commit 5：
修改 ProjectionWorldExtensions.tt，所有 Query 强制 WithAll<ProjectedActorRef>()。

Commit 6：
修改 ProjectionExecutor.tt，移除 firstMeta 和 chunk.Has<ProjectedActorRef>()。

Commit 7：
重新生成模板代码，搜索并清理残留 meta 热路径。

Commit 8：
补测试和 benchmark。
```

---

## 16. 验收标准

```text
1. ProjectionWorldExtensions.Query 必须 WithAll<ProjectedActorRef>()。
2. ProjectionExecutor.Generated.cs 不得出现 chunk.Has<ProjectedActorRef>()。
3. ProjectionExecutor.Generated.cs 不得出现 firstMeta / row meta 读取。
4. ProjectionExecutor.Generated.cs 的 Ensure / Touch 只传 ref ProjectedActorRef。
5. ProjectedActorRef 保存 ActorTypeId / KeepAliveTicks / ReleasePolicy。
6. MarkProjected 时写完整 ProjectedActorRef。
7. ClearActor 只清 ActorId，不清 projection 配置。
8. ActiveProjectedActorList.Sweep 继续通过 BindingUtility 清理 ref。
9. FullPipeline 保持 0 allocation。
```
