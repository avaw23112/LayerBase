# 🚀 LayerBase: 工业级高性能游戏架构总线

“在苛求性能的领域，一切不必要的抽象都是罪恶。LayerBase 为您扫清障碍。”

**LayerBase** 是一款专为高性能 C# 游戏开发（如 Unity、Godot、纯 C# 服务端）打造的“分层事件架构与通讯总线”框架。

它彻底打破了传统 OOP 事件总线（Event Bus）的性能瓶颈，采用激进的**面向数据（Data-Oriented Design, DOD）**和**SOA（Structure of Arrays）**底层设计。极简、高效、安全，能够为您的大型项目提供稳如泰山的事件流转和状态管理，其实测 TPS（每秒吞吐量）已达到惊人的**1.5亿次以上**。

---

## 🤔 为什么要造这个轮子？ (Why LayerBase?)

在大型复杂游戏（特别是高频动作、MMO 或大型单机）开发中，随着系统体量的剧增，传统的事件机制往往会演变成一场“到处起飞、谁调了谁不知道”的灾难。作为开发者，我们常常在**“架构的规范性”**与**“底层的极致性能”**之间被迫做出妥协。

LayerBase 为此而生。我们希望用一套彻底的底层重构，在现代 C# 游戏开发生态中，找回那份久违的**秩序与极速**。

### 1. ⚔️ 横向对比：我们在生态中的进化史
让我们用一段大家再熟悉不过的开发“踩坑史”，来看看 LayerBase 的架构是如何一步步演进而来的：

*   **第一阶段：单例乱飞的噩梦 (The Singleton Era)**
    游戏立项之初，为了贪图方便，随手就是 `GameManager.Instance.UpdateHealth()`。很快，随着系统增多，模块之间形成了极其严重的网状耦合。代码变成了牵一发而动全身的“意大利面条”，稍微改动一点逻辑，其他模块就跟着崩溃。
*   **第二阶段：事件满天飞的失控 (The EventBus Era)**
    痛定思痛后为了解耦，我们引入了 `Action`、`UniRx`、`MediatR` 或是各种泛型 `EventBus`。表面上解耦了，但**新的灾难随之降临：时序失控**。一个事件抛出去，犹如泥牛入海，你根本不知道被谁先接住了、谁后接住了。更要命的是，传统事件总线底层全靠 `Dictionary<Type, List<Delegate>>` 死撑。在海量实体的高频交互下，大量的装箱拆箱、虚函数调用和惨烈的 CPU 缓存未命中（Cache Miss），将性能上限死死卡在了几十万到两百万 TPS。
*   **第三阶段：秩序与性能的终极融合 (LayerBase 时代)**
    为了找回**可控的时序和架构的透明度**，我们不能再让事件随地乱抛了。于是，我们引入了基于 DI（依赖注入）的 `Layer -> Service -> Manager` 强约束架构。但这又带来一个挑战：过度的架构抽象通常会毁掉底层的运行效率。
    所以，我们做出了一个违背祖宗的决定：在架构的底层，我们汲取了纯 ECS 框架（如 `Arch` / `Entitas`）的思想，**用彻底的面向数据设计（Data-Oriented Design, DOD）重写了事件总线**。这让我们既保住了事件流转的绝对顺序和易用性，又在执行效率上实现了恐怖的飞跃——达到了与顶级 C++ / C# ECS 框架同级别的 L1/L2 缓存命中率（飙升至 1.5亿 TPS 以上）。

### 2. 📊 直观剖析：我们为什么这么快？
传统的 EventBus 在内存中是典型的 **AOS (Array of Structures)** 布局。当你派发一个事件时，CPU 需要进行多次极其昂贵的非连续内存跳转：

```text
❌ 传统 EventBus 分发路径 (Cache Miss 灾难)
EventBus 
  └─> [哈希计算] 
        └─> [Dictionary Bucket 查找] 
              └─> [List 堆内存跳转] 
                    └─> [Handler 对象 (包含上下文/委托)] 
                          └─> 虚函数 Invoke 
```

而在 LayerBase 中，我们在启动期（Build）通过源生成器，冷酷地将同类事件的所有处理器“拆解、脱水”，转化为 **SOA (Structure of Arrays)** 布局：

```text
✅ LayerBase 零分支分发引擎 (完美 Cache 亲和性)
EventBucket<T>
 ├── Delegate[] SyncHandlers  [ ptr | ptr | ptr | ptr ] -> 纯净的连续函数指针，CPU 极速顺序预取
 ├── Delegate[] AsyncHandlers [ ptr | ptr | ptr | ptr ] -> 物理隔离，彻底消灭 if(isAsync) 检查
 └── Circuit[]  FaultCircuits [ 0 | 0 | 1 | 0 ] -> 仅在抛出异常时才访问，绝不污染热路径
```
配合**硬件级位图跳跃 (Bitmask Skipping)** 和 **`Unsafe` 指针越界消除**，我们彻底清空了热路径上的 `if/else` 分支与边界检查，让 CPU 流水线全速狂奔。

### 3. 🛡️ 工业级基建：秩序与自由的平衡
跑得快很重要，但跑得稳才是活下去的关键：
*   **统一架构约束**：强制推行 `Layer -> Service -> Manager` 三层递进架构。搭配特性（Attribute）自动注入，让百人团队的协同开发拥有静态可溯源的范式。你的代码结构，就是你的架构图。
*   **自愈熔断机制**：任何 Handler 抛出未捕获异常，系统会瞬间对其进行**精准物理熔断**。故障绝不阻塞同层其他业务，下一帧通过“两段式重建”平滑剔除失效节点，实现系统级自愈。
*   **死循环静态防卫**：在调用 `Build()` 时，底层着色图算法（Three-Color Algorithm）会静态审计整个事件流向。发现死循环风险？控制台直接抛出环路路径，拒绝黑盒运行。
*   **零开销异步生态 (`LBTask`)**：现代游戏离不开异步流。LayerBase 自带专为游戏泵（Pump）优化的 `LBTask`，实现了“同步路径零 GC 分配”，配合源生成器，让异步逻辑与同步流无缝融合。

### 4. ⚖️ 坦诚的局限性与门槛 (Trade-offs)
世界上没有完美的银弹，为了达到 1.5 亿的极限吞吐量，LayerBase 做出了必要的取舍：
1.  **事件强制为 `struct`**：为了彻底消灭 GC 并在 SOA 中狂奔，事件传递是强制值拷贝的。如果您在事件中传递几十 KB 的大结构体，拷贝成本反而会吃掉路由优势。
2.  **更高的心智门槛**：如果您只是想开发一款简单的休闲小游戏，传统的 `EventBus` 就足够了。LayerBase 要求开发者理解层级注册时序、传播方向（Global/Bubble/Drop）以及特性挂载。
3.  **动态挂载的微小阵痛**：极速分发依赖于扁平的 SOA 数组。如果在运行时极其疯狂地动态添加/移除 Handler，会导致底层触发重建。LayerBase 更鼓励**静态拓扑**：场景初始化时搭建完毕，运行时一路狂奔。

### 🎯 结论：谁适合使用 LayerBase？
LayerBase 绝不是一个用来快速搭建“原型玩具”的轻量级脚手架。它是一把为**“3A 级性能要求或超大规模复杂逻辑”**量身定制的重型武器。

如果您正在开发一款**包含成千上万个实体交互的 MMO、每帧需要处理海量碰撞与状态同步的高频动作游戏、或是追求极致响应吞吐量的帧同步网关**——此时，市面上大多数架构都会面临可悲的 CPU 瓶颈。在这个性能与复杂度的“修罗场”里，**LayerBase** 将为您扫清一切底层障碍。

---

## ⚡ 标准基准测试 (BenchmarkDotNet)

我们使用权威的 `BenchmarkDotNet` 进行了严苛的基准压测。测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路轰炸)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在包含 10 个物理层级的真实业务架构下，发送一百万次事件仅需 6 毫秒，TPS 强行突破 1.5 亿。即便在最恶劣的、每一层都强行挂载逻辑的极端高压环境下，系统依然死死维持住了近 6000 万的超高吞吐。对于那些对帧率精打细算的游戏而言，LayerBase 的架构损耗已无限趋近于 0。

---

## 📦 怎么安装？

1. **源码引入**：将仓库中的 `LayerBase` 和 `LayerBase.Task` 项目目录直接添加到您的解决方案中并建立引用。
2. **配置源生成器 (Source Generator)**：
   框架重度依赖源生成器以消除所有的反射开销，请务必确保配置了 `LayerBase.Generator` 分析器：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
3. **环境要求**：完美支持 `.NET Standard 2.1`（对 Unity/Godot 极度友好），建议在 `.NET 8.0/9.0` 环境运行以完全解锁底层 `Unsafe` 加速特性。

---

## 📖 怎么用？ (最佳实践手册)

LayerBase 强烈推荐使用 `Layer -> Service -> Manager` 的三层架构，结合特性的自动装配，让您的代码不仅跑得快，长得还漂亮。

### Step 1: 定义您的事件 (Event Structs)
请记住我们的硬规矩：为了杜绝 GC 垃圾，所有的事件必须声明为 `struct`！

```csharp
public struct DamageEvent
{
    public int TargetId;
    public float Amount;
}
public struct PlayerDeathEvent { }
```

### Step 2: 编写具体的业务逻辑 (Manager)
Manager 是真正干活的地方。继承 `ILayerContext` 即可自动感知自己所属的层级，并霸道地解锁所有 `Send` 与 `Post` 派发能力。
强烈推荐使用 `[Subscribe]` 和 `[SubscribeAsync]` 特性，让源生成器在编译期为您默默写好绑定代码，**零反射，全内联**。

```csharp
using LayerBase.DI;
using LayerBase.Core.Event;
using LayerBase.Async;

// 必须标记 partial，供源生成器植入绑定逻辑
public partial class DamageManager : ILayerContext
{
    // 【同步处理】：使用 [Subscribe] 特性
    [Subscribe]
    private EventHandledState OnTakeDamage(in DamageEvent e)
    {
        Console.WriteLine($"实体 {e.TargetId} 受到 {e.Amount} 伤害");
        
        if (e.Amount > 100)
        {
            // 向下坠落，让底层的物理层/逻辑层去收尸
            this.SendDrop(new PlayerDeathEvent()); 
        }
        
        // Continue: 兄弟们继续，我也听完了
        // Handled: 这事我管了，后面的别插手
        return EventHandledState.Continue;
    }

    // 【异步处理】：使用 [SubscribeAsync] 特性，完美融合分帧计时
    [SubscribeAsync]
    private async LBTask OnPlayerDeath(PlayerDeathEvent e)
    {
        await LBTask.Delay(TimeSpan.FromSeconds(3f)); // 零 GC 延迟，就是这么丝滑
        Console.WriteLine("玩家复活...");
    }
}
```

### Step 3: 组织业务模块 (Service)
Service 是大管家，负责将相关的 Manager 组合起来，并挂载到指定的 Layer 中。
通过 `[OwnerLayer]` 特性，您可以将一个服务**硬编码绑定**到特定的 Layer 上。相信我，这种静态约束会让接手你代码的同事感动落泪的。

```csharp
using LayerBase.DI;

// 强行把这个服务绑死在 GameLogicLayer 层级
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService 
{
    // 配置依赖注入，将 Manager 注册进窝里
    public void ConfigureServices(IServiceCollection services) 
    { 
        // 划重点：在这里注册的顺序，就是该层级内事件响应的【绝对优先级顺序】！
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### Step 4: 定义层级容器 (Layer) & 初始化拓扑 (LayerHub)
Layer 是最宏观的护城河，代表了系统的处理优先级界限。在游戏入口处，搭建你的世界。

```csharp
using LayerBase.Layers;
using LayerBase.LayerHub;

// 定义层级
public class UILayer : Layer { }
public class GameLogicLayer : Layer { }

// 在游戏 Awake / _Ready 中拔地而起
public void InitGame()
{
    LayerHub.CreateLayers()
            .Push(new UILayer())          // Index 0: 顶层 (最先收到 Bubble, 最后收到 Drop)
            .Push(new GameLogicLayer())   // Index 1: 底层
            .Build();                     // 一键 Build，全图扫描 [OwnerLayer]，自动组装！
}
```

### Step 5: 触发事件与驱动循环 (Send/Post/Pump)

**尽情派发：**
```csharp
// 【霸道同步】全局广播，立即阻塞当前线程，穿透并碾压所有层级
LayerHub.Send(new DamageEvent { TargetId = 99, Amount = 50f });

// 【优雅异步】将事件丢进脏队列，转身就走，等待下一帧的 Pump 为你料理后事
LayerHub.Post(new DamageEvent { TargetId = 1, Amount = 10f });
```

**主循环的心跳：**
如果你用了 `Post`、`LBTask` 或者延迟任务，别忘了在主循环中接入引擎的心跳（Pump）。放心，如果脏队列是空的，这行代码的耗时只有**几个纳秒**。
```csharp
void Update(float deltaTime)
{
    LayerHub.Pump(deltaTime);
}
```

---

## 🛠 高级特性：架构师的玩具箱

### 1. 流式过滤与拦截 (Fluent API)
除了傻瓜式的特性绑定，LayerBase 还提供了如 LINQ 般行云流水的链式 API。你可以在 Service 或 Manager 中直接把控事件流。
它的杀手锏在于：拦截判定发生在路由的**最早期闭包内**。不符合条件的事件会被“一脚踢开”，绝对不会唤醒你的庞大业务逻辑块。

```csharp
public partial class PlayerManager : ILayerContext
{
    private int _myEntityId = 10;

    public void Initialize()
    {
        // 🌊 优雅，实在太优雅了
        this.OnEvent<DamageEvent>()
            .Where((in DamageEvent e) => e.TargetId == _myEntityId) // 编译期拦截，不符即滚
            .Handle((in DamageEvent e) => 
            {
                // 处理受击逻辑...
                return EventHandledState.Handled;
            });
    }
}
```

### 2. 后台并行处理 (Parallel Handlers)
如果你有极其吃 CPU 且**不碰主线程状态**的纯计算逻辑（比如：极其阴间的寻路算法下发、海量战斗日志落盘），并行订阅就是你的救星。事件会被无锁丢进 ThreadPool，在后台安静地狂奔，主帧依然如丝般顺滑。

```csharp
// 支持特性一键绑定
[SubscribeParallel]
private EventHandledState OnHeavyComputeTask(in ComputeEvent e)
{
    // 让多线程去头疼吧
    return EventHandledState.Continue;
}

// 流式绑定也行，随你喜欢
this.OnEvent<ComputeEvent>().HandleParallel(...);
```

### 3. 拓扑审计与死循环防御 (Topology Audit)
作为一个写了十年游戏的架构师，我最怕的就是逻辑回环：系统 A 派发了事件给 B，B 一激动又同步派发给了 A。砰，栈溢出了。
LayerBase 对这种“黑盒炸弹”零容忍。在调用 `Build()` 的那一刻，系统会化身为无情的审计员，启动有向图着色算法（Three-Color Algorithm），**全量静态扫描整个游戏的事件流向图**。
- 一旦嗅到**同步死循环**的酸腐味，或者发现**无人监听的死信**，系统会直接在控制台拍出一张清晰的环路路径并抛出异常，逼着你在开发期就解决掉。
- 在 Debug 模式下，随时调用 `GetTopologySummary()`，一张结构严密的文本拓扑图就赫然眼前。整个系统“谁在派发、谁在倾听”一览无余，从此代码再无暗角。

---

*“让性能重归物理极限，让架构回归清晰优雅。这就是 LayerBase。”*
