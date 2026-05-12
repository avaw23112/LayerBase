# LayerBase Query / Bring 模板方法与源生成器修正设计

## 1. 目标

本文档定义 `Query / Bring` 源生成器与 T4 模板的修正方案。

本次修正解决两个核心问题：

```text
1. 缺少 IQueryJob<T...> / IProjectionJobCxE<T...> 模板接口，导致生成器生成的 Job 无法适配 Flow 模板。

2. Query + Bring 链路没有把 ProjectResult 纳入 Batch/Post 行为判断，导致无法表达：
   - 跳过 Actor 行为
   - 跳过 Actor 行为但保活
   - 执行 Actor 行为并投递事件
```

最终链路必须对齐当前流式 API：

```csharp
Query()
    .Bring()
    .ForEach(ref job)
    .Batch()
    .Post();
```

其中：

```text
ForEach(ref job):
  绑定源生成器生成的 Job。

Batch():
  保持批处理语义。

Post():
  根据 ProjectResult 执行 Fail / Touch / Success 分支。
```

---

## 2. 最终语义

### 2.1 纯 ECS Query

无 `[Bring]` 的 `[Query]` 方法生成：

```csharp
Query<T0, T1, ...>()
    .ForEach(ref job);
```

语义：

```text
只执行 ECS Query + ForEach。
不创建 Actor。
不 Touch Actor。
不 Post ActorEvent。
用户方法必须返回 void。
```

---

### 2.2 Query + Bring Projection

带 `[Bring]` 的 `[Query]` 方法生成：

```csharp
Query<T0, T1, ...>()
    .Bring<TEvent0, ...>()
    .ForEach(ref job)
    .Batch()
    .Post();
```

语义：

```text
ForEach(ref job):
  调用用户写的 [Query] + [Bring] 方法。
  用户方法返回 ProjectResult。

Batch().Post():
  根据 ProjectResult 决定是否 Touch / Post。
```

---

## 3. ProjectResult 行为规则

`ProjectResult` 的行为在 `Batch().Post()` 阶段生效。

```text
ProjectResult.Fail:
  不 Ensure ProjectedActor。
  不 Touch ProjectedActor。
  不 Add Event 到 Batch。
  不 Post ActorEvent。

ProjectResult.Touch:
  Ensure / Touch ProjectedActor。
  不 Add Event 到 Batch。
  不 Post ActorEvent。

ProjectResult.Success:
  Ensure / Touch ProjectedActor。
  Add Event 到 Batch。
  Batch.PostTo(actorWorld) 时投递 ActorEvent。
```

注意：

```text
ProjectResult 不能回滚用户方法已经写入的 ECS 数据。
如果希望跳过 ECS 数据操作，用户方法必须在修改组件前 return ProjectResult.Fail。
```

正确写法：

```csharp
if (!aoi.IsVisible)
{
    return ProjectResult.Fail;
}

position.X += velocity.X;
position.Y += velocity.Y;

moveEvent = new MoveViewEvent(
    x: position.X,
    y: position.Y);

return ProjectResult.Success;
```

错误预期：

```csharp
position.X += velocity.X;

return ProjectResult.Fail;
```

这种写法仍然会修改 ECS 数据。`Fail` 只会跳过 Actor Touch 和 Actor Event Post。

---

## 4. 生成代码目标形态

### 4.1 纯 Query 生成代码

用户代码：

```csharp
[Query]
private void OnUpdatePosition(
    ref PositionComponent position,
    in VelocityComponent velocity)
{
    position.X += velocity.X;
    position.Y += velocity.Y;
}
```

生成代码：

```csharp
public void UpdatePosition()
{
    var job = new __UpdatePositionJob(this);

    global::LayerBase.ServiceECSExtensions
        .Query<
            global::Game.Tests.PositionComponent,
            global::Game.Tests.VelocityComponent>(this)
        .ForEach(ref job);
}

private readonly struct __UpdatePositionJob :
    IQueryJob<
        global::Game.Tests.PositionComponent,
        global::Game.Tests.VelocityComponent>
{
    private readonly global::Game.Tests.MoveService _self;

    public __UpdatePositionJob(
        global::Game.Tests.MoveService self)
    {
        _self = self;
    }

    public void Execute(
        Entity entity,
        ref global::Game.Tests.PositionComponent c0,
        ref global::Game.Tests.VelocityComponent c1)
    {
        _self.OnUpdatePosition(
            ref c0,
            in c1);
    }
}
```

---

### 4.2 Query + Bring 生成代码

用户代码：

```csharp
[Query]
[Bring<MoveViewEvent>]
private ProjectResult OnUpdateEnemyView(
    ref PositionComponent position,
    in VelocityComponent velocity,
    in AoiComponent aoi,
    ref MoveViewEvent moveEvent)
{
    if (!aoi.IsVisible)
    {
        return ProjectResult.Fail;
    }

    position.X += velocity.X;
    position.Y += velocity.Y;

    moveEvent = new MoveViewEvent(
        x: position.X,
        y: position.Y);

    return ProjectResult.Success;
}
```

生成代码：

```csharp
public void UpdateEnemyView()
{
    var job = new __UpdateEnemyViewJob(this);

    global::LayerBase.ServiceECSExtensions
        .Query<
            global::Game.Tests.PositionComponent,
            global::Game.Tests.VelocityComponent,
            global::Game.Tests.AoiComponent>(this)
        .Bring<global::Game.Tests.MoveViewEvent>()
        .ForEach(ref job)
        .Batch()
        .Post();
}

private readonly struct __UpdateEnemyViewJob :
    IProjectionJob3x1<
        global::Game.Tests.PositionComponent,
        global::Game.Tests.VelocityComponent,
        global::Game.Tests.AoiComponent,
        global::Game.Tests.MoveViewEvent>
{
    private readonly global::Game.Tests.EnemyViewService _self;

    public __UpdateEnemyViewJob(
        global::Game.Tests.EnemyViewService self)
    {
        _self = self;
    }

    public ProjectResult Execute(
        Entity entity,
        ref global::Game.Tests.PositionComponent c0,
        ref global::Game.Tests.VelocityComponent c1,
        ref global::Game.Tests.AoiComponent c2,
        ref global::Game.Tests.MoveViewEvent e0)
    {
        return _self.OnUpdateEnemyView(
            ref c0,
            in c1,
            in c2,
            ref e0);
    }
}
```

说明：

```text
Job 接口层统一使用 ref。
调用用户方法时再按用户原始 ref / in 转发。
```

---

## 5. 源生成器修改方案

## 5.1 GenerateQueryInvocation

纯 Query 分支生成：

```csharp
private static void GenerateQueryInvocation(
    StringBuilder sb,
    QueryMethodInfo method)
{
    string compGeneric =
        BuildComponentGenericArguments(method);

    sb.AppendLine(
        $"            var job = new __{method.EntryPointName}Job(this);");

    sb.AppendLine();

    sb.AppendLine(
        "            global::LayerBase.ServiceECSExtensions");

    sb.AppendLine(
        $"                .Query<{compGeneric}>(this)");

    sb.AppendLine(
        "                .ForEach(ref job);");
}
```

说明：

```text
使用 global::LayerBase.ServiceECSExtensions.Query<T...>(this) 是为了避免生成代码依赖 using 解析扩展方法。
```

---

## 5.2 GenerateBringInvocation

带 Bring 分支生成：

```csharp
private static void GenerateBringInvocation(
    StringBuilder sb,
    QueryMethodInfo method)
{
    string compGeneric =
        BuildComponentGenericArguments(method);

    string eventGeneric =
        BuildEventGenericArguments(method);

    sb.AppendLine(
        $"            var job = new __{method.EntryPointName}Job(this);");

    sb.AppendLine();

    sb.AppendLine(
        "            global::LayerBase.ServiceECSExtensions");

    sb.AppendLine(
        $"                .Query<{compGeneric}>(this)");

    sb.AppendLine(
        $"                .Bring<{eventGeneric}>()");

    sb.AppendLine(
        "                .ForEach(ref job)");

    sb.AppendLine(
        "                .Batch()");

    sb.AppendLine(
        "                .Post();");
}
```

说明：

```text
ref job 必须放在 ForEach 上。
Post() 不接收 job。
Post() 是 Batch 流的终点。
```

---

## 5.3 BuildJobInterfaceName

Job 接口生成规则：

```text
无 Bring:
  IQueryJob<TComponent...>

有 Bring:
  IProjectionJob{ComponentCount}x{EventCount}<TComponent..., TEvent...>
```

代码：

```csharp
private static string BuildJobInterfaceName(
    QueryMethodInfo method)
{
    string jobGeneric =
        BuildJobGenericArguments(method);

    bool hasBring =
        method.BringEventTypes.Length > 0;

    if (!hasBring)
    {
        return $"IQueryJob<{jobGeneric}>";
    }

    int componentCount =
        method.ComponentTypes.Length;

    int eventCount =
        method.BringEventTypes.Length;

    return
        $"IProjectionJob{componentCount}x{eventCount}<{jobGeneric}>";
}
```

---

## 5.4 GenerateJobStruct

将当前固定生成 `IQueryJob<T...>` 的逻辑替换为：

```csharp
private static void GenerateJobStruct(
    StringBuilder sb,
    QueryMethodInfo method)
{
    bool hasBring =
        method.BringEventTypes.Length > 0;

    string jobInterfaceName =
        BuildJobInterfaceName(method);

    string selfTypeName =
        GetTypeDisplayName(method.MethodSymbol.ContainingType);

    string methodName =
        method.MethodSymbol.Name;

    sb.AppendLine(
        $"        private readonly struct __{method.EntryPointName}Job : {jobInterfaceName}");

    sb.AppendLine("        {");

    sb.AppendLine(
        $"            private readonly {selfTypeName} _self;");

    sb.AppendLine();

    sb.AppendLine(
        $"            public __{method.EntryPointName}Job({selfTypeName} self)");

    sb.AppendLine("            {");
    sb.AppendLine("                _self = self;");
    sb.AppendLine("            }");
    sb.AppendLine();

    string returnType =
        hasBring ? "ProjectResult" : "void";

    sb.AppendLine(
        $"            public {returnType} Execute(");

    List<string> executeParameters =
        BuildExecuteParameters(method);

    for (int i = 0; i < executeParameters.Count; i++)
    {
        string comma =
            i < executeParameters.Count - 1 ? "," : "";

        sb.AppendLine(
            $"                {executeParameters[i]}{comma}");
    }

    sb.AppendLine("            )");
    sb.AppendLine("            {");

    string argStr =
        BuildUserMethodArgumentList(method);

    if (hasBring)
    {
        sb.AppendLine(
            $"                return _self.{methodName}({argStr});");
    }
    else
    {
        sb.AppendLine(
            $"                _self.{methodName}({argStr});");
    }

    sb.AppendLine("            }");
    sb.AppendLine("        }");
}
```

---

## 5.5 BuildExecuteParameters

接口层参数统一用 `ref`。

```csharp
private static List<string> BuildExecuteParameters(
    QueryMethodInfo method)
{
    var parameters =
        new List<string>
        {
            "Entity entity"
        };

    for (int i = 0; i < method.ComponentTypes.Length; i++)
    {
        string typeName =
            GetTypeDisplayName(method.ComponentTypes[i]);

        parameters.Add(
            $"ref {typeName} c{i}");
    }

    for (int i = 0; i < method.BringEventTypes.Length; i++)
    {
        string typeName =
            GetTypeDisplayName(method.BringEventTypes[i]);

        parameters.Add(
            $"ref {typeName} e{i}");
    }

    return parameters;
}
```

说明：

```text
Job 接口统一 ref，便于 Flow 模板复用。
用户方法中的 in / ref 语义由 BuildUserMethodArgumentList 保留。
```

---

## 5.6 BuildUserMethodArgumentList

该方法应继续根据用户原始参数生成：

```text
Entity 参数:
  entity

ref 组件:
  ref c{i}

in 组件:
  in c{i}

ref Bring 事件:
  ref e{i}
```

示例：

```csharp
return _self.OnUpdateEnemyView(
    ref c0,
    in c1,
    in c2,
    ref e0);
```

---

## 5.7 生成器诊断补充

新增诊断：

```text
LB-ECS040:
  Query source generator requires owner type to be compatible with ServiceECSExtensions.Query.

LB-ECS041:
  Bring event count exceeds generated IProjectionJob template limit.

LB-ECS042:
  Component count exceeds generated IQueryJob / IProjectionJob template limit.

LB-ECS043:
  Bring flow must generate ForEach(ref job).Batch().Post().
```

---

## 6. ProjectionDelegates.tt 修改方案

## 6.1 文件头部

增加 `LayerBase.ECS` 引用：

```t4
#nullable enable
using Arch.Core;
using LayerBase.ECS;

namespace LayerBase.ECS.Projection.Flow;
```

---

## 6.2 保留现有 delegate

现有 delegate 继续保留：

```t4
<# for (var c = 0; c <= ComponentAmount; c++) { #>
<# var compG = CG(c); #>
<# if (c == 0) { #>
public delegate bool ProjectionPredicate(
        in Entity entity);
<# } else { #>
public delegate bool ProjectionPredicate<<#= compG #>>(
        <#= PredP(c) #>);
<# } #>
<# for (var e = 1; e <= EventAmount; e++) { #>
<# var evtName = e == 1 ? "ProjectionForEach" : $"ProjectionForEach{e}"; #>
public delegate void <#= evtName #><<#= AG(c, e) #>>(
        <#= FEP(c, e) #>)
    <#= FEC(e) #>;

<# } #>
<# } #>
```

---

## 6.3 新增 IQueryJob 模板

追加：

```t4
<# for (var c = 1; c <= ComponentAmount; c++) { #>
<# var compG = CG(c); #>
public interface IQueryJob<<#= compG #>>
{
    void Execute(
        Entity entity<# for (var i = 0; i < c; i++) { #>,
        ref T<#= i #> c<#= i #><# } #>);
}

<# } #>
```

说明：

```text
IQueryJob<T...> 只用于纯 ECS Query。
不用于 Query + Bring。
```

---

## 6.4 新增 IProjectionJob 模板

追加：

```t4
<# for (var c = 1; c <= ComponentAmount; c++) { #>
<# for (var e = 1; e <= EventAmount; e++) { #>
<# var allG = AG(c, e); #>
public interface IProjectionJob<#= c #>x<#= e #><<#= allG #>>
{
    ProjectResult Execute(
        Entity entity<# for (var i = 0; i < c; i++) { #>,
        ref T<#= i #> c<#= i #><# } #><# for (var i = 0; i < e; i++) { #>,
        ref TEvent<#= i #> e<#= i #><# } #>);
}

<# } #>
<# } #>
```

说明：

```text
IProjectionJob3x1<T0, T1, T2, TEvent0>:
  3 个组件 + 1 个事件。

IProjectionJob8x4<T0...T7, TEvent0...TEvent3>:
  8 个组件 + 4 个事件。

维度写进接口名，避免 IQueryJob<T...> 泛型数量歧义。
```

---

## 7. ProjectionQueryFlow.tt 修改方案

## 7.1 纯 Query Flow 增加 ForEach(ref job)

在 `ProjectionQueryFlow{c}` 中增加：

```t4
    public void ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IQueryJob<<#= compG #>>
    {
        ProjectionExecutor<#= c #><<#= compG #>>.ForEach(
            _world,
            _query,
            _predicate,
            ref job);
    }
```

该方法支持：

```csharp
Query<T0, T1>()
    .ForEach(ref job);
```

---

## 7.2 Bring Flow 增加 ForEach(ref job)

在 `ProjectionBringFlow{c}` 中保留现有 delegate 版：

```t4
    public ProjectionPostFlow<#= c #><#= suffix #><<#= allG #>> ForEach(<#= dName #><<#= allG #>> forEach)
    {
        return new ProjectionPostFlow<#= c #><#= suffix #><<#= allG #>>(_world, _query, _predicate, forEach);
    }
```

新增 Job 版：

```t4
    public ProjectionJobPostFlow<#= c #><#= suffix #><<#= allG #>, TJob> ForEach<TJob>(
        ref TJob job)
        where TJob : struct, IProjectionJob<#= c #>x<#= e #><<#= allG #>>
    {
        return new ProjectionJobPostFlow<#= c #><#= suffix #><<#= allG #>, TJob>(
            _world,
            _query,
            _predicate,
            job);
    }
```

支持：

```csharp
Query<T0, T1, T2>()
    .Bring<TEvent0>()
    .ForEach(ref job)
    .Batch()
    .Post();
```

---

## 7.3 新增 ProjectionJobPostFlow

在 `ProjectionPostFlow{c}` 旁边新增：

```t4
public readonly struct ProjectionJobPostFlow<#= c #><#= suffix #><<#= allG #>, TJob>
    where TJob : struct, IProjectionJob<#= c #>x<#= e #><<#= allG #>>
<#= cons #>
{
    private readonly World _world;
    private readonly Query _query;
    private readonly ProjectionPredicate<#= compGP #>? _predicate;
    private readonly TJob _job;

    internal ProjectionJobPostFlow<#= c #><#= suffix #>(
        World world,
        Query query,
        ProjectionPredicate<#= compGP #>? predicate,
        TJob job)
    {
        // world 参数作用：
        // 当前 ECS World。

        // query 参数作用：
        // 当前 Projection Query。

        // predicate 参数作用：
        // 可选过滤条件。

        // job 参数作用：
        // 源生成器生成的 Projection Job。
        // 它会调用用户写的 [Query] + [Bring] 方法。

        _world = world;
        _query = query;
        _predicate = predicate;
        _job = job;
    }

    public ProjectionJobPostFlow<#= c #><#= suffix #><<#= allG #>, TJob> Batch()
    {
        return this;
    }

    public void Post()
    {
        TJob job =
            _job;

<# if (e == 1) { #>
        ProjectionExecutor<#= c #><<#= compG #>>.Post(
            _world,
            _query,
            _predicate,
            ref job);
<# } else { #>
        ProjectionExecutor<#= c #>_<#= e #>E<<#= allG #>>.Post(
            _world,
            _query,
            _predicate,
            ref job);
<# } #>
    }
}
```

说明：

```text
_job 是 struct 字段。
Post() 中先复制到局部变量，再 ref job 传入执行器。
```

---

## 7.4 0 组件 Flow

当前源生成器不生成 0 组件 Query，因此 Job 版接口第一阶段只要求覆盖 `c = 1..ComponentAmount`。

手写 QueryFlow0 的 delegate 流可继续保留。

---

## 8. ProjectionExecutor.tt 修改方案

## 8.1 纯 Query Job ForEach

在 `ProjectionExecutor{c}` 中新增：

```t4
    public static void ForEach<TJob>(
        World world,
        Query query,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<<#= compG #>>
    {
        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectForEachChunk(
                ref chunk,
                predicate,
                ref job);
        }
    }
```

新增内部方法：

```t4
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectForEachChunk<TJob>(
        ref Chunk chunk,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job)
        where TJob : struct, IQueryJob<<#= compG #>>
    {
<#= FirstComponentRefs(c) #>
        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
<#= RowComponentRefs(c) #>
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(<#= PredicateArgs(c) #>))
            {
                continue;
            }

            job.Execute(
                entity<# for (var i = 0; i < c; i++) { #>,
                ref c<#= i #><# } #>);
        }
    }
```

---

## 8.2 单事件 Projection Job Post

在 `ProjectionExecutor{c}` 中新增 Job 版 `Post`：

```t4
    public static void Post<TEvent0, TJob>(
        World world,
        Query query,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob<#= c #>x1<<#= compG #>, TEvent0>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        ProjectionBatchBuffer<TEvent0> batch0 =
            ProjectionBatchBuffer<TEvent0>.Rent();

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks,
                    ref batch0);
            }

            batch0.PostTo(
                actorWorld);
        }
        finally
        {
            batch0.Dispose();
        }
    }
```

新增内部方法：

```t4
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TEvent0, TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent0> batch0)
        where TEvent0 : struct
        where TJob : struct, IProjectionJob<#= c #>x1<<#= compG #>, TEvent0>
    {
<#= FirstComponentRefs(c) #>
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
<#= RowComponentRefs(c) #>
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(<#= PredicateArgs(c) #>))
            {
                continue;
            }

            TEvent0 e0 =
                default;

            ProjectResult result =
                job.Execute(
                    entity<# for (var i = 0; i < c; i++) { #>,
                    ref c<#= i #><# } #>,
                    ref e0);

            if (result.Kind == ProjectResultKind.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

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

            if (result.Kind == ProjectResultKind.Touch)
            {
                continue;
            }

            batch0.Add(
                actorId,
                in e0);
        }
    }
```

---

## 8.3 多事件 Projection Job Post

在 `ProjectionExecutor{c}_{e}E` 中新增：

```t4
    public static void Post<TJob>(
        World world,
        Query query,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job)
        where TJob : struct, IProjectionJob<#= c #>x<#= e #><<#= allG #>>
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

<# for (var i = 0; i < e; i++) { #>
        ProjectionBatchBuffer<TEvent<#= i #>> batch<#= i #> =
            ProjectionBatchBuffer<TEvent<#= i #>>.Rent();
<# } #>

        try
        {
            foreach (ref Chunk chunk in query.GetChunkIterator())
            {
                CollectPostJobChunk(
                    world,
                    actorWorld,
                    ref chunk,
                    predicate,
                    ref job,
                    nowTicks<# for (var i = 0; i < e; i++) { #>,
                    ref batch<#= i #><# } #>);
            }

<# for (var i = 0; i < e; i++) { #>
            batch<#= i #>.PostTo(
                actorWorld);
<# } #>
        }
        finally
        {
<# for (var i = e - 1; i >= 0; i--) { #>
            batch<#= i #>.Dispose();
<# } #>
        }
    }
```

新增内部方法核心逻辑：

```t4
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostJobChunk<TJob>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<#= compGP #>? predicate,
        ref TJob job,
        long nowTicks<# for (var i = 0; i < e; i++) { #>,
        ref ProjectionBatchBuffer<TEvent<#= i #>> batch<#= i #><# } #>)
        where TJob : struct, IProjectionJob<#= c #>x<#= e #><<#= allG #>>
    {
<#= FirstComponentRefs(c) #>
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
<#= RowComponentRefs(c) #>
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null && !predicate(<#= PredicateArgs(c) #>))
            {
                continue;
            }

<# for (var i = 0; i < e; i++) { #>
            TEvent<#= i #> e<#= i #> =
                default;
<# } #>

            ProjectResult result =
                job.Execute(
                    entity<# for (var i = 0; i < c; i++) { #>,
                    ref c<#= i #><# } #><# for (var i = 0; i < e; i++) { #>,
                    ref e<#= i #><# } #>);

            if (result.Kind == ProjectResultKind.Fail)
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

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

            if (result.Kind == ProjectResultKind.Touch)
            {
                continue;
            }

<# for (var i = 0; i < e; i++) { #>
            batch<#= i #>.Add(
                actorId,
                in e<#= i #>);
<# } #>
        }
    }
```

说明：

```text
同一个 Entity 每轮只 Ensure / Touch 一次。
Success 时才把多个事件分别加入对应 Batch。
Touch 时不加入任何事件。
Fail 时 Actor 也不保活。
```

---

## 9. Helpers.ttinclude 建议新增辅助函数

可选新增以下函数，减少模板重复：

```t4
string ProjectionJobName(int cc, int ec)
{
    return $"IProjectionJob{cc}x{ec}";
}

string ProjectionJobConstraint(int cc, int ec)
{
    return $"where TJob : struct, IProjectionJob{cc}x{ec}<{AG(cc, ec)}>";
}

string JobExecuteArgs(int cc, int ec)
{
    var items = new string[1 + cc + ec];
    int idx = 0;
    items[idx++] = "entity";

    for (int i = 0; i < cc; i++)
        items[idx++] = $"ref c{i}";

    for (int i = 0; i < ec; i++)
        items[idx++] = $"ref e{i}";

    return string.Join(", ", items);
}
```

---

## 10. 保留 delegate 版 API

本次修正不删除现有 delegate 版 API。

继续保留：

```csharp
Query()
    .Bring()
    .ForEach(delegate)
    .Batch()
    .Post();
```

新增 Job 版 API：

```csharp
Query()
    .Bring()
    .ForEach(ref job)
    .Batch()
    .Post();
```

目的：

```text
手写业务可以继续用 delegate。
源生成器使用 ref job，避免闭包和 delegate 分配。
```

---

## 11. 回归测试方案

### 11.1 模板编译测试

```text
ProjectionDelegates_Generates_IQueryJob_1_To_8
ProjectionDelegates_Generates_IProjectionJob_1x1_To_8x10
ProjectionQueryFlow_Generates_ForEach_RefJob_For_Query
ProjectionQueryFlow_Generates_ForEach_RefJob_Batch_Post_For_Bring
ProjectionExecutor_Generates_ProjectResult_Branches
```

---

### 11.2 源生成器生成代码测试

```text
QueryWithoutBring_Generates_Query_ForEach_RefJob
QueryWithBring_Generates_Bring_ForEach_RefJob_Batch_Post
QueryWithBring_Generates_IProjectionJob_CxE
QueryWithBring_ExecuteParameters_AreRef
QueryWithBring_UserMethodCall_PreservesInRef
```

---

### 11.3 功能测试

```text
QueryWithoutBring_ForEach_ExecutesAndMutatesComponent

QueryWithBring_Success:
  Mutates ECS data.
  Touches / Ensures Actor.
  Adds Event to Batch.
  Posts Event.

QueryWithBring_Touch:
  Touches / Ensures Actor.
  Does not Add Event.
  Does not Post Event.

QueryWithBring_Fail:
  Does not Touch Actor.
  Does not Add Event.
  Does not Post Event.
```

---

## 12. 实施顺序

```text
Step 1:
  修改 QueryBringGenerator.cs。
  - Bring 分支生成 ForEach(ref job).Batch().Post()
  - Job 接口改为 IProjectionJobCxE
  - Execute 参数统一 ref
  - 用户方法调用保留 in / ref

Step 2:
  修改 ProjectionDelegates.tt。
  - 补 IQueryJob<T...>
  - 补 IProjectionJobCxE<T...>

Step 3:
  修改 ProjectionQueryFlow.tt。
  - 纯 Query Flow 增加 ForEach(ref job)
  - Bring Flow 增加 ForEach(ref job)
  - 增加 ProjectionJobPostFlow

Step 4:
  修改 ProjectionExecutor.tt。
  - 增加纯 Query Job ForEach 执行器
  - 增加 Projection Job Post 执行器
  - 在 Post 中实现 ProjectResult.Fail / Touch / Success 分支

Step 5:
  运行 T4 重新生成 .cs 文件。

Step 6:
  补 NUnit3 功能测试。
```

---

## 13. 完成标准

完成后应满足：

```text
纯 Query:
  生成 Query().ForEach(ref job)。
  使用 IQueryJob<TComponent...>。
  用户方法返回 void。

Query + Bring:
  生成 Query().Bring().ForEach(ref job).Batch().Post()。
  使用 IProjectionJobCxE<TComponent..., TEvent...>。
  用户方法返回 ProjectResult。

ProjectResult.Fail:
  不 Touch Actor。
  不 Post ActorEvent。

ProjectResult.Touch:
  Touch / Ensure Actor。
  不 Post ActorEvent。

ProjectResult.Success:
  Touch / Ensure Actor。
  Post ActorEvent。

接口层 Execute 参数统一 ref。
用户方法调用保留原始 ref / in 语义。
delegate 版 API 保留。
ref job 版 API 面向源生成器和热路径。
