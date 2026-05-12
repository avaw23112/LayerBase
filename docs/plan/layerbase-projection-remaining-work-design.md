# LayerBase Projection Remaining Work Design

## 1. 文档目标

本文档基于当前 `LayerBase` 最新实现状态，整理 Projection 系统剩余需要补齐和加固的部分。

当前已经完成的主体能力：

```text
ProjectedActorTypeRegistry 保持 runtime-local。
LayerRuntime 持有 ProjectedActorTypeRegistry。
CreateEntity(...).WithProjectedActor<TActor>() 已支持 0~8 组件版本。
Query() / Query<T0...T7>() 已存在。
ProjectionForEach 已支持 0~8 组件 × 1~10 事件。
Bring<TEvent0...TEvent9>() 已存在。
ProjectionExecutor 已支持多事件 Batch 投递。
ProjectionQueryFlow / ProjectionExecutor / ProjectionWorldExtensions / EntityCreateFlow 已有 T4 模板主体。
```

本文档只处理剩余问题：

```text
1. T4 生成链路需要可复现。
2. GeneratedProjectedActorTypes 源生成器产物需要确认和补强。
3. Query0 的空 QueryDescription 语义需要测试。
4. CreateEntity 扩展命名空间需要统一或明确导出。
5. 多事件投递需要补测试。
6. Projection 热路径需要补性能边界测试。
7. 文档和 CI 需要约束生成文件一致性。
```

---

## 2. 保持不变的设计约束

以下设计不再改动：

```text
ProjectedActorTypeRegistry 不改成全局静态表。
ProjectedActorTypeRegistry 继续由 LayerRuntime 持有。
ProjectedActor 创建必须通过当前 world.Runtime.Actors。
Post() 不接收 RuntimeFrameBudget。
Post() 不接收 ActorWorld。
Post() 不接收 currentFrame。
Post() 只负责 Batch -> ActorWorld.PostTo。
ActorWorld.Pump 继续是 RuntimeFrameBudget 消费点。
ProjectedActor 回收继续使用 IPooledActor.RecycleDeadlineTicks。
```

核心链路保持：

```text
CreateEntity(...).WithProjectedActor<TActor>()
  -> 写入当前 Entity 的 ProjectedActorMeta.ActorTypeId
  -> 不立即创建 Actor

Projection.Post()
  -> Query Chunk
  -> Where
  -> ForEach 输出一个或多个事件
  -> EnsureProjectedActor
  -> 多事件 Batch
  -> ActorWorld.PostTo

ActorWorld.Pump(...)
  -> 消费 Actor 邮箱
  -> 消费 RuntimeFrameBudget

EcsWorld.SweepProjectedActors()
  -> 使用 Stopwatch.GetTimestamp()
  -> 比较 IPooledActor.RecycleDeadlineTicks
  -> 回收过期 ProjectedActor
```

---

## 3. 剩余问题总表

| 项目 | 当前状态 | 需要补齐 |
|---|---:|---|
| T4 模板主体 | 已有 | 需要生成脚本和 CI 校验 |
| `.g.cs` 输出路径 | 已提交生成文件 | 需要保证模板可稳定输出到目标路径 |
| Actor 类型源生成器 | 只看到 partial shell | 需要确认生成器实际生成 partial 方法 |
| Query0 | API 已有 | 需要测试空 QueryDescription 是否遍历全部 Entity |
| CreateEntity 命名空间 | 位于 `LayerBase.ECS.Projection.Create` | 需要统一导出或文档明确 |
| 多事件 Batch | 已实现 | 需要补行为测试 |
| 热路径约束 | 设计满足 | 需要补无反射、无字典热路径测试或审查脚本 |
| 生成文件一致性 | 未见约束 | 需要 CI 检查生成结果是否与仓库一致 |

---

## 4. T4 生成链路

### 4.1 问题

当前模板文件已经存在：

```text
LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt
LayerBase/ECS/Projection/Templates/ProjectionQueryFlow.tt
LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt
LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt
LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt
LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt
```

生成文件位于：

```text
LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs
LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs
LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs
LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs
LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs
LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs
```

但是标准 T4 工具默认可能把输出文件生成到模板同目录。

因此必须补一个明确生成脚本。

---

### 4.2 推荐脚本：PowerShell

文件：

```text
scripts/generate-projection-flow.ps1
```

代码：

```powershell
param(
    [string]$Root = (Resolve-Path "$PSScriptRoot/..").Path
)

# Root 参数作用：
# 仓库根目录。
# 默认取 scripts 目录的上一级。

$ErrorActionPreference = "Stop"

# 逻辑说明：
# dotnet-t4 是跨平台 T4 命令行工具。
# 如果本机没有安装，应先执行：
# dotnet tool install --global dotnet-t4

$templates = @(
    @{
        Template = "LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt"
        Output   = "LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs"
    },
    @{
        Template = "LayerBase/ECS/Projection/Templates/ProjectionQueryFlow.tt"
        Output   = "LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs"
    },
    @{
        Template = "LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt"
        Output   = "LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs"
    },
    @{
        Template = "LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt"
        Output   = "LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs"
    },
    @{
        Template = "LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt"
        Output   = "LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs"
    },
    @{
        Template = "LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt"
        Output   = "LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs"
    }
)

foreach ($item in $templates) {
    $templatePath = Join-Path $Root $item.Template
    $outputPath = Join-Path $Root $item.Output

    # templatePath 变量作用：
    # 当前要执行的 T4 模板路径。

    # outputPath 变量作用：
    # 当前模板生成后的目标 .g.cs 文件路径。

    Write-Host "Generating $($item.Output) from $($item.Template)"

    t4 `
        -o $outputPath `
        $templatePath
}
```

---

### 4.3 推荐脚本：Bash

文件：

```text
scripts/generate-projection-flow.sh
```

代码：

```bash
#!/usr/bin/env bash
set -euo pipefail

# ROOT 变量作用：
# 仓库根目录。
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# 逻辑说明：
# dotnet-t4 是跨平台 T4 命令行工具。
# 如果本机没有安装，应先执行：
# dotnet tool install --global dotnet-t4

generate() {
  local template="$1"
  local output="$2"

  # template 参数作用：
  # 当前要执行的 T4 模板路径。

  # output 参数作用：
  # 当前模板生成后的目标 .g.cs 文件路径。

  echo "Generating ${output} from ${template}"
  t4 -o "${ROOT}/${output}" "${ROOT}/${template}"
}

generate "LayerBase/ECS/Projection/Templates/ProjectionDelegates.tt" \
         "LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs"

generate "LayerBase/ECS/Projection/Templates/ProjectionQueryFlow.tt" \
         "LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs"

generate "LayerBase/ECS/Projection/Templates/ProjectionExecutor.tt" \
         "LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs"

generate "LayerBase/ECS/Projection/Templates/ProjectionWorldExtensions.tt" \
         "LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs"

generate "LayerBase/ECS/Projection/Templates/EntityCreateFlow.tt" \
         "LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs"

generate "LayerBase/ECS/Projection/Templates/EntityCreateWorldExtensions.tt" \
         "LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs"
```

---

## 5. CI 校验生成文件一致性

### 5.1 目标

CI 中需要确认：

```text
模板重新生成后的 .g.cs 与仓库提交内容一致。
如果不一致，说明开发者修改了模板但忘记提交生成文件，或者手改了生成文件。
```

---

### 5.2 GitHub Actions 片段

文件：

```text
.github/workflows/projection-generated-check.yml
```

代码：

```yaml
name: Projection Generated Check

on:
  pull_request:
    branches: [ faster, main ]
  push:
    branches: [ faster, main ]

jobs:
  projection-generated-check:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install dotnet-t4
        run: dotnet tool install --global dotnet-t4

      - name: Generate Projection Flow
        run: bash scripts/generate-projection-flow.sh

      - name: Check generated files are committed
        run: |
          git diff --exit-code -- \
            LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs \
            LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs \
            LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs \
            LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs \
            LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs \
            LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs
```

参数说明：

```text
dotnet-version:
  CI 使用的 .NET SDK 版本。
  当前项目如固定 .NET 8，应改成 8.0.x。

git diff --exit-code:
  如果生成后的文件与仓库提交文件不同，CI 失败。
```

---

## 6. Actor 类型源生成器确认

### 6.1 当前风险

当前 `GeneratedProjectedActorTypes.cs` 是 partial shell：

```csharp
internal static partial class GeneratedProjectedActorTypes
{
    public static void RegisterTo(ProjectedActorTypeRegistry registry)
    {
        RegisterGeneratedTypes(registry);
    }

    public static int GetId<TActor>()
        where TActor : class, IPooledActor, new()
    {
        int actorTypeId = -1;
        TryWriteGeneratedId<TActor>(ref actorTypeId);
        return actorTypeId;
    }

    private static partial void RegisterGeneratedTypes(ProjectedActorTypeRegistry registry);

    private static partial void TryWriteGeneratedId<TActor>(ref int actorTypeId)
        where TActor : class, IPooledActor, new();
}
```

如果源生成器没有生成对应 partial 实现，则会出现：

```text
RegisterTo 不注册任何 Actor。
GetId<TActor>() 始终返回 -1。
WithProjectedActor<TActor>() 抛异常。
```

---

### 6.2 必须生成的代码形态

源生成器必须生成类似：

```csharp
using Game.Actors;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Generated;

internal static partial class GeneratedProjectedActorTypes
{
    private const int PlayerViewActorId = 0;

    private static partial void RegisterGeneratedTypes(
        ProjectedActorTypeRegistry registry)
    {
        // registry 参数作用：
        // 当前 LayerRuntime 持有的 ProjectedActorTypeRegistry。
        // 这里不使用全局静态 Registry。

        registry.RegisterGenerated(
            actorTypeId: PlayerViewActorId,
            actorType: typeof(PlayerViewActor),
            factory: static actorWorld =>
                actorWorld.CreateProjectedActor<PlayerViewActor>());
    }

    private static partial void TryWriteGeneratedId<TActor>(
        ref int actorTypeId)
        where TActor : class, IPooledActor, new()
    {
        // actorTypeId 参数作用：
        // 生成器把匹配到的 Actor 类型编号写入该引用。
        // 未匹配时保持 -1。

        if (typeof(TActor) == typeof(PlayerViewActor))
        {
            actorTypeId = PlayerViewActorId;
        }
    }
}
```

约束：

```text
factory 必须是 static lambda。
factory 不允许捕获 ActorWorld。
factory 不允许捕获 LayerRuntime。
factory 不允许捕获 World。
不允许 Activator.CreateInstance。
不允许 MethodInfo.Invoke。
不允许运行时扫描程序集。
```

---

### 6.3 源生成器测试

新增测试目标：

```text
实现 IPooledActor 的测试 Actor 能被生成 ID。
GeneratedProjectedActorTypes.GetId<TestProjectedActor>() >= 0。
LayerRuntime 初始化后 Registry 能通过 ID 创建 Actor。
factory 创建的 Actor 属于当前 LayerRuntime.Actors。
两个 LayerRuntime 创建同一个 ActorTypeId 时，Actor 分别进入各自 ActorWorld。
```

测试伪代码：

```csharp
[Fact]
public void GeneratedProjectedActorType_Should_Create_Actor_In_Current_Runtime()
{
    // 逻辑说明：
    // 验证 runtime-local Registry 没有把 Actor 创建到错误 ActorWorld。

    LayerRuntime runtimeA = new();
    LayerRuntime runtimeB = new();

    Entity entityA = runtimeA.EcsWorld
        .CreateEntity()
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    Entity entityB = runtimeB.EcsWorld
        .CreateEntity()
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    runtimeA.EcsWorld
        .Query()
        .TouchProjectedActor();

    runtimeB.EcsWorld
        .Query()
        .TouchProjectedActor();

    Assert.True(runtimeA.Actors.ContainsProjectedActor(entityA));
    Assert.True(runtimeB.Actors.ContainsProjectedActor(entityB));
}
```

说明：

```text
ContainsProjectedActor 只是测试辅助方法。
如果当前 ActorWorld 没有这个 API，可以通过 TryGetProjectionMeta + TryGetActor 间接验证。
```

---

## 7. Query0 语义测试

### 7.1 风险

当前 `world.Query()` 使用空 `QueryDescription`：

```csharp
QueryDescription description = new QueryDescription();
return new ProjectionQueryFlow0(world, world.Query(in description));
```

需要确认 Arch 对空 `QueryDescription` 的语义是否是：

```text
遍历所有 Entity。
```

如果不是，Query0 会失效。

---

### 7.2 测试：Query0 能 Touch ProjectedActor

```csharp
[Fact]
public void Query0_TouchProjectedActor_Should_Visit_Entity()
{
    // 逻辑说明：
    // 验证空组件 Query 能命中刚创建的 Entity。

    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity()
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    runtime.EcsWorld
        .Query()
        .TouchProjectedActor();

    ref ProjectedActorMeta meta =
        ref runtime.EcsWorld.GetProjectionMeta(entity);

    Assert.True(meta.ActorId.IsValid);
}
```

---

### 7.3 测试：Query0 Where 能过滤

```csharp
[Fact]
public void Query0_Where_False_Should_Not_Create_ProjectedActor()
{
    // 逻辑说明：
    // 验证 Query0 的 Where 可以阻止 Actor 创建。

    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity()
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    runtime.EcsWorld
        .Query()
        .Where(static in Entity entity => false)
        .TouchProjectedActor();

    ref ProjectedActorMeta meta =
        ref runtime.EcsWorld.GetProjectionMeta(entity);

    Assert.False(meta.ActorId.IsValid);
}
```

---

## 8. 多事件投递测试

### 8.1 测试：一个 Entity 输出多个事件

```csharp
[Fact]
public void Projection_Should_Post_Multiple_Events_To_Same_Actor()
{
    // 逻辑说明：
    // 验证 Bring<E0,E1>() 会给同一个 Actor 投递两种事件。

    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity(new PositionComponent())
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    runtime.EcsWorld
        .Query<PositionComponent>()
        .Bring<MoveEvent, FootstepEvent>()
        .ForEach(static (
            in Entity entity,
            ref PositionComponent position,
            ref MoveEvent move,
            ref FootstepEvent footstep) =>
        {
            // entity 参数作用：
            // 当前 Query 命中的 Entity。

            // position 参数作用：
            // 当前 Entity 的位置组件。

            // move 参数作用：
            // 第一个输出事件。

            // footstep 参数作用：
            // 第二个输出事件。

            move = new MoveEvent();
            footstep = new FootstepEvent();
        })
        .Batch()
        .Post();

    runtime.Actors.Pump(...);

    Assert.True(TestProjectedActor.ReceivedMoveEvent);
    Assert.True(TestProjectedActor.ReceivedFootstepEvent);
}
```

说明：

```text
runtime.Actors.Pump(...) 的参数按当前 ActorWorld.Pump 真实签名填写。
如果测试 Actor 不适合用静态字段，可以用测试事件计数器服务。
```

---

### 8.2 测试：Where false 不投递任何事件

```csharp
[Fact]
public void Projection_Where_False_Should_Not_Post_Multiple_Events()
{
    // 逻辑说明：
    // 验证 Where 是唯一筛选点。
    // Where false 时 ForEach 不执行，也不会写入任何 Batch。

    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity(new PositionComponent())
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    runtime.EcsWorld
        .Query<PositionComponent>()
        .Where(static (
            in Entity entity,
            in PositionComponent position) => false)
        .Bring<MoveEvent, FootstepEvent>()
        .ForEach(static (
            in Entity entity,
            ref PositionComponent position,
            ref MoveEvent move,
            ref FootstepEvent footstep) =>
        {
            move = new MoveEvent();
            footstep = new FootstepEvent();
        })
        .Batch()
        .Post();

    ref ProjectedActorMeta meta =
        ref runtime.EcsWorld.GetProjectionMeta(entity);

    Assert.False(meta.ActorId.IsValid);
}
```

---

## 9. CreateEntity 链式创建测试

### 9.1 0 组件创建

```csharp
[Fact]
public void CreateEntity0_WithProjectedActor_Should_Mark_Meta()
{
    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity()
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    ref ProjectedActorMeta meta =
        ref runtime.EcsWorld.GetProjectionMeta(entity);

    Assert.True(meta.ActorTypeId >= 0);
    Assert.False(meta.ActorId.IsValid);
}
```

---

### 9.2 多组件创建

```csharp
[Fact]
public void CreateEntity2_WithProjectedActor_Should_Mark_Meta()
{
    LayerRuntime runtime = new();

    Entity entity = runtime.EcsWorld
        .CreateEntity(
            new PositionComponent(),
            new VelocityComponent())
        .WithProjectedActor<TestProjectedActor>()
        .Entity;

    ref ProjectedActorMeta meta =
        ref runtime.EcsWorld.GetProjectionMeta(entity);

    Assert.True(meta.ActorTypeId >= 0);
    Assert.False(meta.ActorId.IsValid);
}
```

重点：

```text
WithProjectedActor 只标记 ActorTypeId。
ActorId 仍然无效。
只有 Post / Touch 命中后才创建 Actor。
```

---

## 10. 命名空间导出方案

### 10.1 当前问题

`CreateEntity` 扩展位于：

```text
LayerBase.ECS.Projection.Create
```

Query Flow 扩展位于：

```text
LayerBase.ECS.Projection.Flow
```

ProjectedActor 扩展位于：

```text
LayerBase.ECS.Projection
```

用户可能需要多个 using：

```csharp
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Create;
using LayerBase.ECS.Projection.Flow;
```

如果漏掉 `Create` 命名空间，`world.CreateEntity(...)` 不会出现。

---

### 10.2 推荐方案 A：保持分层命名空间

保持当前设计，但补文档：

```csharp
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Create;
using LayerBase.ECS.Projection.Flow;
```

优点：

```text
命名空间清晰。
Create / Flow / Core Projection 可拆开。
```

缺点：

```text
使用方需要记住多个 using。
```

---

### 10.3 推荐方案 B：统一 Facade 命名空间

把生成扩展类的 namespace 改成：

```csharp
namespace LayerBase.ECS.Projection;
```

包括：

```text
EntityCreateWorldExtensions
ProjectionWorldExtensions
```

优点：

```text
用户只需 using LayerBase.ECS.Projection。
API 发现体验最好。
```

缺点：

```text
命名空间更宽。
```

建议：

```text
如果 LayerBase.ECS.Projection 是内部高阶 API，推荐方案 B。
如果你希望 Flow/Create 保持模块边界，推荐方案 A。
```

---

## 11. 热路径约束审查

### 11.1 必须保持

Projection 行循环内禁止：

```text
Dictionary 查找。
反射调用。
Activator.CreateInstance。
MethodInfo.Invoke。
Type.GetType。
Assembly 扫描。
LINQ。
foreach over non-array collection。
装箱。
object[] 事件容器。
```

允许：

```text
chunk.GetFirst<T>()。
Unsafe.Add。
数组访问。
泛型静态调用。
ActorWorld.PostTo<TEvent>()。
ProjectedActorBinding.TouchProjectedActor。
ProjectedActorBinding.EnsureProjectedActor 只在 Actor 缺失时进入。
```

---

### 11.2 简单静态审查脚本

文件：

```text
scripts/check-projection-hotpath.ps1
```

代码：

```powershell
param(
    [string]$Root = (Resolve-Path "$PSScriptRoot/..").Path
)

# Root 参数作用：
# 仓库根目录。

$ErrorActionPreference = "Stop"

$files = @(
    "LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs",
    "LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs",
    "LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs"
)

$forbidden = @(
    "Dictionary<",
    "Activator.CreateInstance",
    "MethodInfo",
    ".Invoke(",
    "Assembly.",
    "GetType(",
    "object[]",
    "dynamic"
)

foreach ($file in $files) {
    $path = Join-Path $Root $file
    $text = Get-Content $path -Raw

    foreach ($word in $forbidden) {
        if ($text.Contains($word)) {
            throw "Forbidden hot-path token '$word' found in $file"
        }
    }
}

Write-Host "Projection hot-path check passed."
```

---

## 12. CI 集成建议

在主 CI 中增加：

```yaml
- name: Generate Projection Flow
  run: bash scripts/generate-projection-flow.sh

- name: Check generated projection files
  run: |
    git diff --exit-code -- \
      LayerBase/ECS/Projection/Flow/ProjectionDelegates.g.cs \
      LayerBase/ECS/Projection/Flow/ProjectionQueryFlow.g.cs \
      LayerBase/ECS/Projection/Flow/ProjectionExecutor.g.cs \
      LayerBase/ECS/Projection/Flow/ProjectionWorldExtensions.g.cs \
      LayerBase/ECS/Projection/Create/EntityCreateFlow.g.cs \
      LayerBase/ECS/Projection/Create/EntityCreateWorldExtensions.g.cs

- name: Check projection hot path
  shell: pwsh
  run: ./scripts/check-projection-hotpath.ps1
```

---

## 13. 最终验收清单

### 13.1 编译验收

```text
项目能通过 dotnet build。
生成器项目能参与编译。
GeneratedProjectedActorTypes partial 方法有实际生成实现。
```

### 13.2 API 验收

```text
world.CreateEntity().WithProjectedActor<TActor>() 可编译。
world.CreateEntity(c0, c1).WithProjectedActor<TActor>() 可编译。
world.Query().TouchProjectedActor() 可编译。
world.Query<T0>().Bring<E0, E1>().ForEach(...).Batch().Post() 可编译。
world.Query<T0,T1,T2,T3>().Bring<E0,E1,E2,E3>().ForEach(...).Batch().Post() 可编译。
```

### 13.3 行为验收

```text
WithProjectedActor 只标记 ActorTypeId，不立即创建 Actor。
TouchProjectedActor 可以延迟创建 Actor。
Post 可以延迟创建 Actor。
多事件 ForEach 会把所有事件投递到同一个 ProjectedActor。
Where false 时不执行 ForEach。
Where false 时不创建 Actor。
```

### 13.4 性能验收

```text
Projection 行循环没有 Dictionary。
Projection 行循环没有反射。
Projection 行循环没有 Activator.CreateInstance。
Projection 行循环没有 MethodInfo.Invoke。
多事件投递不使用 object[]。
多事件投递不装箱。
```

---

## 14. 下一步执行顺序

第一步：

```text
补 scripts/generate-projection-flow.ps1。
补 scripts/generate-projection-flow.sh。
```

第二步：

```text
补 CI 生成一致性检查。
```

第三步：

```text
确认 Actor 类型源生成器实际生成 GeneratedProjectedActorTypes partial 实现。
```

第四步：

```text
补 Query0 行为测试。
补 CreateEntity 链式测试。
补多事件投递测试。
```

第五步：

```text
决定命名空间方案：
A. 保持 Projection / Projection.Create / Projection.Flow 分离。
B. 统一到 LayerBase.ECS.Projection。
```

第六步：

```text
补热路径静态审查脚本。
```

---

## 15. 结论

当前 Projection 主体功能已经基本完成。

剩余工作不是大规模架构重写，而是：

```text
生成链路可复现。
源生成器产物可确认。
Query0 语义可验证。
多事件行为可测试。
命名空间体验可整理。
热路径约束可自动检查。
```

完成这些后，当前 Projection 系统才算从“功能写出来”进入“工程可长期维护”的状态。
