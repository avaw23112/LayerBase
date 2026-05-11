# LayerBase Projection Other Components Update Design

## 1. 本文档目标

本文档只覆盖 `ProjectedActorTypeRegistry` 之外的更新内容。

本版确认：

```text
ProjectedActorTypeRegistry 保持 runtime-local。
LayerRuntime 继续持有 ProjectedActorTypeRegistry。
ProjectedActorBinding 继续通过 world.Runtime.ProjectedActorTypes 创建 Actor。
Factory 仍然接收当前 ActorWorld 参数，避免引用错误 ActorWorld。
```

本文档重点更新：

```text
1. CreateEntity(...).WithProjectedActor<TActor>() 链式创建。
2. ProjectionForEach 支持多事件输出。
3. Bring<TEvent...>() 支持多事件类型。
4. Batch 支持多事件批量投递。
5. ProjectionExecutor 支持多事件输出收集。
6. T4 模板补全，避免手写 g.cs。
7. Query0 / Touch0 的边界语义。
8. 测试要求。
```

术语说明：

```text
runtime-local:
  每个 LayerRuntime 各自持有一份对象。
  不共享可变状态。
  本文里指 ProjectedActorTypeRegistry 属于某个 LayerRuntime。

T4:
  Text Template Transformation Toolkit。
  它是 C# 常用的文本模板生成工具。
  在这里用于生成重复的泛型重载源码。
  生成结果是普通 .cs 文件，不是运行时反射，也不是热路径逻辑。

Batch:
  批量投递缓冲。
  Projection.ForEach 先把 actorId 和 event 放进 Batch。
  Post() 再统一调用 ActorWorld.PostTo。
```

---

## 2. 当前已确认的问题

当前实现已经有：

```text
Query<T0> 到 Query<T0...T7>。
Where。
Bring<TEvent>。
ForEach(... ref TEvent output)。
Batch。
Post。
TouchProjectedActor。
```

当前缺失：

```text
1. 没有 CreateEntity<T...>().WithProjectedActor<TActor>() 链式创建。
2. Bring 只支持 1 个 TEvent。
3. ForEach 只支持 1 个 TEvent 输出。
4. Batch 只支持 1 个事件数组。
5. ProjectionExecutor 只收集 1 个事件。
6. QueryFlow / Executor 的 T4 模板主体没有补全。
7. 没有 Query0 / 0 组件版本。
```

---

## 3. 保留 runtime-local Registry

当前结构保留：

```csharp
public sealed partial class LayerRuntime
{
    public World EcsWorld { get; private set; } = null!;

    internal ProjectedActorTypeRegistry ProjectedActorTypes { get; private set; } = null!;

    internal void InitializeEcsWorld()
    {
        // 逻辑说明：
        // 每个 LayerRuntime 都拥有自己的 EcsWorld 和 ProjectedActorTypeRegistry。
        // Registry 是 runtime-local，不是全局静态表。
        // 这样可以保证 ProjectedActor 始终创建到当前 Runtime 的 ActorWorld。

        EcsWorld = World.Create();
        EcsWorld.BindRuntime(this);

        ProjectedActorTypes = new ProjectedActorTypeRegistry();

        GeneratedProjectedActorTypes.RegisterTo(
            ProjectedActorTypes);
    }
}
```

`ProjectedActorBinding` 保持：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        ActorWorld actorWorld,
        Entity entity,
        ref ProjectedActorMeta meta,
        long nowTicks)
    {
        // world 参数作用：
        // 当前 ECS World。
        // 它绑定了所属 LayerRuntime。

        // actorWorld 参数作用：
        // 当前 world.Runtime.Actors。
        // Actor 必须创建进这个 ActorWorld。

        // entity 参数作用：
        // 当前 Projection 命中的 ECS Entity。
        // 创建 Actor 后要把它加入当前 World 的 ActiveProjectedActorList。

        // meta 参数作用：
        // 当前 Entity 行上的 ProjectedActorMeta 引用。

        // nowTicks 参数作用：
        // 本次 Projection 开始时取到的 Stopwatch 时间戳。
        // 用于刷新 IPooledActor.RecycleDeadlineTicks。

        ProjectedActorHandle handle =
            world.Runtime.ProjectedActorTypes.CreateActorByTypeId(
                actorWorld,
                meta.ActorTypeId);

        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                meta.KeepAliveTicks);

        meta.BindActor(
            handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }
}
```

---

## 4. CreateEntity 链式创建目标

目标 API：

```csharp
Entity entity = world
    .CreateEntity(
        new PositionComponent(),
        new VelocityComponent())
    .WithProjectedActor<PlayerViewActor>()
    .Entity;
```

或者：

```csharp
var flow = world
    .CreateEntity(
        position,
        velocity)
    .WithProjectedActor<PlayerViewActor>(
        keepAliveSeconds: 0.2f,
        releasePolicy: ProjectedActorReleasePolicy.ReturnToPool);
```

语义：

```text
CreateEntity:
  创建 ECS Entity。
  写入组件。
  返回 EntityCreateFlowN。

WithProjectedActor:
  给刚创建的 Entity 标记 ProjectedActorMeta。
  不立即创建 Actor。
  Actor 仍然在 Post / Touch 命中时延迟创建。

Entity:
  返回最终创建的 Entity。
```

---

## 5. EntityCreateFlow0

文件：

```text
LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs
```

代码形态：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Create;

public readonly struct EntityCreateFlow0
{
    private readonly World _world;

    private readonly Entity _entity;

    internal EntityCreateFlow0(
        World world,
        Entity entity)
    {
        // world 参数作用：
        // 当前 ECS World。

        // entity 参数作用：
        // 刚刚创建出的 Entity。

        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 逻辑说明：
            // 返回当前创建链路中的 Entity。
            return _entity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow0 WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        // keepAliveSeconds 参数作用：
        // ProjectedActor 未被 Post 或 Touch 命中后仍保留的秒数。

        // releasePolicy 参数作用：
        // ProjectedActor 超时后的释放策略。

        _world.WithProjectedActor<TActor>(
            _entity,
            keepAliveSeconds,
            releasePolicy);

        return this;
    }
}
```

---

## 6. EntityCreateFlowN

生成范围：

```text
EntityCreateFlow1<T0>
EntityCreateFlow2<T0, T1>
...
EntityCreateFlow8<T0, ..., T7>
```

生成代码形态：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Create;

public readonly struct EntityCreateFlow2<T0, T1>
{
    private readonly World _world;

    private readonly Entity _entity;

    internal EntityCreateFlow2(
        World world,
        Entity entity)
    {
        // world 参数作用：
        // 当前 ECS World。

        // entity 参数作用：
        // 刚刚创建出的 Entity。

        _world = world;
        _entity = entity;
    }

    public Entity Entity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 逻辑说明：
            // 返回当前创建链路中的 Entity。
            return _entity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCreateFlow2<T0, T1> WithProjectedActor<TActor>(
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        // keepAliveSeconds 参数作用：
        // ProjectedActor 未被 Post 或 Touch 命中后仍保留的秒数。

        // releasePolicy 参数作用：
        // ProjectedActor 超时后的释放策略。

        _world.WithProjectedActor<TActor>(
            _entity,
            keepAliveSeconds,
            releasePolicy);

        return this;
    }
}
```

说明：

```text
EntityCreateFlowN 本身不保存组件。
组件已经写入 Arch Chunk。
Flow 只保存 World 和 Entity，用于后续追加 ProjectedActorMeta。
```

---

## 7. CreateEntity 扩展入口

文件：

```text
LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs
```

代码形态：

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Create;

public static class EntityCreateWorldExtensions
{
    public static EntityCreateFlow0 CreateEntity(
        this World world)
    {
        // world 参数作用：
        // 当前 ECS World。

        Entity entity =
            world.Create();

        return new EntityCreateFlow0(
            world,
            entity);
    }

    public static EntityCreateFlow2<T0, T1> CreateEntity<T0, T1>(
        this World world,
        in T0 c0,
        in T1 c1)
    {
        // world 参数作用：
        // 当前 ECS World。

        // c0 参数作用：
        // 创建 Entity 时写入的第 1 个组件值。

        // c1 参数作用：
        // 创建 Entity 时写入的第 2 个组件值。

        Entity entity =
            world.Create(
                c0,
                c1);

        return new EntityCreateFlow2<T0, T1>(
            world,
            entity);
    }
}
```

生成范围：

```text
CreateEntity()
CreateEntity<T0>(...)
CreateEntity<T0,T1>(...)
...
CreateEntity<T0,...,T7>(...)
```

注意：

```text
如果 Arch 的 World.Create 泛型重载不是 in 参数形式，则按当前 Arch 实际 API 调整。
CreateEntity 是 LayerBase Projection 语义入口，不替代 Arch 原生 Create。
```

---

## 8. 多事件 Projection 目标

当前单事件：

```csharp
.Bring<MoveEvent>()
.ForEach(static (
    in Entity entity,
    ref PositionComponent position,
    ref MoveEvent move) =>
{
    move = new MoveEvent();
})
.Batch()
.Post();
```

目标多事件：

```csharp
world.Query<PositionComponent, VelocityComponent>()
    .Bring<MoveEvent, FootstepEvent>()
    .ForEach(static (
        in Entity entity,
        ref PositionComponent position,
        ref VelocityComponent velocity,
        ref MoveEvent move,
        ref FootstepEvent footstep) =>
    {
        // move 参数作用：
        // 第 1 个输出事件。

        // footstep 参数作用：
        // 第 2 个输出事件。

        move = new MoveEvent();
        footstep = new FootstepEvent();
    })
    .Batch()
    .Post();
```

设计语义：

```text
一个 Entity 通过一次 ForEach 可以输出多个事件。
多个事件投递给同一个 ProjectedActor。
每种事件使用独立 ProjectionBatchBuffer<TEvent>。
Post() 依次把所有 Batch 投递给 ActorWorld.PostTo。
```

---

## 9. 事件数量生成范围

建议先设：

```text
MaxComponentAmount = 8
MaxEventAmount = 4
```

如果你坚持事件数量也到 8，则设：

```text
MaxEventAmount = 8
```

本文推荐模板支持：

```text
组件数量：0..8
事件数量：0..8
```

其中：

```text
EventCount = 0:
  TouchProjectedActor。
  不走 Bring。
  不走 ForEach。
  不走 Batch。
  不走 Post。

EventCount >= 1:
  Bring<TEvent...>。
  ForEach(... ref TEvent...)。
  Batch。
  Post。
```

---

## 10. ProjectionDelegates 多事件设计

文件：

```text
LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs
```

生成示例：

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public delegate bool ProjectionPredicate<T0, T1>(
    in Entity entity,
    in T0 c0,
    in T1 c1);

public delegate void ProjectionForEach<T0, T1, TEvent0>(
    in Entity entity,
    ref T0 c0,
    ref T1 c1,
    ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach<T0, T1, TEvent0, TEvent1>(
    in Entity entity,
    ref T0 c0,
    ref T1 c1,
    ref TEvent0 e0,
    ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;

public delegate void ProjectionForEach<T0, T1, TEvent0, TEvent1, TEvent2>(
    in Entity entity,
    ref T0 c0,
    ref T1 c1,
    ref TEvent0 e0,
    ref TEvent1 e1,
    ref TEvent2 e2)
    where TEvent0 : struct
    where TEvent1 : struct
    where TEvent2 : struct;
```

注意：

```text
组件泛型统一命名 T0..TN。
事件泛型统一命名 TEvent0..TEventN。
ForEach 不返回 bool。
筛选只由 Where 负责。
```

---

## 11. Query0 Delegates

0 组件版本：

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public delegate bool ProjectionPredicate(
    in Entity entity);

public delegate void ProjectionForEach<TEvent0>(
    in Entity entity,
    ref TEvent0 e0)
    where TEvent0 : struct;

public delegate void ProjectionForEach<TEvent0, TEvent1>(
    in Entity entity,
    ref TEvent0 e0,
    ref TEvent1 e1)
    where TEvent0 : struct
    where TEvent1 : struct;
```

说明：

```text
Query0 用于只依赖 Entity 的投影。
Query0 不读取 ECS 组件列。
Query0 仍然需要 ProjectedActorMeta 列。
```

---

## 12. ProjectionBatch 多事件设计

单事件继续使用：

```csharp
ProjectionBatchBuffer<TEvent>
```

多事件不需要做一个复杂的聚合类型，可以在 Executor 内部直接租多个 batch：

```csharp
ProjectionBatchBuffer<TEvent0> batch0 =
    ProjectionBatchBuffer<TEvent0>.Rent();

ProjectionBatchBuffer<TEvent1> batch1 =
    ProjectionBatchBuffer<TEvent1>.Rent();
```

Post 阶段：

```csharp
batch0.PostTo(actorWorld);
batch1.PostTo(actorWorld);
```

Dispose 阶段：

```csharp
batch1.Dispose();
batch0.Dispose();
```

说明：

```text
每种事件类型独立 Batch。
每种事件类型单独走 ActorWorld.PostTo<TEvent>()。
不需要 object[]。
不需要接口装箱。
不需要反射。
```

---

## 13. ProjectionQueryFlow 多事件设计

当前单事件：

```csharp
public ProjectionBringFlow2<T0, T1, TEvent> Bring<TEvent>()
    where TEvent : struct
```

需要扩展：

```csharp
public ProjectionBringFlow2<T0, T1, TEvent0> Bring<TEvent0>()
    where TEvent0 : struct

public ProjectionBringFlow2<T0, T1, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
    where TEvent0 : struct
    where TEvent1 : struct
```

示例：

```csharp
public readonly struct ProjectionBringFlow2<T0, T1, TEvent0, TEvent1>
    where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;

    private readonly Query _query;

    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionBringFlow2(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已经构建好的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 过滤条件。

        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2<T0, T1, TEvent0, TEvent1> ForEach(
        ProjectionForEach<T0, T1, TEvent0, TEvent1> forEach)
    {
        // forEach 参数作用：
        // 当前 Projection 的多事件输出逻辑。
        // 它不负责筛选，只负责修改组件和写入事件输出。

        return new ProjectionPostFlow2<T0, T1, TEvent0, TEvent1>(
            _world,
            _query,
            _predicate,
            forEach);
    }
}
```

---

## 14. ProjectionPostFlow 多事件设计

示例：

```csharp
public readonly struct ProjectionPostFlow2<T0, T1, TEvent0, TEvent1>
    where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;

    private readonly Query _query;

    private readonly ProjectionPredicate<T0, T1>? _predicate;

    private readonly ProjectionForEach<T0, T1, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow2(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach<T0, T1, TEvent0, TEvent1> forEach)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已经构建好的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 过滤条件。

        // forEach 参数作用：
        // 多事件输出逻辑。

        _world = world;
        _query = query;
        _predicate = predicate;
        _forEach = forEach;
    }

    public ProjectionPostFlow2<T0, T1, TEvent0, TEvent1> Batch()
    {
        // 逻辑说明：
        // Batch 是语义阶段。
        // 实际 batch buffer 在 Post() 执行时租用。

        return this;
    }

    public void Post()
    {
        // 逻辑说明：
        // Post 不接收 RuntimeFrameBudget。
        // Post 只负责收集事件并投递到 ActorWorld.PostTo。

        ProjectionExecutor2<T0, T1>.Post(
            _world,
            _query,
            _predicate,
            _forEach);
    }
}
```

---

## 15. ProjectionExecutor 多事件设计

示例：2 组件 + 2 事件。

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using LayerBase.Actor;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

internal static partial class ProjectionExecutor2<T0, T1>
{
    public static void Post<TEvent0, TEvent1>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach<T0, T1, TEvent0, TEvent1> forEach)
        where TEvent0 : struct
        where TEvent1 : struct
    {
        // world 参数作用：
        // 当前 ECS World。
        // 它已经绑定 LayerRuntime。

        // query 参数作用：
        // 已经构建好的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 条件。

        // forEach 参数作用：
        // 多事件输出逻辑。

        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        ProjectionBatchBuffer<TEvent1> batch1 =
            ProjectionBatchBuffer<TEvent1>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    forEach,
                    nowTicks,
                    ref batch0,
                    ref batch1);
            }

            batch0.PostTo(
                actorWorld);

            batch1.PostTo(
                actorWorld);
        }
        finally
        {
            batch1.Dispose();
            batch0.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent0, TEvent1>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach<T0, T1, TEvent0, TEvent1> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0,
        ref ProjectionBatchBuffer<TEvent1> batch1)
        where TEvent0 : struct
        where TEvent1 : struct
    {
        // world 参数作用：
        // 当前 ECS World。

        // actorWorld 参数作用：
        // 当前 world.Runtime.Actors。
        // Actor 缺失时通过它创建 ProjectedActor。

        // chunk 参数作用：
        // 当前正在遍历的 Arch Chunk。

        // predicate 参数作用：
        // 可选 Where 条件。

        // forEach 参数作用：
        // 多事件输出逻辑。

        // nowTicks 参数作用：
        // 本次 Projection 开始时取到的 Stopwatch 时间戳。

        // batch0 参数作用：
        // 第 1 种事件类型的批量缓冲。

        // batch1 参数作用：
        // 第 2 种事件类型的批量缓冲。

        ref T0 first0 =
            ref chunk.GetFirst<T0>();

        ref T1 first1 =
            ref chunk.GetFirst<T1>();

        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
            ref T0 c0 =
                ref Unsafe.Add(
                    ref first0,
                    row);

            ref T1 c1 =
                ref Unsafe.Add(
                    ref first1,
                    row);

            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null
                && !predicate(
                    in entity,
                    in c0,
                    in c1))
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            TEvent0 e0 =
                default;

            TEvent1 e1 =
                default;

            forEach(
                in entity,
                ref c0,
                ref c1,
                ref e0,
                ref e1);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);

                actorId =
                    meta.ActorId;

                if (!actorId.IsValid)
                {
                    continue;
                }
            }

            batch0.Add(
                actorId,
                in e0);

            batch1.Add(
                actorId,
                in e1);
        }
    }
}
```

---

## 16. 多事件是否需要可选投递

当前 `ForEach` 不返回 bool，意味着只要通过 `Where`，就会投递全部事件。

如果业务需要某个 Entity 只输出其中一部分事件，不建议恢复 `bool` 返回，而是使用可空行为包装或 Dirty 事件结构：

```csharp
public struct OptionalEvent<TEvent>
    where TEvent : struct
{
    public bool HasValue;

    public TEvent Value;
}
```

但第一版不建议引入 `OptionalEvent<T>`，避免 API 变复杂。

当前规则保持：

```text
Where 控制是否进入 ForEach。
ForEach 输出的所有事件都会进入 Batch。
```

---

## 17. T4 Helpers 更新

文件：

```text
LayerBase/ECS/Projection/Templates/Helpers.ttinclude
```

建议替换为：

```csharp
<#@
    import namespace="System.Linq"
#>
<#+
const int ComponentAmount = 8;
const int EventAmount = 8;

string ComponentGenerics(int count)
{
    return string.Join(", ", Enumerable.Range(0, count).Select(i => $"T{i}"));
}

string EventGenerics(int count)
{
    return string.Join(", ", Enumerable.Range(0, count).Select(i => $"TEvent{i}"));
}

string AllGenerics(int componentCount, int eventCount)
{
    var parts = Enumerable.Range(0, componentCount).Select(i => $"T{i}")
        .Concat(Enumerable.Range(0, eventCount).Select(i => $"TEvent{i}"));

    return string.Join(", ", parts);
}

string PredicateTypeName(int componentCount)
{
    if (componentCount == 0)
    {
        return "ProjectionPredicate";
    }

    return $"ProjectionPredicate<{ComponentGenerics(componentCount)}>";
}

string PredicateParams(int componentCount)
{
    return string.Join(",
        ", new[] { "in Entity entity" }
        .Concat(Enumerable.Range(0, componentCount).Select(i => $"in T{i} c{i}")));
}

string ForEachParams(int componentCount, int eventCount)
{
    return string.Join(",
        ", new[] { "in Entity entity" }
        .Concat(Enumerable.Range(0, componentCount).Select(i => $"ref T{i} c{i}"))
        .Concat(Enumerable.Range(0, eventCount).Select(i => $"ref TEvent{i} e{i}")));
}

string GenericWhereConstraints(int eventCount)
{
    return string.Join("
    ", Enumerable.Range(0, eventCount).Select(i => $"where TEvent{i} : struct"));
}

string FirstComponentRefs(int componentCount)
{
    return string.Join("
        ", Enumerable.Range(0, componentCount)
        .Select(i => $"ref T{i} first{i} = ref chunk.GetFirst<T{i}>();"));
}

string RowComponentRefs(int componentCount)
{
    return string.Join("
            ", Enumerable.Range(0, componentCount)
        .Select(i => $@"ref T{i} c{i} =
                ref Unsafe.Add(
                    ref first{i},
                    row);"));
}

string PredicateArgs(int componentCount)
{
    return string.Join(", ", new[] { "in entity" }
        .Concat(Enumerable.Range(0, componentCount).Select(i => $"in c{i}")));
}

string ForEachArgs(int componentCount, int eventCount)
{
    return string.Join(", ", new[] { "in entity" }
        .Concat(Enumerable.Range(0, componentCount).Select(i => $"ref c{i}"))
        .Concat(Enumerable.Range(0, eventCount).Select(i => $"ref e{i}")));
}

string EventLocalDefaults(int eventCount)
{
    return string.Join("
            ", Enumerable.Range(0, eventCount)
        .Select(i => $@"TEvent{i} e{i} =
                default;"));
}

string BatchDeclarations(int eventCount)
{
    return string.Join("

        ", Enumerable.Range(0, eventCount)
        .Select(i => $@"ProjectionBatchBuffer<TEvent{i}> batch{i} =
            ProjectionBatchBuffer<TEvent{i}>.Rent();"));
}

string BatchRefParams(int eventCount)
{
    return string.Join(",
                    ", Enumerable.Range(0, eventCount)
        .Select(i => $"ref batch{i}"));
}

string BatchMethodParams(int eventCount)
{
    return string.Join(",
        ", Enumerable.Range(0, eventCount)
        .Select(i => $"ref ProjectionBatchBuffer<TEvent{i}> batch{i}"));
}

string BatchPosts(int eventCount)
{
    return string.Join("

            ", Enumerable.Range(0, eventCount)
        .Select(i => $@"batch{i}.PostTo(
                actorWorld);"));
}

string BatchDisposeReverse(int eventCount)
{
    return string.Join("
            ", Enumerable.Range(0, eventCount).Reverse()
        .Select(i => $"batch{i}.Dispose();"));
}

string BatchAdds(int eventCount)
{
    return string.Join("

            ", Enumerable.Range(0, eventCount)
        .Select(i => $@"batch{i}.Add(
                actorId,
                in e{i});"));
}
#>
```

---

## 18. ProjectionDelegates.tt 更新

文件：

```text
LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt
```

核心生成逻辑：

```csharp
<#@ template language="C#" #>
<#@ output extension=".cs" #>
<#@ include file="Helpers.ttinclude" #>
#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public delegate bool ProjectionPredicate(
    in Entity entity);

<# for (var componentCount = 1; componentCount <= ComponentAmount; componentCount++) { #>
public delegate bool ProjectionPredicate<<#= ComponentGenerics(componentCount) #>>(
        <#= PredicateParams(componentCount) #>);

<# } #>

<# for (var componentCount = 0; componentCount <= ComponentAmount; componentCount++) { #>
<# for (var eventCount = 1; eventCount <= EventAmount; eventCount++) { #>
public delegate void ProjectionForEach<<#= AllGenerics(componentCount, eventCount) #>>(
        <#= ForEachParams(componentCount, eventCount) #>)
    <#= GenericWhereConstraints(eventCount) #>;

<# } #>
<# } #>
```

---

## 19. ProjectionExecutor.tt 更新重点

生成维度：

```text
componentCount = 0..8
eventCount = 1..8
```

每个 `ProjectionExecutorN<T...>` 内生成多个 `Post<TEvent...>()` 重载。

组件 0 的 `CollectPostChunk` 不生成 `chunk.GetFirst<T>()`。

事件 N 的 `CollectPostChunk` 生成 N 个局部事件变量和 N 个 batch。

---

## 20. Query0 语义

`world.Query()` 语义建议：

```text
遍历所有 Entity。
不读取任何 ECS 组件。
仍然访问 ProjectedActorMeta。
Where 可选。
```

API：

```csharp
world.Query()
    .Where(static in Entity entity => true)
    .Bring<VisibleEvent>()
    .ForEach(static (
        in Entity entity,
        ref VisibleEvent visible) =>
    {
        // entity 参数作用：
        // 当前 Query 命中的 Entity。

        // visible 参数作用：
        // 输出给 Actor 的可见性事件。

        visible = new VisibleEvent();
    })
    .Batch()
    .Post();
```

如果不想支持全量遍历，第一版也可以暂缓 Query0，只保证 CreateEntity0。但按当前需求，建议补 Query0。

---

## 21. TouchProjectedActor 与事件数量关系

`TouchProjectedActor()` 是事件数量为 0 的行为。

它不需要：

```text
Bring。
ForEach。
Batch。
Post。
```

它只需要：

```text
Query / Where / TouchProjectedActor。
```

因此不用生成 `Bring<>` 的 0 事件版本。

---

## 22. 推荐落地顺序

第一步：

```text
补 EntityCreateFlow0..8。
补 CreateEntity0..8。
实现 CreateEntity(...).WithProjectedActor<TActor>()。
```

第二步：

```text
补 Helpers.ttinclude。
让模板能生成 componentCount × eventCount。
```

第三步：

```text
升级 ProjectionDelegates.tt。
生成 ProjectionPredicate0..8。
生成 ProjectionForEach component 0..8 × event 1..8。
```

第四步：

```text
升级 ProjectionQueryFlow.tt。
生成 Bring<TEvent0..TEventN>()。
生成 ProjectionBringFlowN。
生成 ProjectionPostFlowN。
```

第五步：

```text
升级 ProjectionExecutor.tt。
生成 Post<TEvent0..TEventN>()。
生成多 batch 收集和投递。
```

第六步：

```text
补 Query0。
确认 Query0 遍历语义。
```

第七步：

```text
补测试。
确认单事件旧 API 不破坏。
确认多事件 API 可用。
确认 CreateEntity 链式投影可用。
```

---

## 23. 测试要求

### 23.1 CreateEntity 链式测试

覆盖：

```text
CreateEntity().WithProjectedActor<TActor>()。
CreateEntity<T0>().WithProjectedActor<TActor>()。
CreateEntity<T0,T1>().WithProjectedActor<TActor>()。
CreateEntity<T0...T7>().WithProjectedActor<TActor>()。
WithProjectedActor 不立即创建 Actor。
Post 命中后才创建 Actor。
```

### 23.2 多事件 ForEach 测试

覆盖：

```text
Query<T0>().Bring<E0,E1>().ForEach(... ref E0, ref E1).Post()。
Query<T0,T1>().Bring<E0,E1,E2>().ForEach(...).Post()。
Query<T0,T1,T2,T3>().Bring<E0,E1,E2,E3>().ForEach(...).Post()。
Where 返回 false 时不执行 ForEach。
Where 返回 false 时不写任何 Batch。
```

### 23.3 Batch 投递测试

覆盖：

```text
多事件输出会创建多个 ProjectionBatchBuffer。
每个 Batch 都投递到 ActorWorld.PostTo。
Post 不读取 RuntimeFrameBudget。
ActorWorld.Pump 才消费 RuntimeFrameBudget。
```

### 23.4 Query0 测试

覆盖：

```text
Query().Bring<E0>().ForEach(...).Post()。
Query().Bring<E0,E1>().ForEach(...).Post()。
Query().Where(...).TouchProjectedActor()。
```

### 23.5 模板再生成测试

覆盖：

```text
删除 g.cs 后重新运行 T4 可以生成完整代码。
生成代码可编译。
生成代码不包含反射调用。
生成代码不包含 Dictionary 热路径查询。
生成代码不包含 MethodInfo.Invoke。
生成代码不包含 Activator.CreateInstance。
```

---

## 24. 最终结论

保留：

```text
ProjectedActorTypeRegistry runtime-local。
LayerRuntime.ProjectedActorTypes。
world.Runtime.ProjectedActorTypes.CreateActorByTypeId(actorWorld, id)。
```

需要补齐：

```text
CreateEntity(...).WithProjectedActor<TActor>()。
Query0。
ProjectionForEach 多事件输出。
Bring<TEvent...>() 多事件重载。
ProjectionExecutor 多事件 batch 收集。
T4 模板完整生成。
```

最终目标：

```text
Entity 创建链路可以直接接 ProjectedActor。
Projection 查询链路可以一次输出多个事件。
Post 仍然只负责 Batch -> ActorWorld.PostTo。
ActorWorld.Pump 仍然是唯一 RuntimeFrameBudget 消费者。
```
