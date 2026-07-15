# 11 `[Input]` 增量生成与 QueryBring 的 Scope ECS 路由迁移

> **最高原则：** 以当前 `master` 的 QueryBring 生成逻辑、公开 API 和现有测试为基线；只增加 `[Input]` 参数能力，并把 ECSWorld 路由迁移到对象的 OwnerScope。  
> **代码基线：** 当前 `master`。  
> **依赖阶段：** `02_scope_runtime_resources.md`、`08_di_scope_container.md`、`10_public_api_scope_routing.md`。  
> **禁止扩大范围：** 不引入 QuerySlot、QueryPlan、Query Handle、新 Scheduler或新的返回值。

---

## 0. 本阶段目标

本阶段完成两个独立但兼容的增量：

```text
增量一：
    QueryBringGenerator支持 [Input] 参数。

增量二：
    Service / Context / Layer 的 Query
    使用对象 OwnerScope 的 EcsWorld。
```

必须保持：

```text
普通 Query用户方法返回 void。

Bring Query用户方法返回 ProjectResult。

Generator生成的公开入口始终返回 void。

普通 Query继续：
    Query().ForEach(ref job)

Bring继续：
    Query().Bring().ForEach(ref job).Batch().Post()

无 Input Query的生成代码保持 master兼容。

现有 Query/Bring/Projection测试不修改。
```

---

## 1. `[Input]` 的语义

`[Input]` 表示：

```text
本次 Query调用由调用方提供，
并由所有匹配 Entity共享的数据。
```

它不是：

```text
ECS Component
Entity
Bring Event
DI Service
Provide / From资源
Query返回值
跨 Scope消息
```

示例：

```csharp
[Query]
private void OnMove(
    ref Position position,
    in Velocity velocity,
    [Input] in FrameInput frame,
    [Input] MovementConfig config)
{
    position.Value +=
        velocity.Value
        * frame.DeltaTime
        * config.Speed;
}
```

调用：

```csharp
Move(
    in frame,
    config);
```

适合用作 Input 的数据：

```text
DeltaTime
本次 Query的过滤参数
本次 Query的数值配置
只读上下文
调用方明确传入的 Handle
```

---

## 2. 新增 `InputAttribute`

```csharp
namespace LayerBase.ECS;

[AttributeUsage(
    AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class InputAttribute :
    Attribute
{
}
```

Generator增加：

```csharp
private const string InputAttributeName =
    "LayerBase.ECS.InputAttribute";
```

只有显式标记 `[Input]` 的参数才是 Input。

禁止：

```text
根据参数是值类型自动推断 Input。

根据参数不是 IComponent自动推断 Input。

根据参数位置自动推断 Input。
```

---

## 3. 生成入口为什么仍返回 `void`

### 3.1 Query入口是执行入口

生成入口表示：

```text
使用本次 Input执行一次 Query遍历。
```

Query可能匹配：

```text
零个 Entity
一个 Entity
多个 Entity
```

没有天然唯一的返回值。

返回以下任意类型都会新增 master 中不存在的语义：

```text
第一个结果
最后一个结果
数组
聚合值
Query Handle
EcsQuerySubmitResult
LBTask
```

### 3.2 Input不是 Result

Input只是：

```text
Job在遍历期间读取的调用参数。
```

它不描述：

```text
Query是否完成
处理了多少 Entity
执行结果是什么
```

所以加入 Input不应改变入口返回类型。

### 3.3 `ProjectResult` 属于单个 Entity

Bring Query中的 `ProjectResult` 是每次 `Job.Execute` 的控制值：

```text
Fail
Touch
Success
```

它由 Projection Flow消费，不是整个生成入口的返回值。

### 3.4 Bring仍以 `Batch().Post()`结束

Bring的表现通过：

```text
ECS组件修改
Projection Batch
Actor Event
```

体现。

生成入口只触发该流程，因此继续返回 `void`。

---

## 4. 普通 Query 的生成代码

用户代码：

```csharp
public sealed partial class MovementService :
    IService
{
    [Query]
    private void OnMove(
        ref Position position,
        in Velocity velocity,
        [Input] in FrameInput frame,
        [Input] MovementConfig config)
    {
        position.Value +=
            velocity.Value
            * frame.DeltaTime
            * config.Speed;
    }
}
```

生成入口：

```csharp
public void Move(
    in FrameInput frame,
    MovementConfig config)
{
    var job =
        new __MoveJob(
            this,
            in frame,
            config);

    global::LayerBase
        .ServiceECSExtensions
        .Query<
            Position,
            Velocity>(this)
        .ForEach(ref job);
}
```

生成 Job：

```csharp
private readonly struct __MoveJob :
    IQueryJob<
        Position,
        Velocity>
{
    private readonly MovementService _self;

    private readonly FrameInput _input0;
    private readonly MovementConfig _input1;

    public __MoveJob(
        MovementService self,
        in FrameInput input0,
        MovementConfig input1)
    {
        _self = self;
        _input0 = input0;
        _input1 = input1;
    }

    public void Execute(
        Entity entity,
        ref Position c0,
        in Velocity c1)
    {
        _self.OnMove(
            ref c0,
            in c1,
            in _input0,
            _input1);
    }
}
```

关键规则：

```text
Input出现在生成入口参数中。

Input被复制进原 Job字段。

Input不进入 IQueryJob泛型参数。

Input不进入 Execute参数。

Component Query形状不变。
```

---

## 5. Bring Query 的生成代码

用户代码：

```csharp
[Query]
[Bring<MoveViewEvent>]
private ProjectResult OnUpdateEnemyView(
    ref PositionComponent position,
    in VelocityComponent velocity,
    [Input] in FrameInput frame,
    in AoiComponent aoi,
    ref MoveViewEvent moveEvent)
{
    if (!aoi.IsVisible)
    {
        return ProjectResult.Fail;
    }

    position.X +=
        velocity.X
        * frame.DeltaTime;

    position.Y +=
        velocity.Y
        * frame.DeltaTime;

    moveEvent =
        new MoveViewEvent(
            position.X,
            position.Y);

    return ProjectResult.Success;
}
```

生成入口：

```csharp
public void UpdateEnemyView(
    in FrameInput frame)
{
    var job =
        new __UpdateEnemyViewJob(
            this,
            in frame);

    global::LayerBase
        .ServiceECSExtensions
        .Query<
            PositionComponent,
            VelocityComponent,
            AoiComponent>(this)
        .Bring<MoveViewEvent>()
        .ForEach(ref job)
        .Batch()
        .Post();
}
```

生成 Job 的 `Execute` 仍服从 master 的 Projection接口：

```csharp
public ProjectResult Execute(
    Entity entity,
    ref PositionComponent c0,
    ref VelocityComponent c1,
    ref AoiComponent c2,
    ref MoveViewEvent e0)
{
    return _self.OnUpdateEnemyView(
        ref c0,
        in c1,
        in _input0,
        in c2,
        ref e0);
}
```

Input不改变：

```text
Bring Event尾部规则
ProjectResult语义
Batch().Post()
Projection Job接口
```

---

## 6. 参数分类优先级

master 当前识别：

```text
Entity
Bring Event
ref / in Component
其他参数不支持
```

加入 Input后，顺序必须调整为：

```text
1. 显式 [Input]
2. Entity
3. Bring Event
4. 未标注的 ref / in Component
5. 其他参数报错
```

原因：

```csharp
[Input] in FrameInput frame
```

若不优先识别，会被旧逻辑误判为 Component。

伪代码：

```csharp
foreach (IParameterSymbol param
         in methodSymbol.Parameters)
{
    if (HasInputAttribute(param))
    {
        ParseInput(param);
        continue;
    }

    if (IsEntity(param))
    {
        ParseEntity(param);
        continue;
    }

    if (IsNextBringEvent(param))
    {
        ParseBringEvent(param);
        continue;
    }

    if (param.RefKind
        is RefKind.Ref
        or RefKind.In)
    {
        ParseComponent(param);
        continue;
    }

    ReportUnsupportedParameter(param);
}
```

---

## 7. Input 支持的参数形式

第一阶段支持：

```csharp
[Input] T value

[Input] in T value
```

第一阶段禁止：

```csharp
[Input] ref T value

[Input] out T value
```

### 为什么禁止 `ref/out`

现有 Job是普通：

```csharp
readonly struct
```

它不能安全保存调用方局部变量的可写 by-ref引用。

支持 `ref/out Input` 将迫使框架引入：

```text
ref struct Job
ref字段
Pin
Lease
调用方栈生命周期
延迟完成协议
```

这会把增量功能扩大为 Query架构重写。

需要输出时继续使用：

```text
修改 ECS Component
Bring Event
显式引用类型容器
后续独立 Result功能
```

---

## 8. Input 类型限制

允许：

```text
primitive
enum
普通 struct
不可变 class
接口引用
只读配置对象
调用方管理生命周期的 Handle
```

Generator必须拒绝：

```text
ref struct
Span<T>
ReadOnlySpan<T>
其他 byref-like类型
指针
函数指针
```

因为这些类型不能保存为普通 Job字段。

### 值类型

入口创建 Job时复制：

```text
一次 Query调用使用同一份值快照。
```

### 引用类型

只复制引用，不深复制。

调用方负责保证：

```text
Query执行期间引用有效。

不会发生不受控的跨线程并发写入。
```

---

## 9. Input 生命周期

当前 master 的 Query入口直接执行：

```text
Query().ForEach(ref job)
```

Bring直接执行：

```text
Query().Bring().ForEach(ref job).Batch().Post()
```

Input只在：

```text
ForEach调用 Job.Execute期间
```

被读取。

进入 `Batch().Post()` 的是现有：

```text
Actor目标
Bring Event
Projection数据
```

Input本身不继续保存在 Batch中。

所以第一阶段不需要：

```text
InputPack
Input Pool
Input Lease
Query Handle
异步释放协议
```

如果后续 Query改为真正的延迟 Scheduler：

```text
必须在对应任务中重新定义 Input生命周期。

不得静默沿用本阶段的同步字段捕获模型。
```

---

## 10. 参数顺序必须保留

扩展：

```csharp
private enum QueryUserParameterKind
{
    Entity,
    Component,
    Input,
    BringEvent
}
```

Input的元数据：

```text
Kind = Input
Index = Input数组中的下标
RefKind = None或In
ParameterName = 用户参数名
```

`BuildUserMethodArgumentList` 增加：

```csharp
case QueryUserParameterKind.Input:
{
    string prefix =
        userParameter.RefKind
            == RefKind.In
            ? "in "
            : string.Empty;

    args.Add(
        $"{prefix}_input{userParameter.Index}");

    break;
}
```

Input可以出现在 Bring尾部开始之前的任意位置。

合法：

```csharp
private ProjectResult OnProject(
    Entity entity,
    ref Position position,
    [Input] in FrameInput frame,
    in ViewState view,
    ref MoveEvent moveEvent)
```

非法：

```csharp
private ProjectResult OnProject(
    ref Position position,
    ref MoveEvent moveEvent,
    [Input] in FrameInput frame)
```

因为 Bring Event开始后，后续仍只能是 Bring Event。

---

## 11. `QueryMethodInfo` 增量

在 master现有字段上增加：

```csharp
public ImmutableArray<ITypeSymbol>
    InputTypes {
    get;
    set;
}

public ImmutableArray<RefKind>
    InputRefKinds {
    get;
    set;
}

public ImmutableArray<string>
    InputNames {
    get;
    set;
}
```

公开入口使用原参数名：

```csharp
public void Move(
    in FrameInput frame,
    MovementConfig config)
```

Job内部使用稳定字段名：

```text
_input0
_input1
```

---

## 12. Generator 的最小修改点

### `ExtractQueryMethodInfo`

增加：

```text
识别 InputAttribute。

Input优先于 Component。

记录类型、RefKind和名称。

拒绝 ref/out/byref-like。

保持 Bring尾部限制。
```

不得修改：

```text
普通 Query返回 void校验。

Bring Query返回 ProjectResult校验。

Entity最多一个。

Bring类型与顺序。

至少一个 Component。

EntryPoint命名。
```

### `GenerateMethodSource`

无 Input：

```csharp
public void Move()
```

有 Input：

```csharp
public void Move(
    in FrameInput frame,
    MovementConfig config)
```

返回类型始终为 `void`。

### `GenerateQueryInvocation`

只扩展 Job构造：

```csharp
var job =
    new __MoveJob(
        this,
        in frame,
        config);
```

Query链不变。

### `GenerateBringInvocation`

只扩展 Job构造。

以下链不变：

```csharp
.Query<T...>(this)
.Bring<TEvent...>()
.ForEach(ref job)
.Batch()
.Post();
```

### `GenerateJobStruct`

增加：

```text
readonly Input字段
构造函数参数
字段赋值
```

不修改：

```text
Job接口
Execute参数
普通 Query返回 void
Bring返回 ProjectResult
```

### `BuildUserMethodArgumentList`

增加 Input case，并保持用户原参数顺序。

---

## 13. 不修改 Query Job 接口

Input是 Job捕获状态，不是 ECS提供的数据。

禁止生成：

```csharp
IQueryJob<
    Position,
    Velocity,
    FrameInput>
```

正确：

```text
IQueryJob泛型参数只包含 Component。

Input只存在于 Job字段中。
```

同样禁止修改：

```text
IProjectionJobCxE
Execute签名
ProjectionQueryFlow泛型
Bring泛型参数
```

---

## 14. Scope ECSWorld 路由迁移

Input功能与 Scope路由相互独立。

### master路径

```csharp
public static World ECSWorld(
    this IService service)
{
    return ServiceLayerBinder
        .RequireBinding(service)
        .Runtime
        .EcsWorld;
}
```

### 迁移路径

示意：

```csharp
public static World ECSWorld(
    this IService service)
{
    ScopeObjectBinding binding =
        ScopeObjectBinder
            .RequireBinding(service);

    binding.LocalAccess
        .RequireOwnerThread();

    return binding.LocalAccess
        .EcsWorld;
}
```

只改变：

```text
World来自哪里。
```

不改变：

```text
Generator生成什么 Query链。
```

---

## 15. Service、Context 与 Layer 路由

```text
Service.Query / ECSWorld：
    Service OwnerScope EcsWorld

Context.Query / ECSWorld：
    OwnerService OwnerScope EcsWorld

Push Layer实例 Query / ECSWorld：
    MainScope EcsWorld
```

具体 Layer实例位于 MainScope，不表示 Layer只管理 MainScope对象。

---

## 16. 保留 `LayerRuntime.EcsWorld`

master现有测试直接使用：

```csharp
runtime.EcsWorld
    .CreateEntity();
```

第一阶段必须保留，其新语义是：

```text
LayerRuntime.EcsWorld
    → MainScope EcsWorld兼容门面
```

不得为了 Scope迁移修改原测试。

CustomScope Service仍通过自身 Binding访问自己的 World。

---

## 17. 与 Bring / Projection 的边界

生成代码继续：

```text
Bring<TEvent>()
→ ForEach(ref job)
→ Batch()
→ Post()
```

CustomScope中的 `Post()` 如何进入 22号 Projection管线，由 22号负责。

11号不得改为：

```text
ScopeCall
Query Result
新 Projection API
```

---

## 18. 原测试不得修改

硬门禁：

```text
QueryGeneratorTest.cs 原内容不修改。

原测试名称不修改。

原用户 Query方法不修改。

原调用方式不修改。

原断言不修改。

原 Pump次数不修改。
```

至少原样通过：

```text
QueryWithBringInService
```

以及 master全部 Query、Bring和相关 Projection测试。

---

## 19. 新增测试

新增独立测试文件：

```text
QueryInputGeneratorTests.cs
QueryInputRuntimeTests.cs
ScopeQueryRoutingTests.cs
```

### 生成测试

```text
Input_attribute_has_parameter_target

Input_is_recognized_before_component

Generated_entry_remains_void_with_input

Generated_entry_contains_input_parameters

Generated_job_contains_readonly_input_fields

Generated_job_constructor_receives_inputs

Generated_execute_does_not_add_input_parameters

Generated_user_call_preserves_parameter_order

Input_in_is_forwarded_with_in

Input_value_is_forwarded_by_value

No_input_output_remains_master_compatible

Bring_chain_remains_batch_post
```

### 运行测试

```text
Input_value_is_shared_for_all_entities

Input_in_value_is_shared_for_all_entities

Different_invocations_use_different_input

Input_is_not_treated_as_component

Bring_query_can_use_input

Input_changes_component_result

Worker_scope_query_uses_owner_scope_world_with_input
```

### 非法参数测试

```text
Input_ref_is_rejected

Input_out_is_rejected

Input_span_is_rejected

Input_readonly_span_is_rejected

Input_ref_struct_is_rejected

Input_after_bring_tail_is_rejected

Input_on_entity_is_rejected

Input_on_bring_event_is_rejected
```

---

## 20. Input 诊断

新增：

```text
LBQUERY_INPUT_REF_NOT_SUPPORTED

LBQUERY_INPUT_OUT_NOT_SUPPORTED

LBQUERY_INPUT_BYREFLIKE_NOT_SUPPORTED

LBQUERY_INPUT_AFTER_BRING_NOT_SUPPORTED

LBQUERY_INPUT_ENTITY_NOT_SUPPORTED

LBQUERY_INPUT_BRING_EVENT_NOT_SUPPORTED
```

本任务只要求 Input相关错误有明确诊断。

不得借此大改 master其他静默忽略规则。

---

## 21. 禁止引入的系统

```text
EcsQuerySubmitResult

QueryHandle

QuerySlot

ScopeEcsQueryPlan

Query Registry

Query Contribution

InputPack对象池

Input Lease

新 Query Scheduler

异步 Query完成协议

运行期 Type → Query查找
```

当前 master直接创建 Job并执行 Query Flow。

Input只需成为 Job字段。

---

## 22. master 复用范围

### 原样保留

```text
QueryBringGenerator整体结构
QueryAttribute
BringAttribute
EntryPointAttribute
ProjectResult
Query Job接口
Projection Job接口
ProjectionQueryFlow
Batch / Post
ServiceECSExtensions.Query重载
QueryGeneratorTest
```

### 增量修改

```text
新增 InputAttribute。

QueryMethodInfo增加 Input元数据。

QueryUserParameterKind增加 Input。

生成入口增加 Input形参。

Job增加 Input字段和构造参数。

用户方法调用增加 Input实参。

ECSWorld路由改为 OwnerScope。
```

### 禁止重写

```text
Component分类
Bring分类
Job接口
Query Flow
Projection Flow
Batch / Post
原测试语义
```

---

## 23. 修改代码位置

```text
LayerBase/ECS/
    InputAttribute.cs

LayerBase.Generator/
    QueryBringGenerator.cs

LayerBase/ECS/Extensions/
    ServiceECSExtensions.cs
    LayerECSExtensions.cs
    ContextECSExtensions.cs

LayerBase/DI/
    ServiceLayerBinder.cs
    ScopeObjectBinding.cs

LayerBase/Scope/
    ScopeRuntime.cs
    ScopeLocalAccess.cs

LayerBase/Application/
    LayerRuntime.cs

LayerBase.Test/
    QueryInputGeneratorTests.cs
    QueryInputRuntimeTests.cs
    ScopeQueryRoutingTests.cs
```

注意：

```text
QueryGeneratorTest.cs：
    不修改。

QueryBringGenerator：
    只做 Input参数类别的最小增量。
```

---

## 24. Agent 执行任务

```text
1. 记录 master Generator和测试基线。
2. 新增 InputAttribute。
3. Generator增加 Input Metadata Name。
4. 参数分类时优先识别 [Input]。
5. 支持 [Input] T和 [Input] in T。
6. 拒绝 ref/out和 byref-like Input。
7. QueryMethodInfo记录 Input类型、RefKind和名称。
8. QueryUserParameterKind增加 Input。
9. 生成 public void入口并加入 Input参数。
10. Job增加 readonly Input字段。
11. Job构造函数接收并复制 Input。
12. Execute签名保持 master原样。
13. 用户方法调用按原参数顺序插入 Input。
14. 普通 Query链不变。
15. Bring链不变。
16. 无 Input输出保持 master兼容。
17. ECSWorld改为 OwnerScope LocalAccess。
18. 保留 runtime.EcsWorld MainScope兼容门面。
19. 不修改原 QueryGeneratorTest。
20. 新增 Input和 Scope路由测试。
```

---

## 25. 验收否决项

出现任意一项，任务不通过：

```text
生成入口返回非 void类型

Input进入 IQueryJob泛型参数

Input进入 Execute参数

未标注参数自动推断为 Input

Input ref/out被隐式支持

Input被当作 ECS Component

引入 QuerySlot / QueryPlan / Query Registry

修改 Query/Bring/Batch/Post链路

修改原 QueryGeneratorTest预期

删除 runtime.EcsWorld并改写原测试

Service Query仍使用 Runtime全局 World

WorkerScope Query回退 MainScope World

无 Input生成代码发生无必要变化
```

---

## 26. 最终结果

```text
master原 Query/Bring设计保留。

生成入口继续返回 void。

[Input] 成为入口参数，
并由原 Job struct按值捕获。

Component、Entity、Input、Bring
在 Generator中有明确参数类别。

Scope迁移只改变 EcsWorld来源。

原测试原样通过，
新增测试覆盖 Input和 Scope隔离。
```
