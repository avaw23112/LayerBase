[English](#english) | [中文](#中文)

<a id="中文"></a>

# 🚀 LayerBase: 面向数据的高性能 C# 游戏架构总线

**LayerBase** 是一款专为 Unity、Godot 及纯 C# 服务端打造的高性能事件架构与通讯总线框架。它打破了传统面向对象（OOP）事件总线的性能瓶颈，在底层采用了
**面向数据设计（Data-Oriented Design, DOD）**与**SOA（Structure of Arrays**内存布局。在保证业务代码极简、解耦的同时，为中大型项目提供规范的事件流转控制。

- ECS内核部分使用Arch框架，后续更新计划中会尝试自研 : https://github.com/genaray/Arch

---

## 🤔 架构演进：我们为什么需要 LayerBase？

在游戏项目的生命周期中，业务复杂度的增长往往会推动通讯架构的演进。回顾常见的技术选型，我们可以清晰地看到痛点所在：

### 1. 单例模式的耦合困境

在项目初期，直接调用是最直观的方式（例如 `GameManager.Instance.UpdateHealth()`）。但在中大型项目中，当系统里存在成百上千个
Manager 时，这种方式会促使代码演变为极度复杂的网状引用。模块间的紧密耦合让重构 and 独立测试变得举步维艰。

### 2. 传统事件总线（EventBus）的时序与性能瓶颈

为了实现模块解耦，业界普遍引入了 `Action` 委托、`UniRx` 或是泛型 `EventBus`。这确实切断了硬引用，但同时也引入了两个更为隐蔽的工程问题：

* **隐式的时序陷阱**：在缺乏统一架构指导的情况下，开发者通常会在各个组件的生命周期（如 `Awake`、`Start`）中分散注册事件。这种*
  *无序的注册行为**导致事件响应的先后顺序成为黑盒。当一个事件抛出时，你无法确保数据结算与 UI 表现的确定性顺序，这极易引发偶发性
  Bug。
* **底层的性能暗礁**：传统事件总线的底层普遍依赖 `Dictionary<Type, List<Delegate>>`
  。在面临海量实体（Entity）的高频交互时，字典的哈希计算、委托链的遍历，以及最关键的——**缺乏内存连续性导致的 CPU 缓存未命中（Cache
  Miss）**，通常会将系统的处理上限限制在百万级 TPS。

### 3. 破局：秩序与极速的重构

LayerBase 的设计哲学是：**用强约束的框架收编混乱的注册，用底层的面向数据重构打破性能的枷锁。**

1. **在宏观架构上**：摒弃随地订阅的模式，引入 `Layer -> Service -> Manager` 三层递进的架构。通过依赖注入（DI）和明确的拓扑层级，为团队的协作开发提供完整的心智模型。
2. **在底层执行上**：汲取 ECS 框架的核心思想，在底层采用纯粹的 SOA 数组布局进行事件路由。使得这一架构不仅规范了代码，更在性能上达到了与顶级
   C++/C# ECS 框架同级别的缓存命中率。
3. **在行为建模上**：引入 Actor 模型为每个实体提供独立的行为封装——邮箱、生命周期、事件处理器一应俱全，天然适合建模游戏角色、NPC、子弹等具有独立行为的对象。ECS 负责数据密集的批量处理，Actor 负责行为逻辑的封装，两者通过 Projection 机制无缝桥接。

---

## ⚡ 性能摘要 (Performance Summary)

LayerBase 专为高频事件交互设计，在 .NET 8/9 环境下表现卓越。

- **极致低延迟**：单事件分发开销低至 **1.6 ns**，Request/Response (Call) 开销约 **1.08 ns**。
- **完美的缓存亲和性**：底层采用 SOA 布局与位图跳跃算法，最小化 CPU Cache Miss。
- **零分配 (Zero-Allocation)**：核心分发路径与 `LBTask` 异步路径在同步完成时实现 **0 GC 堆分配**。

> 完整基准测试数据与对比请参阅 [docs/BENCHMARKS.md](docs/BENCHMARKS.md)。

---

## 📊 核心优化技术

为了突破性能天花板，LayerBase 在底层实施了多项物理级优化：

1. **SOA 面向数据布局 (Structure of Arrays)**：将同类事件处理器脱水为连续原生数组，利用 CPU 顺序预取。
2. **硬件级位图跳跃 (Bitmask Skipping)**：利用 `TrailingZeroCount` 指令实现 O(1) 跨层寻址。
3. **无分支与越界消除**：通过按位合并状态与 `Unsafe` 步进，减少分支预测失败并消除数组边界检查。
4. **异步路径短路**：专为游戏优化的 `LBTask` 在同步完成时彻底消除状态机租赁开销。

---

## 🛡️ 工业级基建保障

除了效率，LayerBase 同样注重工程稳健性：

1. **自愈熔断机制**：异常隔离与物理熔断，下一帧自动剔除故障节点。
2. **零分配异步生态 (`LBTask`)**：专为游戏循环调优的结构体 Task。
3. **静态拓扑审计**：Build 期静态扫描，提前发现同步死循环风险。
4. **Actor 对象池**：高频创建/销毁的 Actor（如子弹、特效）可启用对象池，避免 GC 压力。
5. **ECS 蓝图系统**：声明式实体结构定义，支持组件聚合和 Actor 投影绑定。

* 注意：使用 **[SubscribeNotify]** 注册的处理器为了极致性能不捕获异常，需由用户自行保证安全。

---

## 💾 Snap 快照能力

LayerBase 现已内建一套**运行时业务快照**能力，用于保存和恢复需要持久化的 Layer / Service / Manager 状态。

它刻意不是“整份 Runtime 内存镜像”，而是**业务字段显式快照**：

- **`IFullSnap`**：参与 Runtime 级完整快照，由框架在 Build 后自动收集并由 `runtime.FullSnap` 统一调度。
- **`IClipSnap<T>`**：面向局部状态切片，由业务代码主动调用，不进入 FullSnap 文档流。
- **`SnapWriter` / `SnapReader`**：基于 `System.Text.Json.Nodes` 的显式字段写入/读取 API。
- **`SnapArrayWriter` / `SnapArrayReader`**：保留数组逐项读写模型，适合实体列表、背包项等复杂数组数据。

### FullSnap 示例

```csharp
using LayerBase.DI;
using LayerBase.Snap;

public sealed partial class BattleContext : ILayerContext, IFullSnap
{
    public int RoomId;
    public int CurrentFrame;

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("roomId", RoomId);
        writer.WriteInt32("currentFrame", CurrentFrame);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        RoomId = reader.ReadInt32("roomId");
        CurrentFrame = reader.ReadInt32("currentFrame");
    }
}
```

```csharp
var document = runtime.FullSnap.Serialize();
string json = runtime.FullSnap.SerializeJson();

runtime.FullSnap.Deserialize(document);
runtime.FullSnap.DeserializeJson(json);
```

### ClipSnap 示例

```csharp
using LayerBase.Snap;

public sealed class BattleSyncService :
    IClipSnap<MoveClip>,
    IClipSnap<HealthClip>
{
    private float _x;
    private float _y;
    private int _hp;

    MoveClip IClipSnap<MoveClip>.Serialize() => new(_x, _y);
    void IClipSnap<MoveClip>.Deserialize(in MoveClip clip)
    {
        _x = clip.X;
        _y = clip.Y;
    }

    HealthClip IClipSnap<HealthClip>.Serialize() => new(_hp);
    void IClipSnap<HealthClip>.Deserialize(in HealthClip clip)
    {
        _hp = clip.Hp;
    }
}
```

```csharp
MoveClip move = syncService.Clip<MoveClip>().Serialize();
syncService.Clip<MoveClip>().Deserialize(in move);
```

### 默认不进入 FullSnap 的对象

以下对象**不会**被框架默认写入完整快照：

- `EcsWorld`
- `ActorWorld`
- `ProjectedActor`
- Actor 邮箱 / PostScheduler / Timer / Delay 内部队列
- 渲染对象、物理对象、线程对象、`Task`

推荐做法：

- **ECS 批量数据**：在 `IFullSnap.WriteFullSnap` 中通过 Query 选择性导出。
- **关键 Actor 状态**：通过 `IClipSnap<T>` 或 Manager / Service 显式同步。

> 说明：`IClipSnap<T>` 只是**单对象、单切片**的数据导出/导入能力，不是跨 Layer 编排边界。如果一个需求想借此隐藏多层聚合、广播或工作流协调，那通常应被视为设计异味。

---

## 📦 安装指南

1. **NuGet 快速安装 (推荐)**：
   您可以通过 NuGet 包管理器直接安装 LayerBase：
   ```bash
   dotnet add package LayerBase --version 1.4.7.1
   ```
   如果需要固定版本，请以 NuGet 页面或 `LayerBase.csproj` 中的 Version 为准。

2. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
3. **配置源生成器 (Source Generator)**：
   框架依赖源生成器以实现在事件分发热路径上的零反射，请确保在主项目中引入了分析器（注：DI 装配与共享字段绑定在 Build
   阶段仍使用反射，但通过元数据缓存进行了深度优化）：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **环境要求**：支持 `.NET Standard 2.1`（完美兼容 Unity/Godot），但建议在 `.NET 8.0/9.0` 环境下运行。

---

## 📖 最佳实践手册：构建清晰的架构

在大型项目中，扁平架构容易导致模块依赖复杂化。LayerBase 提供多层次的心智模型来管理这种复杂性：

### 三层架构（Layer → Service → Manager）

* 🌍 **Layer（宏观层级）**：**处理优先级与物理界限。**
    * **职责**：代表系统的不同层面（如 `RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`）。它主要用于隔离不同优先级的业务逻辑。
* 🏢 **Service（业务服务）**：**功能聚合与调度。**
    * **职责**：将相关联的功能模块聚合在一起（例如 `PlayerService` 聚合输入、移动、动画等）。它负责依赖注入（DI）配置，对外暴露粗粒度接口，对内管理
      Manager，实现高内聚。
* ⚙️ **Manager（具体逻辑块）**：**具体业务的承载者。**
    * **职责**：遵循单一职责原则 (SRP)，实现具体的微观功能（如处理伤害计算）。Manager 之间尽量避免直接引用，而是通过事件总线进行通讯，实现低耦合。

### ECS 实体组件系统（数据层）

* 📦 **Component**：纯 `struct` 数据块，按 SOA 布局存储，CPU 缓存友好。
* 🔍 **Query**：声明式组件过滤器，批量遍历满足条件的 Entity。
* 🏗️ **Blueprint**：声明式实体结构定义，支持组件聚合（Bundle）和 Actor 投影。

### Actor 行为模型（行为层）

* 🎭 **Actor**：独立行为封装，拥有自己的邮箱和生命周期。
* 📬 **Mailbox**：事件邮箱，支持队列、最新值、合并等多种投递策略。
* 🔄 **Projection**：ECS ↔ Actor 桥梁，数据密集处理与行为逻辑封装的无缝结合。

通过这种结构，项目的代码目录结构能够清晰地反映其系统架构。

---

### Step 1: 定义事件 (Event Structs)

为避免在事件频繁触发时产生 GC 压力，框架要求所有的事件对象必须声明为 `struct`：

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写业务逻辑 (Manager)

Manager 继承自 `ILayerContext`，能够感知自身所处的层级，并具备事件发送与接收能力。推荐使用特性（Attribute）进行绑定，编译器会自动生成关联代码以避免反射开销。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;
using LayerBase; // 包含 ILayerContext, EventHandledState 等

// 类需标记为 partial 配合源生成器
public partial class DamageManager : ILayerContext
{
    //  所有Subscribe都保证注册的先后顺序就是它在事件调度中的顺序。
    // 【安全通知处理】：使用 [Subscribe] 特性 (异常隔离)
    [Subscribe]
    private void OnTakeDamage(in DamageEvent e)
    {
        // 业务逻辑处理...
        if (e.Amount > 100)
        {
            // 向全局广播事件
            this.Send(new PlayerDeathEvent()); 
        }
    }

    // 【同步业务流处理】：使用 [SubscribeFlow] 特性 (支持截断)
    [SubscribeFlow]
    private EventHandledState OnBeforeTakeDamage(in DamageEvent e)
    {
        if (e.Amount <= 0) return EventHandledState.Handled; // 截断后续处理
        return EventHandledState.Continue;
    }

    // 【异步事件处理】：使用 [SubscribeAsync] 特性
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        // 支持使用 LBTask.Delay 进行无 GC 延迟
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); 
    }
}

public partial class PlayerMoveManager : ILayerContext
{
    // 【追求极致性能】：使用 [SubscribeNotify] 特性
    //  必须由用户自身担保逻辑的正确性，框架不负责捕捉、处理、上报异常
    [SubscribeNotify]
    private void OnPlayer(in PlayerMoveEvent e)
    {
        // 示例代码：实际项目中可能使用 Entity Component System 或直接操作数据
        // ref var position = ref Player.Entity.Get<PositionComponent>();
        // position += e.Delta;
    }
}

using LayerBase.Option;

public partial class PlayerInputManager : ILayerContext, IUpdate
{
    // 一个非阻塞的定时事件管道。
    // 最常用的就是把它当做输入缓冲队列，且可在发送端调整缓冲存在的时间
    [SubscribeDelay] public IDelayPublisher<InputEvent> DelayNotify { get; set; }

    public void Update(float deltaTime)
    {
        // 尝试获取当前挂起的数值（取出管道内的事件）,如果没有则返回 False
        if (DelayNotify.TryTake(out InputEvent inputEvent))
        {
            if (inputEvent.Value == InputType.Forward)
            {
                this.Send(new PlayerMoveEvent 
                { 
                    DeltaX = 0, 
                    DeltaY = 1 
                });
            }
        }
    }
}
```

* 注意事件类型的执行顺序：在同一层内 由 `SubscribeNotify -> Subscribe -> SubscribeFlow -> SubscribeAsync`。
* 所有事件都只能保证在同类型的事件中顺序和注册一致，不能保证不同事件类型的事件执行顺序和注册顺序一致。

### Step 3: 组织业务模块 (Service)

Service 负责将相关的 Manager 注册到 DI 容器中。
通过 `[OwnerLayer]` 特性，可以将 Service 静态绑定到指定的 Layer 层级。

此外，您可以使用 `[Mount]` 特性实现**声明式依赖注入**。`[Mount]` 在不同位置有不同语义：

#### 1. Layer 字段 / 属性

```csharp
public partial class GameLogicLayer : Layer
{
    // 表示把 IService 挂载到当前 Layer。挂载顺序决定了 Update 和事件注册的优先级。
    [Mount] private CombatService _combatService;
}
```

#### 2. IService 字段 / 属性

```csharp
public partial class CombatService : IService 
{
    // 如果字段类型是具体的 ILayerContext 实现，源生成器会自动注册：
    // services.TryAddScoped<DamageManager, DamageManager>()
    // 并在运行期自动完成注入。
    [Mount] private DamageManager _damageManager;
}
```

#### 3. 构造函数

```csharp
public class DamageManager : ILayerContext
{
    // 表示选择该构造函数作为 DI 构造器。
    [Mount]
    public DamageManager(SomeDependency dep) { }
}
```

### Step 4: 空间维度的事件触发

LayerBase 提供了精确控制传播范围的 API。在 `Layer`、`Service` 或 `Manager` 内部，您可以直接调用扩展方法来派发事件：

```csharp
// 【Send 族：同步执行，当前执行流会等待分发完成】
this.Send(new DamageEvent()); // 【全局】穿透所有层级广播

// 【Post 族：异步投递，用在一些不紧急，可分帧执行的任务中。有帧预算和背压算法。】
this.Post(new DamageEvent()); 
this.MarkDirty<DamageEvent>();           // 会合并 post 事件，但本体没有内容只有 default
this.PostLatest(new DamageEvent());      // 会留下最后一个事件
this.PostCoalesced(new DamageEvent());    // 根据元数据的算法来合并事件，最后只留下一个事件

// 【Delay 族：向事件缓冲管道发布一个定时的事件】
this.Delay(new PlayerDeathEvent(), 3.5f); // 该事件会在管道中存在 3.5 秒，直到有新的事件覆盖、到时间自动消亡、或者被取出。
```

这种设计有助于精简事件流，减少不必要的跨层搜索。

### Step 5: 引擎生命周期整合

将 LayerBase 接入到具体的游戏引擎（如 Unity 的 `MonoBehaviour`）时，需处理两个关键生命周期：构建（Build）与心跳驱动（Pump）。

* **Build**：在游戏初始化阶段调用，完成特性的扫描、SOA 数组分配及静态死循环审计。
* **Pump**：在每帧更新阶段调用，用于处理队列中的异步事件和时间延迟任务。

```csharp
using UnityEngine;
using LayerBase;
using LayerBase.Layers;

// 定义层级
public class InteractionLayer : Layer { }
public class CoreLogicLayer : Layer { }

public class GameRoot : MonoBehaviour
{
    private LayerRuntime _runtime;

    void Awake()
    {
        // 1. 初始化期：构建拓扑
        _runtime = LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // 索引 0: 上层交互
                .Push(new CoreLogicLayer())   // 索引 1: 下层逻辑
                .SetDebug()                   // 开启 Debug 模式可以从 GetTopologySummary() 获得整个系统的构建图
                .Build()                      // 自动扫描 [OwnerLayer] 并装配
                .Prewarm();                   // [可选] 预热步骤，用于提前触发部分运行时缓存、事件 ID 或策略相关路径

        // 注册全局消息通道，用于捕获框架的异常
        LayerHub.OnLayerEventInfo +=
            info =>
            {
                Debug.LogError($"[{info.LayerIndex}][{info.EventName}][{info.Type}]: {info.Source}{info.Message}");
            };
    }

    void Update()
    {
        // 2. 运行期：驱动事件泵
        // 在空闲状态下，Pump 的调用开销极小
        _runtime.Pump(Time.deltaTime);
    }
}
```

> **Prewarm 说明**：`Prewarm()` 是可选的预热步骤，建议在 Loading 或初始化完成时调用。它能有效避免首次运行时的 JIT 抖动。不调用
`Prewarm()` 也不影响基础功能的正确性。

---

## 🛠 进阶特性指南

### 1. PostFromAnyThread (跨线程事件入口)

`PostFromAnyThread` 允许后台线程安全地提交事件，这些事件会在主线程下一次 `Pump` 时被处理。

```csharp
// 后台线程提交计算完成通知
LayerHub.PostFromAnyThread(new DamageEvent
{
    TargetId = 1,
    Amount = 10
});

// 也可以显式指定策略
LayerHub.PostFromAnyThread(
    new DamageEvent { TargetId = 1, Amount = 10 },
    new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew)
);
```

### 2. PostScheduler 配置

通过 `PostSchedulerOptions` 控制 Post 队列、帧预算和默认背压策略。

```csharp
var options = new PostSchedulerOptions(
    readyCapacity: 1024,
    nextCapacity: 1024,
    maxEventsPerPump: 5000,          // 每帧最多处理事件数
    maxMillisecondsPerPump: 2.0,     // 每帧最大耗时预算 (ms)
    maxWavesPerPump: 1,
    timeCheckInterval: 64,
    defaultBackpressure: BackpressurePolicy.RejectNew,
    maxCompletionsPerPump: 0,
    maxIngressPostsPerPump: 4096     // 每帧从 AnyThread 入口搬运的最大数量
);
```

### 3. FixedUpdate 支持

通过 `SetFixedUpdateOptions` 开启固定步长更新。`Runtime.Pump(deltaTime)` 会自动处理时间累积。

```csharp
_runtime = LayerHub.CreateLayers()
    .Push(new MyLayer())
    .SetFixedUpdateOptions(new FixedUpdateOptions(
        enabled: true,
        fixedDeltaTime: 1f / 60f,
        maxStepsPerPump: 4 // 防止低帧率下单帧补步过多
    ))
    .Build();
```

### 4. 默认事件注册语义

在 `Build` 前已经分配过 `EventTypeId<T>.Id` 的事件，即使没有显式 `MetaData`，也会在 `PostScheduler` 中获得默认的 `Normal`
策略。这意味着任何结构体事件都可以直接作为普通事件被 `Post`。

### 5. Runtime Policy Dump & Event Identity

诊断场景下可以导出当前 Runtime 的事件策略表和身份信息：

```csharp
// 导出所有事件的 Post、Timer、Buffer 策略
var markdown = _runtime.GetPolicyMarkdown();
```

LayerBase 在运行时使用 `EventTypeId<T>.Id` 进行热路径索引，同时保留 `StableId` 和 `StableKey` 用于跨版本记录和诊断。

### 6. 流式过滤与拦截 (Fluent API)

对于需要动态控制订阅条件的场景，LayerBase 提供了优雅的链式 API。

- 优点：可以灵活的创建业务逻辑，适合初期快速开发。
- 缺点：吃不到源生成器的优化，性能较低。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 链式调用：订阅 -> 过滤 -> 处理
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .HandleFlow((in DamageEvent e) => 
            {
                // 处理受击...
                return EventHandledState.Handled;
            });
    }
}
```

### 7. 后台并行处理 (Parallel Handlers)

当面临高 CPU 消耗且**不依赖/不修改主线程状态、长时间运作**的纯计算逻辑时，可使用并行订阅。

> **注意**：并行处理器在独立的线程池中执行，不保证线程安全，需用户自行管理同步。

```csharp
// 通过特性快速绑定并行方法
[SubscribeParallel]
private void OnHeavyComputeTask(in ComputeEvent e)
{
    // 该方法在多线程环境中被安全调度
}
```

### 8. 拓扑结构可视化 (Topology Snapshot)

调用 `LayerHub.GetTopologyMarkdown()` 即可获得一份详细的 Markdown 表格，展示整个系统内各个 Layer 挂载了哪些
Manager，以及它们具体订阅/派发了什么事件。

### 9. 线程模型 (Threading Model)

LayerBase 当前采用**单线程 Runtime 模型**。

除带 `AnyThread` 后缀的 API 外，其余 Runtime API 默认只能在 Runtime 所在线程调用。

**Owner-thread only (仅限所有者线程):**

- `Send` / `Post` / `TryPost` / `PostLatest` / `PostCoalesced`
- `MarkDirty` / `CallAsync`
- `Pump` / `Build` / `Dispose` / `Reset`

**AnyThread (允许跨线程):**

- `PostFromAnyThread`
- `TryPostFromAnyThread`

`PostFromAnyThread` 不会立即派发事件。它会先进入跨线程入口队列，并在下一次 `Runtime.Pump` 中搬运到 `PostScheduler`
，再参与正常的策略处理。

> **注意**：当前版本不在热路径 API 中做线程动态检查。在错误线程调用普通 API 属于未定义行为。`Dispose` / `Reset` 不建议与
`PostFromAnyThread` 并发执行。

---

### 10. Runtime 构建生命周期

LayerBase 的 Build 阶段大致分为：

1. **Prebuild (预构建)**
    - 分配 Layer RouteIndex
    - 绑定 Runtime / EventCenter
    - 执行 `PrepareBuild`
    - 执行源生成器生成的 AutoBinding

2. **基础设施初始化**
    - 初始化 PostScheduler、Timer、DelayManager
    - 构建 ServiceProvider 容器

3. **Layer Build (层级构建)**
    - `SharedFieldBinder` 绑定共享字段
    - 执行 `LifecycleBuild` / `AutoBind` / `Initialize`
    - 执行 `PostBuild` 与 `RuntimeStart`
    - `EventGraphValidator` 校验事件图拓扑

4. **Runtime Dispose (销毁)**
    - 执行 `RuntimeStop`
    - `DisposeLayers` 释放层级资源
    - 清理 Scheduler / Timer / Delay 队列与缓存

---

### 11. ECS 实体组件系统

LayerBase 内建了一套高性能 ECS 实现（基于 Arch.Core），与事件总线深度集成。ECS 以 **World → Archetype → Chunk → Entity** 四层结构组织数据，采用纯 SOA 内存布局，天然对齐 CPU 缓存行。

#### 核心概念

| 概念 | 说明 |
|------|------|
| **World** | ECS 世界实例，每个 `LayerRuntime` 自动创建一个，通过 `runtime.EcsWorld` 访问 |
| **Entity** | 轻量级 ID 句柄（`int Id` + `int Version`），代表一个游戏对象 |
| **Component** | 纯 `struct` 数据块，附着在 Entity 上（如 `Position`、`Health`） |
| **Archetype** | 具有相同组件组合的 Entity 集合，内部由连续 Chunk 数组构成 |
| **Chunk** | 固定容量的数据块（默认 256），同一 Chunk 内组件数组内存连续 |
| **Query** | 组件过滤器，用于批量遍历满足条件的 Entity |

#### 快速上手

```csharp
using Arch.Core;

// 1. 定义组件（纯 struct）
public struct Position { public float X, Y, Z; }
public struct Velocity { public float Dx, Dy, Dz; }

// 2. 创建实体
World world = _runtime.EcsWorld;
Entity player = world.Create(new Position { X = 0, Y = 0, Z = 0 },
                             new Velocity { Dx = 1, Dy = 0, Dz = 0 });

// 3. 查询并遍历（SOA 连续内存，零 GC）
var query = new QueryDescription().WithAll<Position, Velocity>();
world.Query(in query, (ref Position pos, in Velocity vel) =>
{
    pos.X += vel.Dx;
    pos.Y += vel.Dy;
    pos.Z += vel.Dz;
});
```

#### Blueprint 蓝图系统

Blueprint 用于声明式定义实体结构，支持组件聚合（Bundle）和 Actor 投影绑定：

```csharp
// 定义 Bundle：一组相关组件的聚合切片
[LayerBundle]
public partial class TransformBundle : IBundle
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithComponent<Position>()
               .WithComponent<Rotation>()
               .WithComponent<Scale>();
    }
}

// 定义 Blueprint：完整的实体结构声明
[LayerBlueprint]
public partial class PlayerBlueprint : IEntityBlueprint
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithBundle<TransformBundle>()      // 展开 Bundle
               .WithComponent<Health>()             // 追加组件
               .WithComponent<PlayerTag>()
               .WithProjectedActor<PlayerActor>();   // 绑定 Actor 投影
    }
}
```

#### Projection 投影机制（ECS ↔ Actor 桥梁）

Projection 是 LayerBase 的核心创新——它将 ECS 的数据密集优势与 Actor 的行为封装能力合二为一：

- **延迟投影**：Entity 创建时仅标记投影元数据，Actor 实例在首次访问时惰性创建
- **自动回收**：Entity 销毁时，关联的 Actor 自动归还对象池或销毁
- **查询桥接**：通过 `[Query]` + `[Bring]` 特性，ECS 查询结果可直接投递为 Actor 事件

```csharp
// 在 Manager 中使用 ECS 查询
public partial class MovementManager : ILayerContext
{
    // 声明式查询：源生成器自动生成批量遍历代码
    [Query]
    [Bring<PositionChangedEvent>]
    private void OnMove(ref Position pos, in Velocity vel)
    {
        pos.X += vel.Dx;
        // 查询结果可自动投影为 Actor 事件
    }
}

// Fluent 查询 API（运行时动态构建）
this.Query<Position, Velocity>()
    .Where(entity => entity.Has<ActiveTag>())
    .ForEach((ref Position pos, in Velocity vel) =>
    {
        pos.X += vel.Dx;
    });
```

---

### 12. Actor 行为模型

Actor 模型为每个实体提供**独立的行为封装**——每个 Actor 拥有自己的邮箱（Mailbox）、生命周期和事件处理器，天然适合建模游戏角色、NPC、子弹等具有独立行为的对象。

#### 核心概念

| 概念 | 说明 |
|------|------|
| **IActor** | Actor 接口标记，所有 Actor 类型必须实现 |
| **ActorId** | 唯一标识符（`ArchetypeId` + `SlotIndex` + `Generation`），带版本号防 ABA |
| **ActorWorld** | Actor 运行时容器，管理创建、销毁、事件投递和生命周期调度 |
| **Mailbox** | 每个 Actor 的事件邮箱，支持队列、最新值、合并等多种投递策略 |
| **Behaviour** | Actor 的事件处理方法，通过 `[ActorBehaviour]` 特性标记 |

#### 快速上手

```csharp
using LayerBase.Actor;

// 1. 定义事件
public struct DamageEvent
{
    public float Amount;
}

// 2. 定义 Actor
public sealed partial class EnemyActor : IActor
{
    public float Health { get; private set; } = 100f;

    [ActorBehaviour]
    private void OnDamage(in DamageEvent e)
    {
        Health -= e.Amount;
        if (Health <= 0)
        {
            // 销毁自身
            this.Actors().DestroyActor(this.GetActorId());
        }
    }
}

// 3. 创建并使用
var world = _runtime.Actors;
EnemyActor enemy = world.CreateActor<EnemyActor>();

// 向 Actor 投递事件（异步，进入邮箱）
world.PostTo(enemy.GetActorId(), new DamageEvent { Amount = 25f });

// 同步分发（立即执行，绕过邮箱）
world.DispatchNow(enemy.GetActorId(), new DamageEvent { Amount = 10f });
```

#### Actor 生命周期

Actor 实现对应接口即可接入引擎生命周期回调，由 `ActorLifecycleScheduler` 自动调度：

```csharp
public sealed partial class BulletActor : IActor, IStart, IUpdate, IDestroy
{
    private float _lifetime;

    public void Start()
    {
        _lifetime = 0f;
    }

    public void Update(float deltaTime)
    {
        _lifetime += deltaTime;
        if (_lifetime > 5f)
            this.Actors().DestroyActor(this.GetActorId());
    }

    public void Destroy()
    {
        // 清理资源
    }
}
```

| 接口 | 触发时机 |
|------|----------|
| `IStart` | Actor 创建后首次 Pump |
| `IUpdate` | 每帧 Update 阶段 |
| `IFixedUpdate` | 固定步长物理帧 |
| `ILateUpdate` | Update 之后 |
| `IEnable` / `IDisable` | 启用/禁用状态切换 |
| `IDestroy` | Actor 销毁前 |

#### 邮箱投递策略

```csharp
// 队列模式（默认）：先进先出，逐帧处理
world.PostTo(actorId, new DamageEvent { Amount = 10 });

// 最新值模式：只保留最后一次投递
world.PostTo(actorId, new HealthChangedEvent { Current = 75 });

// 延迟投递：指定时间后生效
world.DelayPost(actorId, new PoisonDamageEvent { Amount = 5 }, delaySeconds: 2.0f);

// Ask/Call 模式：请求-响应
LBTask<AttackResponse> response = world.Ask<AttackRequest, AttackResponse>(
    actorId, new AttackRequest { TargetId = 42 });
```

#### Actor 池化

高频创建/销毁的 Actor（如子弹、特效）可启用对象池避免 GC：

```csharp
public sealed partial class BulletActor : IActor, IPooledActor
{
    public long RecycleDeadlineTicks { get; set; }

    public void OnRent()  { /* 从池中取出时重置状态 */ }
    public void OnReturn(){ /* 归还池时清理 */ }
}

// 预热对象池
world.PrewarmPool<BulletActor>(1000);

// 创建池化 Actor
BulletActor bullet = world.CreateActor<BulletActor>(usePool: true);

// 设置池上限
world.SetPoolLimit<BulletActor>(5000);

// 查询池统计
ActorPoolStats stats = world.GetPoolStats<BulletActor>();
```

#### Tag 与 Group 分类

```csharp
// 定义标签
public struct EnemyTag : IActorTag { }
public struct FriendlyTag : IActorTag { }

// 定义分组
public struct CombatGroup : IActorGroup { }
public struct MovementGroup : IActorGroup { }

// 应用标签和分组
[Tag<EnemyTag>]
[Group<CombatGroup>]
public sealed partial class GoblinActor : IActor { }

// 按标签/分组查询
var enemies = world.Query()
    .AllTags<EnemyTag>()
    .AllGroups<CombatGroup>()
    .Build();
```

---

### 13. Call 请求-响应模式

`Call` 提供类型安全的同步/异步请求-响应通道，适合需要返回值的场景（如场景切换确认、数据查询）：

```csharp
// 定义请求/响应
public readonly struct QueryPlayerDataRequest
{
    public readonly int PlayerId;
}

public readonly struct QueryPlayerDataResponse
{
    public readonly string Name;
    public readonly int Level;
}

// 在 Layer 或 Service 中注册处理器
public partial class PlayerLayer : Layer
{
    [Call]
    private LBTask<QueryPlayerDataResponse> HandleQuery(QueryPlayerDataRequest req)
    {
        var data = GetService<PlayerDatabase>().Get(req.PlayerId);
        return LBTask.FromResult(new QueryPlayerDataResponse
        {
            Name = data.Name,
            Level = data.Level
        });
    }
}

// 调用方
var response = await runtime.CallAsync<QueryPlayerDataRequest, QueryPlayerDataResponse>(
    new QueryPlayerDataRequest { PlayerId = 42 });
```

---

### 14. Timer 定时调度器

`TimerScheduler` 提供基于时间轴的事件调度能力，支持一次性定时和频率门控：

```csharp
// 获取 Timer 实例
var timer = _runtime.Timer;

// 注册一次性定时事件（3 秒后触发）
timer.Schedule(TimeSpan.FromSeconds(3), () =>
{
    Console.WriteLine("3 秒已到！");
});

// 设置频率门控（每 0.5 秒最多触发一次）
timer.SetFrequency(0.5);

// 在 Tick 中驱动
timer.Tick(deltaTime);
```

---

### 15. Job 后台任务调度器

`JobScheduler` 提供线程池级别的后台任务调度，适合 IO 密集或计算密集的异步工作：

```csharp
// 创建调度器（默认使用 CPU 核心数的线程）
using var scheduler = new JobScheduler(workerCount: 4);

// 提交后台任务
scheduler.Schedule(() =>
{
    // 在后台线程执行耗时操作
    var data = LoadAssetFromDisk("hero.png");

    // 回到主线程投递结果
    LayerHub.PostFromAnyThread(new AssetLoadedEvent { Data = data });
});
```

---

## ⚠️ 核心设计边界与时序约束 (Core Design Boundaries)

为了在 managed 环境下压榨出极致性能，LayerBase 在设计上做了一些权衡，开发者**必须**了解这些物理边界：

### 1. 故障隔离：单帧中断，次帧自愈

LayerBase 采用了高效的 SOA 批量分发引擎。为了保证极致的 CPU 缓存亲和力，同步分发循环中未对每一个 Handler 包裹 `try-catch`。

* **行为**：如果某个 Handler 抛出未处理异常，**本次分发的后续 Handler 将被跳过**以保护调用栈。
* **自愈**：系统会立即熔断故障节点，在**下一帧（或下一次分发）**，故障节点将被彻底剔除，系统恢复正常。
* **建议**：关键业务请自行包裹 `try-catch`，或利用 `EventMetaData` 进行全局观察。

### 2. 时序约定：同步永远领先于异步

在一个 Event 类型下，无论注册顺序如何：

* **同步 Handler** 总是会被优先批量执行。其中 `[SubscribeFlow]` 具备截断事件（Handled）的能力，而 `[SubscribeNotify]` 和
  `[Subscribe]` 为强制全量分发。
* **异步 Handler** 只有在所有同步逻辑跑完后，才会统一启动。
* **结论**：不要依赖同步与异步之间的混合注册顺序。

### 3. 层级上限：64 层物理硬限制

为了实现 O(1) 的跨层位图路由，LayerBase 内部使用了 `ulong` 位图标记层状态。

* **物理限制**：单实例支持的最大 Layer 数量为 **64**。
* **溢出处理**：尝试 `Push` 第 65 层时会抛出 `InvalidOperationException`。

---

<a id="english"></a>

# 🚀 LayerBase: Data-Oriented High-Performance C# Game Architecture Bus

**LayerBase** is a high-performance event architecture and communication bus framework designed specifically for Unity,
Godot, and pure C# servers. It breaks through the performance bottlenecks of traditional Object-Oriented Programming (
OOP) event buses by adopting **Data-Oriented Design (DOD)** and **SOA (Structure of Arrays)** memory layout at its core.
While keeping business code minimal and decoupled, it provides standardized event-flow control for medium-to-large
projects.

---

## 🤔 Architecture Evolution: Why Do We Need LayerBase?

Throughout the lifecycle of a game project, the growth in business complexity inevitably drives the evolution of the
communication architecture. Looking back at common technology choices, the pain points become clear:

### 1. The Singleton Coupling Dilemma

In the early stages of a project, direct invocation is the most intuitive approach (e.g.,
`GameManager.Instance.UpdateHealth()`). However, in medium-to-large projects, when hundreds or thousands of Managers
exist, this approach degrades into an extremely complex web of references. The tight coupling between modules makes
refactoring and isolated testing nearly impossible.

### 2. The Timing and Performance Bottlenecks of Traditional EventBuses

To decouple modules, the industry widely adopted `Action` delegates, `UniRx`, or various generic `EventBus`
implementations. While this cuts off hard references, it introduces two hidden engineering disasters:

* **Implicit Timing Traps**: Without a unified architectural guideline, developers usually register events scattered
  across the lifecycles of various components (like `Awake` or `Start`). This **unordered registration behavior** turns
  the execution order of event responses into a black box. When an event is fired, you cannot guarantee the
  deterministic order of data settlement versus UI updates, making it a hotbed for sporadic bugs.
* **Underlying Performance Reefs**: The core implementation of traditional event buses generally relies on
  `Dictionary<Type, List<Delegate>>`. When faced with high-frequency interactions among massive numbers of entities,
  frequent dictionary hashing, delegate chain iteration, and crucially—**CPU Cache Misses caused by the lack of memory
  contiguity**—often restrict the system's processing ceiling to just a few million TPS.

### 3. The Breakthrough: Reconstructing Order and Speed

LayerBase's design philosophy is: **Tame chaotic registrations with a strongly constrained framework, and shatter
performance shackles with data-oriented reconstruction.**

1. **On Macro Architecture**: Abandon the free-for-all subscription pattern and introduce a
   `Layer -> Service -> Manager` three-tier progressive architecture. Through Dependency Injection (DI) and explicit
   topology layers, it provides the team with a complete mental model for collaborative development.
2. **On Micro Execution**: Draw inspiration from ECS frameworks. Adopt pure SOA array layouts for event routing at the
   lowest level. This ensures that the architecture not only standardizes the code but also achieves cache hit rates on
   par with top-tier C++/C# ECS frameworks.
3. **On Behaviour Modelling**: Introduce the Actor model to provide independent behavioral encapsulation for each
   entity—mailboxes, lifecycle, and event handlers all included, naturally suited for modeling game characters, NPCs,
   bullets, and other objects with autonomous behaviour. ECS handles data-intensive bulk processing while Actors handle
   behavioral logic encapsulation, seamlessly bridged through the Projection mechanism.

---

## ⚡ Performance Summary

LayerBase is designed for high-frequency event interactions, excelling in .NET 8/9 environments.

- **Extreme Low Latency**: Single event dispatch overhead as low as **1.6 ns**, Request/Response (Call) overhead approx
  **1.08 ns**.
- **Perfect Cache Affinity**: Underlying SOA layout and bitmask skipping algorithm minimize CPU Cache Misses.
- **Zero-Allocation**: Core dispatch paths and `LBTask` async paths achieve **0 GC Heap Allocation** when completed
  synchronously.

> For detailed benchmark data and comparisons, please refer to [docs/BENCHMARKS.md](docs/BENCHMARKS.md).

---

## 📊 Core Optimization Technologies

To break through performance ceilings, LayerBase implements several physical-level optimizations:

1. **SOA Data-Oriented Layout (Structure of Arrays)**: Dehydrates handlers of the same event type into contiguous native
   arrays, leveraging CPU sequential prefetching.
2. **Hardware-Level Bitmask Skipping**: Uses the `TrailingZeroCount` instruction for O(1) inter-layer addressing.
3. **Branchless and Unsafe Bounds Check Elimination**: Combines states via bitwise OR and uses `Unsafe` stepping to
   reduce branch prediction failures and eliminate array bounds checks.
4. **Async Path Short-circuit**: `LBTask`, optimized for games, completely eliminates state machine leasing overhead
   when completed synchronously.

---

## 🛡️ Industrial-Grade Infrastructure Guarantees

Beyond efficiency, LayerBase focuses on engineering robustness:

1. **Self-Healing Circuit Breaker**: Exception isolation and physical tripping; faulty nodes are automatically purged in
   the next frame.
2. **Zero-Allocation Async Ecosystem (`LBTask`)**: A struct-based Task model specifically tuned for game loops.
3. **Static Topology Audit**: Static scan during the Build phase to detect synchronous infinite loop risks early.
4. **Actor Object Pooling**: High-frequency creation/destruction Actors (e.g., bullets, VFX) can enable object pooling
   to avoid GC pressure.
5. **ECS Blueprint System**: Declarative entity structure definitions supporting component aggregation and Actor
   projection binding.

* Note: Handlers registered with **[SubscribeNotify]** do not capture exceptions for maximum performance; users must
  guarantee safety.

---

## 💾 Snap Snapshot Capability (New)

LayerBase now includes a built-in **runtime business snapshot** system for saving and restoring the Layer / Service /
Manager state that should actually be persisted.

It is intentionally **not** a full runtime memory image. Instead, it is an **explicit business-field snapshot** model:

- **`IFullSnap`**: participates in runtime-wide full snapshots, automatically collected after Build and orchestrated by `runtime.FullSnap`.
- **`IClipSnap<T>`**: a local state slice exported/imported explicitly by business code, outside the FullSnap document flow.
- **`SnapWriter` / `SnapReader`**: explicit field write/read APIs built on `System.Text.Json.Nodes`.
- **`SnapArrayWriter` / `SnapArrayReader`**: indexed array read/write helpers for inventories, entity batches, and other structured arrays.

### FullSnap Example

```csharp
using LayerBase.DI;
using LayerBase.Snap;

public sealed partial class BattleContext : ILayerContext, IFullSnap
{
    public int RoomId;
    public int CurrentFrame;

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("roomId", RoomId);
        writer.WriteInt32("currentFrame", CurrentFrame);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        RoomId = reader.ReadInt32("roomId");
        CurrentFrame = reader.ReadInt32("currentFrame");
    }
}
```

```csharp
var document = runtime.FullSnap.Serialize();
string json = runtime.FullSnap.SerializeJson();

runtime.FullSnap.Deserialize(document);
runtime.FullSnap.DeserializeJson(json);
```

### ClipSnap Example

```csharp
using LayerBase.Snap;

public sealed class BattleSyncService :
    IClipSnap<MoveClip>,
    IClipSnap<HealthClip>
{
    private float _x;
    private float _y;
    private int _hp;

    MoveClip IClipSnap<MoveClip>.Serialize() => new(_x, _y);
    void IClipSnap<MoveClip>.Deserialize(in MoveClip clip)
    {
        _x = clip.X;
        _y = clip.Y;
    }

    HealthClip IClipSnap<HealthClip>.Serialize() => new(_hp);
    void IClipSnap<HealthClip>.Deserialize(in HealthClip clip)
    {
        _hp = clip.Hp;
    }
}
```

```csharp
MoveClip move = syncService.Clip<MoveClip>().Serialize();
syncService.Clip<MoveClip>().Deserialize(in move);
```

### Objects Excluded from FullSnap by Default

The framework does **not** write the following into full snapshots by default:

- `EcsWorld`
- `ActorWorld`
- `ProjectedActor`
- Actor mailboxes / PostScheduler / Timer / Delay internal queues
- render objects, physics objects, threads, and `Task`

Recommended usage:

- **Bulk ECS state**: selectively export it from `IFullSnap.WriteFullSnap` via Query.
- **Important Actor state**: synchronize it explicitly through `IClipSnap<T>` or a Manager / Service.

> Note: `IClipSnap<T>` is only a **single-object, single-slice** export/import mechanism. It is not a cross-layer orchestration boundary. If a request needs multi-layer aggregation, broadcast, or workflow coordination, hiding that behind ClipSnap is usually a design smell.

---

## 📦 Installation Guide

1. **Quick Install via NuGet (Recommended)**:
   You can easily install LayerBase through the NuGet Package Manager:
   ```bash
   dotnet add package LayerBase --version 1.4.7.1
   ```
   If you need a fixed version, please refer to the NuGet page or `Version` in `LayerBase.csproj`.

2. **Source Code Integration**: Add the `LayerBase` and `LayerBase.Task` project directories from the repository
   directly to your solution and reference them.
3. **Configure Source Generator**:
   The framework relies on Source Generators to achieve zero-reflection in the event dispatch hot path. Ensure the
   analyzer is referenced in your main project (Note: DI assembly and shared field binding still utilize reflection
   during the Build phase, but are heavily optimized via metadata caching):
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **Environment Requirements**: Supports `.NET Standard 2.1` (perfectly compatible with Unity/Godot), but running on
   `.NET 8.0/9.0` is recommended.

---

## 📖 Best Practices Manual: Building a Clear Architecture

In large projects, flat architectures easily lead to convoluted module dependencies. LayerBase provides multiple
mental models to manage this complexity:

### Three-Tier Architecture (Layer → Service → Manager)

* 🌍 **Layer (Macro Level)**: **Handles processing priorities and physical boundaries.**
    * **Role**: Represents different tiers of the system (e.g., `RenderLayer`, `PhysicsLayer`, `CoreLogicLayer`). It is
      primarily used to isolate business logic of different priorities.
* 🏢 **Service (Business Service)**: **Function aggregation and scheduling.**
    * **Role**: Groups related functional modules (e.g., `PlayerService` grouping input, movement, animation). It
      handles Dependency Injection (DI) configurations, exposes coarse-grained interfaces outwardly, and manages
      Managers inwardly to achieve high cohesion.
* ⚙️ **Manager (Specific Logic Block)**: **The carrier of concrete business.**
    * **Role**: Adheres to the Single Responsibility Principle (SRP) to implement a specific micro-feature (like
      handling damage calculations). Managers generally avoid referencing each other directly, communicating entirely
      through the event bus to achieve low coupling.

### ECS Entity Component System (Data Layer)

* 📦 **Component**: Pure `struct` data blocks stored in SOA layout, CPU cache-friendly.
* 🔍 **Query**: Declarative component filters for batch-iterating entities matching specific criteria.
* 🏗️ **Blueprint**: Declarative entity structure definitions supporting component aggregation (Bundles) and Actor projection.

### Actor Behaviour Model (Behaviour Layer)

* 🎭 **Actor**: Independent behavioral encapsulation with its own mailbox and lifecycle.
* 📬 **Mailbox**: Event mailbox supporting queue, latest-value, merge, and other delivery policies.
* 🔄 **Projection**: ECS ↔ Actor bridge, seamlessly combining data-intensive processing with behavioral logic encapsulation.

Through this structure, the project's directory layout transparently reflects its system architecture.

---

### Step 1: Define Events (Event Structs)

To avoid GC pressure when events are triggered frequently, the framework requires all event objects to be declared as
`struct`:

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: Write Business Logic (Manager)

Managers inherit from `ILayerContext`, allowing them to perceive their layer and providing powerful event sending and
receiving capabilities. It is highly recommended to use attributes for binding; the compiler will automatically generate
the code to avoid reflection overhead.

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;
using LayerBase; // Includes ILayerContext, EventHandledState, etc.

// The class must be marked as partial to work with the source generator
public partial class DamageManager : ILayerContext
{
    // All Subscribe registrations preserve registration order as dispatch order.
    // [Safe Notify Handling]: using the [Subscribe] attribute (exception isolation)
    [Subscribe]
    private void OnTakeDamage(in DamageEvent e)
    {
        // Business logic handling...
        if (e.Amount > 100)
        {
            // Broadcast event globally
            this.Send(new PlayerDeathEvent()); 
        }
    }

    // [Synchronous Flow Handling]: using the [SubscribeFlow] attribute (supports truncation)
    [SubscribeFlow]
    private EventHandledState OnBeforeTakeDamage(in DamageEvent e)
    {
        if (e.Amount <= 0) return EventHandledState.Handled; // Truncate further processing
        return EventHandledState.Continue;
    }

    // [Asynchronous Event Handling]: using the [SubscribeAsync] attribute
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        // Supports GC-free delays using LBTask.Delay
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); 
    }
}

public partial class PlayerMoveManager : ILayerContext
{
    // [Pursuing extreme performance]: use the [SubscribeNotify] attribute
    // The user must guarantee correctness on their own; the framework does not capture, handle, or report exceptions here
    [SubscribeNotify]
    private void OnPlayer(in PlayerMoveEvent e)
    {
        // Example: in actual projects, you might use ECS or operate on data directly
        // ref var position = ref Player.Entity.Get<PositionComponent>();
        // position += e.Delta;
    }
}

using LayerBase.Option;

public partial class PlayerInputManager : ILayerContext, IUpdate
{
    // A non-blocking delayed event pipeline.
    // The most common use is as an input buffer queue, while letting the sender control how long the buffer exists.
    [SubscribeDelay] public IDelayPublisher<InputEvent> DelayNotify { get; set; }

    public void Update(float deltaTime)
    {
        // Try to take the currently pending value out of the pipeline; returns false if none exists
        if (DelayNotify.TryTake(out InputEvent inputEvent))
        {
            if (inputEvent.Value == InputType.Forward)
            {
                this.Send(new PlayerMoveEvent 
                { 
                    DeltaX = 0, 
                    DeltaY = 1 
                });
            }
        }
    }
}
```

* Note the execution order between event kinds inside the same layer:
  `SubscribeNotify -> Subscribe -> SubscribeFlow -> SubscribeAsync`.
* All events only guarantee that order is consistent with registration order within the same event type. They do not
  guarantee execution order across different event types.

### Step 3: Organize Business Modules (Service)

Services are responsible for registering related Managers into the DI container.
By using the `[OwnerLayer]` attribute, a Service can be statically bound to a specific Layer.

Additionally, you can use the `[Mount]` attribute for **declarative dependency injection**. The `[Mount]` attribute has
different semantics depending on its location:

#### 1. Layer Fields / Properties

```csharp
public partial class GameLogicLayer : Layer
{
    // Represents mounting an IService to the current Layer. 
    // The mounting order determines the priority of Updates and event registrations.
    [Mount] private CombatService _combatService;
}
```

#### 2. IService Fields / Properties

```csharp
public partial class CombatService : IService 
{
    // If the field type is a concrete ILayerContext implementation, the source generator automatically registers it:
    // services.TryAddScoped<DamageManager, DamageManager>()
    // and automatically performs injection at runtime.
    [Mount] private DamageManager _damageManager;
}
```

#### 3. Constructors

```csharp
public class DamageManager : ILayerContext
{
    // Represents selecting this constructor as the DI constructor.
    [Mount]
    public DamageManager(SomeDependency dep) { }
}
```

### Step 4: Spatial Event Triggering

LayerBase provides APIs for precise propagation direction control. Within a `Layer`, `Service`, or `Manager`, you can
call extension methods directly to dispatch events:

```csharp
// ⚔️ [Send Family: Synchronous execution, the current thread blocks until dispatch finishes]
this.Send(new DamageEvent()); // Broadcast across all layers

// 📨 [Post Family: Asynchronous delivery for work that is not urgent and can be spread across frames.]
this.Post(new DamageEvent()); 
this.MarkDirty<DamageEvent>();           // Merges post events, but payload has no content (only default)
this.PostLatest(new DamageEvent());      // Keeps only the last event
this.PostCoalesced(new DamageEvent());    // Merges events according to metadata logic, keeping only one

// ⏳ [Delay Family: Publish a timed event into the event buffer pipeline]
this.Delay(new PlayerDeathEvent(), 3.5f); // The event stays in the pipeline for 3.5 seconds until overwritten, expired, or taken out.
```

This design helps streamline event flows and reduces unnecessary cross-layer searching.

### Step 5: Engine Lifecycle Integration

When integrating LayerBase into a specific game engine (e.g., Unity's `MonoBehaviour`), two key lifecycles must be
handled: Build and Pump.

* **Build**: Called during the game's initialization phase to scan attributes, allocate SOA arrays, and statically audit
  infinite loops.
* **Pump**: Called in the per-frame update phase to process queued asynchronous events and delayed tasks.

```csharp
using UnityEngine;
using LayerBase;
using LayerBase.Layers;

// Define Layers
public class InteractionLayer : Layer { }
public class CoreLogicLayer : Layer { }

public class GameRoot : MonoBehaviour
{
    private LayerRuntime _runtime;

    void Awake()
    {
        // 1. Initialization phase: Construct topology
        _runtime = LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // Index 0: Upper Interaction Layer
                .Push(new CoreLogicLayer())   // Index 1: Lower Logic Layer
                .SetDebug()                   // Enable Debug mode to obtain the build graph via GetTopologySummary().
                .Build()                      // Automatically scan [OwnerLayer] and assemble
                .Prewarm();                   // [Optional] Prewarm step to trigger caches, event IDs, or policy paths early

        // Register the global message channel to capture framework exceptions
        LayerHub.OnLayerEventInfo +=
            info =>
            {
                Debug.LogError($"[{info.LayerIndex}][{info.EventName}][{info.Type}]: {info.Source}{info.Message}");
            };
    }

    void Update()
    {
        // 2. Runtime phase: Drive the event pump
        // Under idle states, the cost of calling Pump is negligible
        _runtime.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 Advanced Features Guide

### 1. PostFromAnyThread (Cross-thread Event Ingress)

`PostFromAnyThread` allows background threads to safely submit events, which will be processed during the next `Pump` on
the main thread.

```csharp
// Submit a calculation completion notification from a background thread
LayerHub.PostFromAnyThread(new DamageEvent
{
    TargetId = 1,
    Amount = 10
});

// Explicit policy specification is also supported
LayerHub.PostFromAnyThread(
    new DamageEvent { TargetId = 1, Amount = 10 },
    new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew)
);
```

### 2. PostScheduler Configuration

Control the Post queue, frame budget, and default backpressure policy via `PostSchedulerOptions`.

```csharp
var options = new PostSchedulerOptions(
    readyCapacity: 1024,
    nextCapacity: 1024,
    maxEventsPerPump: 5000,          // Max events processed per frame
    maxMillisecondsPerPump: 2.0,     // Time budget per frame (ms)
    maxWavesPerPump: 1,
    timeCheckInterval: 64,
    defaultBackpressure: BackpressurePolicy.RejectNew,
    maxCompletionsPerPump: 0,
    maxIngressPostsPerPump: 4096     // Max events moved from AnyThread ingress per frame
);
```

### 3. FixedUpdate Support

Enable fixed-step updates via `SetFixedUpdateOptions`. `Runtime.Pump(deltaTime)` will automatically handle time
accumulation.

```csharp
_runtime = LayerHub.CreateLayers()
    .Push(new MyLayer())
    .SetFixedUpdateOptions(new FixedUpdateOptions(
        enabled: true,
        fixedDeltaTime: 1f / 60f,
        maxStepsPerPump: 4 // Prevents excessive catch-up steps at low frame rates
    ))
    .Build();
```

### 4. Default Event Registration Semantics

Events that have been assigned an `EventTypeId<T>.Id` before `Build` will receive a default `Normal` policy in the
`PostScheduler`, even without explicit `MetaData`. This means any struct event can be directly `Post`ed as a normal
event.

### 5. Runtime Policy Dump & Event Identity

Export the current Runtime's event policy table and identity information for diagnostics:

```csharp
// Export Post, Timer, and Buffer policies for all events
var markdown = _runtime.GetPolicyMarkdown();
```

LayerBase uses `EventTypeId<T>.Id` for hot-path indexing at runtime, while retaining `StableId` and `StableKey` for
cross-version recording and diagnostics.

### 6. Fluent Filtering and Interception (Fluent API)

For scenarios requiring dynamic subscription conditions, LayerBase offers an elegant fluent API.

- Advantage: build business logic flexibly, suitable for fast early-stage development.
- Disadvantage: does not benefit from source-generator optimization; performance is lower.

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // Chainable calls: Subscribe -> Filter -> Handle
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .HandleFlow((in DamageEvent e) => 
            {
                // Handle damage...
                return EventHandledState.Handled;
            });
    }
}
```

### 7. Background Parallel Processing (Parallel Handlers)

When dealing with pure computation that consumes high CPU and **does not depend on or modify main-thread state**,
parallel subscriptions can be used.

> **Note**: Parallel handlers run in a separate thread pool; thread safety is not guaranteed and must be managed by the
> user.

```csharp
// Quickly bind parallel methods via attribute
[SubscribeParallel]
private void OnHeavyComputeTask(in ComputeEvent e)
{
    // This method is safely scheduled in a multi-threaded environment
}
```

### 8. Topology Snapshot

Call `LayerHub.GetTopologyMarkdown()` to get a detailed Markdown report covering all Layers, Event subscriptions, Call
routes, and Shared Fields across the system.

### 9. Threading Model

LayerBase currently adopts a **single-threaded Runtime model**.

Except for APIs with the `AnyThread` suffix, other Runtime APIs must default to being called from the Runtime thread (
usually the main thread).

**Owner-thread only:**

- `Send` / `Post` / `TryPost` / `PostLatest` / `PostCoalesced`
- `MarkDirty` / `CallAsync`
- `Pump` / `Build` / `Dispose` / `Reset`

**AnyThread:**

- `PostFromAnyThread`
- `TryPostFromAnyThread`

`PostFromAnyThread` does not dispatch events immediately. It enters a cross-thread ingress queue and is moved to the
`PostScheduler` during the next `Runtime.Pump`.

> **Note**: The current version does not perform dynamic thread checks in hot-path APIs. Calling normal APIs from the
> wrong thread is undefined behavior. Concurrent calls to `Dispose` / `Reset` with `PostFromAnyThread` are not
> recommended.

### 10. Runtime Build Lifecycle

The Build phase of LayerBase is divided into:

1. **Prebuild**
    - Assign Layer RouteIndex
    - Bind Runtime / EventCenter
    - Execute `PrepareBuild`
    - Execute Source Generator generated AutoBinding

2. **Infrastructure Initialization**
    - Initialize PostScheduler, Timer, and DelayManager
    - Construct the ServiceProvider container

3. **Layer Build**
    - `SharedFieldBinder` binding for shared fields
    - Execute `LifecycleBuild` / `AutoBind` / `Initialize`
    - Execute `PostBuild` and `RuntimeStart`
    - `EventGraphValidator` topology validation

4. **Runtime Dispose**
    - Execute `RuntimeStop`
    - `DisposeLayers` to release resources
    - Clear Scheduler / Timer / Delay queues and caches

---

### 11. ECS Entity Component System

LayerBase includes a high-performance ECS implementation (based on Arch.Core) deeply integrated with the event bus. ECS organizes data in a four-layer structure: **World → Archetype → Chunk → Entity**, using pure SOA memory layout that naturally aligns with CPU cache lines.

#### Core Concepts

| Concept | Description |
|---------|-------------|
| **World** | ECS world instance; each `LayerRuntime` auto-creates one, accessible via `runtime.EcsWorld` |
| **Entity** | Lightweight ID handle (`int Id` + `int Version`), representing a game object |
| **Component** | Pure `struct` data blocks attached to entities (e.g., `Position`, `Health`) |
| **Archetype** | Collection of entities sharing the same component composition, internally composed of contiguous Chunk arrays |
| **Chunk** | Fixed-capacity data block (default 256); component arrays within the same chunk are memory-contiguous |
| **Query** | Component filter for batch-iterating entities matching specific criteria |

#### Quick Start

```csharp
using Arch.Core;

// 1. Define components (pure struct)
public struct Position { public float X, Y, Z; }
public struct Velocity { public float Dx, Dy, Dz; }

// 2. Create entities
World world = _runtime.EcsWorld;
Entity player = world.Create(new Position { X = 0, Y = 0, Z = 0 },
                             new Velocity { Dx = 1, Dy = 0, Dz = 0 });

// 3. Query and iterate (SOA contiguous memory, zero GC)
var query = new QueryDescription().WithAll<Position, Velocity>();
world.Query(in query, (ref Position pos, in Velocity vel) =>
{
    pos.X += vel.Dx;
    pos.Y += vel.Dy;
    pos.Z += vel.Dz;
});
```

#### Blueprint System

Blueprints declaratively define entity structures, supporting component aggregation (Bundles) and Actor projection binding:

```csharp
// Define Bundle: an aggregated slice of related components
[LayerBundle]
public partial class TransformBundle : IBundle
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithComponent<Position>()
               .WithComponent<Rotation>()
               .WithComponent<Scale>();
    }
}

// Define Blueprint: complete entity structure declaration
[LayerBlueprint]
public partial class PlayerBlueprint : IEntityBlueprint
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithBundle<TransformBundle>()      // Expand Bundle
               .WithComponent<Health>()             // Append component
               .WithComponent<PlayerTag>()
               .WithProjectedActor<PlayerActor>();   // Bind Actor projection
    }
}
```

#### Projection Mechanism (ECS ↔ Actor Bridge)

Projection is LayerBase's core innovation—it combines ECS's data-intensive advantages with Actor's behavioral encapsulation:

- **Lazy Projection**: Entity creation only marks projection metadata; Actor instances are lazily created on first access
- **Auto-Reclamation**: When an entity is destroyed, its associated Actor is automatically returned to the object pool or destroyed
- **Query Bridge**: Via `[Query]` + `[Bring]` attributes, ECS query results can be directly delivered as Actor events

```csharp
// Using ECS queries in Managers
public partial class MovementManager : ILayerContext
{
    // Declarative query: source generator auto-generates batch iteration code
    [Query]
    [Bring<PositionChangedEvent>]
    private void OnMove(ref Position pos, in Velocity vel)
    {
        pos.X += vel.Dx;
        // Query results can be auto-projected as Actor events
    }
}

// Fluent query API (runtime dynamic construction)
this.Query<Position, Velocity>()
    .Where(entity => entity.Has<ActiveTag>())
    .ForEach((ref Position pos, in Velocity vel) =>
    {
        pos.X += vel.Dx;
    });
```

---

### 12. Actor Behaviour Model

The Actor model provides **independent behavioral encapsulation** for each entity—each Actor has its own mailbox, lifecycle, and event handlers, naturally suited for modeling game characters, NPCs, bullets, and other objects with autonomous behavior.

#### Core Concepts

| Concept | Description |
|---------|-------------|
| **IActor** | Actor interface marker; all Actor types must implement it |
| **ActorId** | Unique identifier (`ArchetypeId` + `SlotIndex` + `Generation`), with version number to prevent ABA |
| **ActorWorld** | Actor runtime container managing creation, destruction, event delivery, and lifecycle scheduling |
| **Mailbox** | Per-actor event mailbox supporting queue, latest-value, merge, and other delivery policies |
| **Behaviour** | Actor's event handler methods, marked with `[ActorBehaviour]` attribute |

#### Quick Start

```csharp
using LayerBase.Actor;

// 1. Define events
public struct DamageEvent
{
    public float Amount;
}

// 2. Define Actor
public sealed partial class EnemyActor : IActor
{
    public float Health { get; private set; } = 100f;

    [ActorBehaviour]
    private void OnDamage(in DamageEvent e)
    {
        Health -= e.Amount;
        if (Health <= 0)
        {
            // Destroy self
            this.Actors().DestroyActor(this.GetActorId());
        }
    }
}

// 3. Create and use
var world = _runtime.Actors;
EnemyActor enemy = world.CreateActor<EnemyActor>();

// Post event to Actor (async, enters mailbox)
world.PostTo(enemy.GetActorId(), new DamageEvent { Amount = 25f });

// Synchronous dispatch (immediate execution, bypasses mailbox)
world.DispatchNow(enemy.GetActorId(), new DamageEvent { Amount = 10f });
```

#### Actor Lifecycle

Actors implement corresponding interfaces to hook into engine lifecycle callbacks, automatically scheduled by `ActorLifecycleScheduler`:

```csharp
public sealed partial class BulletActor : IActor, IStart, IUpdate, IDestroy
{
    private float _lifetime;

    public void Start()
    {
        _lifetime = 0f;
    }

    public void Update(float deltaTime)
    {
        _lifetime += deltaTime;
        if (_lifetime > 5f)
            this.Actors().DestroyActor(this.GetActorId());
    }

    public void Destroy()
    {
        // Cleanup resources
    }
}
```

| Interface | Trigger Timing |
|-----------|---------------|
| `IStart` | First Pump after Actor creation |
| `IUpdate` | Every frame during Update phase |
| `IFixedUpdate` | Fixed-step physics frame |
| `ILateUpdate` | After Update phase |
| `IEnable` / `IDisable` | Enable/disable state transitions |
| `IDestroy` | Before Actor destruction |

#### Mailbox Delivery Policies

```csharp
// Queue mode (default): FIFO, processed per frame
world.PostTo(actorId, new DamageEvent { Amount = 10 });

// Latest mode: only keeps the last delivery
world.PostTo(actorId, new HealthChangedEvent { Current = 75 });

// Delayed delivery: takes effect after specified time
world.DelayPost(actorId, new PoisonDamageEvent { Amount = 5 }, delaySeconds: 2.0f);

// Ask/Call mode: request-response
LBTask<AttackResponse> response = world.Ask<AttackRequest, AttackResponse>(
    actorId, new AttackRequest { TargetId = 42 });
```

#### Actor Pooling

High-frequency creation/destruction Actors (e.g., bullets, VFX) can enable object pooling to avoid GC:

```csharp
public sealed partial class BulletActor : IActor, IPooledActor
{
    public long RecycleDeadlineTicks { get; set; }

    public void OnRent()  { /* Reset state when taken from pool */ }
    public void OnReturn(){ /* Cleanup when returned to pool */ }
}

// Prewarm the object pool
world.PrewarmPool<BulletActor>(1000);

// Create pooled Actor
BulletActor bullet = world.CreateActor<BulletActor>(usePool: true);

// Set pool limit
world.SetPoolLimit<BulletActor>(5000);

// Query pool statistics
ActorPoolStats stats = world.GetPoolStats<BulletActor>();
```

#### Tag and Group Classification

```csharp
// Define tags
public struct EnemyTag : IActorTag { }
public struct FriendlyTag : IActorTag { }

// Define groups
public struct CombatGroup : IActorGroup { }
public struct MovementGroup : IActorGroup { }

// Apply tags and groups
[Tag<EnemyTag>]
[Group<CombatGroup>]
public sealed partial class GoblinActor : IActor { }

// Query by tag/group
var enemies = world.Query()
    .AllTags<EnemyTag>()
    .AllGroups<CombatGroup>()
    .Build();
```

---

### 13. Call Request-Response Pattern

`Call` provides a type-safe synchronous/asynchronous request-response channel, suitable for scenarios requiring return values (e.g., scene transition confirmations, data queries):

```csharp
// Define request/response
public readonly struct QueryPlayerDataRequest
{
    public readonly int PlayerId;
}

public readonly struct QueryPlayerDataResponse
{
    public readonly string Name;
    public readonly int Level;
}

// Register handler in Layer or Service
public partial class PlayerLayer : Layer
{
    [Call]
    private LBTask<QueryPlayerDataResponse> HandleQuery(QueryPlayerDataRequest req)
    {
        var data = GetService<PlayerDatabase>().Get(req.PlayerId);
        return LBTask.FromResult(new QueryPlayerDataResponse
        {
            Name = data.Name,
            Level = data.Level
        });
    }
}

// Caller
var response = await runtime.CallAsync<QueryPlayerDataRequest, QueryPlayerDataResponse>(
    new QueryPlayerDataRequest { PlayerId = 42 });
```

---

### 14. Timer Scheduler

`TimerScheduler` provides timeline-based event scheduling capabilities, supporting one-time timers and frequency gating:

```csharp
// Get Timer instance
var timer = _runtime.Timer;

// Register one-time timer (triggers after 3 seconds)
timer.Schedule(TimeSpan.FromSeconds(3), () =>
{
    Console.WriteLine("3 seconds elapsed!");
});

// Set frequency gate (at most once per 0.5 seconds)
timer.SetFrequency(0.5);

// Drive in Tick
timer.Tick(deltaTime);
```

---

### 15. Job Background Task Scheduler

`JobScheduler` provides thread-pool-level background task scheduling, suitable for IO-intensive or compute-intensive async work:

```csharp
// Create scheduler (defaults to CPU core count threads)
using var scheduler = new JobScheduler(workerCount: 4);

// Submit background task
scheduler.Schedule(() =>
{
    // Execute time-consuming work on background thread
    var data = LoadAssetFromDisk("hero.png");

    // Post result back to main thread
    LayerHub.PostFromAnyThread(new AssetLoadedEvent { Data = data });
});
```

---

## ⚠️ Core Design Boundaries and Timing Constraints

To squeeze out extreme performance in a managed environment, LayerBase makes specific design trade-offs. Developers **must** be aware of these physical boundaries:

### 1. Fault Isolation: Single-Frame Interruption, Next-Frame Self-Healing

LayerBase utilizes a high-efficiency SOA batch dispatch engine. To maintain peak CPU cache affinity, the synchronous
dispatch loop does not wrap every individual Handler in a `try-catch`.

* **Behavior**: If a Handler throws an unhandled exception, **subsequent Handlers in the current dispatch will be
  skipped** to protect the call stack.
* **Healing**: The system instantly trips the breaker for the faulty node. In the **next frame (or next dispatch)**, the
  faulty node is purged, and the system restores normal operation.
* **Recommendation**: Wrap critical business logic in `try-catch` manually, or use `EventMetaData` for global
  observation.

### 2. Timing Contract: Synchronous Always Precedes Asynchronous

For any given Event type, regardless of registration order:

* **Synchronous Handlers** are always batch-executed first and have the authority to truncate the event (Handled).
* **Asynchronous Handlers** are only triggered after all synchronous logic has completed.
* **Conclusion**: Do not rely on a mixed registration order between sync and async handlers.

### 3. Layer Limit: Hard 64-Layer Boundary

To achieve O(1) cross-layer bitmap routing, LayerBase internally uses a `ulong` bitmap to track layer states.

* **Physical Limit**: A single instance supports a maximum of **64 Layers**.
* **Overflow Handling**: Attempting to `Push` a 65th layer will throw an `InvalidOperationException`.
  `.
