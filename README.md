# 🚀 LayerBase: 面向数据的高性能 C# 游戏架构总线

**LayerBase** 是一款专为 Unity、Godot 及纯 C# 服务端打造的高性能事件架构与通讯总线框架。

它打破了传统面向对象（OOP）事件总线的性能瓶颈，在底层采用了**面向数据设计（Data-Oriented Design, DOD）**与**SOA（Structure of Arrays）**内存布局。在保证业务代码极简、解耦的同时，为中大型项目提供规范的事件流转控制，实测单核 TPS 达到 **1.5 亿次/秒** 的物理级吞吐量。

---

## 🤔 架构演进：我们为什么需要 LayerBase？

在游戏项目的生命周期中，业务复杂度的增长往往会推动通讯架构的演进。回顾常见的技术选型，我们可以清晰地看到痛点所在：

### 1. 单例模式的耦合困境
在项目初期，直接调用是最直观的方式（例如 `GameManager.Instance.UpdateHealth()`）。但在中大型项目中，当系统里存在成百上千个 Manager 时，这种方式会促使代码演变为极度复杂的网状引用。模块间的紧密耦合让重构和独立测试变得举步维艰。

### 2. 传统事件总线（EventBus）的时序与性能瓶颈
为了实现模块解耦，业界普遍引入了 `Action` 委托、`UniRx` 或是泛型 `EventBus`。这确实切断了硬引用，但同时也引入了两个更为隐蔽的工程问题：

*   **隐式的时序陷阱**：在缺乏统一架构指导的情况下，开发者通常会在各个组件的生命周期（如 `Awake`、`Start`）中分散注册事件。这种**无序的注册行为**导致事件响应的先后顺序成为黑盒。当一个事件抛出时，你无法确保数据结算与 UI 表现的确定性顺序，这极易引发偶发性 Bug。
*   **底层的性能暗礁**：传统事件总线的底层普遍依赖 `Dictionary<Type, List<Delegate>>`。在面临海量实体（Entity）的高频交互时，字典的哈希计算、委托链的遍历，以及最关键的——**缺乏内存连续性导致的 CPU 缓存未命中（Cache Miss）**，通常会将系统的处理上限限制在百万级 TPS。

### 3. 破局：秩序与极速的重构
LayerBase 的设计哲学是：**用强约束的框架收编混乱的注册，用底层的面向数据重构打破性能的枷锁。**

1.  **在宏观架构上**：摒弃随地订阅的模式，引入 `Layer -> Service -> Manager` 三层递进的架构。通过依赖注入（DI）和明确的拓扑层级，让事件的流向和处理顺序重新回归绝对的确定性。
2.  **在底层执行上**：汲取 ECS 框架的核心思想，在底层采用纯粹的 SOA 数组布局进行事件路由。使得这一架构不仅规范了代码，更在性能上达到了与顶级 C++/C# ECS 框架同级别的缓存命中率。

---

## 📊 直观剖析：我们为什么这么快？

为了突破传统架构的性能天花板，LayerBase 在底层实施了全方位的物理级优化：

### 1. SOA 面向数据布局 (Structure of Arrays)
传统的 EventBus 在内存中是典型的 **AOS (Array of Structures)** 布局。派发事件时，CPU 必须在堆内存中进行多次非连续跳转：

```text
❌ 传统 EventBus 分发路径 (面临严重的 Cache Miss)
EventBus 
  └─> [哈希计算定位 Bucket] 
        └─> [读取 List 内存块] 
              └─> [跳转至 Handler 对象内存 (包含上下文/委托)] 
                    └─> 虚函数 Invoke 
```

而在 LayerBase 中，系统在构建期（Build）通过源生成器，将同类事件的所有处理器“拆解并脱水”，转化为连续的原生数组（**SOA 布局**）：

```text
✅ LayerBase 零分支分发引擎 (完美的 Cache 亲和性)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> 纯净的连续函数指针，CPU 极速顺序预取
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> 物理隔离同步与异步，消灭分支判断
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> 仅在抛出异常时才访问，绝不污染热路径
```
由于热路径中只剩下紧凑的委托指针，CPU L1/L2 缓存可以实现近乎完美的顺序预取（Prefetching）。

### 2. 硬件级位图跳跃 (Bitmask Skipping)
在跨层级分发（如全局广播）时，LayerBase 不遍历任何字典。每个层级的活跃状态被映射进一个 `ulong` 整数中。利用现代 CPU 指令（`BitOperations.TrailingZeroCount`），**仅需 1 个时钟周期的位运算**，即可精准计算出下一个存在订阅者的层级，将层级间的跳转开销降至最低。

### 3. 无分支与越界消除 (Branchless & Unsafe Offsets)
*   **位运算状态合并**：在核心循环中，将多个 Handler 的返回状态通过按位或（`|`）合并，大幅压缩了分支预测指令（Branch Prediction）。
*   **指针偏移**：在支持的运行时下，底层直接获取数组的原生指针并通过 `Unsafe.Add` 步进，彻底消除了 JIT 在循环内的数组边界检查（BCE）。

---

## ⚡ 标准基准测试 (BenchmarkDotNet)

测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路轰炸)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在包含 10 个物理层级的真实业务架构下，发送一百万次事件仅需 6 毫秒。即便在每一层都强行挂载逻辑的极端高压环境下，系统依然维持了约 6000 万的超高吞吐。对于性能预算极其吃紧的游戏主循环而言，LayerBase 的架构调度损耗已无限趋近于 0。

---

## 🛡️ 工业级基建保障

除了追求极致的运行效率，LayerBase 在工程稳健性上也提供了全套设施：

- **自愈熔断机制**：当事件 Handler 抛出未捕获异常时，系统会精准定位并物理熔断该节点，局部故障绝不阻塞同层其他业务。在下一帧，引擎通过“两段式零分配重建（Two-Pass Zero-Allocation Rebuild）”平滑剔除失效节点，实现系统自愈。
- **零分配异步生态 (`LBTask`)**：现代游戏开发高度依赖异步操作。框架内置了专为游戏帧循环调优的 `LBTask` 结构体任务。在同步完成路径下可实现 **0 GC 堆分配**，让异步逻辑免除内存抖动困扰。
- **静态拓扑审计**：在调用 `Build()` 构建层级时，底层的着色图算法（Three-Color Algorithm）会静态扫描整个事件网络。若发现**同步死循环**风险，控制台将直接打印环路警告，拒绝黑盒运行。

---

## 📦 安装指南

1. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
2. **配置源生成器 (Source Generator)**：
   框架依赖源生成器以实现零反射的特性自动绑定，请确保在主项目中引入了分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
3. **环境要求**：支持 `.NET Standard 2.1`（完美兼容 Unity/Godot），建议在 `.NET 8.0/9.0` 环境下运行以解锁完整的 `Unsafe` 硬件加速。

---

## 📖 最佳实践手册：构建清晰的架构

在大型项目中，扁平架构容易导致模块依赖复杂化。LayerBase 的三层结构旨在通过空间隔离来管理这种复杂性：

*   🌍 **Layer（宏观层级）**：**处理优先级与物理界限。**
    *   **职责**：代表系统的不同层面（如 `RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`）。它不包含具体业务逻辑，主要用于定义事件流向（Bubble 向上，Drop 向下）的边界，确保整体系统时序的确定性。
*   🏢 **Service（业务服务）**：**功能聚合与调度。**
    *   **职责**：将相关联的功能模块聚合在一起（例如 `PlayerService` 聚合输入、移动、动画等）。它负责依赖注入（DI）配置，对外暴露粗粒度接口，对内管理 Manager，实现高内聚。
*   ⚙️ **Manager（具体逻辑块）**：**具体业务的承载者。**
    *   **职责**：遵循单一职责原则 (SRP)，实现具体的微观功能（如处理伤害计算）。Manager 之间尽量避免直接引用，而是通过事件总线进行通讯，实现低耦合。

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

// 类需标记为 partial 配合源生成器
public partial class DamageManager : ILayerContext
{
    // 【同步事件处理】：使用 [Subscribe] 特性
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        // 业务逻辑处理
        
        if (e.Amount > 100)
        {
            // 向下坠落：将事件传递给更底层的系统
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // 返回 Continue: 允许其他同事件的 Manager 继续处理
        // 返回 Handled: 截断该事件的后续传播
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
```

### Step 3: 组织业务模块 (Service)
Service 负责将相关的 Manager 注册到 DI 容器中。
通过 `[OwnerLayer]` 特性，可以将 Service 静态绑定到指定的 Layer 层级。

```csharp
using LayerBase.DI;

// 绑定至 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 注册 Manager。此处注册的先后顺序即决定了同层级内事件响应的优先级。
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 空间维度的事件触发
LayerBase 提供了精确控制传播方向的 API。在 `Layer`、`Service` 或 `Manager` 内部，您可以直接调用扩展方法来派发事件：

```csharp
// 【Send 族：同步执行，当前执行流会等待分发完成】
this.SendLocal(new DamageEvent());  // 【同层】仅在当前层级广播
this.SendBubble(new DamageEvent()); // 【冒泡】向索引更小的上层抛出（如逻辑层发往UI层）
this.SendDrop(new DamageEvent());   // 【坠落】向索引更大的下层抛出（如UI层发往逻辑层）
this.SendGlobal(new DamageEvent()); // 【全局】穿透所有层级广播

// 【Post 族：异步投递，将事件压入队列，由下一帧的 Pump 处理】
this.PostBubble(new DamageEvent());
this.PostGlobal(new DamageEvent()); 

// 【Delay 族：定时投递】
this.DelayDrop(new PlayerDeathEvent(), 3.5f); // 在指定秒数后向下层级派发
```
这种设计有助于避免无向广播可能导致的循环触发和不必要的性能消耗。

### Step 5: 引擎生命周期整合
将 LayerBase 接入到具体的游戏引擎（如 Unity 的 `MonoBehaviour`）时，需处理两个关键生命周期：构建（Build）与心跳驱动（Pump）。

*   **Build**：在游戏初始化阶段调用，完成特性的扫描、SOA 数组分配及静态死循环审计。
*   **Pump**：在每帧更新阶段调用，用于处理队列中的异步事件和时间延迟任务。

```csharp
using UnityEngine;
using LayerBase.Layers;
using LayerBase.LayerHub;

public class GameRoot : MonoBehaviour
{
    // 定义层级
    public class InteractionLayer : Layer { }
    public class CoreLogicLayer : Layer { }

    void Awake()
    {
        // 1. 初始化期：构建拓扑
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // 索引 0: 上层交互
                .Push(new CoreLogicLayer())   // 索引 1: 下层逻辑
                .SetDebug()                   // 开启Debug模式可以从GetTopologySummary()获得整个系统的构建图。对性能影响不大。
                .Build();                     // 自动扫描 [OwnerLayer] 并装配

        //注册全局消息通道，用于捕获框架的异常
        LayerHub.OnLayerEventInfo +=
            info =>
            {
                GD.PrintErr($"[{info.LayerIndex}][{info.EventName}][{info.Type}]: {info.Source}{info.Message}");
            };
    }

    void Update()
    {
        // 2. 运行期：驱动事件泵
        // 在空闲状态下，Pump 的调用开销极小
        LayerHub.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 进阶特性指南

### 1. 流式过滤与拦截 (Fluent API)
对于需要动态控制订阅条件的场景，LayerBase 提供了优雅的链式 API。它最大的优势在于：拦截条件会在路由的最早期（包装委托内部）执行，不符合条件的事件会被直接短路，避免了不必要的函数调用。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 链式调用：订阅 -> 过滤 -> 处理
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .Handle((in DamageEvent e) => 
            {
                // 处理受击...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)
当面临高 CPU 消耗且**不依赖/不修改主线程状态**的纯计算逻辑（如寻路数据打包、耗时日志序列化）时，可使用并行订阅。事件将进入无锁队列并由 ThreadPool 在后台异步消化，保障主线程的帧率稳定。

```csharp
// 通过特性快速绑定并行方法
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 该方法在多线程环境中被安全调度
    return EventHandledState.Continue;
}
```

### 3. 拓扑结构可视化 (Topology Audit)
开启 Debug 模式后，调用 `GetTopologySummary()` 即可在控制台输出一张清晰的文本结构图，展示整个系统内各个 Layer 挂载了哪些 Manager，以及它们具体订阅/派发了什么事件。这在大型项目中是排查系统耦合度不可或缺的工具。

### 4. 切片式独立处理器 (Standalone EventHandler)
有时候，您可能有一段非常独立的逻辑，它只处理某一个特定事件，将其塞入一个庞大的 Manager 显得过于臃肿。LayerBase 支持“切片式”的独立处理器。
只需实现 `IEventHandler<T>` 或 `IEventHandlerAsync<T>` 接口，并挂载 `[OwnerLayer]` 特性，源生成器就会在编译期自动将其注册到对应的层级中：

```csharp
// 这是一个独立的、切片式的事件处理器
[OwnerLayer(typeof(GameLogicLayer))] 
public class PlayerDamageHandler : IEventHandler<DamageEvent>
{
    public EventHandledState Deal(in DamageEvent e)
    {
        // 专注处理伤害逻辑...
        return EventHandledState.Continue;
    }
}
```

### 5. 事件元数据与全局异常观察 (Event MetaData)
对于某些核心事件（如网络同步包或核心状态流转），如果在分发过程中有任何 Handler 抛出了异常，我们通常希望能在全局第一时间捕获，以进行统一的日志打点或崩溃上报。

LayerBase 提供了一套**无侵入式、零反射**的元数据（MetaData）注册方案：
您只需定义一个继承自 `EventMetaData<T>` 的类。源生成器会在编译期自动将其与您的事件绑定（**注：要求事件 `struct` 必须声明为 `partial`**）。

```csharp
// 1. 事件必须声明为 partial struct
public partial struct CoreSyncEvent 
{ 
    public byte[] Data; 
}

// 2. 定义该事件的元数据配置
public class CoreSyncEventMetaData : EventMetaData<CoreSyncEvent>
{
    // [可选]：定义事件所属的分类树，方便进行拓扑分类或模块检索
    public override EventCategoryToken Category => EventCatalogue.Path("Network", "Sync").GetToken();

    // 🏆 全局异常拦截点
    // 任何 Handler 在处理 CoreSyncEvent 抛出异常时，都会自动路由到这里！
    public override void OnEventExpectation(CoreSyncEvent e, Exception exception)
    {
        // 在这里进行统一的容错处理、日志打点或崩溃上报
        Console.WriteLine($"[严重错误] 同步事件处理失败: {exception.Message}");
    }
}
```