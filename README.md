# 🚀 LayerBase: 下一代高性能 C# 游戏开发全能架构框架

**LayerBase** 是一款专为 Godot、Unity 等 C# 驱动的游戏引擎设计的核心架构基座。它打破了传统开发中模块化、依赖注入与事件系统割裂的状态，将 **项目组织（Layering）**、**轻量级 DI 容器**、**异步任务（LBTask）** 与 **超极速事件总线** 深度融合。

针对大型游戏项目中的“事件风暴”与“复杂依赖网”，LayerBase 提供了一个极简、高性能且时序完全确定的通讯底座。

---

## 🌟 核心特性

*   **📦 一体化集成**：告别碎片化插件，一个框架解决模块解耦、依赖注入与异步通讯。
*   **⚡ 极致分发性能**：基于位图跳转（Bitmask）与硬件加速指令，同步分发吞吐量平均达 **25,000,000+ TPS**。
*   **🪄 声明式开发**：配合 **Source Generator**，使用 `[Subscribe]` 等特性即可完成自动连线，无需手动注册。
*   **🔄 严格时序保证**：事件注册顺序严格对齐配置顺序，彻底消灭异步环境下的执行顺序陷阱。
*   **💎 零损耗空转**：采用活跃层级屏蔽技术，自动跳过空闲层级，Pump 开销低至 **260 纳秒**。
*   **🔋 内存零压力**：全链路采用懒加载（Lazy Loading）与分段对象池，稳态运行下实现 **Zero-GC**。

---

## 📊 性能表现：突破“音障”

LayerBase 专为高压事件流优化，成功通过了 **“10,000 个同步事件 1ms 内分发”** 的严苛挑战。

| 指标 (每 1,000,000 次操作) | 表现数据 | 平均延迟 (单次) |
| :--- | :--- | :--- |
| **同步广播 (Sync Global)** | **~25,140,000 TPS** | **~40 ns** |
| **异步端到端 (Async Relay)** | **~3,070,000 TPS** | **~325 ns** |
| **异步入队 (Enqueue)** | **~20,000,000 Ops** | **~50 ns** |
| **空载循环 (Idle Pump)** | **~260 ns / Pump** | **≈ 0% CPU 占用** |

> **开发建议**：即便你的系统每帧产生 1,000 个同步事件，LayerBase 也仅占用不到 0.1ms 的主线程时间，为你昂贵的渲染与 AI 逻辑留出充足余量。

---

## 🛠️ 快速上手

### 1. 声明事件与组件
利用 `partial` 类与 `[Subscribe]` 特性，让业务逻辑自动感知层级上下文。

```csharp
public struct DamageEvent { public int Value; }

public partial class CombatManager : ILayerContext
{
    // 同步订阅：支持返回 EventHandledState 执行层级拦截
    [Subscribe]
    private EventHandledState OnDamage(in DamageEvent evt)
    {
        Console.WriteLine($"处理伤害: {evt.Value}");
        // 利用 ILayerContext 自动获得的 API 进行本地分发
        this.SendLocal(new VfxEvent { Type = "Blood" });
        return EventHandledState.Continue;
    }

    [SubscribeAsync] // 异步接力处理，不阻塞主线程
    private async LBTask OnLevelLoad(LevelLoadEvent evt) => await LBTask.Delay(100);
}
```

### 2. 配置服务 (注册即顺序)
代码的物理编写顺序即为事件处理的优先级。想要谁先拦截事件，就把它排在前面。

```csharp
public class CombatService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LogManager, LogManager>(); 
        services.AddSingleton<CombatManager, CombatManager>(); 
    }
}
```

### 3. 初始化并运行
```csharp
// 构建层级链条
var logicLayer = new GameLogicLayer();
logicLayer.RegisterService(new CombatService());

LayerHub.CreateLayers().Push(logicLayer).Build();

// 在游戏引擎的每帧更新中调用
public void OnUpdate(float deltaTime) => LayerHub.Pump(deltaTime);
```

---

## 📡 极简 API 设计: [Verb][Scope] 模式

LayerBase 弃用了模糊的命名，采用统一的 **动作 + 作用域** 模式，让每一条信息的流向一目了然：

| 作用域 (Scope) | 同步 (Sync) | 异步 (Async/分帧) | 延迟 (Delay) |
| :--- | :--- | :--- | :--- |
| **Local** (仅当前层) | `SendLocal` | `PostLocal` | `DelayLocal` |
| **Bubble** (向上冒泡) | `SendBubble` | `PostBubble` | `DelayBubble` |
| **Drop** (向下下沉) | `SendDrop` | `PostDrop` | `DelayDrop` |
| **Global** (全局广播) | `SendGlobal` | `PostGlobal` | `DelayGlobal` |

---

## 💎 核心技术黑科技

*   **Zero-Lookup Injection**：通过 Source Generator 在编译期将层级索引注入对象私有字段，彻底消灭运行时字典查找。
*   **Hardware bit-scanning**：利用 CPU 原生指令 `TZCNT` 优化位图扫描，实现 $O(1)$ 的跳转速度。
*   **Implicit Context Propagation**：利用 DI 容器解析链路，自动为 Manager 及其子对象“染色”所属层级信息。
*   **Active-Only Pumping**：只有真正有待处理任务的层级才会进入 Pump 循环，大幅压低大规模项目中的背景开销。

---

## 📥 安装

1.  克隆本项目或引用 `LayerBase.dll`。
2.  在 `.csproj` 中将 `LayerBase.Generator` 添加为 `Analyzer`。
3.  开启你的高性能开发之旅。

---
**LayerBase** - *Powering the next generation of C# Game Architecture.*
