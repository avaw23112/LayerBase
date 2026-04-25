[English](#english) | [中文](#中文)

<a id="中文"></a>

# 🚀 LayerBase: 面向数据的高性能 C# 游戏架构总线

**LayerBase** 是一款专为 Unity、Godot 及纯 C# 服务端打造的高性能事件架构与通讯总线框架。它打破了传统面向对象（OOP）事件总线的性能瓶颈，在底层采用了
**面向数据设计（Data-Oriented Design, DOD）**与**SOA（Structure of Arrays**内存布局。在保证业务代码极简、解耦的同时，为中大型项目提供规范的事件流转控制。

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

---

## ⚡ 标准基准测试 (Standard Benchmarks)

### 跨框架增长对比：事件种类与扇出扩张

以下数据全部来自 `LayerBase.BenchMark.Compare/bin/Release/net8.0/BenchmarkDotNet.Artifacts/results`。我们分别观察两类增长：

1. 固定每事件订阅密度时，事件种类从 `32 -> 128 -> 256` 的批量扩张。
2. 单事件下订阅数从 `1 -> 4 -> 8 -> 16` 的扇出扩张。
3. 所有对比场景的 `Allocated` 均为 `0 B`。
4. 所有LayerBase的处理器都经过 **[SubscribeNotify]** 的特性纯化。

#### 不同事件种类下增长对比（固定批次，多事件）

**每事件 2 个订阅者**

| 事件种类 |  委托 批量耗时 | MessagePipe 批量耗时 |  LayerBase 批量耗时 | LayerBase 单事件均摊 | LayerBase 相对 32 事件增长 | MessagePipe 相对 32 事件增长 | LayerBase 相对 MessagePipe |
|:-----|---------:|-----------------:|----------------:|----------------:|---------------------:|-----------------------:|-------------------------:|
| 32   | 6.185 ns |       194.922 ns |  **196.616 ns** |    **6.144 ns** |                1.00x |                  1.00x |                    -0.9% |
| 128  | 27.48 ns |      1,259.72 ns | **1,286.73 ns** |   **10.053 ns** |                6.54x |                  6.46x |                    -2.1% |
| 256  | 57.95 ns |      2,669.05 ns | **2,671.42 ns** |   **10.435 ns** |               13.59x |                 13.69x |                    -0.1% |

**每事件 3 个订阅者**

| 事件种类 |   委托 批量耗时 | MessagePipe 批量耗时 |  LayerBase 批量耗时 | LayerBase 单事件均摊 | LayerBase 相对 32 事件增长 | MessagePipe 相对 32 事件增长 | LayerBase 相对 MessagePipe |
|:-----|----------:|-----------------:|----------------:|----------------:|---------------------:|-----------------------:|-------------------------:|
| 32   | 10.746 ns |       238.655 ns |  **197.714 ns** |    **6.179 ns** |                1.00x |                  1.00x |                   +17.2% |
| 128  |  93.67 ns |      1,426.10 ns | **1,320.44 ns** |   **10.316 ns** |                6.68x |                  5.98x |                    +7.4% |
| 256  | 362.05 ns |      2,943.14 ns | **2,870.13 ns** |   **11.211 ns** |               14.52x |                 12.33x |                    +2.5% |

#### 单事件不同订阅者数量下增长对比（100 万次 Notify）

| 订阅者数量 | C# event 单次耗时 | MessagePipe 单次耗时 | LayerBase 单次耗时 | C# event 相对 1 订阅增长 | MessagePipe 相对 1 订阅增长 | LayerBase 相对 1 订阅增长 | LayerBase 相对 MessagePipe |
|:------|--------------:|-----------------:|---------------:|-------------------:|----------------------:|--------------------:|-------------------------:|
| 1     |     0.3476 ns |        1.7939 ns |  **1.6117 ns** |              1.00x |                 1.00x |               1.00x |                   +10.2% |
| 4     |    10.8657 ns |        2.9491 ns |  **2.8477 ns** |             31.26x |                 1.64x |               1.77x |                    +3.4% |
| 8     |    19.2476 ns |        4.8975 ns |  **3.4861 ns** |             55.37x |                 2.73x |               2.16x |                   +28.8% |
| 16    |    35.9193 ns |        9.6484 ns |  **6.1484 ns** |            103.34x |                 5.38x |               3.81x |                   +36.3% |

* 在多事件且少量订阅者的模型中，我们的SOA架构并未发挥出其独有优势，因此只能做到贴近MessagePipe的性能。
* 但只要一个事件的订阅者数量超过3，那么SOA带来的稳定性与性能的提升就会披露头角。

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

在跨层级分发时，LayerBase 不遍历任何字典。每个层级的活跃状态被映射进一个 `ulong` 整数中。利用现代 CPU 指令（
`BitOperations.TrailingZeroCount`），**仅需 1 个时钟周期的位运算**，即可精准计算出下一个存在订阅者的层级，将层级间的跳转开销降至最低。

### 3. 无分支与越界消除 (Branchless & Unsafe Offsets)

* **位运算状态合并**：在核心循环中，将多个 Handler 的返回状态通过按位或（`|`）合并，大幅压缩了分支预测指令（Branch
  Prediction）。
* **指针偏移**：在支持的运行时下，底层直接获取数组的原生指针并通过 `Unsafe.Add` 步进，彻底消除了 JIT 在循环内的数组边界检查（BCE）。

### 4. 构建持久性数据

- 在层级构建之初便将一切计算完成，并将结果缓存在内存中。因此在热路径中，我们 **不做计算、不做查找、不做写入、永远只读**。

---

## 🛡️ 工业级基建保障

除了追求极致的运行效率，LayerBase 在工程稳健性上也提供了全套设施：

1. **自愈熔断机制**：当事件 Handler 抛出未捕获异常时，系统会精准定位并物理熔断该节点，局部故障绝不阻塞同层其他业务。在下一帧，引擎通过“两段式零分配重建（Two-Pass
   Zero-Allocation Rebuild）”平滑剔除失效节点，实现系统自愈。
2. **零分配异步生态 (`LBTask`)**：现代游戏开发高度依赖异步操作。框架内置了专为游戏帧循环调优的 `LBTask` 结构体任务。在同步完成路径下可实现
   **0 GC 堆分配**，让异步逻辑免除内存抖动困扰。
3. **静态拓扑审计**：在调用 `Build()` 构建层级时，底层的着色图算法（Three-Color Algorithm）会静态扫描整个事件网络。若发现*
   *同步死循环**风险，控制台将直接打印环路警告，拒绝黑盒运行。

* 注意：在使用 **[SubscribeNotify]** 特性来注册事件时，由于它极度纯化，因此该事件默认开发者自己管理异常，即它不使用异常熔断机制提供容错。

---

## 📦 安装指南

1. **NuGet 快速安装 (推荐)**：
   您可以通过 NuGet 包管理器直接安装 LayerBase：
   ```bash
   dotnet add package LayerBase --version 1.4.0
   ```
2. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
3. **配置源生成器 (Source Generator)**：
   框架依赖源生成器以实现零反射的特性自动绑定，请确保在主项目中引入了分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **环境要求**：支持 `.NET Standard 2.1`（完美兼容 Unity/Godot），但建议在 `.NET 8.0/9.0` 环境下运行。

---

## 📖 最佳实践手册：构建清晰的架构

在大型项目中，扁平架构容易导致模块依赖复杂化。LayerBase 的三层结构旨在通过空间隔离来管理这种复杂性：

* 🌍 **Layer（宏观层级）**：**处理优先级与物理界限。**
    * **职责**：代表系统的不同层面（如 `RenderLayer`、`PhysicsLayer`、`CoreLogicLayer`）。它主要用于隔离不同优先级的业务逻辑。
* 🏢 **Service（业务服务）**：**功能聚合与调度。**
    * **职责**：将相关联的功能模块聚合在一起（例如 `PlayerService` 聚合输入、移动、动画等）。它负责依赖注入（DI）配置，对外暴露粗粒度接口，对内管理
      Manager，实现高内聚。
* ⚙️ **Manager（具体逻辑块）**：**具体业务的承载者。**
    * **职责**：遵循单一职责原则 (SRP)，实现具体的微观功能（如处理伤害计算）。Manager 之间尽量避免直接引用，而是通过事件总线进行通讯，实现低耦合。

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
    //  所有Subscribe都保证注册的先后顺序就是它在事件调度中的顺序。
    // 【同步事件处理】：使用 [Subscribe] 特性
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        // 业务逻辑处理
        
        if (e.Amount > 100)
        {
            // 向全局广播事件
            this.Send(new PlayerDeathEvent()); 
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

public partial class PlayerMoveManager : ILayerContext
{
    // 【追求极致性能】：使用 [SubscribeNotify] 特性
    //  必须由用户自身担保逻辑的正确性，框架不负责捕捉、处理、上报异常
    [SubscribeNotify]
    private void OnPlayer(in PlayerMoveEvent e)
    {
        ref PositionComponent = ref Player.Entity.Get<PositionComponent>();
        PositionComponent += e.delta;
    }
}

using LayerBase.Option;

public partial class PlayerInputManager : ILayerContext,IUpdate
{
    //一个非阻塞的定时事件管道。
    //最常用的就是把它当做输入缓冲队列，且可在发送端调整缓冲存在的时间
    [SubscribeDelay] public IDelayPublisher<InputEvent> DelayNotify { get; set; }

    void Update(float deltaTime)
    {
        //尝试获取当前挂起的数值（取出管道内的事件）,如果没有则返回False
        if(DelayNotify.TryTake(out InputEvent inputEvent))
        {
            if(inputEvent.Value == InputType.Forward)
            {
                this.Send(new PlayerMoveEvent (){e.delat = new Vector2(0,1)});
            }
        }
    }
}

```

* 注意事件事件类型的执行顺序：在同一层内 由Notify -> sync -> async.
* 所有事件都只能保证在同类型的事件中顺序和注册一致，不能保证不同事件类型的事件执行顺序和注册顺序一致.

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
        services.AddScoped<DamageManager, DamageManager>();
    }
}
```

### Step 4: 空间维度的事件触发

LayerBase 提供了精确控制传播范围的 API。在 `Layer`、`Service` 或 `Manager` 内部，您可以直接调用扩展方法来派发事件：

```csharp
// 【Send 族：同步执行，当前执行流会等待分发完成】
this.SendLocal(new DamageEvent());  // 【同层】仅在当前层级广播
this.Send(new DamageEvent()); // 【全局】穿透所有层级广播

// 【Post 族：异步投递，用在一些不紧急，可分帧执行的任务中。
this.PostLocal(new DamageEvent());
this.Post(new DamageEvent()); 

// 【Delay 族：向事件缓冲管道发布一个定时的事件】
this.DelayGlobal(new PlayerDeathEvent(), 3.5f); // 该事件会在管道中存在3.5秒，直到有新的事件覆盖、到时间自动消亡、或者被取出。
this.DelayLocal(new PlayerDeathEvent(), 3.5f); 

// 注意：全局事件都是是经过特殊优化的，性能比其他种类都要好。
LayerHub.Send(new MyEvent());
LayerHub.Post(new MyEvent());
```

这种设计有助于精简事件流，减少不必要的跨层搜索。

### Step 5: 引擎生命周期整合

将 LayerBase 接入到具体的游戏引擎（如 Unity 的 `MonoBehaviour`）时，需处理两个关键生命周期：构建（Build）与心跳驱动（Pump）。

* **Build**：在游戏初始化阶段调用，完成特性的扫描、SOA 数组分配及静态死循环审计。
* **Pump**：在每帧更新阶段调用，用于处理队列中的异步事件和时间延迟任务。

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
                .SetDebug()                   // 开启Debug模式可以从GetTopologyMarkdown()获得整个系统的构建图。
                .Build();                     // 自动扫描 [OwnerLayer] 并装配

        //注册全局消息通道，用于捕获框架的异常
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
        LayerHub.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 进阶特性指南

### 1. 流式过滤与拦截 (Fluent API)

对于需要动态控制订阅条件的场景，LayerBase 提供了优雅的链式 API

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
            .Handle((in DamageEvent e) => 
            {
                // 处理受击...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)

当面临高 CPU 消耗且**不依赖/不修改主线程状态、长时间运作**的纯计算逻辑（如寻路数据打包、耗时日志序列化）时，可使用并行订阅。事件将进入无锁队列并由
ThreadPool 在后台异步消化，保障主线程的帧率稳定。

```csharp
// 通过特性快速绑定并行方法
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 该方法在多线程环境中被安全调度
    return EventHandledState.Continue;
}
```

### 3. 拓扑结构可视化 (Topology Snapshot)

调用 `LayerHub.GetTopologyMarkdown()` 即可获得一份详细的 Markdown 表格，展示整个系统内各个 Layer 挂载了哪些
Manager，以及它们具体订阅/派发了什么事件。

### 4. 事件元数据与全局异常观察 (Event MetaData)

对于某些核心事件（如网络同步包或核心状态流转），如果在分发过程中有任何 Handler 抛出了异常，我们通常希望能在全局第一时间捕获，以进行统一的日志打点或崩溃上报。

LayerBase 提供了一套**无侵入式、零反射**的元数据（MetaData）注册方案：
您只需定义一个继承自 `EventMetaData<T>` 的类。源生成器会在编译期自动将其与您的事件绑定（**注：要求事件 `struct`
必须声明为 `partial`**）。

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

### 5. 本地仿RPC调用 (Layer Call Subsystem)

在某些场景下，您需要的是**点对点**的 Request/Response 调用，而不是基于广播的事件分发。LayerBase 提供了 `Call` 子系统：

* **定位**：单目标功能切片调用，用于明确的跨层服务交互。
* **功能**：按层寻址、单目标命中、支持 `await` 异步等待、具有明确返回值。
* **约束**：不参与广播传播。每个 Request 类型在编译期强制绑定至唯一 Layer 和 Response 类型。

```csharp
// 这是一个独立的、切片式的事件处理器
[OwnerLayer(typeof(CoreLayer))]
public sealed class ServiceLookupCallHandler : ILayerCallHandler<ServiceLookupRequest, ServiceLookupResponse>
{
    public async LBTask<ServiceLookupResponse> HandleAsync(ServiceLookupRequest request,
                                                           CancellationToken cancellationToken = default)
    {
        //可从目标层的DI容器中取出功能模块
        var sceneService = this.Get<SceneService>();
        //进行异步的场景切换功能
        await sceneService.SwitchTo(request.Value);
        //直接返回响应
        return new ServiceLookupResponse(sceneService.LastScene);
    }
}

// 外部调用
ServiceLookupResponse resp = await LayerHub.CallAsync<CoreLayer, ServiceLookupRequest, ServiceLookupResponse>(new ServiceLookupRequest());
```

### 6. 声明式共享内存 (Shared Field v2)

解决组件间在不建立显式引用关系的情况下，如何安全、高效地共享内存状态。

* **显式边界**：不再使用枚举推断范围，而是通过 `(Type ownerType, string localKey)` 显式指定状态的归属。
* **自动生命周期**：标记为 `[Public]` 的字段在 `Build()` 阶段会自动实例化（支持引用类型 and 值类型）。
* **强制只读**：`[From]` 端只能接收只读投影（如 `IReadOnlyList<T>`），禁止拿走可写容器（如 `List<T>`），彻底杜绝逻辑后门。

```csharp
public sealed class InventoryModule : ILayerContext
{
    // 在 Service 范围内发布一个状态，Key 为 "items"
    [Public(typeof(InventoryService), "items")]
    private List<string> _items = new();
}

public sealed class HudModule : ILayerContext
{
    // 从指定的 Service 中引用该状态，强制使用只读接口
    [From(typeof(InventoryService), "items")]
    private IReadOnlyList<string> _items = default!;
}

// 全局作用域请使用 typeof(GlobalScope)
[Public(typeof(GlobalScope), "config")]
private AppConfig _config;
```

### 7. 注册总览与健康审计 (Topology & Health Audit)

在大型项目中，排查“谁发了消息、谁在听、为什么没响应”是极大的负担。LayerBase v1.4.0 引入了全量的拓扑快照。

* **一键导出**：调用 `LayerHub.GetTopologyMarkdown()` 即可获得一份详细的 Markdown 表格，涵盖所有 Layer、Event 订阅关系、Call
  路由以及共享字段。
* **僵尸代码审计**：自动识别并标记：
    * **Zombie Event**：有订阅但没人发的事件。
    * **Unused Producer**：有人发但没人听的事件。
    * **Dead Call**：定义了 Handler 但从未被调用的 Call。
    * **Orphaned Public**：发布了状态但全项目没人引用的 Key。

### 8. Roslyn 智能开发体验 (Diagnostics & Code Fixes)

为了降低心智负担，框架自带了深度集成的 Roslyn 插件：

* **编译期拦截**：非法的方法签名、`[Public]` 键位碰撞、`[From]` 类型不匹配、类缺少 `partial` 关键字，都会在**按下 F5 之前**以
  Error 形式提示。
* **一键修复 (Code Fix)**：按下 `Alt+Enter`，自动修正订阅方法签名、自动补全 `async` 关键字、自动同步共享字段类型。

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

* **同步 Handler** 总是会被优先批量执行，并具备截断事件（Handled）的能力。
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

---

## ⚡ Standard Benchmarks

### Cross-Framework Growth Comparison: Event Variety and Fan-out Expansion

The following figures are taken directly from
`LayerBase.BenchMark.Compare/bin/Release/net8.0/BenchmarkDotNet.Artifacts/results`. They cover two growth dimensions:

1. Batch expansion as the number of event kinds grows from `32 -> 128 -> 256` while keeping the subscriber density per
   event fixed.
2. Fan-out expansion for a single event as subscriber count grows from `1 -> 4 -> 8 -> 16`.
3. All comparison cases report `Allocated = 0 B`.
4. Every LayerBase handler in these comparisons is purified through the **[SubscribeNotify]** attribute path.

#### Growth by Event Kind Count (fixed batch, many event types)

**2 subscribers per event**

| Event Kinds | Delegate Batch Cost | MessagePipe Batch Cost | LayerBase Batch Cost | LayerBase Avg/Event | LayerBase Scale vs 32 | MessagePipe Scale vs 32 | LayerBase vs MessagePipe |
|:------------|--------------------:|-----------------------:|---------------------:|--------------------:|----------------------:|------------------------:|-------------------------:|
| 32          |            6.185 ns |             194.922 ns |       **196.616 ns** |        **6.144 ns** |                 1.00x |                   1.00x |                    -0.9% |
| 128         |            27.48 ns |            1,259.72 ns |      **1,286.73 ns** |       **10.053 ns** |                 6.54x |                   6.46x |                    -2.1% |
| 256         |            57.95 ns |            2,669.05 ns |      **2,671.42 ns** |       **10.435 ns** |                13.59x |                  13.69x |                    -0.1% |

**3 subscribers per event**

| Event Kinds | Delegate Batch Cost | MessagePipe Batch Cost | LayerBase Batch Cost | LayerBase Avg/Event | LayerBase Scale vs 32 | MessagePipe Scale vs 32 | LayerBase vs MessagePipe |
|:------------|--------------------:|-----------------------:|---------------------:|--------------------:|----------------------:|------------------------:|-------------------------:|
| 32          |           10.746 ns |             238.655 ns |       **197.714 ns** |        **6.179 ns** |                 1.00x |                   1.00x |                   +17.2% |
| 128         |            93.67 ns |            1,426.10 ns |      **1,320.44 ns** |       **10.316 ns** |                 6.68x |                   5.98x |                    +7.4% |
| 256         |           362.05 ns |            2,943.14 ns |      **2,870.13 ns** |       **11.211 ns** |                14.52x |                  12.33x |                    +2.5% |

#### Growth by Subscriber Count for a Single Event (1,000,000 Notify calls)

| Subscribers | C# event Cost/Notify | MessagePipe Cost/Notify | LayerBase Cost/Notify | C# event Scale vs 1 | MessagePipe Scale vs 1 | LayerBase Scale vs 1 | LayerBase vs MessagePipe |
|:------------|---------------------:|------------------------:|----------------------:|--------------------:|-----------------------:|---------------------:|-------------------------:|
| 1           |            0.3476 ns |               1.7939 ns |         **1.6117 ns** |               1.00x |                  1.00x |                1.00x |                   +10.2% |
| 4           |           10.8657 ns |               2.9491 ns |         **2.8477 ns** |              31.26x |                  1.64x |                1.77x |                    +3.4% |
| 8           |           19.2476 ns |               4.8975 ns |         **3.4861 ns** |              55.37x |                  2.73x |                2.16x |                   +28.8% |
| 16          |           35.9193 ns |               9.6484 ns |         **6.1484 ns** |             103.34x |                  5.38x |                3.81x |                   +36.3% |

* In the model with many event kinds and only a small number of subscribers per event, our SOA architecture does not yet
  show its unique advantage, so it can only stay close to MessagePipe in performance.
* But once a single event has more than 3 subscribers, the stability and performance uplift brought by SOA starts to
  become clear.

---

## 📊 Visual Breakdown: Why Are We So Fast?

To break through the performance ceiling of traditional architectures, LayerBase implements comprehensive physical-level
optimizations at the bottom layer:

### 1. SOA Data-Oriented Layout (Structure of Arrays)

A traditional EventBus uses a typical **AOS (Array of Structures)** layout in memory. When dispatching an event, the CPU
must perform multiple expensive, non-contiguous memory jumps:

```text
❌ Traditional EventBus Dispatch Path (Severe Cache Misses)
EventBus 
  └─> [Hash calculation to locate Bucket] 
        └─> [Read List memory block] 
              └─> [Jump to Handler Object memory (Contains Context/Delegate)] 
                    └─> Virtual function Invoke 
```

In contrast, LayerBase uses source generators during the build phase to "dismantle and dehydrate" all handlers for the
same event type, transforming them into contiguous native arrays (**SOA Layout**):

```text
✅ LayerBase Branchless Dispatch Engine (Perfect Cache Affinity)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> Pure contiguous function pointers, CPU ultra-fast sequential prefetch
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> Physically isolated sync & async, eliminating branch checks
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> Accessed ONLY when exceptions are thrown, never polluting the hot path
```

Since only compact delegate pointers remain in the hot path, the CPU's L1/L2 cache achieves near-perfect sequential
prefetching.

### 2. Hardware-Level Bitmask Skipping

When dispatching across layers, LayerBase does not iterate through any dictionaries. The active state of each layer is
mapped into a single `ulong` integer. Utilizing modern CPU instructions ( `BitOperations.TrailingZeroCount`), **a single
clock cycle of bitwise operation** can precisely calculate the next layer containing a subscriber, minimizing
inter-layer jump overhead.

### 3. Branchless and Unsafe Bounds Check Elimination

* **Bitwise State Aggregation**: In the core loop, the return states of multiple Handlers are merged using bitwise OR (
  `|`), drastically compressing branch prediction instructions.
* **Pointer Offsets**: In supported runtimes, the underlying engine directly acquires the native pointer of the array
  and steps through it via `Unsafe.Add`, completely eliminating JIT array bounds check (BCE) instructions inside the
  loop.

### 4. Build-Time Persistent Data

- Everything is computed during layer construction and cached in memory from the very beginning. Therefore, on the hot
  path, we **do not compute, do not look up, do not write, and only ever read**.

---

## 🛡️ Industrial-Grade Infrastructure Guarantees

Beyond pursuing extreme execution efficiency, LayerBase also provides a complete suite of facilities for engineering
robustness:

1. **Self-Healing Circuit Breaker**: When an event Handler throws an unhandled exception, the system instantly locates
   and physically trips the breaker for that specific node. Local failures will never block other businesses in the same
   layer. In the next frame, the engine smoothly purges the invalid node via a "Two-Pass Zero-Allocation Rebuild",
   achieving system self-healing.
2. **Zero-Allocation Asynchronous Ecosystem (`LBTask`)**: Modern game development relies heavily on async operations.
   The framework comes with `LBTask`, a struct-based task model specifically tuned for the game loop. It achieves **0 GC
   Heap Allocation** on synchronously completed paths, freeing your async logic from memory jitter nightmares.
3. **Static Topology Audit**: During `Build()` to construct the layers, the underlying Three-Color Algorithm statically
   scans the entire event network. If a **synchronous infinite loop** risk is detected, a loop warning is printed
   directly to the console, refusing black-box execution.

* Note: when registering handlers with the **[SubscribeNotify]** attribute, the path is extremely purified. That means
  exception handling is left to the developer by default, and this path does not use the circuit-breaker fault-tolerance
  mechanism.

---

## 📦 Installation Guide

1. **Quick Install via NuGet (Recommended)**:
   You can easily install LayerBase through the NuGet Package Manager:
   ```bash
   dotnet add package LayerBase --version 1.4.0
   ```
2. **Source Code Integration**: Add the `LayerBase` and `LayerBase.Task` project directories from the repository
   directly to your solution and reference them.
3. **Configure Source Generator**:
   The framework relies on Source Generators to achieve zero-reflection attribute binding. Ensure the analyzer is
   referenced in your main project:
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
4. **Environment Requirements**: Supports `.NET Standard 2.1` (perfectly compatible with Unity/Godot), but running on
   `.NET 8.0/9.0` is recommended.

---

## 📖 Best Practices Manual: Building a Clear Architecture

In large projects, flat architectures easily lead to convoluted module dependencies. LayerBase's three-tier structure
manages this complexity through spatial isolation:

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

// The class must be marked as partial to work with the source generator
public partial class DamageManager : ILayerContext
{
    // All Subscribe registrations preserve registration order as dispatch order.
    // [Synchronous Event Handling]: using the [Subscribe] attribute
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        // Business logic handling...
        
        if (e.Amount > 100)
        {
            // Broadcast event globally
            this.Send(new PlayerDeathEvent()); 
        }
        
        // Return Continue: allow other Managers in the same or subsequent layers to process this event
        // Return Handled: truncate further propagation of this event
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
        ref PositionComponent = ref Player.Entity.Get<PositionComponent>();
        PositionComponent += e.delta;
    }
}

using LayerBase.Option;

public partial class PlayerInputManager : ILayerContext,IUpdate
{
    // A non-blocking delayed event pipeline.
    // The most common use is as an input buffer queue, while letting the sender control how long the buffer exists.
    [SubscribeDelay] public IDelayPublisher<InputEvent> DelayNotify { get; set; }

    void Update(float deltaTime)
    {
        // Try to take the currently pending value out of the pipeline; returns false if none exists
        if(DelayNotify.TryTake(out InputEvent inputEvent))
        {
            if(inputEvent.Value == InputType.Forward)
            {
                this.Send(new PlayerMoveEvent (){e.delat = new Vector2(0,1)});
            }
        }
    }
}
```

* Note the execution order between event kinds inside the same layer: `Notify -> sync -> async`.
* All events only guarantee that order is consistent with registration order within the same event type. They do not
  guarantee execution order across different event types.

### Step 3: Organize Business Modules (Service)

Services are responsible for registering related Managers into the DI container.
By using the `[OwnerLayer]` attribute, a Service can be statically bound to a specific Layer.

```csharp
using LayerBase.DI;
using LayerBase.Layers;

// Bound to the GameLogicLayer
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    public void ConfigureServices(IServiceCollection services) 
    { 
        // Register Managers. The order of registration dictates the priority of event responses within this layer.
        services.AddScoped<DamageManager, DamageManager>();
    }
}
```

### Step 4: Spatial Event Triggering

LayerBase provides APIs for precise propagation direction control. Within a `Layer`, `Service`, or `Manager`, you can
call extension methods directly to dispatch events:

```csharp
// ⚔️ [Send Family: Synchronous execution, the current thread blocks until dispatch finishes]
this.SendLocal(new DamageEvent());  // [Local] Broadcast only within the current layer
this.Send(new DamageEvent()); // [Global] Penetrate and broadcast across all layers

// 📨 [Post Family: Asynchronous delivery for work that is not urgent and can be spread across frames.
this.PostLocal(new DamageEvent());
this.Post(new DamageEvent()); 

// ⏳ [Delay Family: Publish a timed event into the event buffer pipeline]
this.DelayGlobal(new PlayerDeathEvent(), 3.5f); // The event stays in the pipeline for 3.5 seconds until overwritten, expired, or taken out.
this.DelayLocal(new PlayerDeathEvent(), 3.5f); 

// Note: global events are specially optimized and perform better than other kinds.
LayerHub.Send(new MyEvent());
LayerHub.Post(new MyEvent());
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
using LayerBase.Layers;
using LayerBase.LayerHub;

public class GameRoot : MonoBehaviour
{
    // Define Layers
    public class InteractionLayer : Layer { }
    public class CoreLogicLayer : Layer { }

    void Awake()
    {
        // 1. Initialization phase: Construct topology
        LayerHub.CreateLayers()
                .Push(new InteractionLayer()) // Index 0: Upper Interaction Layer
                .Push(new CoreLogicLayer())   // Index 1: Lower Logic Layer
                .SetDebug()                   // Enable Debug mode to obtain the build graph via GetTopologySummary().
                .Build();                     // Automatically scan [OwnerLayer] and assemble

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
        LayerHub.Pump(Time.deltaTime);
    }
}
```

---

## 🛠 Advanced Features Guide

### 1. Fluent Filtering and Interception (Fluent API)

For scenarios requiring dynamic subscription conditions, LayerBase offers an elegant fluent API.

- Advantage: it lets you build business logic flexibly and is suitable for fast early-stage development.
- Disadvantage: it does not benefit from source-generator optimization, so performance is lower.

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // Chainable calls: Subscribe -> Filter -> Handle
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) 
            .Handle((in DamageEvent e) => 
            {
                // Handle damage...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. Background Parallel Processing (Parallel Handlers)

When dealing with pure computation that consumes high CPU, **does not depend on or modify main-thread state, and runs
for a long time** (such as pathfinding data packaging or heavy log serialization), parallel subscriptions can be used.
Events enter a lock-free queue and are consumed asynchronously by the ThreadPool in the background, protecting
main-thread frame stability.

```csharp
// Quickly bind parallel methods via attribute
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // This method is safely scheduled in a multi-threaded environment
    return EventHandledState.Continue;
}
```

### 3. Topology Snapshot

Call `LayerHub.GetTopologyMarkdown()` to get a detailed Markdown report covering all Layers, Event subscriptions, Call
routes, and Shared Fields across the system.

### 4. Event MetaData and Global Exception Observation

For critical events (like network sync packets or core state transitions), if any Handler throws an exception during
dispatch, we generally want to catch it globally and immediately for unified logging or crash reporting.

LayerBase provides a **non-intrusive, zero-reflection** MetaData registration solution:
You simply define a class inheriting from `EventMetaData<T>`. The source generator will automatically bind it to your
event at compile time (**Note: the event `struct` must be declared as `partial`**).

```csharp
// 1. The event must be declared as a partial struct
public partial struct CoreSyncEvent 
{ 
    public byte[] Data; 
}

// 2. Define the metadata configuration for the event
public class CoreSyncEventMetaData : EventMetaData<CoreSyncEvent>
{
    // [Optional]: Define a category tree for the event, useful for topological categorization or module retrieval
    public override EventCategoryToken Category => EventCatalogue.Path("Network", "Sync").GetToken();

    // 🏆 Global Exception Interception Point
    // Any unhandled exception thrown by a Handler processing CoreSyncEvent will be automatically routed here!
    public override void OnEventExpectation(CoreSyncEvent e, Exception exception)
    {
        // Handle fault tolerance, logging, or crash reporting here
        Console.WriteLine($"[Critical Error] Sync event processing failed: {exception.Message}");
    }
}
```

### 5. Layer-Addressable Calls (Layer Call Subsystem)

In certain scenarios, you need **point-to-point** Request/Response calls instead of broadcast-based event dispatching.
LayerBase provides the `Call` subsystem:

* **Positioning**: A single-target functional slice call used for explicit cross-layer service interactions.
* **Features**: Layer-addressed routing, single-target resolution, `await`-friendly async execution, and an explicit
  response value.
* **Constraints**: It does not participate in broadcast propagation. Each Request type is bound at compile time to
  exactly one Layer and one Response type.

```csharp
// This is an independent slice-style call handler
[OwnerLayer(typeof(CoreLayer))]
public sealed class ServiceLookupCallHandler : ILayerCallHandler<ServiceLookupRequest, ServiceLookupResponse>
{
    public async LBTask<ServiceLookupResponse> HandleAsync(ServiceLookupRequest request,
                                                           CancellationToken cancellationToken = default)
    {
        // Resolve the functional module from the target layer's DI container
        var sceneService = this.Get<SceneService>();
        // Perform the async scene switch
        await sceneService.SwitchTo(request.Value);
        // Return the response directly
        return new ServiceLookupResponse(sceneService.LastScene);
    }
}

// External call
ServiceLookupResponse resp = await LayerHub.CallAsync<CoreLayer, ServiceLookupRequest, ServiceLookupResponse>(new ServiceLookupRequest());
```

### 6. Declarative Shared Memory (Shared Field v2)

Safely and efficiently share memory state between components without establishing explicit references.

* **Explicit Boundaries**: Instead of implicit enums, use `(Type ownerType, string localKey)` to explicitly specify
  state ownership.
* **Automatic Lifecycle**: Fields marked `[Public]` are automatically instantiated during `Build()` (supports both
  reference and value types).
* **Strict Read-Only**: `[From]` side can only receive read-only projections (e.g., `IReadOnlyList<T>`), forbidding
  writable containers (e.g., `List<T>`), closing logic loopholes.

```csharp
public sealed class InventoryModule : ILayerContext
{
    // Publish state within Service scope with Key "items"
    [Public(typeof(InventoryService), "items")]
    private List<string> _items = new();
}

public sealed class HudModule : ILayerContext
{
    // Consume state from specific Service, forcing read-only interface
    [From(typeof(InventoryService), "items")]
    private IReadOnlyList<string> _items = default!;
}

// For global scope, use typeof(GlobalScope)
[Public(typeof(GlobalScope), "config")]
private AppConfig _config;
```

### 7. Registration Overview & Health Audit

In large projects, debugging "who sent what, who is listening, and why no response" is a huge burden. LayerBase v1.4.0
introduces full topology snapshots.

* **Topology Export**: Call `LayerHub.GetTopologyMarkdown()` to get a detailed Markdown report covering all Layers,
  Event subscriptions, Call routes, and Shared Fields.
* **Dead Code Audit**: Automatically detects and flags:
    * **Zombie Event**: Subscribed but never produced.
    * **Unused Producer**: Produced but no subscribers.
    * **Dead Call**: Handler defined but never invoked.
    * **Orphaned Public**: Key published but never consumed.

### 8. Roslyn Developer Experience (Diagnostics & Code Fixes)

Deeply integrated Roslyn analyzers provide:

* **Compile-time Guardrails**: Catch invalid signatures, key collisions, and missing `partial` modifiers before
  execution.
* **One-Click Fixes**: Use `Alt+Enter` to auto-fix method signatures, inject `async` keywords, or sync shared field
  types.

---

## ⚠️ Core Design Boundaries and Timing Constraints

To squeeze out extreme performance in a managed environment, LayerBase makes specific design trade-offs. Developers *
*must** be aware of these physical boundaries:

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
