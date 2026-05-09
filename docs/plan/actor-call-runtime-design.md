# LayerBase Actor Call Runtime Design

> 文件名：`actor-call-runtime-design.md`  
> 适用分支：`faster`  
> 适用工程：`avaw23112/LayerBase`  
> 目标：为 Actor 系统补齐 `DelayPost`、`DispatchNow`、`Ask`、`DelayAsk`、`ImmediatelyAsk`、`Actor Call` 与 `[ActorCallBehaviour]`，并与 `LayerRuntime`、`Layer`、`IService`、`ILayerContext` 的调用链完整衔接。  
> 约束：不使用反射绑定；热路径不使用字典查询；关键路径利于 JIT 内联；对齐现有 `[Call]` / `IAutoCallBinder` 的源生成器自动绑定思路。  

---

## 1. 当前仓库基线

### 1.1 现有 Layer Call 语义

当前仓库已经具备 Layer 级 Request / Response 调用语义：

```text
[Call]
    -> CallAutoBindGenerator
    -> IAutoCallBinder
    -> Layer.RegisterCallHandler
    -> LayerCallRouteId<TRequest, TResponse>
    -> LayerCallInvoker<TRequest, TResponse>
    -> LayerRuntime.CallAsync<TLayer, TRequest, TResponse>
    -> LBTask<TResponse>
```

现有 Call 设计具备以下特点：

- `[Call]` 由源生成器扫描，不再走反射绑定。
- 生成器让 `Layer` 或 `IService` 的 `partial` 类型实现 `IAutoCallBinder`。
- `IAutoCallBinder.AutoBindCalls(Layer layer)` 在 Layer Build 阶段被自动调用。
- `[Call]` 允许声明在 `Layer` 和 `IService` 上。
- `ILayerContext` 不允许直接声明 `[Call]`。
- `Layer.RegisterCallHandler<TRequest, TResponse>` 把 Handler 转成 `LayerCallInvoker<TRequest, TResponse>`。
- `Layer` 内部用 `object?[] m_callRouteInvokers` 按 routeId 存 invoker。
- `Layer.CallAsync<TRequest, TResponse>` 热路径通过泛型静态 routeId + 数组索引找到 invoker。
- `LayerRuntime.CallAsync<TLayer, TRequest, TResponse>` 通过 runtimeId + version + 泛型静态缓存减少重复查找。
- 返回值统一为 `LBTask<TResponse>`。

Actor 侧应复用这套心智模型，但不直接复用 `IAutoCallBinder`，因为 Actor 已经有自己的 `IGeneratedActorMeta` 和 `ActorTypeMetaBuilder` 生成链路。

### 1.2 当前 Actor 基线

当前 Actor 已经具备：

```text
ActorWorld
ActorId
ActorContext
IGeneratedActorMeta
ActorTypeMetaBuilder
ActorTypeMeta<TActor>
ActorBehaviourEntry
TypedActorStorage<TActor>
BehaviourArchetype
Actor 邮箱
Actor Pump
Actor Query
Actor Pool
Actor Debug
```

当前 Actor 创建链路：

```text
LayerRuntime
    -> ActorWorld Actors
        -> CreateActor<TActor>()
            -> new TActor() / ActorPool<TActor>.Rent()
            -> ActorGeneratedAccess.RequireGenerated(actor)
            -> ActorTypeMetaCache.GetOrBuild<TActor>(generated)
            -> GetOrCreateArchetype(...)
            -> TypedActorStorage<TActor>.AllocateSlot(...)
            -> generated.ActorInit(new ActorContext(world, actorId))
            -> storage.RegisterLifecycleInterfaces(...)
```

当前 Actor Pump 链路：

```text
LayerRuntime.Pump(deltaTime)
    -> TimeScheduler.Tick
    -> DelayManager.Tick
    -> LayerBaseSynchronizationContext.Update
    -> FixedUpdate accumulator
    -> PostIngressQueue.DrainTo
    -> PostScheduler.Pump
    -> RuntimeFrameBudget actorBudget
    -> ActorWorld.Pump(deltaTime, fixedDeltaTime, pumpFixedUpdate, ref actorBudget)
    -> LayerChain.Pump(deltaTime)
```

Actor Call 设计必须嵌入上述链路，而不是另建一套 Runtime。

---

## 2. 最终通信语义

Actor 通信分为两组。

### 2.1 单向事件语义

```text
[ActorBehaviour]
    -> Post
    -> DelayPost
    -> DispatchNow
```

| API | 是否进入 Actor 邮箱 | 是否受背压 | 是否受帧预算 | 是否立即执行 |
|---|---:|---:|---:|---:|
| `Post` | 是 | 是 | 是 | 否 |
| `DelayPost` | 到期后进入 | 到期后受控 | 到期后受控 | 否 |
| `DispatchNow` | 否 | 否 | 否 | 是 |

### 2.2 Request / Response 语义

```text
[ActorCallBehaviour]
    -> Ask
    -> DelayAsk
    -> ImmediatelyAsk
    -> Actor Call
```

| API | 是否进入 Actor 邮箱 | 是否受背压 | 是否受帧预算 | 返回值 |
|---|---:|---:|---:|---|
| `Ask` | 是 | 是 | 是 | `LBTask<TResponse>` |
| `DelayAsk` | 到期后进入 | 到期后受控 | 到期后受控 | `LBTask<TResponse>` |
| `ImmediatelyAsk` | 否 | 否 | 否 | `LBTask<TResponse>` |
| `Actor Call` | 由入口决定 | 由入口决定 | 由入口决定 | `LBTask<TResponse>` |

---

## 3. 术语解释

### 3.1 JIT

JIT 是 Just-In-Time Compilation，意思是“即时编译”。

在 .NET 中，C# 代码先编译成 IL。

IL 是 Intermediate Language，意思是“中间语言”。

程序运行时，JIT 会把 IL 编译成本机机器码。

利于 JIT 优化的代码通常有这些特点：

- 泛型类型固定。
- 分支少。
- 虚调用少。
- 方法体短。
- 热路径少用接口转发。
- 少用反射。
- 少用装箱。
- 少用字典查找。
- 尽量通过数组索引访问。
- 尽量使用可内联的小方法。

### 3.2 内联

内联是 JIT 的一种优化。

它会把一个小方法的代码直接展开到调用点。

这样可以减少方法调用成本，也能让 JIT 继续优化展开后的代码。

本设计会在热路径方法上使用：

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
```

这表示“建议 JIT 尽量内联该方法”。

### 3.3 热路径

热路径是运行时非常频繁执行的代码路径。

例如：

- 每帧 Pump。
- 每次 Actor Post。
- 每次 Ask。
- 每次 ImmediatelyAsk。
- 每次邮箱取消息。
- 每次通过 routeId 找 invoker。

热路径不能使用反射，也不应使用字典查找。

### 3.4 冷路径

冷路径是不频繁执行的代码路径。

例如：

- Build 阶段。
- Actor 类型首次创建。
- ActorTypeMeta 构建。
- routeId 数组扩容。
- 生成器生成代码。
- Debug Dump。

冷路径可以接受少量字典、排序、数组扩容。

### 3.5 routeId

`routeId` 是 Request / Response 类型组合对应的整数编号。

例如：

```text
GetHpRequest + GetHpResponse -> routeId 3
FindPathRequest + FindPathResponse -> routeId 4
```

运行时不直接比较 `Type`。

运行时只用 `routeId` 作为数组下标。

### 3.6 invoker

`invoker` 是由源生成器生成的调用委托。

它把“调用某个 Actor 的某个方法”变成一个强类型函数指针式入口。

ActorCall invoker 的目标形态：

```text
ActorCallInvoker<TActor, TRequest, TResponse>
```

它接收 Actor 实例、Request、CancellationToken，返回 `LBTask<TResponse>`。

### 3.7 背压

背压是系统保护机制。

当消息生产速度大于消费速度时，系统不能无限堆积消息。

背压策略可以包括：

- 拒绝新消息。
- 丢弃旧消息。
- 合并消息。
- 扩容。
- 限制一帧处理数量。

### 3.8 帧预算

帧预算是每帧允许 Actor 系统消耗的最大工作量。

它保护主线程不会被 Actor 消息或生命周期处理拖死。

---

## 4. 总体架构

最终架构：

```text
LayerRuntime
    ├── EventCenter
    ├── PostScheduler
    ├── TimeScheduler
    ├── DelayPublisherManager
    ├── ServiceProvider
    └── ActorWorld
            ├── ActorDelayScheduler
            │       └── ActorTimeWheel
            ├── BehaviourArchetype[]
            ├── EventBucket[]
            ├── ActorCallRoute runtime
            ├── Actor lifecycle scheduler
            └── Actor query cache
```

调用方向：

```text
系统层
    ├── Layer
    ├── IService
    └── ILayerContext
          ↓
LayerRuntime.Actors
          ↓
ActorWorld
          ↓
具体 Actor
```

Actor 内部需要访问系统层时：

```text
Actor
    -> ActorContext
    -> LayerRuntime
    -> CallAsync<TLayer, TRequest, TResponse>
    -> GetService<TService>
```

这样可以支持：

```text
系统层 -> 具体实体层
具体实体层 -> 系统层
```

但默认主方向仍然是：

```text
系统层调度 Actor
Actor 处理实体级逻辑
```

---

## 5. 源生成器绑定模型

### 5.1 不新增反射绑定

Actor Call 不允许使用：

```text
MethodInfo
GetCustomAttribute
MakeGenericMethod
Invoke
DynamicInvoke
```

这些都不能出现在 Actor Call 热路径。

`[ActorCallBehaviour]` 必须完全由源生成器处理。

### 5.2 对标 IAutoCallBinder 的设计

现有 `[Call]` 的绑定方式是：

```text
[Call]
    -> 生成 partial class : IAutoCallBinder
    -> AutoBindCalls(layer)
    -> Register<TRequest, TResponse>(layer, generatedHandler)
```

Actor 的绑定方式应是：

```text
[ActorCallBehaviour]
    -> 生成 partial class : IGeneratedActorMeta
    -> __BuildActorMeta(builder)
    -> builder.AddCallBehaviour<TActor, TRequest, TResponse>(generatedInvoker)
```

原因：

- Actor 已经依赖 `IGeneratedActorMeta` 注入 `ActorContext`。
- Actor 创建时已经调用 `ActorGeneratedAccess.RequireGenerated(actor)`。
- Actor 类型元数据已经通过 `ActorTypeMetaBuilder` 构建。
- 继续使用 `IGeneratedActorMeta` 能避免新增运行时扫描。
- 不需要像 `IAutoCallBinder` 那样在 Build 阶段遍历服务实例。
- Actor 的 Call 元数据是类型级元数据，适合进入 `ActorTypeMeta<TActor>`。

---

## 6. ActorCallBehaviourAttribute

```csharp
using System;

namespace LayerBase.Actor;

[AttributeUsage(
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ActorCallBehaviourAttribute : Attribute
{
    // AttributeTargets.Method：
    // 限制该特性只能标记方法。
    //
    // AllowMultiple = false：
    // 同一个方法不能重复声明 ActorCallBehaviour。
    //
    // Inherited = false：
    // 子类不会继承父类方法上的 ActorCallBehaviour。
    // 这样可以避免源生成器误注册继承来的方法。
}
```

---

## 7. ActorCallBehaviour 签名规则

### 7.1 唯一合法签名

```csharp
[ActorCallBehaviour]
private LBTask<TResponse> Method(
    in TRequest request,
    CancellationToken cancellationToken)
    where TRequest : struct
    where TResponse : struct
```

### 7.2 强制要求

- 必须是实例方法。
- 不能是泛型方法。
- 必须返回 `LayerBase.Async.LBTask<TResponse>`。
- `TResponse` 必须是 struct。
- 第一个参数必须是 `in TRequest`。
- `TRequest` 必须是 struct。
- 第二个参数必须是 `CancellationToken`。
- 不支持 `Task<TResponse>`。
- 不支持 `ValueTask<TResponse>`。
- 不支持同步返回 `TResponse`。
- 不支持省略 `CancellationToken`。
- 不支持 `ref` / `out` Request。
- 不支持多个 Request 参数。

---

## 8. Actor Call 路由类型

### 8.1 ActorCallRouteId

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

internal static class ActorCallRouteId<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    // Id 字段：
    // 当前 Request / Response 类型组合对应的整数路由编号。
    //
    // 必要逻辑：
    // 泛型静态字段会为每个 TRequest / TResponse 组合初始化一次。
    // 热路径读取 Id 只是读静态 int，不需要字典查询。
    public static readonly int Id = ActorCallRouteRegistry.GetNextId();
}

internal static class ActorCallRouteRegistry
{
    private static int s_nextId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNextId()
    {
        // 必要逻辑：
        // 这里和现有 LayerCallRouteRegistry 对齐。
        // routeId 从 0 开始递增，适合作为数组下标。
        return Interlocked.Increment(ref s_nextId) - 1;
    }
}
```

### 8.2 ActorCallInvoker

```csharp
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

internal delegate LBTask<TResponse> ActorCallInvoker<TActor, TRequest, TResponse>(
    TActor actor,
    in TRequest request,
    CancellationToken cancellationToken)
    where TActor : class, IActor
    where TRequest : struct
    where TResponse : struct;

// actor 参数：
// 当前被调用的 Actor 实例。
//
// request 参数：
// 调用方传入的请求结构体。
//
// cancellationToken 参数：
// 调用方取消等待时传入的取消令牌。
//
// 必要逻辑：
// 所有 [ActorCallBehaviour] 方法都由源生成器适配为该委托。
// 运行时不需要反射，也不需要判断返回值类型。
```

### 8.3 ActorCallEntry

```csharp
namespace LayerBase.Actor;

internal readonly struct ActorCallEntry
{
    public readonly int RouteId;
    public readonly Type RequestType;
    public readonly Type ResponseType;
    public readonly Delegate Invoker;

    public ActorCallEntry(
        int routeId,
        Type requestType,
        Type responseType,
        Delegate invoker)
    {
        // routeId 参数：
        // Request / Response 对应的整数路由编号。
        //
        // requestType 参数：
        // 请求类型，仅用于 Debug、Dump 和错误信息。
        //
        // responseType 参数：
        // 响应类型，仅用于 Debug、Dump 和错误信息。
        //
        // invoker 参数：
        // 源生成器生成的强类型 ActorCallInvoker。
        //
        // 必要逻辑：
        // RouteId 用于热路径数组索引。
        // Type 只用于非热路径诊断，不参与派发。
        RouteId = routeId;
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }
}
```

---

## 9. ActorTypeMeta 扩展

当前 `ActorTypeMetaBuilder` 已经收集 `ActorBehaviourEntry`、Tag、Group。

需要新增 Call 行为收集。

```csharp
namespace LayerBase.Actor;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();
    private readonly List<ActorCallEntry> _callEntries = new();

    private readonly HashSet<int> _eventIds = new();
    private readonly HashSet<int> _callRouteIds = new();
    private readonly HashSet<int> _tagIds = new();
    private readonly HashSet<int> _groupIds = new();

    public void AddCallBehaviour<TActor, TRequest, TResponse>(
        ActorCallInvoker<TActor, TRequest, TResponse> invoker)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        // TActor 泛型参数：
        // 当前注册 ActorCallBehaviour 的 Actor 类型。
        //
        // TRequest 泛型参数：
        // 请求结构体类型。
        //
        // TResponse 泛型参数：
        // 响应结构体类型。
        //
        // invoker 参数：
        // 源生成器生成的强类型调用委托。
        //
        // 必要逻辑：
        // 使用 ActorCallRouteId<TRequest,TResponse>.Id 获取 routeId。
        // 同一个 Actor 类型不能重复注册相同 Request / Response。
        if (invoker == null)
        {
            throw new ArgumentNullException(nameof(invoker));
        }

        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;

        if (!_callRouteIds.Add(routeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has call behaviour for request {typeof(TRequest).Name} and response {typeof(TResponse).Name}.");
        }

        _callEntries.Add(new ActorCallEntry(
            routeId,
            typeof(TRequest),
            typeof(TResponse),
            invoker));
    }

    internal ActorTypeMeta<TActor> Build<TActor>()
        where TActor : class, IActor
    {
        // 必要逻辑：
        // Event 行为和 Call 行为都按整数 ID 排序。
        // 这样构建结果稳定，方便 Debug 和测试。
        ActorBehaviourEntry[] entries = _entries
            .OrderBy(static entry => entry.EventTypeId)
            .ToArray();

        ActorCallEntry[] callEntries = _callEntries
            .OrderBy(static entry => entry.RouteId)
            .ToArray();

        int[] eventTypeIds = entries
            .Select(static entry => entry.EventTypeId)
            .ToArray();

        int[] tagIds = _tagIds
            .OrderBy(static id => id)
            .ToArray();

        int[] groupIds = _groupIds
            .OrderBy(static id => id)
            .ToArray();

        return new ActorTypeMeta<TActor>(
            new BehaviourSignature(eventTypeIds),
            entries,
            callEntries,
            tagIds,
            groupIds);
    }
}
```

`ActorTypeMeta<TActor>` 需要扩展：

```csharp
namespace LayerBase.Actor;

public sealed class ActorTypeMeta<TActor>
    where TActor : class, IActor
{
    public BehaviourSignature Signature { get; }

    public ActorBehaviourEntry[] Behaviours { get; }

    public ActorCallEntry[] CallBehaviours { get; }

    public int[] TagIds { get; }

    public int[] GroupIds { get; }

    public ActorTypeMeta(
        BehaviourSignature signature,
        ActorBehaviourEntry[] behaviours,
        ActorCallEntry[] callBehaviours,
        int[] tagIds,
        int[] groupIds)
    {
        // signature 参数：
        // 单向 ActorBehaviour 事件签名。
        //
        // behaviours 参数：
        // 单向 ActorBehaviour 入口集合。
        //
        // callBehaviours 参数：
        // Request / Response ActorCallBehaviour 入口集合。
        //
        // tagIds 参数：
        // Actor 类型静态 Tag 编号。
        //
        // groupIds 参数：
        // Actor 类型静态 Group 编号。
        //
        // 必要逻辑：
        // ActorBehaviour 与 ActorCallBehaviour 必须分开保存。
        // 否则 Post 与 Ask 的语义会混在同一条管线里。
        Signature = signature;
        Behaviours = behaviours;
        CallBehaviours = callBehaviours;
        TagIds = tagIds;
        GroupIds = groupIds;
    }
}
```

---

## 10. 源生成器生成代码

### 10.1 示例 Actor

```csharp
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;

namespace Game.Battle;

public readonly struct GetHpRequest
{
    // EntityId 参数：
    // 业务实体编号。
    // 如果调用方已经通过 ActorId 精确指定 Actor，
    // 该字段可以不用。
    public readonly int EntityId;

    public GetHpRequest(int entityId)
    {
        // entityId 参数：
        // 外部传入的业务实体编号。
        EntityId = entityId;
    }
}

public readonly struct GetHpResponse
{
    // CurrentHp 参数：
    // 当前生命值。
    public readonly int CurrentHp;

    public GetHpResponse(int currentHp)
    {
        // currentHp 参数：
        // 当前生命值。
        CurrentHp = currentHp;
    }
}

public sealed partial class EnemyActor : IActor
{
    private int _hp = 100;

    [ActorCallBehaviour]
    private LBTask<GetHpResponse> OnGetHp(
        in GetHpRequest request,
        CancellationToken cancellationToken)
    {
        // request 参数：
        // 调用方传入的请求数据。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // 即使当前结果可以同步得到，也统一使用 LBTask.FromResult 包装。
        // ActorCallBehaviour 只允许返回 LBTask<TResponse>。
        return LBTask<GetHpResponse>.FromResult(new GetHpResponse(_hp));
    }
}
```

### 10.2 生成器输出核心代码

```csharp
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;

namespace Game.Battle;

partial class EnemyActor : global::LayerBase.Actor.IGeneratedActorMeta
{
    private global::LayerBase.Actor.ActorContext __actorContext;

    void global::LayerBase.Actor.IGeneratedActorMeta.__BuildActorMeta(
        global::LayerBase.Actor.ActorTypeMetaBuilder builder)
    {
        // builder 参数：
        // Actor 类型元数据构建器。
        //
        // 必要逻辑：
        // 源生成器把 [ActorCallBehaviour] 注册为强类型 invoker。
        // 运行时只使用 routeId + 数组索引，不使用反射。

        builder.AddCallBehaviour<EnemyActor, GetHpRequest, GetHpResponse>(
            static (
                EnemyActor actor,
                in GetHpRequest request,
                CancellationToken cancellationToken) =>
            {
                // actor 参数：
                // 当前被调用的 Actor 实例。
                //
                // request 参数：
                // 当前调用请求。
                //
                // cancellationToken 参数：
                // 调用方取消等待时使用。
                //
                // 必要逻辑：
                // 原方法已经返回 LBTask<TResponse>，
                // 因此这里不做同步包装，也不做返回类型判断。
                return actor.OnGetHp(in request, cancellationToken);
            });
    }

    void global::LayerBase.Actor.IGeneratedActorMeta.ActorInit(
        global::LayerBase.Actor.ActorContext context)
    {
        // context 参数：
        // ActorWorld 创建 Actor 后注入的上下文。
        //
        // 必要逻辑：
        // ActorContext 保存 ActorId、ActorWorld、LayerRuntime。
        // 后续 Actor 自身可以通过它发起 Post、Ask、Call 或访问服务。
        __actorContext = context;
    }
}
```

---

## 11. TypedActorStorage Call 路由表

### 11.1 存储结构

`TypedActorStorage<TActor>` 应新增：

```csharp
private object?[] _callInvokersByRouteId;
private Type?[] _callRequestTypesByRouteId;
private Type?[] _callResponseTypesByRouteId;
```

这些数组只在构建或扩容时写入。

热路径只读取。

### 11.2 构建路由表

```csharp
namespace LayerBase.Actor;

internal sealed partial class TypedActorStorage<TActor>
    where TActor : class, IActor
{
    private object?[] _callInvokersByRouteId = Array.Empty<object?>();
    private Type?[] _callRequestTypesByRouteId = Array.Empty<Type?>();
    private Type?[] _callResponseTypesByRouteId = Array.Empty<Type?>();

    private void BuildCallRoutes(ActorTypeMeta<TActor> meta)
    {
        // meta 参数：
        // 当前 Actor 类型的完整元数据。
        //
        // 必要逻辑：
        // Call 路由表只在类型元数据建立时构建。
        // 热路径不使用字典，也不做 Type 比较。
        foreach (ActorCallEntry entry in meta.CallBehaviours)
        {
            EnsureCallRouteCapacity(entry.RouteId);

            if (_callInvokersByRouteId[entry.RouteId] != null)
            {
                throw new InvalidOperationException(
                    $"Duplicate ActorCall route on actor type {typeof(TActor).Name}.");
            }

            _callInvokersByRouteId[entry.RouteId] = entry.Invoker;
            _callRequestTypesByRouteId[entry.RouteId] = entry.RequestType;
            _callResponseTypesByRouteId[entry.RouteId] = entry.ResponseType;
        }
    }

    private void EnsureCallRouteCapacity(int routeId)
    {
        // routeId 参数：
        // Request / Response 类型组合对应的整数路由编号。
        //
        // 必要逻辑：
        // routeId 是数组下标。
        // 当 routeId 超过当前数组长度时扩容。
        // 该方法只在构建路由表时调用，不在热路径调用。
        if ((uint)routeId < (uint)_callInvokersByRouteId.Length)
        {
            return;
        }

        int newSize = _callInvokersByRouteId.Length == 0
            ? 4
            : _callInvokersByRouteId.Length;

        while (newSize <= routeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _callInvokersByRouteId, newSize);
        Array.Resize(ref _callRequestTypesByRouteId, newSize);
        Array.Resize(ref _callResponseTypesByRouteId, newSize);
    }
}
```

---

## 12. ImmediatelyAsk 热路径

### 12.1 ActorWorld API

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // actorId 参数：
        // 要立即请求的目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // ImmediatelyAsk 不进入邮箱，不参与背压，不消耗帧预算。
        // 它直接通过 ActorId 找到 Storage，再通过 routeId 找 invoker。
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                actorId,
                ActorCallFailureKind.InvalidActorId);
        }

        return _archetypes[actorId.ArchetypeId]
            .ImmediatelyAsk<TRequest, TResponse>(
                actorId,
                in request,
                cancellationToken);
    }
}
```

### 12.2 TypedActorStorage 热路径

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

internal sealed partial class TypedActorStorage<TActor>
    where TActor : class, IActor
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
        int slotIndex,
        int generation,
        in TRequest request,
        CancellationToken cancellationToken)
        where TRequest : struct
        where TResponse : struct
    {
        // slotIndex 参数：
        // Actor 在当前 TypedActorStorage 中的槽位。
        //
        // generation 参数：
        // ActorId 中携带的代数，用于防止旧 ID 命中新 Actor。
        //
        // request 参数：
        // 当前请求数据。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // 热路径只做数组边界检查、generation 检查、状态检查、routeId 数组查找。
        // 不做字典查找，不做反射，不做 Type 比较。

        if (!IsAlive(slotIndex, generation))
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                ActorCallFailureKind.ActorNotFound);
        }

        int routeId = ActorCallRouteId<TRequest, TResponse>.Id;
        object?[] invokers = _callInvokersByRouteId;

        if ((uint)routeId >= (uint)invokers.Length)
        {
            return ActorCallFailure.Unsupported<TResponse, TRequest, TResponse>();
        }

        var invoker = invokers[routeId] as ActorCallInvoker<TActor, TRequest, TResponse>;
        if (invoker == null)
        {
            return ActorCallFailure.Unsupported<TResponse, TRequest, TResponse>();
        }

        TActor? actor = _actors[slotIndex];
        if (actor == null)
        {
            return ActorCallFailure.InvalidActor<TResponse>(
                ActorCallFailureKind.ActorNotFound);
        }

        try
        {
            return invoker(actor, in request, cancellationToken);
        }
        catch (Exception ex)
        {
            return LBTask<TResponse>.FromException(ex);
        }
    }
}
```

---

## 13. Ask 邮箱管线

### 13.1 ActorCallMail

```csharp
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

internal readonly struct ActorCallMail<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public readonly TRequest Request;
    public readonly CancellationToken CancellationToken;
    public readonly LBTaskSource<TResponse> Source;

    public ActorCallMail(
        in TRequest request,
        CancellationToken cancellationToken,
        LBTaskSource<TResponse> source)
    {
        // request 参数：
        // 请求结构体数据。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // source 参数：
        // 用于完成 Ask 返回给调用方的 LBTask。
        //
        // 必要逻辑：
        // Ask 进入邮箱后不能直接返回结果。
        // 因此需要保存一个完成源，等邮件被 Pump 时再设置结果、异常或取消。
        Request = request;
        CancellationToken = cancellationToken;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
}
```

### 13.2 Ask API

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> Ask<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // Ask 进入 Actor 邮箱。
        // 所以它受邮箱背压与 ActorWorld.Pump 帧预算控制。
        if (cancellationToken.IsCancellationRequested)
        {
            return LBTask<TResponse>.FromCanceled(cancellationToken);
        }

        var source = LBTaskSource<TResponse>.Rent();
        var mail = new ActorCallMail<TRequest, TResponse>(
            in request,
            cancellationToken,
            source);

        PostResult postResult = TryPostCall(
            actorId,
            in mail);

        if (!postResult.IsSuccess)
        {
            source.SetException(new ActorCallException(
                ActorCallFailureKind.MailboxFull,
                postResult.Message));
        }

        return new LBTask<TResponse>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PostResult TryPostCall<TRequest, TResponse>(
        ActorId actorId,
        in ActorCallMail<TRequest, TResponse> mail)
        where TRequest : struct
        where TResponse : struct
    {
        // actorId 参数：
        // 目标 Actor。
        //
        // mail 参数：
        // 要写入邮箱的 ActorCall 邮件。
        //
        // 必要逻辑：
        // 该方法只负责路由到目标 Archetype。
        // 真正写入邮箱由 TypedActorStorage 完成。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return PostResult.Failure(
                ActorPostStatus.ActorNotFound,
                "Invalid ActorId.ArchetypeId.",
                PostFailureKind.InvalidActorId);
        }

        return _archetypes[actorId.ArchetypeId]
            .PostCall(actorId, in mail);
    }
}
```

### 13.3 ActorCallColumn

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Async;

namespace LayerBase.Actor;

internal sealed class ActorCallColumn<TActor, TRequest, TResponse>
    where TActor : class, IActor
    where TRequest : struct
    where TResponse : struct
{
    private readonly ActorCallInvoker<TActor, TRequest, TResponse> _invoker;

    public ActorCallColumn(
        ActorCallInvoker<TActor, TRequest, TResponse> invoker)
    {
        // invoker 参数：
        // 源生成器生成的强类型 ActorCall 调用入口。
        //
        // 必要逻辑：
        // Column 保存强类型 invoker。
        // Pump 时不需要反射，也不需要 Type 判断。
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispatch(
        TActor actor,
        in ActorCallMail<TRequest, TResponse> mail)
    {
        // actor 参数：
        // 当前被调用的 Actor 实例。
        //
        // mail 参数：
        // 当前要处理的请求邮件。
        //
        // 必要逻辑：
        // 如果请求已经取消，不调用 Handler。
        // 如果 Handler 抛异常，把异常写入 mail.Source。
        // 如果 Handler 返回 LBTask<TResponse>，把结果转接到 mail.Source。
        if (mail.CancellationToken.IsCancellationRequested)
        {
            mail.Source.SetCanceled(mail.CancellationToken);
            return;
        }

        try
        {
            LBTask<TResponse> task = _invoker(
                actor,
                in mail.Request,
                mail.CancellationToken);

            ActorCallTaskBridge.Forward(task, mail.Source);
        }
        catch (Exception ex)
        {
            mail.Source.SetException(ex);
        }
    }
}
```

---

## 14. ActorCallTaskBridge

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Async;

namespace LayerBase.Actor;

internal static class ActorCallTaskBridge
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward<TResponse>(
        LBTask<TResponse> task,
        LBTaskSource<TResponse> target)
        where TResponse : struct
    {
        // task 参数：
        // ActorCallBehaviour 返回的 LBTask。
        //
        // target 参数：
        // Ask / DelayAsk 返回给调用方的 LBTaskSource。
        //
        // 必要逻辑：
        // 如果 task 已经完成，直接同步写入结果。
        // 如果 task 未完成，挂 continuation，在完成后写入 target。
        // 该桥接逻辑应复用 LBTask 现有 Awaiter 行为。
        var awaiter = task.GetAwaiter();

        if (awaiter.IsCompleted)
        {
            CompleteImmediately(awaiter, target);
            return;
        }

        awaiter.OnCompleted(() =>
        {
            CompleteImmediately(task.GetAwaiter(), target);
        });
    }

    private static void CompleteImmediately<TResponse>(
        LBTask<TResponse>.Awaiter awaiter,
        LBTaskSource<TResponse> target)
        where TResponse : struct
    {
        // awaiter 参数：
        // LBTask<TResponse> 的 awaiter。
        //
        // target 参数：
        // 需要被完成的目标完成源。
        //
        // 必要逻辑：
        // GetResult 可能抛异常。
        // 抛异常时需要转成 target.SetException。
        try
        {
            TResponse response = awaiter.GetResult();
            target.SetResult(response);
        }
        catch (OperationCanceledException ex)
        {
            target.SetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            target.SetException(ex);
        }
    }
}
```

---

## 15. DelayPost / DelayAsk 时间轮

### 15.1 时间轮职责

Actor 延迟任务由 `ActorDelayScheduler` 管理。

它与 Event 系统的 `DelayPublisherManager` 分离。

原因：

- `DelayPublisherManager` 面向 Event Post。
- Actor Delay 需要携带 ActorId。
- DelayAsk 需要携带 `LBTaskSource<TResponse>`。
- Actor 延迟任务必须处理 Actor 销毁、Generation 失效和 ActorWorld Dispose。
- Actor 延迟任务到期后应进入 Actor 自己的 Post / Ask 管线。

### 15.2 ActorDelayScheduler 字段

```csharp
namespace LayerBase.Actor;

internal sealed class ActorDelayScheduler
{
    private readonly ActorWorld _world;
    private readonly ActorTimeWheel _timeWheel;

    public ActorDelayScheduler(
        ActorWorld world,
        ActorTimeWheelOptions options)
    {
        // world 参数：
        // 当前延迟任务所属的 ActorWorld。
        //
        // options 参数：
        // 时间轮配置。
        //
        // 必要逻辑：
        // ActorDelayScheduler 不执行业务逻辑。
        // 它只在任务到期后调用 ActorWorld.Post 或 ActorWorld.Ask。
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _timeWheel = new ActorTimeWheel(options);
    }

    public DelayPostHandle Schedule(IActorDelayTask task, float delaySeconds)
    {
        // task 参数：
        // 要被延迟执行的 Actor 延迟任务。
        //
        // delaySeconds 参数：
        // 延迟秒数。
        //
        // 必要逻辑：
        // 任务被放入时间轮。
        // 到期时由 Tick 取出执行。
        return _timeWheel.Schedule(task, delaySeconds);
    }

    public void Tick(float deltaTime)
    {
        // deltaTime 参数：
        // 当前帧经过的时间。
        //
        // 必要逻辑：
        // 时间轮推进后，会执行到期任务。
        // 到期任务只转为 Post / Ask，不直接调用 ActorBehaviour。
        _timeWheel.Tick(deltaTime);
    }

    public void Clear()
    {
        // 必要逻辑：
        // ActorWorld Dispose 或 Runtime Dispose 时调用。
        // 清理所有延迟任务，并取消 DelayAsk 返回的 LBTask。
        _timeWheel.Clear();
    }
}
```

### 15.3 DelayPostTask

```csharp
namespace LayerBase.Actor;

internal sealed class DelayPostTask<TEvent> : IActorDelayTask
    where TEvent : struct
{
    private readonly ActorWorld _world;
    private readonly ActorId _actorId;
    private readonly TEvent _value;
    private readonly ActorPostPolicy? _postPolicy;
    private readonly ActorMailFullPolicy? _fullPolicy;

    public DelayPostTask(
        ActorWorld world,
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        // world 参数：
        // 任务所属 ActorWorld。
        //
        // actorId 参数：
        // 到期后要投递的目标 Actor。
        //
        // value 参数：
        // 到期后投递的事件。
        //
        // postPolicy 参数：
        // 到期后 Post 使用的投递策略。
        //
        // fullPolicy 参数：
        // 到期后 Post 使用的邮箱满策略。
        //
        // 必要逻辑：
        // DelayPost 到期后只转成普通 Post。
        // 因此它仍然受背压和帧预算保护。
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorId = actorId;
        _value = value;
        _postPolicy = postPolicy;
        _fullPolicy = fullPolicy;
    }

    public void Execute()
    {
        // 必要逻辑：
        // 到期后进入普通 Post 管线。
        _ = _world.Post(
            _actorId,
            in _value,
            _postPolicy,
            _fullPolicy);
    }

    public void Cancel()
    {
        // 必要逻辑：
        // DelayPost 没有返回结果。
        // 取消时不需要完成 LBTask。
    }
}
```

### 15.4 DelayAskTask

```csharp
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

internal sealed class DelayAskTask<TRequest, TResponse> : IActorDelayTask
    where TRequest : struct
    where TResponse : struct
{
    private readonly ActorWorld _world;
    private readonly ActorId _actorId;
    private readonly TRequest _request;
    private readonly CancellationToken _cancellationToken;
    private readonly LBTaskSource<TResponse> _source;

    public DelayAskTask(
        ActorWorld world,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken,
        LBTaskSource<TResponse> source)
    {
        // world 参数：
        // 任务所属 ActorWorld。
        //
        // actorId 参数：
        // 到期后要请求的目标 Actor。
        //
        // request 参数：
        // 到期后写入邮箱的请求。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // source 参数：
        // DelayAsk 返回给调用方的完成源。
        //
        // 必要逻辑：
        // DelayAsk 延迟期间不占用 Actor 邮箱。
        // 到期后才调用 Ask。
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _actorId = actorId;
        _request = request;
        _cancellationToken = cancellationToken;
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public void Execute()
    {
        // 必要逻辑：
        // 如果延迟期间已经取消，不进入 Actor 邮箱。
        // 否则执行 Ask，并把 Ask 的结果转接到 DelayAsk 的 source。
        if (_cancellationToken.IsCancellationRequested)
        {
            _source.SetCanceled(_cancellationToken);
            return;
        }

        LBTask<TResponse> task = _world.Ask<TRequest, TResponse>(
            _actorId,
            in _request,
            _cancellationToken);

        ActorCallTaskBridge.Forward(task, _source);
    }

    public void Cancel()
    {
        // 必要逻辑：
        // DelayAsk 有返回值。
        // 取消时必须完成返回给调用方的 LBTask。
        _source.SetCanceled(_cancellationToken);
    }
}
```

---

## 16. LayerRuntime 生命周期集成

### 16.1 ActorWorld 创建

当前 `LayerRuntime` 构造函数中已经创建：

```text
Actors = new ActorWorld(this)
```

ActorCall 继续沿用该模型。

### 16.2 Build 阶段

`LayerRuntime.LayersBuilder.Build()` 当前顺序包括：

```text
Install LayerBaseSynchronizationContext
Tasks = new WorldTaskApi(...)
LayerChain.Prebuild
InitializeScheduler
InitializeTimer
InitializeDelay
BuildServiceProvider
LayerChain.Build
```

ActorWorld 需要新增：

```csharp
internal void PrepareRuntimeBuild()
internal void CompleteRuntimeBuild()
```

建议插入点：

```text
BuildServiceProvider 之后
LayerChain.Build 之前或之后
```

推荐：

```text
BuildServiceProvider
Actors.PrepareRuntimeBuild()
LayerChain.Build
Actors.CompleteRuntimeBuild()
```

原因：

- ActorWorld 已经在 Runtime 构造时存在。
- Build 阶段需要拿到 Runtime 的 PolicyTable。
- Actor 延迟调度器需要在 Runtime 完成基础设施初始化后可用。
- Actor 不需要参与 LayerChain Build，但需要知道 Runtime 已经 Build 完成。

### 16.3 Pump 阶段

当前 `LayerRuntime.Pump` 已经在 `PostScheduler.Pump` 后创建 ActorBudget 并调用 `Actors.Pump(...)`。

Actor Delay 应合入 `Actors.Pump` 内部最前面：

```text
Actors.Pump
    -> ActorDelayScheduler.Tick(deltaTime)
    -> Pump ActorBehaviour / ActorCall mails
    -> SweepPendingDestroy
    -> Pump Actor FixedUpdate
    -> Pump Actor Update
    -> Pump Actor LateUpdate
    -> SweepPendingDestroy
```

这样 `DelayPost` / `DelayAsk` 到期后进入邮箱，仍受同一帧预算保护。

### 16.4 Dispose 阶段

`LayerRuntime.Dispose()` 需要增加：

```csharp
Actors.Dispose();
```

`ActorWorld.Dispose()` 负责：

- 取消所有 DelayAsk。
- 清空 DelayPost。
- 清空所有 Actor 邮箱。
- Sweep 所有 Alive Actor。
- 调用 `IDestroy`。
- 归还池化 Actor。
- 清空 QueryCache。
- 清空 Debug 状态。

---

## 17. ActorWorld 生命周期控制

### 17.1 ActorWorld 状态

```csharp
namespace LayerBase.Actor;

internal enum ActorWorldState
{
    Created = 0,
    Building = 1,
    Running = 2,
    Stopping = 3,
    Disposed = 4
}
```

### 17.2 ActorWorld 生命周期 API

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld : IDisposable
{
    private ActorWorldState _state;

    internal void PrepareRuntimeBuild()
    {
        // 必要逻辑：
        // Runtime Build 开始后，ActorWorld 进入 Building。
        // 此阶段允许注册配置，但不建议 Pump。
        if (_state == ActorWorldState.Disposed)
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }

        _state = ActorWorldState.Building;
    }

    internal void CompleteRuntimeBuild()
    {
        // 必要逻辑：
        // Runtime Build 完成后，ActorWorld 进入 Running。
        // DelayPost、Ask、Actor Pump 都应该只在 Running 后使用。
        if (_state == ActorWorldState.Disposed)
        {
            throw new ObjectDisposedException(nameof(ActorWorld));
        }

        _state = ActorWorldState.Running;
    }

    internal void RuntimeStop()
    {
        // 必要逻辑：
        // Runtime 停止时不再接受新 Actor 消息。
        // 已存在的 DelayAsk 应取消，避免调用方悬挂。
        if (_state == ActorWorldState.Disposed)
        {
            return;
        }

        _state = ActorWorldState.Stopping;
        DelayScheduler.Clear();
    }

    public void Dispose()
    {
        // 必要逻辑：
        // ActorWorld 释放时必须完整清理 Actor、邮箱、延迟任务和池。
        if (_state == ActorWorldState.Disposed)
        {
            return;
        }

        _state = ActorWorldState.Disposed;

        DelayScheduler.Clear();
        DestroyAllActorsImmediately();
        ClearAllMailboxes();
        ClearQueryCache();
        ClearPools();
    }
}
```

### 17.3 Actor 生命周期顺序

单个 Actor 生命周期顺序：

```text
CreateActor<TActor>
    -> 构造实例
    -> 读取生成元数据
    -> 分配 Storage Slot
    -> ActorContext 注入
    -> RegisterLifecycleInterfaces
    -> IStart.Start
    -> Running

DestroyActor(actorId)
    -> MarkPendingDestroy
    -> 拒绝新 Post / Ask / DispatchNow / ImmediatelyAsk
    -> SweepPendingDestroy
    -> 清空邮箱
    -> 取消挂起 Ask
    -> IDestroy.Destroy
    -> Remove lifecycle handles
    -> 归还池或释放引用
    -> generation 增加
```

---

## 18. 系统层到实体层接口

### 18.1 LayerRuntime 直接入口

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TActor CreateActor<TActor>(bool usePool = false)
        where TActor : class, IActor, new()
    {
        // usePool 参数：
        // true 表示从 ActorPool<TActor> 租用实例。
        // false 表示直接 new TActor()。
        //
        // 必要逻辑：
        // LayerRuntime 是系统层访问 ActorWorld 的最直接入口。
        return Actors.CreateActor<TActor>(usePool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> AskActor<TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求数据。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // Runtime 入口直接转发 ActorWorld。
        return Actors.Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

### 18.2 Layer 入口

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Layers;

namespace LayerBase;

public static class LayerActorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActorWorld Actors(this Layer layer)
    {
        // layer 参数：
        // 当前系统层实例。
        //
        // 必要逻辑：
        // Layer 已经持有 OwnerContext。
        // 这里直接访问 OwnerContext.Actors，不需要字典查询。
        if (layer == null)
        {
            throw new ArgumentNullException(nameof(layer));
        }

        return layer.OwnerContext?.Actors
               ?? throw new InvalidOperationException("Layer not attached to LayerRuntime.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this Layer layer,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // layer 参数：
        // 发起请求的 Layer。
        //
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // 系统层到实体层的常规入口。
        return layer.Actors().Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

### 18.3 IService 入口

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class ServiceActorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActorWorld Actors(this IService service)
    {
        // service 参数：
        // 当前 Service 实例。
        //
        // 必要逻辑：
        // Service 在 Layer Build / ServiceProvider Resolve 阶段已通过 ServiceLayerBinder 绑定到 Layer 或 Runtime。
        // 这里通过 Binder 拿到所属 LayerRuntime，再访问 ActorWorld。
        // 该入口不应使用反射。
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        var binding = ServiceLayerBinder.RequireBinding(service);
        return binding.Runtime.Actors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this IService service,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // service 参数：
        // 发起请求的服务实例。
        //
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // Service 层调用实体层时，不需要先拿 Layer。
        // 它可以通过绑定信息直达 Runtime.Actors。
        return service.Actors().Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

### 18.4 ILayerContext 入口

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.DI;

namespace LayerBase;

public static class LayerContextActorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ActorWorld Actors(this ILayerContext context)
    {
        // context 参数：
        // 当前 LayerContext 模块实例。
        //
        // 必要逻辑：
        // ILayerContext 不直接拥有 Runtime。
        // 它通过 ServiceLayerBinder 绑定到所属 Layer 或 Runtime。
        // 这里对齐现有 GetService<T>() 的使用方式。
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var binding = ServiceLayerBinder.RequireBinding(context);
        return binding.Runtime.Actors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LBTask<TResponse> AskActor<TRequest, TResponse>(
        this ILayerContext context,
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : struct
        where TResponse : struct
    {
        // context 参数：
        // 发起请求的 ILayerContext 模块。
        //
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // ILayerContext 通常承载系统内更细粒度模块。
        // 它需要能调用实体层 Actor，但不应该自己声明 [Call]。
        return context.Actors().Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

---

## 19. Actor 到系统层接口

Actor 内部通过 `ActorContext` 访问 Runtime。

### 19.1 ActorContext 扩展

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;
using LayerBase.Layers;

namespace LayerBase.Actor;

public readonly struct ActorContext
{
    public ActorId ActorId { get; }

    internal ActorWorld World { get; }

    internal LayerRuntime Runtime { get; }

    internal ActorContext(
        ActorWorld world,
        ActorId actorId)
    {
        // world 参数：
        // 当前 Actor 所属的 ActorWorld。
        //
        // actorId 参数：
        // 当前 Actor 的运行时身份。
        //
        // 必要逻辑：
        // ActorWorld 持有 LayerRuntime。
        // ActorContext 需要保留 Runtime，方便 Actor 访问系统层能力。
        World = world;
        Runtime = world.Runtime
            ?? throw new InvalidOperationException("ActorWorld is not attached to LayerRuntime.");
        ActorId = actorId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TService GetService<TService>()
        where TService : class
    {
        // TService 泛型参数：
        // 要从 Runtime ServiceProvider 中获取的服务类型。
        //
        // 必要逻辑：
        // 允许 Actor 读取 Runtime 级服务。
        // 不建议在高频 ActorBehaviour 中频繁调用。
        // 高频依赖应在 Actor 初始化时缓存。
        return Runtime.GetService<TService>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> CallLayer<TLayer, TRequest, TResponse>(
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TLayer : Layer
        where TRequest : struct
        where TResponse : struct
    {
        // TLayer 泛型参数：
        // 目标系统层类型。
        //
        // TRequest 泛型参数：
        // 请求类型。
        //
        // TResponse 泛型参数：
        // 响应类型。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 取消等待时使用。
        //
        // 必要逻辑：
        // 允许具体 Actor 反向请求系统层。
        // 该路径复用现有 LayerRuntime.CallAsync 缓存机制。
        return Runtime.CallAsync<TLayer, TRequest, TResponse>(
            request,
            cancellationToken);
    }
}
```

---

## 20. Actor Call API

### 20.1 Ask

```csharp
public LBTask<TResponse> Ask<TRequest, TResponse>(
    ActorId actorId,
    in TRequest request,
    CancellationToken cancellationToken = default)
    where TRequest : struct
    where TResponse : struct;
```

语义：

```text
Ask = 进入 Actor 邮箱的 Request / Response
```

### 20.2 ImmediatelyAsk

```csharp
public LBTask<TResponse> ImmediatelyAsk<TRequest, TResponse>(
    ActorId actorId,
    in TRequest request,
    CancellationToken cancellationToken = default)
    where TRequest : struct
    where TResponse : struct;
```

语义：

```text
ImmediatelyAsk = 不进邮箱，直接调用 ActorCallBehaviour
```

### 20.3 DelayAsk

```csharp
public LBTask<TResponse> DelayAsk<TRequest, TResponse>(
    ActorId actorId,
    in TRequest request,
    float delaySeconds,
    CancellationToken cancellationToken = default)
    where TRequest : struct
    where TResponse : struct;
```

语义：

```text
DelayAsk = 先进入 ActorDelayScheduler，到期后进入 Ask
```

### 20.4 Actor Call

第一版 Actor Call 是类型标注版 Ask。

```csharp
public LBTask<TResponse> Call<TActor, TRequest, TResponse>(
    ActorId actorId,
    in TRequest request,
    CancellationToken cancellationToken = default)
    where TActor : class, IActor
    where TRequest : struct
    where TResponse : struct;
```

实现：

```csharp
using System.Runtime.CompilerServices;
using System.Threading;
using LayerBase.Async;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LBTask<TResponse> Call<TActor, TRequest, TResponse>(
        ActorId actorId,
        in TRequest request,
        CancellationToken cancellationToken = default)
        where TActor : class, IActor
        where TRequest : struct
        where TResponse : struct
    {
        // TActor 泛型参数：
        // 期望调用的 Actor 类型。
        //
        // TRequest 泛型参数：
        // 请求类型。
        //
        // TResponse 泛型参数：
        // 响应类型。
        //
        // actorId 参数：
        // 目标 Actor。
        //
        // request 参数：
        // 请求结构体。
        //
        // cancellationToken 参数：
        // 调用方取消等待时使用。
        //
        // 必要逻辑：
        // 第一版 Call 不做自动查找 Actor。
        // 它只在 ActorId 已知时进行类型约束调用。
        // 内部可转发到 Ask，但应在 Storage 层校验 Actor 类型是否匹配 TActor。
        return Ask<TRequest, TResponse>(
            actorId,
            in request,
            cancellationToken);
    }
}
```

后续再考虑：

```text
Call<TActor, TRequest, TResponse>(in TRequest request)
CallGroup<TGroup, TRequest, TResponse>(in TRequest request)
```

第一版不实现自动路由，避免多 Actor 响应冲突。

---

## 21. DispatchNow

### 21.1 API

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DispatchResult DispatchNow<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        // actorId 参数：
        // 目标 Actor。
        //
        // value 参数：
        // 要立即派发的事件。
        //
        // 必要逻辑：
        // DispatchNow 直接调用 ActorBehaviour。
        // 不进入邮箱，不参与背压，不消耗帧预算。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return DispatchResult.Failure(
                DispatchFailureKind.InvalidActorId,
                "Invalid ActorId.ArchetypeId.");
        }

        return _archetypes[actorId.ArchetypeId]
            .DispatchNow(actorId, in value);
    }
}
```

### 21.2 适用范围

`DispatchNow` 适合：

- 框架内部强同步派发。
- 初始化阶段。
- 测试。
- 少量强一致逻辑。

不建议：

- 普通业务到处替代 Post。
- 高频大规模广播。
- 在 ActorBehaviour 中递归 DispatchNow 导致调用栈过深。

---

## 22. 失败与异常语义

### 22.1 DispatchResult

```csharp
namespace LayerBase.Actor;

public readonly struct DispatchResult
{
    public readonly bool IsSuccess;
    public readonly DispatchFailureKind FailureKind;
    public readonly string? Message;
    public readonly Exception? Exception;

    private DispatchResult(
        bool isSuccess,
        DispatchFailureKind failureKind,
        string? message,
        Exception? exception)
    {
        // isSuccess 参数：
        // 是否成功。
        //
        // failureKind 参数：
        // 失败类型。
        //
        // message 参数：
        // 调试消息。
        //
        // exception 参数：
        // Handler 抛出的异常。
        IsSuccess = isSuccess;
        FailureKind = failureKind;
        Message = message;
        Exception = exception;
    }

    public static DispatchResult Success()
    {
        return new DispatchResult(
            true,
            DispatchFailureKind.None,
            null,
            null);
    }

    public static DispatchResult Failure(
        DispatchFailureKind failureKind,
        string message,
        Exception? exception = null)
    {
        return new DispatchResult(
            false,
            failureKind,
            message,
            exception);
    }
}
```

### 22.2 ActorCallFailure

```csharp
using LayerBase.Async;

namespace LayerBase.Actor;

internal static class ActorCallFailure
{
    public static LBTask<TResponse> InvalidActor<TResponse>(
        ActorCallFailureKind kind)
        where TResponse : struct
    {
        // kind 参数：
        // Actor 调用失败类型。
        //
        // 必要逻辑：
        // Ask / ImmediatelyAsk 失败时不能返回 null。
        // 必须返回一个完成为异常的 LBTask。
        return LBTask<TResponse>.FromException(
            new ActorCallException(kind));
    }

    public static LBTask<TResponse> InvalidActor<TResponse>(
        ActorId actorId,
        ActorCallFailureKind kind)
        where TResponse : struct
    {
        // actorId 参数：
        // 失败的目标 ActorId。
        //
        // kind 参数：
        // 失败类型。
        //
        // 必要逻辑：
        // 携带 ActorId 有利于 Debug。
        return LBTask<TResponse>.FromException(
            new ActorCallException(kind, actorId));
    }

    public static LBTask<TResponse> Unsupported<TResponse, TRequest, TExpectedResponse>()
        where TResponse : struct
        where TRequest : struct
        where TExpectedResponse : struct
    {
        // TRequest 泛型参数：
        // 请求类型。
        //
        // TExpectedResponse 泛型参数：
        // 响应类型。
        //
        // 必要逻辑：
        // 目标 Actor 不支持该 Request / Response 路由时返回异常任务。
        return LBTask<TResponse>.FromException(
            new ActorCallException(
                ActorCallFailureKind.UnsupportedRequest,
                typeof(TRequest),
                typeof(TExpectedResponse)));
    }
}
```

---

## 23. Enable / PendingDestroy 语义

### 23.1 Enable

建议第一版：

```text
Enable 只控制生命周期，不默认阻断消息。
```

也就是说：

- Disabled Actor 不执行 `IUpdate`。
- Disabled Actor 不执行 `IFixedUpdate`。
- Disabled Actor 不执行 `ILateUpdate`。
- Disabled Actor 默认仍可 `Post`。
- Disabled Actor 默认仍可 `Ask`。
- Disabled Actor 默认仍可 `DispatchNow`。
- Disabled Actor 默认仍可 `ImmediatelyAsk`。

如果业务需要禁用消息，应通过 `ActorMailDisabledPolicy` 或 Actor 自己的状态判断完成。

### 23.2 PendingDestroy

`PendingDestroy` 必须阻断所有新入口。

| API | PendingDestroy 结果 |
|---|---|
| `Post` | 失败 |
| `DelayPost` 到期后 | 失败或丢弃 |
| `DispatchNow` | 失败 |
| `Ask` | 返回失败 LBTask |
| `DelayAsk` 到期后 | 返回失败 LBTask |
| `ImmediatelyAsk` | 返回失败 LBTask |
| `Call` | 返回失败 LBTask |

---

## 24. 热路径性能约束

### 24.1 禁止事项

热路径禁止：

```text
反射
MethodInfo.Invoke
Delegate.DynamicInvoke
Dictionary<Type, ...>
ConcurrentDictionary<Type, ...>
LINQ
闭包分配
装箱
字符串拼接
异常作为普通控制流
```

### 24.2 允许事项

热路径允许：

```text
泛型静态字段
数组索引
uint 边界检查
Volatile.Read
AggressiveInlining 小方法
强类型 delegate 调用
```

### 24.3 关键路径

```text
ImmediatelyAsk:
ActorWorld -> BehaviourArchetype -> TypedActorStorage<TActor> -> routeId -> invoker array -> ActorCallInvoker

Ask:
ActorWorld -> BehaviourArchetype -> TypedActorStorage<TActor> -> call mailbox -> dirty slot -> Pump -> invoker array -> ActorCallInvoker

Layer/Service/ILayerContext -> Actor:
Layer.OwnerContext.Actors
ServiceLayerBinder binding -> Runtime.Actors
```

---

## 25. 文件结构

建议新增：

```text
LayerBase/Actor/Core/ActorCallBehaviourAttribute.cs
LayerBase/Actor/Call/ActorCallRoute.cs
LayerBase/Actor/Call/ActorCallEntry.cs
LayerBase/Actor/Call/ActorCallMail.cs
LayerBase/Actor/Call/ActorCallColumn.cs
LayerBase/Actor/Call/ActorCallFailure.cs
LayerBase/Actor/Call/ActorCallException.cs
LayerBase/Actor/Call/ActorCallTaskBridge.cs
LayerBase/Actor/Delay/ActorDelayScheduler.cs
LayerBase/Actor/Delay/ActorTimeWheel.cs
LayerBase/Actor/Delay/IActorDelayTask.cs
LayerBase/Actor/Delay/DelayPostHandle.cs
LayerBase/Actor/Delay/DelayPostTask.cs
LayerBase/Actor/Delay/DelayAskTask.cs
LayerBase/Actor/Dispatch/DispatchResult.cs
LayerBase/Actor/Dispatch/DispatchFailureKind.cs
LayerBase/Actor/Storage/ActorWorld.Ask.cs
LayerBase/Actor/Storage/ActorWorld.Call.cs
LayerBase/Actor/Storage/ActorWorld.Delay.cs
LayerBase/Actor/Storage/ActorWorld.Dispatch.cs
LayerBase/Actor/Storage/ActorWorld.Lifecycle.cs
LayerBase/Actor/Extensions/LayerActorExtensions.cs
LayerBase/Actor/Extensions/ServiceActorExtensions.cs
LayerBase/Actor/Extensions/LayerContextActorExtensions.cs
```

修改：

```text
LayerBase/Actor/Core/ActorContext.cs
LayerBase/Actor/Core/IGeneratedActorMeta.cs
LayerBase/Actor/Meta/ActorTypeMeta.cs
LayerBase/Actor/Meta/ActorTypeMetaBuilder.cs
LayerBase/Actor/Storage/ActorWorld.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
LayerBase/Actor/Storage/TypedActorStorage.cs
LayerBase/Actor/Storage/BehaviourArchetype.cs
LayerBase.Generator/LayerBase.Generator/ActorBehaviourGenerator.cs
LayerBase/Application/LayerRuntime.cs
```

---

## 26. 分阶段实现

### Phase 1：ActorCallBehaviour 元数据

完成：

- 新增 `[ActorCallBehaviour]`。
- 生成器扫描 `[ActorCallBehaviour]`。
- 只接受异步统一签名。
- 扩展 `ActorTypeMetaBuilder`。
- 扩展 `ActorTypeMeta<TActor>`。
- 扩展 `TypedActorStorage<TActor>` Call 路由数组。

验收：

- 合法签名通过。
- 非法签名编译时报错。
- 重复 Request / Response 编译或构建时报错。
- 不影响现有 `[ActorBehaviour]`。

### Phase 2：ImmediatelyAsk / DispatchNow

完成：

- `DispatchNow<TEvent>`。
- `ImmediatelyAsk<TRequest, TResponse>`。
- Storage 直达调用。
- 失败结果与异常包装。

验收：

- 不进入邮箱。
- 不消耗帧预算。
- 不受背压影响。
- ActorId 失效时失败。
- PendingDestroy 时失败。
- Handler 抛异常时返回失败结果或失败 LBTask。

### Phase 3：Ask 邮箱管线

完成：

- `ActorCallMail<TRequest,TResponse>`。
- `ActorCallColumn<TActor,TRequest,TResponse>`。
- Call 邮箱列。
- DirtySlot 接入。
- `Ask<TRequest,TResponse>`。

验收：

- Ask Pump 前不执行。
- Ask Pump 后执行。
- Ask 受帧预算影响。
- 取消后不调用 Handler。
- Handler 异步完成后调用方能拿到结果。

### Phase 4：DelayPost / DelayAsk

完成：

- `ActorDelayScheduler`。
- `ActorTimeWheel`。
- `DelayPost`。
- `DelayAsk`。
- `DelayPostHandle`。
- ActorWorld Dispose 清理延迟任务。

验收：

- 延迟未到不进入邮箱。
- 到期后进入 Post / Ask。
- DelayAsk 取消后返回 Canceled。
- Actor 销毁后 DelayAsk 返回失败。
- Runtime Dispose 时 DelayAsk 不悬挂。

### Phase 5：Runtime / Service / LayerContext 接口

完成：

- `LayerRuntime.CreateActor / AskActor`。
- `Layer.Actors()`。
- `IService.Actors()`。
- `ILayerContext.Actors()`。
- `ActorContext.CallLayer`。
- `ActorContext.GetService`。

验收：

- Layer 可以调用 Actor。
- IService 可以调用 Actor。
- ILayerContext 可以调用 Actor。
- Actor 可以调用 Layer Call。
- Actor 可以访问 Runtime Service。
- 无反射。
- 热路径无字典查询。

---

## 27. 测试计划

### 27.1 生成器测试

- `[ActorCallBehaviour]` 合法签名通过。
- 返回 `TResponse` 报错。
- 返回 `Task<T>` 报错。
- 缺少 `CancellationToken` 报错。
- Request 不是 struct 报错。
- Response 不是 struct 报错。
- 重复 Request / Response 报错。

### 27.2 ImmediatelyAsk 测试

- 立即调用 Handler。
- 不进入邮箱。
- 不消耗帧预算。
- 返回 `LBTask<TResponse>`。
- 取消令牌已取消时返回 Canceled。
- UnsupportedRequest 返回失败 LBTask。
- Handler 抛异常返回失败 LBTask。

### 27.3 Ask 测试

- Ask 进入邮箱。
- Pump 前不调用 Handler。
- Pump 后调用 Handler。
- 受帧预算限制。
- 取消后不调用 Handler。
- Handler 返回异步 LBTask 时能正确转接。

### 27.4 DelayPost / DelayAsk 测试

- 延迟未到不进入邮箱。
- 到期后进入普通路径。
- DelayAsk 取消后不进入邮箱。
- Runtime Dispose 后 DelayAsk 完成取消。
- Actor Destroy 后 DelayAsk 返回失败。

### 27.5 系统层接口测试

- Layer 调用 Actor Ask。
- IService 调用 Actor Ask。
- ILayerContext 调用 Actor Ask。
- Actor 调用 LayerRuntime.CallAsync。
- Actor 调用 Runtime.GetService。
- 多 Runtime 下不会串 Runtime。
- Runtime Reset 后缓存清空。

---

## 28. Definition of Done

完成标准：

- `[ActorCallBehaviour]` 完全由源生成器绑定。
- Actor Call 不使用反射。
- ImmediatelyAsk 热路径不使用字典。
- Ask Pump 热路径不使用字典。
- Layer / IService / ILayerContext 都能访问 ActorWorld。
- Actor 能反向访问 LayerRuntime Call 和 Runtime Service。
- ActorWorld 生命周期接入 LayerRuntime Build / Pump / Dispose。
- DelayPost / DelayAsk 由 Actor 自己的时间轮控制。
- DelayAsk 不会悬挂。
- PendingDestroy 语义完整。
- Dispose 语义完整。
- Debug 能看到 Actor Call route。
- 测试覆盖成功、失败、取消、销毁、延迟、Runtime Reset。

---

## 29. 非目标

第一版不做：

- Group 自动 Actor Call。
- 多 Actor 聚合响应。
- Broadcast Ask。
- 跨 ActorWorld 自动路由。
- 跨线程直接执行 ActorBehaviour。
- 跨线程直接执行 ActorCallBehaviour。
- 网络同步协议。
- 空间查询。
- AOI。
- Actor 状态快照。
- Unity GameObject 生命周期绑定。

---

## 30. 最终调用模型

```text
系统层 -> 实体层：

Layer / IService / ILayerContext
    -> ActorWorld.Ask / Post / DelayPost / DelayAsk / DispatchNow / ImmediatelyAsk
        -> ActorId
            -> TypedActorStorage<TActor>
                -> [ActorBehaviour]
                -> [ActorCallBehaviour]

实体层 -> 系统层：

Actor
    -> ActorContext
        -> LayerRuntime
            -> CallAsync<TLayer,TRequest,TResponse>
            -> GetService<TService>
```

核心原则：

- `[ActorBehaviour]` 处理单向事件。
- `[ActorCallBehaviour]` 处理 Request / Response。
- 普通路径进入邮箱，受背压和帧预算保护。
- Immediate 路径绕过邮箱，直接调用。
- Delay 路径先进入时间轮，到期后转入普通路径。
- 源生成器负责绑定。
- Runtime 热路径只做泛型静态 ID + 数组索引 + 强类型委托调用。
