# LayerBase Projection 修复文档

## 1. 当前未达标项

根据当前 `faster` 分支代码，已经确认：

```text
1. ProjectedActorTypeRegistry 已保持 runtime-local，这部分保留。
2. ProjectionDelegates.g.cs 已生成多事件 ProjectionForEach 到 10 个事件。
3. ProjectionQueryFlow.g.cs 仍然只支持单事件 Bring<TEvent>()。
4. ProjectionExecutor.g.cs 仍然只支持单事件 Batch 收集和投递。
5. ProjectionWorldExtensions.g.cs 仍然没有 Query() 的 0 组件入口。
6. CreateEntity(...).WithProjectedActor<TActor>() 链式创建还没接好。
7. ProjectionQueryFlow.tt、ProjectionExecutor.tt、ProjectionWorldExtensions.tt 仍然是占位模板，不能完整再生成。
```

本次修复目标：

```text
保留 runtime-local ProjectedActorTypeRegistry。
补齐 CreateEntity(...).WithProjectedActor<TActor>()。
补齐 Query0。
补齐 Bring<TEvent0..TEvent9>()。
补齐 ProjectionPostFlow 多事件版本。
补齐 ProjectionExecutor 多事件 Batch 收集和投递。
补齐 T4 模板，避免继续手写 .g.cs。
Post() 仍然只负责 Batch -> ActorWorld.PostTo。
RuntimeFrameBudget 仍然只由 ActorWorld.Pump 消费。
```

---

## 2. 保留不改：runtime-local Registry

当前结构保留：

```csharp
public sealed partial class LayerRuntime
{
    public World EcsWorld { get; private set; } = null!;

    internal ProjectedActorTypeRegistry ProjectedActorTypes { get; private set; } = null!;

    internal void InitializeEcsWorld()
    {
        // 逻辑说明：
        // 每个 LayerRuntime 独立持有一个 EcsWorld 和一个 ProjectedActorTypeRegistry。
        // Registry 不做全局静态表，避免多 Runtime 时创建到错误 ActorWorld。

        EcsWorld = World.Create();
        EcsWorld.BindRuntime(this);

        ProjectedActorTypes = new ProjectedActorTypeRegistry();

        GeneratedProjectedActorTypes.RegisterTo(
            ProjectedActorTypes);
    }
}
```

`ProjectedActorBinding` 继续使用：

```csharp
ProjectedActorHandle handle =
    world.Runtime.ProjectedActorTypes.CreateActorByTypeId(
        actorWorld,
        meta.ActorTypeId);
```

不要改成全局 Registry。

---

## 3. 修复一：CreateEntity(...).WithProjectedActor<TActor>()

### 3.1 目标 API

```csharp
Entity entity = world
    .CreateEntity(
        new PositionComponent(),
        new VelocityComponent())
    .WithProjectedActor<PlayerViewActor>(
        keepAliveSeconds: 0.2f,
        releasePolicy: ProjectedActorReleasePolicy.ReturnToPool)
    .Entity;
```

语义：

```text
CreateEntity:
  创建 ECS Entity 并写入组件。

WithProjectedActor:
  只写 ProjectedActorMeta。
  不立即创建 Actor。
  Actor 在 Projection.Post 或 TouchProjectedActor 命中时延迟创建。

Entity:
  返回创建出的 Entity。
```

### 3.2 新增文件

```text
LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs
LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs
LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt
LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt
```

### 3.3 EntityCreateFlow0 代码形态

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
        // 后续 WithProjectedActor 会通过它写 ProjectedActorMeta。

        // entity 参数作用：
        // 刚刚创建出的 ECS Entity。

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

### 3.4 EntityCreateFlowN 代码形态

生成 1 到 8 组件版本。示例：2 组件版本。

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
        // 后续 WithProjectedActor 会通过它写 ProjectedActorMeta。

        // entity 参数作用：
        // 刚刚创建出的 ECS Entity。

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
EntityCreateFlowN 不保存组件值。
组件已经写入 Arch Chunk。
Flow 只保存 World 和 Entity。
```

### 3.5 CreateEntity 扩展代码形态

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

如果当前 Arch 的 `World.Create` 不是 `in` 参数签名，模板按真实 API 改。

---

## 4. 修复二：Query0

### 4.1 目标 API

```csharp
world.Query()
    .Bring<VisibleEvent>()
    .ForEach(static (
        in Entity entity,
        ref VisibleEvent visible) =>
    {
        // entity 参数作用：
        // 当前 Query 命中的 Entity。

        // visible 参数作用：
        // 输出给 ProjectedActor 的可见性事件。

        visible = new VisibleEvent();
    })
    .Batch()
    .Post();
```

### 4.2 Query0 语义

```text
Query() 遍历当前 World 中所有 Entity。
不读取任何普通 ECS 组件列。
仍然读取 ProjectedActorMeta 列。
Where 可选。
```

如果当前 Arch 的空 QueryDescription 无法表达“遍历所有 Entity”，需要单独实现 `ProjectionQueryFlow0` 的 chunk 遍历器，不要强行依赖有组件 Query。

---

## 5. 修复三：ProjectionWorldExtensions.tt

当前文件不能继续只写：

```csharp
// See generated ProjectionWorldExtensions.g.cs for the concrete emitted bodies.
```

必须生成：

```text
Query()
Query<T0>()
...
Query<T0..T7>()
```

模板形态：

```csharp
<#@ template language="C#" #>
<#@ output extension=".cs" #>
<#@ include file="Helpers.ttinclude" #>
#nullable enable
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public static class ProjectionWorldExtensions
{
    public static ProjectionQueryFlow0 Query(
        this World world)
    {
        // world 参数作用：
        // 当前 ECS World。

        QueryDescription description =
            new QueryDescription();

        return new ProjectionQueryFlow0(
            world,
            world.Query(in description));
    }

<# for (var c = 1; c <= ComponentAmount; c++) { #>
    public static ProjectionQueryFlow<#= c #><<#= CG(c) #>> Query<<#= CG(c) #>>(
        this World world)
    {
        // world 参数作用：
        // 当前 ECS World。

        QueryDescription description =
            new QueryDescription();

        description.WithAll<<#= CG(c) #>>();

        return new ProjectionQueryFlow<#= c #><<#= CG(c) #>>(
            world,
            world.Query(in description));
    }

<# } #>
}
```

---

## 6. 修复四：ProjectionQueryFlow 多事件

### 6.1 当前问题

当前 `ProjectionQueryFlow.g.cs` 只有：

```csharp
Bring<TEvent>()
ForEach(ProjectionForEach<..., TEvent>)
Post<TEvent>()
```

需要生成：

```csharp
Bring<TEvent0>()
Bring<TEvent0, TEvent1>()
...
Bring<TEvent0, ..., TEvent9>()
```

### 6.2 QueryFlow0 示例

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public readonly struct ProjectionQueryFlow0
{
    private readonly World _world;

    private readonly Query _query;

    private readonly ProjectionPredicate? _predicate;

    internal ProjectionQueryFlow0(
        World world,
        Query query,
        ProjectionPredicate? predicate = null)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已构建的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 过滤条件。

        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow0 Where(
        ProjectionPredicate predicate)
    {
        // predicate 参数作用：
        // Entity 过滤条件。

        return new ProjectionQueryFlow0(
            _world,
            _query,
            predicate);
    }

    public ProjectionBringFlow0<TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
        where TEvent0 : struct
        where TEvent1 : struct
    {
        // 逻辑说明：
        // 声明当前 Projection 输出两个事件类型。

        return new ProjectionBringFlow0<TEvent0, TEvent1>(
            _world,
            _query,
            _predicate);
    }

    public void TouchProjectedActor()
    {
        // 逻辑说明：
        // 只创建或刷新 ProjectedActor。
        // 不输出事件，不参与 RuntimeFrameBudget。

        ProjectionExecutor0.Touch(
            _world,
            _query,
            _predicate);
    }
}
```

### 6.3 QueryFlowN 示例

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public readonly struct ProjectionQueryFlow2<T0, T1>
{
    private readonly World _world;

    private readonly Query _query;

    private readonly ProjectionPredicate<T0, T1>? _predicate;

    internal ProjectionQueryFlow2(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate = null)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已构建的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 过滤条件。

        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionQueryFlow2<T0, T1> Where(
        ProjectionPredicate<T0, T1> predicate)
    {
        // predicate 参数作用：
        // 组件过滤条件。

        return new ProjectionQueryFlow2<T0, T1>(
            _world,
            _query,
            predicate);
    }

    public ProjectionBringFlow2<T0, T1, TEvent0, TEvent1> Bring<TEvent0, TEvent1>()
        where TEvent0 : struct
        where TEvent1 : struct
    {
        // 逻辑说明：
        // 声明当前 Projection 输出两个事件类型。

        return new ProjectionBringFlow2<T0, T1, TEvent0, TEvent1>(
            _world,
            _query,
            _predicate);
    }

    public void TouchProjectedActor()
    {
        // 逻辑说明：
        // 只创建或刷新 ProjectedActor。
        // 不输出事件，不参与 RuntimeFrameBudget。

        ProjectionExecutor2<T0, T1>.Touch(
            _world,
            _query,
            _predicate);
    }
}
```

---

## 7. ProjectionBringFlow 多事件

示例：2 组件 + 2 事件。

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

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
        // 已构建的 Arch Query。

        // predicate 参数作用：
        // 可选 Where 过滤条件。

        _world = world;
        _query = query;
        _predicate = predicate;
    }

    public ProjectionPostFlow2<T0, T1, TEvent0, TEvent1> ForEach(
        ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
    {
        // forEach 参数作用：
        // 多事件输出逻辑。
        // 它只负责修改组件和写输出事件，不负责筛选。

        return new ProjectionPostFlow2<T0, T1, TEvent0, TEvent1>(
            _world,
            _query,
            _predicate,
            forEach);
    }
}
```

---

## 8. ProjectionPostFlow 多事件

```csharp
using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

public readonly struct ProjectionPostFlow2<T0, T1, TEvent0, TEvent1>
    where TEvent0 : struct
    where TEvent1 : struct
{
    private readonly World _world;

    private readonly Query _query;

    private readonly ProjectionPredicate<T0, T1>? _predicate;

    private readonly ProjectionForEach2<T0, T1, TEvent0, TEvent1> _forEach;

    internal ProjectionPostFlow2(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已构建的 Arch Query。

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
        // 实际 ProjectionBatchBuffer 在 Post() 执行时租用。

        return this;
    }

    public void Post()
    {
        // 逻辑说明：
        // Post 不接收 ActorWorld。
        // Post 不接收 RuntimeFrameBudget。
        // Post 只负责收集 Batch 并调用 ActorWorld.PostTo。

        ProjectionExecutor2<T0, T1>.Post(
            _world,
            _query,
            _predicate,
            _forEach);
    }
}
```

---

## 9. 修复五：ProjectionExecutor 多事件

### 9.1 当前问题

当前 Executor 只有：

```csharp
ProjectionBatchBuffer<TEvent> batch;
TEvent output = default;
forEach(..., ref output);
batch.Add(actorId, in output);
batch.PostTo(actorWorld);
```

需要生成：

```csharp
ProjectionBatchBuffer<TEvent0> batch0;
ProjectionBatchBuffer<TEvent1> batch1;

TEvent0 e0 = default;
TEvent1 e1 = default;

forEach(..., ref e0, ref e1);

batch0.Add(actorId, in e0);
batch1.Add(actorId, in e1);

batch0.PostTo(actorWorld);
batch1.PostTo(actorWorld);
```

### 9.2 Executor 多事件示例

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using LayerBase.Actor;
using LayerBase.ECS.Projection;

namespace LayerBase.ECS.Projection.Flow;

internal static class ProjectionExecutor2<T0, T1>
{
    public static void Post<TEvent0, TEvent1>(
        World world,
        Query query,
        ProjectionPredicate<T0, T1>? predicate,
        ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach)
        where TEvent0 : struct
        where TEvent1 : struct
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 已构建的 Arch Query。

        // predicate 参数作用：
        // 可选组件过滤条件。

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
}
```

### 9.3 CollectPostChunk 多事件示例

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void CollectPostChunk<TEvent0, TEvent1>(
    World world,
    ActorWorld actorWorld,
    ref Chunk chunk,
    ProjectionPredicate<T0, T1>? predicate,
    ProjectionForEach2<T0, T1, TEvent0, TEvent1> forEach,
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
    // ProjectedActor 缺失时通过它创建 Actor。

    // chunk 参数作用：
    // 当前正在遍历的 Chunk。

    // predicate 参数作用：
    // 可选 Where 条件。

    // forEach 参数作用：
    // 多事件输出逻辑。

    // nowTicks 参数作用：
    // 本次 Projection 开始时取到的 Stopwatch 时间戳。

    // batch0 参数作用：
    // 第 1 个事件类型的 Batch。

    // batch1 参数作用：
    // 第 2 个事件类型的 Batch。

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

        if (predicate != null && !predicate(in entity, in c0, in c1))
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
```

---

## 10. 多事件投递顺序

规则：

```text
同一个 Projection 内：
  先遍历全部 Entity。
  同步收集所有事件类型的 Batch。
  最后按事件泛型顺序依次 PostTo。

例如 Bring<E0,E1,E2>：
  batch0.PostTo(actorWorld)
  batch1.PostTo(actorWorld)
  batch2.PostTo(actorWorld)
```

注意：

```text
这不保证同一个 Entity 的 E0、E1、E2 在 Actor 邮箱中的严格交错顺序。
如果业务需要严格顺序，应把多个事件合并成一个复合事件。
```

---

## 11. 修复六：T4 Helpers 补齐

当前 `Helpers.ttinclude` 只够生成 delegate。需要补 Flow / Executor 需要的函数：

```csharp
<#+
string EventName(int eventCount)
{
    return eventCount == 1
        ? "ProjectionForEach"
        : $"ProjectionForEach{eventCount}";
}

string WhereConstraints(int eventCount)
{
    return string.Join(
        "\n        ",
        Enumerable.Range(0, eventCount).Select(i => $"where TEvent{i} : struct"));
}

string FirstComponentRefs(int componentCount)
{
    var lines = new string[componentCount];

    for (int i = 0; i < componentCount; i++)
    {
        lines[i] = $"        ref T{i} first{i} = ref chunk.GetFirst<T{i}>();";
    }

    return string.Join("\n", lines);
}

string RowComponentRefs(int componentCount)
{
    var lines = new string[componentCount];

    for (int i = 0; i < componentCount; i++)
    {
        lines[i] =
$@"            ref T{i} c{i} =
                ref Unsafe.Add(
                    ref first{i},
                    row);";
    }

    return string.Join("\n", lines);
}

string PredicateArgs(int componentCount)
{
    var values = new string[1 + componentCount];

    values[0] = "in entity";

    for (int i = 0; i < componentCount; i++)
    {
        values[1 + i] = $"in c{i}";
    }

    return string.Join(", ", values);
}

string ForEachArgs(int componentCount, int eventCount)
{
    var values = new string[1 + componentCount + eventCount];

    int index = 0;
    values[index++] = "in entity";

    for (int i = 0; i < componentCount; i++)
    {
        values[index++] = $"ref c{i}";
    }

    for (int i = 0; i < eventCount; i++)
    {
        values[index++] = $"ref e{i}";
    }

    return string.Join(", ", values);
}
#>
```

还需要补：

```text
EventDefaults(eventCount)
BatchDeclarations(eventCount)
BatchRefArgs(eventCount)
BatchMethodParams(eventCount)
BatchPosts(eventCount)
BatchDisposes(eventCount)
BatchAdds(eventCount)
```

这些函数用于自动生成多事件 Batch 代码。

---

## 12. 模板必须从占位改成真生成

这三个文件不能再只是占位注释：

```text
ProjectionQueryFlow.tt
ProjectionExecutor.tt
ProjectionWorldExtensions.tt
```

必须做到：

```text
删除对应 .g.cs 后，运行 T4 能重新生成完整代码。
```

生成维度：

```text
componentCount = 0..8
eventCount = 1..10
```

---

## 13. 编译风险检查

### 13.1 delegate 命名避免歧义

保留当前命名方式：

```text
1 个事件：ProjectionForEach
2 个事件：ProjectionForEach2
...
10 个事件：ProjectionForEach10
```

不要只靠泛型参数数量重载同名 `ProjectionForEach`，否则 0 组件多事件和 1 组件单事件容易出现阅读和推断问题。

### 13.2 Query0 不生成空泛型

正确：

```csharp
public delegate bool ProjectionPredicate(
    in Entity entity);
```

错误：

```csharp
ProjectionPredicate<>
```

### 13.3 ProjectionExecutor0 不生成组件列读取

`ProjectionExecutor0` 不能出现：

```csharp
chunk.GetFirst<T0>();
```

只访问：

```csharp
chunk.FirstProjection();
chunk.Entities.DangerousGetReference();
```

### 13.4 多事件 Batch 必须 finally Dispose

生成：

```csharp
try
{
    ...
}
finally
{
    batchN.Dispose();
    ...
    batch0.Dispose();
}
```

不要依赖多个 `using` 嵌套生成复杂代码。

---

## 14. 验收测试

### 14.1 CreateEntity 链式创建

必须通过：

```csharp
Entity entity = world
    .CreateEntity(
        new PositionComponent(),
        new VelocityComponent())
    .WithProjectedActor<PlayerViewActor>()
    .Entity;
```

断言：

```text
Entity 创建成功。
ProjectedActorMeta.ActorTypeId 写入成功。
ProjectedActorMeta.ActorId 仍然是 Invalid。
第一次 Post 或 Touch 后才创建 Actor。
```

### 14.2 Query0 单事件

必须通过：

```csharp
world.Query()
    .Bring<VisibleEvent>()
    .ForEach(static (
        in Entity entity,
        ref VisibleEvent visible) =>
    {
        visible = new VisibleEvent();
    })
    .Batch()
    .Post();
```

断言：

```text
ForEach 被调用。
Batch 被写入。
ActorWorld.PostTo 被调用。
```

### 14.3 Query2 多事件

必须通过：

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
        move = new MoveEvent();
        footstep = new FootstepEvent();
    })
    .Batch()
    .Post();
```

断言：

```text
MoveEvent 被投递。
FootstepEvent 被投递。
两个事件投递到同一个 ActorId。
Post 不读取 RuntimeFrameBudget。
```

### 14.4 Query4 四事件

必须通过：

```csharp
world.Query<C0, C1, C2, C3>()
    .Where(static (
        in Entity entity,
        in C0 c0,
        in C1 c1,
        in C2 c2,
        in C3 c3) =>
    {
        return true;
    })
    .Bring<E0, E1, E2, E3>()
    .ForEach(static (
        in Entity entity,
        ref C0 c0,
        ref C1 c1,
        ref C2 c2,
        ref C3 c3,
        ref E0 e0,
        ref E1 e1,
        ref E2 e2,
        ref E3 e3) =>
    {
        e0 = new E0();
        e1 = new E1();
        e2 = new E2();
        e3 = new E3();
    })
    .Batch()
    .Post();
```

断言：

```text
4 个事件都被投递。
Where 为 false 时 4 个事件都不投递。
```

### 14.5 模板再生成

必须做到：

```text
删除 ProjectionQueryFlow.g.cs。
删除 ProjectionExecutor.g.cs。
删除 ProjectionWorldExtensions.g.cs。
运行 T4。
重新生成文件。
dotnet build 通过。
```

---

## 15. 最终修复清单

必须修：

```text
1. 新增 EntityCreateFlow.tt。
2. 新增 EntityCreateWorldExtensions.tt。
3. 生成 EntityCreateFlow.g.cs。
4. 生成 EntityCreateWorldExtensions.g.cs。
5. 补 ProjectionWorldExtensions.tt 真生成逻辑。
6. 补 ProjectionQueryFlow.tt 真生成逻辑。
7. 补 ProjectionExecutor.tt 真生成逻辑。
8. 重新生成 ProjectionWorldExtensions.g.cs。
9. 重新生成 ProjectionQueryFlow.g.cs。
10. 重新生成 ProjectionExecutor.g.cs。
```

保留：

```text
ProjectedActorTypeRegistry runtime-local。
Post() 不参与 RuntimeFrameBudget。
ForEach 不返回 bool。
Where 负责筛选。
时间戳回收模型。
```

完成后，目标 API 应同时支持：

```csharp
world.CreateEntity(c0, c1)
    .WithProjectedActor<MyActor>();
```

以及：

```csharp
world.Query<C0, C1>()
    .Bring<E0, E1, E2>()
    .ForEach(static (
        in Entity entity,
        ref C0 c0,
        ref C1 c1,
        ref E0 e0,
        ref E1 e1,
        ref E2 e2) =>
    {
        e0 = new E0();
        e1 = new E1();
        e2 = new E2();
    })
    .Batch()
    .Post();
```
