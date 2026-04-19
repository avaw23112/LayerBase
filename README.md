# 🚀 LayerBase: 工业级高性能游戏架构总线

“在苛求性能的领域，一切不必要的抽象都是罪恶。LayerBase 为您扫清障碍。”

**LayerBase** 是一款专为高性能 C# 游戏开发（如 Unity、Godot、纯 C# 服务端）打造的“分层事件架构与通讯总线”框架。

它彻底打破了传统 OOP 事件总线（Event Bus）的性能瓶颈，采用激进的**面向数据（Data-Oriented Design, DOD）**和**SOA（Structure of Arrays）**底层设计。极简、高效、安全，能够为您的大型项目提供稳如泰山的事件流转和状态管理，其实测 TPS（每秒吞吐量）已达到惊人的**1.5亿次以上**。

---

## 🎯 我们解决了什么问题？

在大型复杂游戏（特别是高频动作、MMO 或大型单机）开发中，随着代码量的剧增，传统的事件机制往往会引发“到处起飞”、“谁调了谁不知道”的灾难。作为架构师，我们深知这种痛苦。LayerBase 为此而生，致力于解决三大核心痛点：

1. **统一开发流程 (Unified Workflow)**
   - 告别“意大利面条式”的随地订阅！LayerBase 强力推行 `Layer -> Service -> Manager` 三层递进架构。搭配基于特性（Attribute）的无缝依赖注入，让百人团队的协同开发拥有统一、清晰且静态可溯源的范式。你的代码结构就是你的架构图。
2. **全套工业级基建 (Complete Infrastructure)**
   - LayerBase 绝不仅仅是一个简陋的 `Dictionary<Type, Delegate>`。它自带一整套重型装备：零 GC 分配的异步任务系统 (`LBTask`)、多线程并行队列 (`HandleParallel`)、死循环防卫静态审计，以及自动化拓扑可视化。你不需要再去满世界找插件，底层的坑我们已经帮你踩平了。
3. **绝对的性能管控 (Performance Control)**
   - 将事件路由的开销从“不可控的运行时黑盒”降维打击至“物理极限的寄存器与缓存交互”。对于性能预算极其吃紧的 60FPS/120FPS 游戏，你再也不用为底层事件抛发和架构调度的开销买单。

---

## 🤔 我们为什么选择 LayerBase？ (架构对比与选型指南)

如果你还在犹豫是否要引入 LayerBase，不妨看看它在当前 C# 开发生态中的真实坐标。

### 1. ⚔️ 横向对比：降维打击的统治力
在 C# 游戏开发（尤其是 Unity/Godot）领域，常见的事件通讯方案大致分为三代，而 LayerBase 的表现如下：
*   **对比第一代/第二代框架（如原生 `Action` / `SendMessage` / `UniRx` / `MediatR` / 泛型 `EventBus`）**：传统事件总线高度依赖字典查找（`Dictionary<Type, List<Delegate>>`），在分发时伴随着海量的封箱拆箱、虚函数调用，甚至多播委托的拷贝。它们的极限 TPS 通常在 50万 到 200万 之间。而 LayerBase 通过 **SOA 连续数组 + 零分支引擎**，直接将 TPS 抬升到了 **1.5亿**。这是近乎 **50~100 倍的降维打击**。在绝对的底层效率面前，一切冗杂的抽象都显得苍白无力。
*   **对比第三代框架（面向数据的纯 ECS 架构如 `Arch` / `Entitas`）**：ECS 的核心灵魂是基于 SOA 的极速缓存命中。LayerBase 虽不是 ECS 框架，但在“事件路由”这一特定战场上，它通过底层的暴力重构，达到了**与顶级 C++ / C# ECS 框架完全同级别的指令吞吐率和 L1/L2 缓存命中率**。

### 2. 🛡️ 能力护城河：秩序与自由的平衡
跑得快很重要，但跑得稳才是活下去的关键：
*   **物理空间隔离**：独创 `Local/Global/Bubble/Drop` 四维传播模型，完美契合游戏的渲染树或逻辑层级。底层的 UI 点击绝对不会莫名其妙地穿透并污染到顶层的物理计算。
*   **工业级自愈熔断**：这是我们引以为傲的底座。任何 Handler 抛出未捕获的异常，系统会在千分之一毫秒内对其进行**精准定位并物理熔断**。更绝的是，故障绝不阻塞同层其他业务！在下一帧，引擎通过无锁的“脏标记延迟重建”平滑剔除失效节点，实现系统级自愈。
*   **零开销异步生态 (`LBTask`)**：现代游戏离不开异步流。LayerBase 自带专为游戏泵（Pump）优化的 `LBTask`，实现了“同步路径零 GC 分配”，配合源生成器，让你写异步逻辑就像写同步一样行云流水，无惧任何 GC 尖峰。

### 3. ⚖️ 坦诚的局限性与门槛 (Trade-offs)
世界上没有完美的银弹，我也不打算卖狗皮膏药。为了达到 1.5 亿的 TPS，LayerBase 做出了极其残忍的妥协：
1.  **事件强制为 `struct`**：为了彻底消灭 GC 并在 SOA 中狂奔，事件传递是强制值拷贝的。这意味着，如果你往事件里塞了一个几十 KB 的大结构体，拷贝成本反而会吃掉路由优势。同时，你也无法在传递过程中修改事件的值并让后续节点看到（除非你在外部维护状态）。
2.  **更高的心智门槛**：如果你只是想做一个简单的《Flappy Bird》，传统的 `EventBus.Subscribe()` 就足够了。LayerBase 强制要求开发者理解层级注册时序、传播方向以及特性挂载，这对于小体量项目来说无疑是“杀鸡用牛刀”。
3.  **动态挂载的轻微阵痛**：极速分发的代价是对扁平数组（SOA）的强依赖。如果在游戏运行时**极其疯狂**地动态添加/移除单个 Handler，会导致底层不断触发两段式重建。虽然重建是零分配的，但也会有微小开销。LayerBase 鼓励的是**静态拓扑**：在场景初始化时建好大厦，然后在运行时一路狂奔。

### 🎯 结论：谁适合使用 LayerBase？
**LayerBase 绝不是一个用来快速搭建“原型玩具”的轻量级脚手架。它是一把专为“3A 级性能要求或超大规模复杂逻辑”量身定制的重型狙击枪。**

如果你正在开发一款**包含成千上万个实体交互的 MMO、每帧需要处理海量碰撞与状态同步的高频动作游戏、或是追求极致响应吞吐量的帧同步网关**——此时，市面上大多数基于字典和虚函数的架构都会让你看到可悲的 CPU 瓶颈。而在这个性能的“修罗场”里，**LayerBase** 将是你唯一且无可替代的最佳选择。

---

## ⚡ 性能王牌：标准基准测试 (BenchmarkDotNet)

我们使用权威的 `BenchmarkDotNet` 进行了严苛的基准压测。测试环境：`.NET 8.0`, `X64 RyuJIT`, `Intel Core i7-12650H`。

| 场景描述                                        | 任务量 (事件派发次数) | 平均耗时 (Mean) | 等效处理量 (次/秒) | GC 内存分配 (Allocated) |
|-------------------------------------------------|----------------------:|----------------:|----------------------:|------------------------:|
| **轻度负载 (模拟真实业务)** <br> 10 层架构，单层订阅   | **1,000,000** 次      | **6.38 ms**     | **~156,700,000**      | **0 B** (热路径无分配)  |
| **极限高压 (全链路轰炸)** <br> 10 层架构，层层都订阅 | **1,000,000** 次      | **16.81 ms**    | **~59,400,000**       | **0 B** (热路径无分配)  |
| **1ms 挑战 (常见3层架构)** <br> 3 层架构全订阅       | **10,000** 次         | **91.41 μs**    | **~109,300,000**      | **0 B** (热路径无分配)  |

> 💡 **数据解读**：在包含 10 个物理层级的真实业务架构下，发送一百万次事件仅需 6 毫秒，TPS 强行突破 1.5 亿。即便在最恶劣的、每一层都强行挂载逻辑的极端高压环境下，系统依然死死维持住了近 6000 万的超高吞吐。对于那些对帧率精打细算的游戏而言，LayerBase 的架构损耗已无限趋近于 0。

---

## 🏎️ 我们为什么快？ (底层黑科技大起底)

LayerBase 性能登峰造极的秘诀，在于我们极其贪婪地压榨了 CPU 的每一条指令和每一次缓存加载。为了突破那层薄薄的性能天花板，我们实施了以下 5 项最具毁灭性的底层优化：

1. **硬件级位图跳跃 (Bitmask Skipping)**
   - 跨层级分发时，我们抛弃了任何形式的遍历。利用现代 CPU 硬件指令（如 `BitOperations.TrailingZeroCount`），**只需 1 个时钟周期的硬件级运算**，即可精准“瞬移”跨过所有空闲层级，将跨层路由损耗物理消灭。
2. **纯粹的 SOA 数组布局 (Structure of Arrays)**
   - 传统框架的事件封装会导致灾难性的内存碎片。我们在构建时刻，冷酷地将同类事件的所有处理器“拆解、脱水”，强行揉捏成**高度连续的 `Delegate[]` 原生数组**。在热路径中，CPU L1/L2 缓存可以体会到最极致的顺序预取（Prefetching）快感。
3. **零分支执行引擎 (Branchless Execution)**
   - 我们将同步处理（Sync）与异步处理（Async）进行彻底的物理隔离。在核心分发循环内，**不包含任何 `if(isAsync)` 的分支判断**；同时，我们利用**位运算状态合并（Bitwise Aggregation）**技术，将繁琐的返回状态检查压缩到极致，让 CPU 分支预测器再也不用为了猜你的心思而“翻车”。
4. **指针级越界消除 (Unsafe Offset)**
   - 在支持的现代运行时下，我们直接掀了 JIT 的桌子。使用 `MemoryMarshal` 获取数组头部的原生指针，结合 `Unsafe.Add` 进行粗暴的内存偏移，**彻底拔除循环内部一切数组边界检查（BCE）的汇编指令**。
5. **两段式零分配重建 (Two-Pass Zero-Allocation)**
   - 当节点发生异常需要熔断并自愈时，系统采用两段式扫描预计算目标大小，直接在目标数组上完成内存拷贝与装配。全程**绝对不产生任何临时集合（如 `List<T>`），确保 100% 的 0 GC 内存分配**，拒绝因为架构治理而带来的哪怕一微秒的 GC 抖动。

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
