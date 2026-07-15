# 01 LayerBase 强制架构、AOT、性能与代码规范

> **规范地位：** 本文是所有迁移任务和实现提交的强制门禁。任何阶段文档、代码实现、测试或 `faster` 现有结构与本文冲突时，必须先停止实施并提交架构讨论，不得通过局部兼容层绕过。  
> **代码基线：** `7dee16c46d72a68f502554f693aed0c314b22be3`  
> **复用来源：** Git 分支 `faster`

---

## 1. 十二条不可破坏的架构公理

```text
1. Layer 是最高级业务管理结构。
2. LayersBuilder.Push 是唯一 LayerIndex 和 Layer 顺序来源。
3. Scope 是 Layer 之下的执行与资源隔离维度。
4. 每个 Service / Context 必须同时具有 OwnerLayer 和 OwnerScope。
5. Scope 间只允许 ScopeEvent MPSC 与 ScopeCall MPSC。
6. EventCenter 每 Scope 一份，但 Handler 顺序继续使用 Push LayerIndex。
7. DI / Mount / Provide / From 只能发生在同 Scope、同 Layer。
8. 本地 Call 以 CurrentScope + Request + Response 寻址，不包含 Layer。
9. ActorWorld 不属于 ScopeRuntime，只由 MainScope Owner Thread 写入和推进。
10. PostFromAnyThread 与 SubscribeParallel 必须删除。
11. Build 能确定的结构必须在 Freeze 前转成 Slot、Offset、Count 和 Invoker。
12. 实现前必须先检查 faster，可复用代码不得重复实现。
```

这些公理不可被以下理由绕过：

```text
为了统一模型
为了少写一层对象
为了兼容旧 API
为了先跑通测试
为了简化 Generator
为了避免修改现有 Plan
```

---

## 2. Layer-first 与 Scope-local 强制边界

### 2.1 Layer 管理内容

Layer 必须继续管理：

```text
Service 注册
Context 归属
DI 范围
Mount 范围
Provide / From 范围
Tool 范围
Event Handler 的 Layer 顺序
Lifecycle 顺序
业务模块归属
```

### 2.2 Scope 管理内容

Scope 负责：

```text
Owner Thread
TickRate / FixedAccumulator
EventCenter
PostScheduler
Timer / Delay
EcsWorld / EcsScheduler
SynchronizationContext
ScopeLocalCallRegistry
ScopeEventInbox
ScopeCallInbox
Scope-local Fault / Diagnostics
```

### 2.3 允许扁平化，不允许所有权反转

Build 后允许：

```text
ScopeLayerSlice[]
LayerProviderRuntime[]
ObjectSlot[]
Invoker[]
Range
BitSet
```

禁止因此宣称：

```text
Scope 拥有 Layer
Layer 只属于 MainScope
CustomScope 没有 Layer
Service 只属于 Scope
ObjectPlan 是生命周期的业务根
```

---

## 3. Build、Activate、Running、Stop 分期

### 3.1 编译期

允许：

```text
源生成器
Analyzer
语义模型
生成 Factory / Invoker / Setter / Route
生成 AOT 闭合泛型
```

禁止 Runtime Assembly 依赖：

```text
Microsoft.CodeAnalysis
CSharpSyntaxTree
SyntaxTree
SemanticModel
Compilation
```

### 3.2 Runtime Build 冷路径

允许：

```text
Dictionary
List
HashSet
稳定排序
冲突检查
拓扑分析
受控反射
容量计算
Plan 合并
```

必须完成：

```text
LayerIndex
ScopeId
ProviderSlot
ServiceSlot
ContextSlot
ToolSlot
ObjectSlot
LocalCallId
ScopeEvent RouteId
ScopeCall RouteId
Handler / Lifecycle Range
数组长度和队列容量
```

### 3.3 Activate

必须在目标 Scope Owner Thread：

```text
创建 Scope 本地资源
按 LayerIndex创建 Provider
创建 Service / Context
Attach Binding
Mount
Provide / From
Event Handler 注册
LocalCall Handler 绑定
Initialize / PostBuild
Prewarm / Freeze
RuntimeStart
```

### 3.4 Running 热路径

只允许：

```text
数组下标
Span / ReadOnlySpan
readonly struct
直接 Delegate / Function Pointer
BitSet
对象池
有界 RingQueue
预计算 Range
```

禁止：

```text
Type→对象动态查找
Dictionary / List / HashSet 路由
LINQ
反射调用
字符串 Key 查找
运行期排序
运行期计算结构长度
```

### 3.5 Stop / Dispose

```text
控制命令必须通过 ScopeCall
目标 Scope Owner Thread执行
RuntimeStop / Dispose 按 Layer 逆序
MainScope 最后停止
ActorWorld 最后释放
```

---

## 4. IL2CPP 与 AOT 强制规范

### 4.1 禁止依赖 JIT

禁止：

```text
Reflection.Emit
DynamicMethod
Expression.Compile
运行期生成泛型方法
运行期生成代理类型
运行期编译表达式或语法树
```

### 4.2 禁止动态语言路径

禁止：

```text
dynamic
ExpandoObject
DLR Binder
运行期 duck typing
```

### 4.3 泛型必须 AOT 可达

必须通过以下方式确保闭合泛型：

```text
源生成直接调用
显式注册
Preserve
link.xml
AOT 测试场景
```

不得只在：

```text
MakeGenericType
MakeGenericMethod
Activator.CreateInstance(Type)
MethodInfo.Invoke
```

路径中出现。

### 4.4 非泛型桥接

需要非泛型调用时优先：

```text
源生成 Invoker
预注册 Delegate
显式非泛型接口
稳定 Slot
```

---

## 5. 游戏引擎友好性

### 5.1 禁止 ModuleInitializer

禁止：

```text
ModuleInitializerAttribute
程序集加载即注册
模块加载即创建线程
模块加载即扫描程序集
```

原因：

```text
Unity / IL2CPP 裁剪行为不稳定
编辑器重载时机不可控
启动顺序隐式
测试隔离困难
```

### 5.2 显式生命周期

所有运行对象必须由：

```text
Build
Activate
RuntimeStart
Stop
Dispose
```

显式管理。

不得依赖：

```text
GC Finalizer 释放核心资源
静态构造启动 Worker
编辑器域重载隐式恢复
```

### 5.3 禁止隐式 ThreadPool

核心运行路径禁止：

```text
Task.Run
ThreadPool.QueueUserWorkItem
Parallel.For
PLINQ
async void
```

后台计算使用：

```text
WorkerEventJob
```

跨 Scope 使用：

```text
ScopeEvent / ScopeCall
```

### 5.4 主线程不得阻塞

禁止在 MainScope：

```text
Thread.Sleep
WaitHandle.WaitOne 无预算等待
Task.Wait
Task.Result
同步 Join 长时间 Worker
```

Stop/Dispose 协调应由 Pump 和 ScopeCall Response 推进。

---

## 6. Scope 通讯强制规范

### 6.1 仅允许两条通道

```text
ScopeEventInbox
ScopeCallInbox
```

禁止：

```text
CompletionPort
ControlQueue
ResponseQueue
StopQueue
DisposeQueue
ProjectionQueue
ActorCommandQueue
WorkerResultQueue
```

### 6.2 生命周期控制

```text
ScopeActivateCall
ScopeStopCall
ScopeDisposeCall
```

必须使用 ScopeCall Request/Response。

禁止通过：

```text
共享 volatile flag
跨线程 direct StopLocal
跨线程 direct DisposeLocal
独立信号队列
```

控制目标 Scope。

### 6.3 唤醒原语

允许：

```text
AutoResetEvent
ManualResetEventSlim
平台信号
```

但只用于：

```text
通知 Inbox 非空
等待 Worker 退出
```

不能承载命令语义。

---

## 7. Event 强制规范

```text
每 Scope 一个原 EventCenter
按 Push LayerIndex执行原 Handler 注册流程
注册完成后不保留第二份 Handler Registry
```

必须保留：

```text
Notify
Subscribe
Flow
Async
手动订阅 / Unsubscribe
Circuit
Prewarm
Freeze
Bucket / Fast Cache
Reflection Fallback 诊断
```

禁止：

```text
ServiceSlot 代替 LayerIndex
ContextSlot 代替 LayerIndex
ObjectSlot 派发替代原 EventCenter
新建 EventHandlerRange / HandlerEntry
ScopeEvent 到达后绕过 EventCenter
SubscribeParallel
```

---

## 8. DI、Mount、Provide / From 强制规范

### 8.1 DI

```text
Scope：
    隔离 Provider 实例。

Layer：
    限制解析范围。
```

同 Scope、同 Layer才允许：

```text
构造注入
this.Get<T>()
Mount
Provide / From
```

禁止：

```text
Scope 根 Provider 搜索所有 Layer
WorldServiceRoot 跨 Layer fallback
MainScope Provider fallback
Runtime Root 获取 Layer 业务 Service
ScopeRef.GetService
```

### 8.2 From

必须显式来源 Service：

```csharp
[From(
    typeof(InventoryService),
    "Items")]
private IReadOnlyList<Item> _items = null!;
```

绑定键：

```text
ScopeId
+ LayerIndex
+ ProviderServiceType
+ LocalKey
```

禁止：

```text
[From]
[From("Items")]
仅按字段类型推断来源
仅按 LocalKey 匹配
跨 Layer或跨 Scope绑定
```

---

## 9. Call 强制规范

### 9.1 本地 Call

```csharp
Result result =
    await this.Call<
        Request,
        Result>(
            in request);
```

范围：

```text
CurrentScope
+ RequestType
+ ResponseType
```

同一 Scope 同一 Request/Response 唯一 Handler。

Layer 仅记录：

```text
Handler 实例归属
DI
生命周期
诊断
```

不参与 Call 地址。

旧 TLayer Call API直接删除，不保留 Obsolete。

### 9.2 跨 Scope Call

```csharp
Result result =
    await this.Scope<TargetScope>()
        .Call<
            Request,
            Result>(
                in request);
```

跨 Scope Call 属于 03 号 Transport，不与本地 Call 实现混合。

---

## 10. Post 与 Worker 强制规范

删除：

```text
PostFromAnyThread
TryPostFromAnyThread
PostIngressQueue
SubscribeParallel
ParallelSubscriptionQueue
```

本地 Post：

```text
OwnerScope PostScheduler
```

跨线程：

```text
ScopeRef<TScope>.TryPost
```

WorkerEventJob：

```text
显式 Input
纯计算
Result ScopeEvent
回 OriginScope
```

Job Execute 禁止访问：

```text
ServiceProvider
ScopeRuntime
EventCenter
PostScheduler
Timer
EcsWorld
ActorWorld
引擎主线程对象
```

---

## 11. ActorWorld 强制规范

```text
ActorWorld 属于 LayerRuntime.MainActorRuntime
只由 MainScope Owner Thread调用
```

禁止：

```text
ScopeRuntime.ActorWorld
CustomScope EcsWorld 持有 ActorWorld
ScopeActorGateway 暴露 ActorWorld
CustomScope 直接 Create/Enable/Disable/Release/Post Actor
```

CustomScope 使用：

```text
ScopeEvent<MainScope>
ScopeCall<MainScope>
ActorHandle
DTO
```

Projection 不得新增专用跨 Scope 队列。

---

## 12. 热路径容器与内存规范

### 12.1 禁止容器

Running 热路径禁止：

```text
Dictionary
ConcurrentDictionary
List 动态 Add/Remove
HashSet
Queue<T>
ConcurrentQueue<T>
LINQ
```

允许这些类型存在于：

```text
Build
Activate
Diagnostics
测试
```

前提是不会进入稳态热路径。

### 12.2 允许结构

```text
固定数组
ArrayPool
对象池
Span
BitSet
LocalRingQueue
LockedBoundedRingQueue
预计算索引
```

### 12.3 稳态零分配

以下路径目标为 `0 B/op`：

```text
EventCenter.Send
PostScheduler Submit / Pump
ScopeEvent Submit / Dispatch
ScopeCall Request / Response
ScopeLocalCall
DI Get<T>
Lifecycle Tick
ECS Query Submit
WorkerEventJob Submit / Result
Actor Batch Apply
```

---

## 13. 锁与并发规范

OwnerScope 本地资源：

```text
只由 Owner Thread修改
默认无锁
```

允许锁：

```text
Build 冷路径
MPSC Producer 端短临界区
对象池冷路径
Diagnostics Snapshot
```

禁止：

```text
热路径大锁
锁内调用用户 Handler
跨 Scope 共享可变对象
用锁修复错误所有权
```

---

## 14. 源生成器使用原则

必须或优先生成：

```text
Service / Context Factory
Constructor Invoker
Mount Setter
Provide Getter
From Setter / Unbinder
Event AutoBind
LocalCall Invoker
ScopeEvent / ScopeCall Dispatcher
ECS Query Bridge
Tool Factory
Snap Invoker
AOT Closure
```

不强制源生成：

```text
一次性拓扑 DFS
Build 冲突报告
Markdown Diagnostics
容量估算
非热路径审计
```

生成代码必须：

```text
确定性
可调试
可追踪 Source Location
不依赖文件枚举顺序
不依赖 Dictionary 枚举顺序
```

---

## 15. faster 复用门禁

每个任务开始前必须搜索 `faster`。

提交必须包含：

```text
faster commit / path / type
复用方式
保留的算法、池、布局、Invoker、快路径
必须修改的 Owner / Route / Lifecycle
复用测试
复用 Benchmark
```

默认拒绝：

```text
已有 RingQueue，却重新写 Queue
已有 ActorWorld Pool，却重写 Actor 存储
已有 EventCenter Bucket，却重写派发
已有 WorkerRuntime，却另建线程池
已有 LBTask Promise，却改用 TaskCompletionSource
已有生成器 Setter，却改用热路径反射
```

允许不复用的前提：

```text
现有实现与强制架构公理直接冲突
且提交说明明确列出无法拆分复用的原因
```

---

## 16. 命名与代码规范

### 16.1 名称必须表达所有权

推荐：

```text
OwnerScope
OwnerLayer
ScopeLocal
LayerProvider
ScopeEndpoint
MainActorRuntime
ScopeExecutionPlan
LayerBuildPlan
```

避免：

```text
Global
Manager
Context
Root
Shared
```

用于掩盖所有权。

### 16.2 Try API

可能失败的有界操作必须提供：

```text
TryPost
TryCall
TryRent
TryEnqueue
```

失败必须明确返回：

```text
QueueFull
ScopeStopped
RuntimeDisposed
InvalidGeneration
RouteNotFound
```

不得静默 Drop。

### 16.3 注释

所有跨线程结构必须说明：

```text
谁写
谁读
所有权何时转移
失败时谁释放
Stop 时如何终结
```

---

## 17. 测试和 CI 强制门禁

### 17.1 架构扫描

源码扫描必须证明：

```text
无 PostFromAnyThread
无 SubscribeParallel
无 TLayer Call API
无 ModuleInitializer
Runtime Assembly 无 Microsoft.CodeAnalysis
ScopeRuntime 无 ActorWorld
跨 Scope 只有 EventInbox / CallInbox
```

### 17.2 Layer-first 测试

```text
LayerIndex follows Push order
Every Service has OwnerLayer and OwnerScope
CustomScope preserves Layer order
DI cannot cross Layer
Event registration uses LayerIndex
Lifecycle runs by Layer order
```

### 17.3 AOT 测试

至少执行：

```text
IL2CPP Development Build
裁剪测试
生成代码可达性测试
无反射 fallback 热路径测试
```

### 17.4 Benchmark Gate

同环境至少三轮。

```text
热路径从 0 B/op 变成分配：
    默认拒绝

吞吐或延迟退化 > 15%：
    必须 Profile、解释并批准
```

---

## 18. 允许例外

仅允许在文档明确标注的冷路径中使用：

```text
Dictionary / List / HashSet
反射
字符串报告
LINQ
```

例外申请必须写明：

```text
调用阶段
调用频率
不进入 Running 的证明
替代方案为何更差
测试和 Profile 结果
```

不得为热路径申请“临时例外”。

---

## 19. 最终验收否决项

出现以下任意一项，提交直接否决：

```text
Scope 成为业务对象管理根

Layer 只对 MainScope 有意义

CustomScope 没有 Layer

Service / Context 缺少 OwnerLayer

DI 跨 Layer fallback

From 来源不明确

EventCenter 丢失原功能

本地 Call 重新依赖 Layer

旧 TLayer Call API仍存在

PostFromAnyThread仍存在

SubscribeParallel仍存在

Scope 间出现第三条消息通道

ScopeRuntime 持有 ActorWorld

Worker Result 回全局队列或 MainScope

运行期 Roslyn / SyntaxTree / JIT

ModuleInitializer

热路径 Type / Dictionary / List 查询

Build 可确定的数据在 Running 重算

未检查 faster 就重复实现成熟代码
```
