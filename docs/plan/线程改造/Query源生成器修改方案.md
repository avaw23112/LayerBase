# Query 源生成器修改方案：Static Query + 显式输入 + 无 _self Job

## 0. 修改目标

新的 `[Query]` 规则：

```text id="l8v7tt"
[Query] 方法必须是 static。
[Query] 方法不能依赖实例字段、实例属性、实例方法。
Query 需要的外部数据必须作为入口参数显式传入。
生成的 Job 不再持有 _self。
生成的 Job 只持有输入参数副本。
EcsWorker 执行 Job 时只接触 ECS 组件和输入值。
```

最终目标：

```text id="i9o0vk"
Query 方法 = 纯 ECS Job 函数。
生成入口方法 = 主线程提交 ECS 任务。
生成 Job = 输入数据 + ECS 执行逻辑。
```

---

# 1. 用户侧最终写法

## 1.1 普通 Query

用户写：

```csharp id="cqlx2z"
public sealed partial class MovementService : IService
{
    public void Update(float deltaTime)
    {
        Move(deltaTime);
    }

    [Query]
    private static void OnMove(
        float deltaTime,
        ref Position position,
        in Velocity velocity)
    {
        position.Value += velocity.Value * deltaTime;
    }
}
```

生成器生成：

```csharp id="zgreov"
public void Move(float deltaTime)
{
    var job = new __MoveJob(deltaTime);

    global::LayerBase.ServiceECSExtensions
        .Query<Position, Velocity>(this)
        .ForEach(ref job);
}

private readonly struct __MoveJob : IQueryJob<Position, Velocity>
{
    private readonly float _deltaTime;

    public __MoveJob(float deltaTime)
    {
        _deltaTime = deltaTime;
    }

    public void Execute(
        Entity entity,
        ref Position c0,
        in Velocity c1)
    {
        OnMove(_deltaTime, ref c0, in c1);
    }
}
```

注意：生成入口方法仍然是实例方法，因为它需要 `this.Query<...>()` 找到当前 Service 绑定的 Runtime/ECS。
但是生成的 Job 不再持有 `this`。

---

## 1.2 Bring Query

用户写：

```csharp id="1dszpl"
public sealed partial class MovementService : IService
{
    public void Update(float deltaTime)
    {
        MoveView(deltaTime);
    }

    [Query]
    [Bring<MoveViewEvent>]
    private static ProjectResult OnMoveView(
        float deltaTime,
        ref Position position,
        in Velocity velocity,
        ref MoveViewEvent moveView)
    {
        position.Value += velocity.Value * deltaTime;

        moveView = new MoveViewEvent(position.Value);

        return ProjectResult.Success;
    }
}
```

生成器生成：

```csharp id="z1fvqp"
public void MoveView(float deltaTime)
{
    var job = new __MoveViewJob(deltaTime);

    global::LayerBase.ServiceECSExtensions
        .Query<Position, Velocity>(this)
        .Bring<MoveViewEvent>()
        .ForEach(ref job)
        .Batch()
        .Post();
}

private readonly struct __MoveViewJob :
    IProjectionJob2x1<Position, Velocity, MoveViewEvent>
{
    private readonly float _deltaTime;

    public __MoveViewJob(float deltaTime)
    {
        _deltaTime = deltaTime;
    }

    public ProjectResult Execute(
        Entity entity,
        ref Position c0,
        ref Velocity c1,
        ref MoveViewEvent e0)
    {
        return OnMoveView(_deltaTime, ref c0, in c1, ref e0);
    }
}
```

Bring 语义保持不变：

```text id="ltogqk"
Fail    -> 不 Touch，不 Post
Touch   -> Touch，不 Post
Success -> Touch，并 Post Bring Event
```

---

# 2. 参数分类规则

当前 `QueryBringGenerator` 的参数分类主要是：

```text id="sluc8z"
Entity
组件参数 ref/in
Bring 事件参数 ref
```

现在要增加第四类：

```text id="q7mbln"
Input 参数
```

新的参数分类如下。

---

## 2.1 Input 参数

Input 参数是 Query 执行需要的外部输入。

示例：

```csharp id="k0zaag"
float deltaTime
int frameIndex
in MoveInput input
```

规则：

```text id="p5yzy5"
Input 参数必须出现在组件参数之前。
Input 参数必须是值类型，或显式允许的 readonly immutable 类型。
Input 参数会被复制进生成 Job。
Input 参数不会进入 Query<T...> 泛型列表。
```

推荐：

```csharp id="x0nkr6"
[Query]
private static void OnMove(
    in MoveInput input,
    ref Position position,
    in Velocity velocity)
{
}
```

不推荐但可允许：

```csharp id="rnbhck"
[Query]
private static void OnMove(
    float deltaTime,
    float speedScale,
    ref Position position,
    in Velocity velocity)
{
}
```

---

## 2.2 Entity 参数

Entity 参数保持现有语义：

```csharp id="fr2sv2"
Entity entity
```

规则：

```text id="g1an9e"
Entity 最多出现一次。
Entity 不进入 Query<T...> 泛型列表。
Entity 由 Execute 参数提供。
```

推荐位置：

```csharp id="hbg4w3"
[Query]
private static void OnMove(
    float deltaTime,
    Entity entity,
    ref Position position,
    in Velocity velocity)
{
}
```

---

## 2.3 Component 参数

组件参数继续使用：

```csharp id="tdo88s"
ref Position position
in Velocity velocity
```

规则：

```text id="mejte5"
ref = 可写组件。
in = 只读组件。
组件参数进入 Query<T...> 泛型列表。
组件参数生成 Execute 中的 c0、c1、c2...
```

为了和 Input 参数区分，建议生成器判断组件类型时不要只看 `ref/in`，而要看类型是否是 ECS Component。

例如：

```csharp id="o5bwgv"
private static bool IsComponentType(ITypeSymbol type)
{
    return ImplementsInterface(type, "LayerBase.ECS.IComponent");
}
```

否则 `in MoveInput input` 会被误判成 ECS 组件。

---

## 2.4 Bring Event 参数

Bring Event 参数保持当前规则：

```csharp id="8b89fh"
ref MoveViewEvent moveView
```

规则：

```text id="n7n5yw"
只有带 [Bring] 的 Query 才能出现 Bring Event 参数。
Bring Event 参数必须位于方法末尾。
Bring Event 参数必须按 BringAttribute 声明顺序出现。
Bring Event 参数必须是 ref。
Bring Query 必须返回 ProjectResult。
```

当前生成器已经有类似规则：Bring 分支必须返回 `ProjectResult`，普通 Query 必须返回 `void`。
当前也要求 Bring 参数按声明顺序出现在方法末尾，并且必须是 `ref`。

---

# 3. 推荐参数顺序

为了减少歧义，推荐强制顺序：

```text id="ookn2p"
Input 参数
Entity 参数
Component 参数
Bring Event 参数
```

例如：

```csharp id="m77tft"
[Query]
[Bring<HitEvent>]
private static ProjectResult OnHitCheck(
    in HitQueryInput input,
    Entity entity,
    ref Position position,
    in Collider collider,
    ref HitEvent hit)
{
}
```

生成器可以允许 Entity 和 Component 交换顺序，但不建议允许 Input 穿插在组件中间。

不允许：

```csharp id="wvxxmi"
[Query]
private static void OnMove(
    ref Position position,
    float deltaTime,
    in Velocity velocity)
{
}
```

原因：

```text id="f7xsaq"
输入参数穿插在组件中间，会让生成器和用户都难以判断哪些进入 Query 泛型，哪些进入 Job 字段。
```

---

# 4. QueryMethodInfo 结构修改

当前 `QueryMethodInfo` 需要扩展。

新增：

```csharp id="er9o2d"
internal sealed class QueryMethodInfo
{
    public IMethodSymbol MethodSymbol { get; init; }
    public ClassDeclarationSyntax ClassDeclaration { get; init; }
    public string EntryPointName { get; init; }

    public ImmutableArray<QueryInputParameterInfo> InputParameters { get; init; }

    public ImmutableArray<ITypeSymbol> ComponentTypes { get; init; }
    public ImmutableArray<RefKind> ComponentRefKinds { get; init; }

    public ImmutableArray<ITypeSymbol> BringEventTypes { get; init; }

    public ImmutableArray<QueryUserParameterInfo> UserParameters { get; init; }

    public bool HasEntity { get; init; }
    public bool ReturnsProjectResult { get; init; }
}
```

新增：

```csharp id="6z56e3"
internal sealed class QueryInputParameterInfo
{
    public string Name { get; init; }
    public ITypeSymbol Type { get; init; }
    public RefKind RefKind { get; init; }
    public int Index { get; init; }
}
```

扩展 `QueryUserParameterKind`：

```csharp id="vja1ut"
internal enum QueryUserParameterKind
{
    Input,
    Entity,
    Component,
    BringEvent
}
```

`QueryUserParameterInfo` 对 Input 的含义：

```csharp id="ztbrn6"
Kind = Input
Index = input parameter index
RefKind = original input ref kind
```

---

# 5. ExtractQueryMethodInfo 修改

当前生成器在 `ExtractQueryMethodInfo` 里会处理 `[Query]` 方法、检查 class partial、禁止泛型方法、识别 BringAttribute。

这里要新增/修改以下规则。

---

## 5.1 强制 static

新增：

```csharp id="xuws3t"
if (!methodSymbol.IsStatic)
{
    // 这里不要 return null 静默失败，应该产生 Diagnostic。
    return QueryMethodInfo.Invalid(... QueryMethodMustBeStatic ...);
}
```

如果你暂时不想重构诊断流，也可以先 return null，但我不建议。

原因：

```text id="669mhh"
用户写错时如果静默不生成入口方法，会非常难排查。
```

应该明确报错：

```text id="etovr3"
LBQ001: [Query] method must be static.
```

---

## 5.2 继续禁止泛型方法

保留：

```csharp id="cp2hy5"
if (methodSymbol.IsGenericMethod)
{
    report LBQ002;
}
```

当前生成器已经禁止泛型方法，只是目前是 return null。

---

## 5.3 解析 BringAttribute

保留现有逻辑。

Bring 的判断仍然是：

```csharp id="q0s4oj"
bool hasBring = bringEventTypes.Length > 0;
```

然后：

```csharp id="eftlgq"
if (hasBring && !returnsProjectResult) error;
if (!hasBring && !returnsVoid) error;
```

---

## 5.4 新参数分类流程

建议按这个顺序解析：

```csharp id="mw98zh"
bool componentStarted = false;
bool bringTailStarted = false;

foreach (var param in parameters)
{
    if (IsEntity(param))
    {
        // Entity
        componentStarted = true; // Entity 后面不再允许 Input
        AddEntity();
        continue;
    }

    if (IsNextBringEvent(param))
    {
        // Bring event tail
        bringTailStarted = true;
        AddBringEvent();
        continue;
    }

    if (bringTailStarted)
    {
        error: component/input cannot appear after Bring event tail;
    }

    if (IsComponentParameter(param))
    {
        componentStarted = true;
        AddComponent();
        continue;
    }

    if (IsInputParameter(param))
    {
        if (componentStarted)
        {
            error: input parameter must appear before Entity/component parameters;
        }

        AddInput();
        continue;
    }

    error: unsupported query parameter;
}
```

---

# 6. IsComponentParameter 设计

当前逻辑是：

```text id="l62fbh"
ref 或 in 就当组件
```

这会和 `in MoveInput input` 冲突。

新逻辑应该是：

```csharp id="yaew2k"
private static bool IsComponentParameter(IParameterSymbol param)
{
    if (param.RefKind != RefKind.Ref && param.RefKind != RefKind.In)
    {
        return false;
    }

    return IsComponentType(param.Type);
}
```

组件类型判断：

```csharp id="aa82yu"
private static bool IsComponentType(ITypeSymbol type)
{
    return ImplementsInterface(type, "LayerBase.ECS.IComponent");
}
```

如果 LayerBase 允许不实现 `IComponent` 的 Arch 组件，那要改成：

```text id="nle9pz"
方案 A：
  强制 LayerBase Query 组件必须实现 IComponent。

方案 B：
  提供 [Component] 标记或 ComponentTypeRegistry。

方案 C：
  通过 ref/in + 非 Input 标记判断，但这会继续有歧义。
```

我建议方案 A：

```text id="wjxcq0"
LayerBase 的 [Query] 源生成器只支持实现 IComponent 的组件。
```

这样模型最清楚。

---

# 7. IsInputParameter 设计

输入参数允许：

```text id="0ypfq2"
普通值类型参数：float deltaTime
in readonly struct 参数：in MoveInput input
```

不允许：

```text id="u5q9c6"
ref input
out input
class input
string input 第一版也不建议
object input
List<T>
Dictionary<TKey,TValue>
```

代码：

```csharp id="bclrsf"
private static bool IsInputParameter(IParameterSymbol param)
{
    if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.Out)
    {
        return false;
    }

    if (!param.Type.IsValueType)
    {
        return false;
    }

    if (IsComponentType(param.Type))
    {
        return false;
    }

    if (IsMetadataType(param.Type, EntityMetadataName))
    {
        return false;
    }

    return true;
}
```

允许 `in MoveInput`：

```csharp id="4ujotl"
param.RefKind == RefKind.In && param.Type.IsValueType && !IsComponentType(param.Type)
```

---

# 8. GenerateQueryInvocation 修改

当前普通 Query 生成：

```csharp id="un4ez2"
var job = new __MoveJob(this);

global::LayerBase.ServiceECSExtensions
    .Query<Position, Velocity>(this)
    .ForEach(ref job);
```

新版本生成：

```csharp id="x49iyz"
var job = new __MoveJob(deltaTime);

global::LayerBase.ServiceECSExtensions
    .Query<Position, Velocity>(this)
    .ForEach(ref job);
```

如果是多个输入：

```csharp id="mwxbel"
var job = new __MoveJob(deltaTime, speedScale, in config);
```

生成入口签名：

```csharp id="4w6udw"
public void Move(float deltaTime, float speedScale, in MoveConfig config)
```

实现函数：

```csharp id="r93m5x"
private static void GenerateQueryInvocation(StringBuilder sb, QueryMethodInfo method)
{
    string compGeneric = BuildComponentGenericArguments(method);
    string inputArgs = BuildInputArgumentList(method);

    sb.AppendLine($"            var job = new __{method.EntryPointName}Job({inputArgs});");
    sb.AppendLine();
    sb.AppendLine("            global::LayerBase.ServiceECSExtensions");
    sb.AppendLine($"                .Query<{compGeneric}>(this)");
    sb.AppendLine("                .ForEach(ref job);");
}
```

---

# 9. GenerateBringInvocation 修改

当前 Bring 生成：

```csharp id="kos7gf"
var job = new __MoveViewJob(this);

global::LayerBase.ServiceECSExtensions
    .Query<Position, Velocity>(this)
    .Bring<MoveViewEvent>()
    .ForEach(ref job)
    .Batch()
    .Post();
```

新版本：

```csharp id="cubha0"
var job = new __MoveViewJob(deltaTime);

global::LayerBase.ServiceECSExtensions
    .Query<Position, Velocity>(this)
    .Bring<MoveViewEvent>()
    .ForEach(ref job)
    .Batch()
    .Post();
```

只改 Job 构造，不改 Query/Bring 链路。

---

# 10. GenerateMethodSource 修改

当前入口方法没有参数：

```csharp id="xe41d1"
public void Move()
```

新版本要根据 input 参数生成：

```csharp id="yp40s1"
public void Move(float deltaTime)
```

或者：

```csharp id="r3c66q"
public void Move(in MoveInput input)
```

生成：

```csharp id="kbn7k5"
private static void GenerateMethodSource(StringBuilder sb, QueryMethodInfo method)
{
    string entryPoint = method.EntryPointName!;
    string entryParams = BuildEntryPointParameterList(method);

    sb.AppendLine($"        public void {entryPoint}({entryParams})");
    sb.AppendLine("        {");

    if (method.BringEventTypes.Length > 0)
    {
        GenerateBringInvocation(sb, method);
    }
    else
    {
        GenerateQueryInvocation(sb, method);
    }

    sb.AppendLine("        }");
    sb.AppendLine();

    GenerateJobStruct(sb, method);
    sb.AppendLine();
}
```

---

# 11. GenerateJobStruct 修改

当前 Job 生成：

```csharp id="yr1gc9"
private readonly SelfType _self;

public __MoveJob(SelfType self)
{
    _self = self;
}
```

要改成输入字段：

```csharp id="5xvf3i"
private readonly float _deltaTime;
private readonly MoveConfig _config;

public __MoveJob(float deltaTime, in MoveConfig config)
{
    _deltaTime = deltaTime;
    _config = config;
}
```

如果没有输入参数：

```csharp id="s9yel1"
public __MoveJob()
{
}
```

但 readonly struct 无字段时可以不生成构造函数，入口中直接：

```csharp id="fi7j9q"
var job = new __MoveJob();
```

---

## 11.1 Job 字段生成

```csharp id="w2dhzp"
private static void EmitInputFields(StringBuilder sb, QueryMethodInfo method)
{
    foreach (var input in method.InputParameters)
    {
        string typeName = GetTypeDisplayName(input.Type);
        string fieldName = GetInputFieldName(input);

        sb.AppendLine($"            private readonly {typeName} {fieldName};");
    }
}
```

命名建议：

```text id="e6r0c4"
input 参数名 deltaTime -> _deltaTime
input 参数名 config -> _config
冲突时 -> _input0, _input1
```

---

## 11.2 Job 构造函数生成

```csharp id="zc361i"
public __MoveJob(float deltaTime, in MoveConfig config)
{
    _deltaTime = deltaTime;
    _config = config;
}
```

注意：即使入口参数是 `in MoveConfig config`，字段赋值也是复制一份：

```csharp id="plxa8x"
_config = config;
```

这是好事，因为异步执行时需要提交时刻的快照。

---

## 11.3 Execute 调用生成

当前 Execute 调用：

```csharp id="vebmf7"
_self.OnMove(ref c0, in c1);
```

新版本：

```csharp id="ye1ujj"
OnMove(_deltaTime, ref c0, in c1);
```

Bring：

```csharp id="z1ekvr"
return OnMoveView(_deltaTime, ref c0, in c1, ref e0);
```

如果方法声明里有 Entity：

```csharp id="dt1dfx"
OnMove(_deltaTime, entity, ref c0, in c1);
```

BuildUserMethodArgumentList 要支持 Input：

```csharp id="dj1sat"
case QueryUserParameterKind.Input:
    args.Add(GetInputFieldName(userParameter.Index));
    break;
```

如果原始 input 是 `in MoveInput input`，调用时可以生成：

```csharp id="4l1dck"
in _input
```

所以 Input 还要记录 RefKind。

```csharp id="y7u0p9"
case QueryUserParameterKind.Input:
{
    var input = method.InputParameters[userParameter.Index];
    string field = GetInputFieldName(input);

    if (input.RefKind == RefKind.In)
        args.Add($"in {field}");
    else
        args.Add(field);

    break;
}
```

---

# 12. EntryPoint 名称规则

保持当前规则：

```text id="jotdlp"
[EntryPoint("Move")] -> Move
OnMove -> Move
```

当前生成器就是这么做的。

新变化只是入口方法可能带参数：

```csharp id="yrwp60"
public void Move(float deltaTime)
```

---

# 13. Bring 事件参数规则保持不变

当前 Bring 参数逻辑可以保留：

```text id="6wu11d"
BringAttribute 声明几个事件，方法末尾就必须有几个 ref event 参数。
```

例如：

```csharp id="uwf7cn"
[Query]
[Bring<DamageEvent, HitViewEvent>]
private static ProjectResult OnHit(
    in HitInput input,
    ref Position position,
    in Collider collider,
    ref DamageEvent damage,
    ref HitViewEvent view)
{
}
```

生成：

```csharp id="zfqy9d"
private readonly struct __HitJob :
    IProjectionJob2x2<Position, Collider, DamageEvent, HitViewEvent>
{
    private readonly HitInput _input;

    public ProjectResult Execute(
        Entity entity,
        ref Position c0,
        ref Collider c1,
        ref DamageEvent e0,
        ref HitViewEvent e1)
    {
        return OnHit(in _input, ref c0, in c1, ref e0, ref e1);
    }
}
```

---

# 14. Diagnostics 设计

建议新增诊断码。

## LBQ001：Query method must be static

```text id="gryc2w"
[Query] 方法必须是 static。
```

示例错误：

```csharp id="fo90dh"
[Query]
private void OnMove(ref Position position)
{
}
```

提示：

```text id="12x7mr"
Change method to static and pass required external state as entry-point input parameters.
```

---

## LBQ002：Query input must appear before ECS components

```text id="2ton7v"
Input 参数必须出现在 Entity / Component 参数之前。
```

错误：

```csharp id="tpac7l"
[Query]
private static void OnMove(
    ref Position position,
    float deltaTime,
    in Velocity velocity)
{
}
```

---

## LBQ003：Unsupported Query parameter

```text id="ivlsky"
无法判断参数是 Input、Entity、Component 还是 BringEvent。
```

常见原因：

```text id="10mhx2"
组件类型没有实现 IComponent。
Input 是 class。
Input 使用 ref/out。
Bring Event 没放在末尾。
```

---

## LBQ004：Component parameter must be ref or in

```text id="02k4q4"
组件参数必须是 ref 或 in。
```

错误：

```csharp id="cnlaxa"
[Query]
private static void OnMove(Position position)
{
}
```

---

## LBQ005：Bring Query must return ProjectResult

当前已有逻辑，建议变成明确诊断，而不是 return null。

---

## LBQ006：Plain Query must return void

当前已有逻辑，建议变成明确诊断。

---

# 15. 兼容迁移策略

你如果决定彻底强制 static，我建议直接做破坏性升级。

旧写法：

```csharp id="ctmf7a"
[Query]
private void OnMove(ref Position position, in Velocity velocity)
{
    position.Value += velocity.Value * _deltaTime;
}
```

新写法：

```csharp id="s3y5w7"
[Query]
private static void OnMove(
    float deltaTime,
    ref Position position,
    in Velocity velocity)
{
    position.Value += velocity.Value * deltaTime;
}
```

调用改为：

```csharp id="69vwo6"
Move(deltaTime);
```

文档中给迁移提示：

```text id="gk7my3"
实例字段 -> Query 输入参数。
实例方法 -> static helper。
实例状态统计 -> ECS Result / Event 回主线程。
```

不建议做 `[QueryLegacy]`，因为会让心智模型重新变脏。

---

# 16. QueryBringGenerator 修改点清单

## 16.1 ExtractQueryMethodInfo

修改：

```text id="o0hsfm"
1. 检查 methodSymbol.IsStatic。
2. 新增 InputParameters。
3. 参数分类从“ref/in 就是组件”改为：
   - Entity
   - BringEvent
   - Component = ref/in + IComponent
   - Input = value/in struct + 非 IComponent
4. Input 必须在组件前。
5. Bring 仍然必须在末尾。
6. 错误用 Diagnostic，不要静默 return null。
```

---

## 16.2 GenerateMethodSource

修改：

```text id="j0p2bq"
public void EntryPoint()
```

变成：

```text id="cskgqp"
public void EntryPoint(input parameters...)
```

---

## 16.3 GenerateQueryInvocation / GenerateBringInvocation

修改：

```text id="mkgm3i"
new __Job(this)
```

变成：

```text id="wcqzqb"
new __Job(input parameters...)
```

---

## 16.4 GenerateJobStruct

删除：

```text id="ss0rik"
private readonly SelfType _self;
```

新增：

```text id="b38x9a"
private readonly input fields...
```

删除：

```text id="ne5ln9"
_self.OnXxx(...)
```

改成：

```text id="2kf7pm"
OnXxx(input fields..., components..., bring events...)
```

---

## 16.5 BuildExecuteParameters

基本保持不变。

Execute 仍然只接收：

```text id="vpo5ba"
Entity
ECS components
Bring events
```

Input 不进入 Execute 参数，因为它已经存到 Job 字段中。

---

## 16.6 BuildUserMethodArgumentList

新增 Input 分支。

```csharp id="njw18d"
case QueryUserParameterKind.Input:
    args.Add(BuildInputFieldArgument(method, userParameter));
    break;
```

---

# 17. 最终生成器前后对比

## 改造前

```text id="g9gdw1"
[Query] instance method
  ↓
生成入口 Move()
  ↓
new __MoveJob(this)
  ↓
Job.Execute
  ↓
_self.OnMove(...)
```

风险：

```text id="nzkh6w"
EcsWorker 可以访问业务实例。
```

---

## 改造后

```text id="aidmin"
[Query] static method
  ↓
生成入口 Move(input...)
  ↓
new __MoveJob(input...)
  ↓
Job.Execute
  ↓
OnMove(input fields..., components...)
```

结果：

```text id="991zdy"
Job 不持有业务对象。
EcsWorker 只访问 ECS 组件和输入值。
```

---

# 18. 最终建议

可以直接把规则定死：

```text id="birpgo"
[Query] 方法必须 static。
```

同时把 Query 的外部输入模型升级为：

```text id="ls6mj3"
入口方法参数 -> Job readonly 字段 -> static Query 方法参数
```

这样 LayerBase 的 Query 源生成器会从：

```text id="sf75t3"
帮用户把实例方法塞进 ECS Query
```

进化成：

```text id="klicpx"
帮用户把纯静态 ECS 函数包装成可同步/异步调度的 Job
```

这才适合你现在的异步 ECS 线程模型。
