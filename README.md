# 🚀 LayerBase: 高性能一体化游戏架构总线

**LayerBase** 是一款专为 Godot、Unity 等 C# 驱动的游戏引擎设计的“四合一”架构框架。它打破了层级组织、依赖注入与事件通讯的界限，提供了一个
**极致性能、时序确定、零空转损耗**的通讯底座。

---

## ⚡ 性能王牌：百万级事件总线

LayerBase 的核心竞争力在于其**性能触及天花板的事件分发引擎**。通过位图跳转（Bitmask）与零查找字段注入技术，它彻底消灭了分发路径上的所有字典查找与结构体包装开销。

### 📊 性能实测 (Windows 10 / .NET 8)

| 指标               | 表现数据                   | 平均延迟         | 说明            |
|:-----------------|:-----------------------|:-------------|:--------------|
| **同步分发 (Sync)**  | **~25,140,000 TPS**    | **~40 ns**   | 适用于高频战斗、物理反馈  |
| **1ms 极限挑战**     | **10,000 个事件 / 0.4ms** | **达标**       | 轻松承载极大规模事件风暴  |
| **异步接力 (Async)** | **~3,070,000 TPS**     | **~325 ns**  | 跨层级分帧解耦的最佳实践  |
| **空载损耗 (Idle)**  | **~260 ns / Pump**     | **≈ 0% CPU** | 64个空层级依然保持零占用 |

---

## 🛠️ 功能详解与使用指南

### 1. 基础：定义事件 (Events)

推荐使用 `struct` 定义事件，以获得极致的内存性能。

```csharp
public struct DamageEvent { public int Value; }
public struct LogEvent { public string Text; }
```

### 2. 核心：层级构建 (Layering)

层级是逻辑的容器。通过 `LayerHub` 组织它们的上下游关系（如：UI层 -> 逻辑层 -> 物理层）。

```csharp
// 1. 定义具体层级
public class GameLogicLayer : Layer { }
public class UILayer : Layer { }

// 2. 构建并初始化链条 (UI -> Logic)
LayerHub.CreateLayers()
    .Push(new UILayer())         
    .Push(new GameLogicLayer())  
    .Build();                    

// 3. 在引擎 Update 中驱动
public void OnUpdate(float delta) => LayerHub.Pump(delta);
```

### 3. 组织：服务系统与自动化注册 (OwnerLayer)

Service 是功能的组织者。通过 `[OwnerLayer]` 特性，你可以将 Service 自动挂载到指定层级，**无需在 Layer 类中手动编写注册代码
**。

```csharp
// 使用特性自动绑定到 GameLogicLayer
[OwnerLayer(typeof(GameLogicLayer))]
public class CombatService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 关键规则：注册顺序 = 事件处理优先级顺序。
        // MessageManager 会比 DamageManager 先处理同一个事件。
        services.AddSingleton<MessageManager, MessageManager>();
        services.AddSingleton<DamageManager, DamageManager>();
    }
}
```

### 4. 逻辑：智能插件 (Managers)

Manager 是业务实现者。只需标记 `ILayerContext` 即可自动感知所属层级（由宿主 Service 决定），并解锁所有发送 API。

```csharp
// 必须标记 partial 供 Source Generator 植入代码
public partial class DamageManager : ILayerContext
{
    // 同步订阅：支持返回 EventHandledState 执行层级拦截
    [Subscribe]
    private EventHandledState OnDamage(in DamageEvent evt)
    {
        // 【能力 1】本地分发：仅在当前层级内部流转
        this.SendLocal(new LogEvent { Text = "本层内部逻辑" });
        
        // 【能力 2】向上冒泡：发送给当前层及所有上层
        this.SendBubble(new LogEvent { Text = "通知 UI 层更新显示" });
        
        return EventHandledState.Continue;
    }
}
```

### 5. 通讯：API 矩阵 [Verb][Scope] 模式

LayerBase 提供了一套清晰、一致的 API 命名规范：

| 作用域 (Scope) | 同步 (立即执行)    | 异步 (分帧执行)    | 说明                 |
|:------------|:-------------|:-------------|:-------------------|
| **Local**   | `SendLocal`  | `PostLocal`  | 仅限当前层级             |
| **Bubble**  | `SendBubble` | `PostBubble` | 向上冒泡（往 Layer 0 方向） |
| **Drop**    | `SendDrop`   | `PostDrop`   | 向下下沉（往底层方向）        |
| **Global**  | `SendGlobal` | `PostGlobal` | 全局有序广播             |

### 6. 异步：专用任务系统 (LBTask)

**LBTask** 是专为游戏设计的轻量化异步任务模型，完美适配游戏 Pump 循环。

```csharp
[SubscribeAsync]
private async LBTask OnLevelLoad(LevelLoadEvent evt)
{
    // 分帧等待：让出当前帧，下一帧继续执行
    await LBTask.Yield();
    
    // 逻辑计时等待
    await LBTask.Delay(TimeSpan.FromSeconds(1));
    
    // 异步全局广播
    this.PostGlobal(new MsgEvent { Text = "异步处理完成" });
}
```

### 7. 进阶：并行线路 (Parallel Handlers)

对于耗时任务（IO/复杂计算），可使用脱离主线程的并行线路。

```csharp
// 1. 初始化后台调度器
LayerHub.InitializeJobScheduler(workerCount: 4);

// 2. 标记并行处理 (后台线程执行，不卡顿主帧)
[SubscribeParallel]
private EventHandledState OnHeavyCalculation(in DataEvent evt) => EventHandledState.Continue;
```

---

## 💎 技术黑科技

* **Zero-Lookup Injection**：通过 Source Generator 在编译期将层级索引注入字段，分发时**零字典查找**。
* **Active-Only Pumping**：实时监控活跃度，**自动跳过空闲层级**，Pump 损耗仅 **260ns**。
* **Implicit Context Propagation**：利用 DI 解析链路，让 Manager 无感获得层级上下文。
* **Auto-Registration**：基于特性的 Service 发现机制，大幅简化层级组织代码。

---

## 📥 安装

1. 通过 NuGet 或引用 DLL 引入 `LayerBase`。
2. 在 `.csproj` 中配置 **LayerBase.Generator** 作为 Analyzer (核心依赖)：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator.csproj" 
                         OutputItemType="Analyzer" 
                         ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```

---
**LayerBase** - *Powering the next generation of C# Game Architecture.*
