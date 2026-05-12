# LayerBase 框架功能实施计划

## 概述

本计划基于 `layerbase-framework-feature-design-updated.md` 设计文档，详细规划了 Query/Bring、Bundle/Blueprint、DTO 分类三大功能的实施步骤。

---

## 当前代码库状态分析

### 已有基础设施

1. **ECS 系统** (基于 Arch ECS)
   - `World`, `Entity`, `Query`, `Chunk` 等核心类型
   - `ForEach<T0..T7>` 委托和 `IForEach<T0..T7>` 接口
   - `QueryDescription` 查询描述符

2. **Projection 系统** (已有)
   - `ProjectionQueryFlow0..8` - 查询流程对象
   - `ProjectionExecutor0..8` - 执行器
   - `ProjectedActorMeta` - Actor 投影元数据
   - `ProjectedActorTypeRegistry` - Actor 类型注册表
   - `EntityCreateFlow0..8` - 实体创建流程

3. **源生成器** (已有)
   - `ProjectedActorTypeGenerator` - 生成 Actor 类型注册
   - `ActorBehaviourGenerator` - 生成 Actor 行为代码
   - `EventMetaDataGenerator` - 生成事件元数据
   - `LayerServiceGenerator` - 生成 Layer 服务注册

4. **Actor 系统** (已有)
   - `IActor`, `IPooledActor` 接口
   - `ActorWorld` - Actor 世界管理
   - `ActorId` - Actor 标识符

### 需要新增的功能

1. **DTO Marker 接口** - `IComponent`, `IActorEvent`, `IRequest`, `IResponse` 等
2. **Query/Bring Attribute** - `[Query]`, `[Bring<T>]`, `[EntryPoint]`
3. **ProjectResult** - 查询结果控制枚举
4. **Bundle/Blueprint 系统** - `IBundle`, `IEntityBlueprint`, `EntityBlueprintBuilder`
5. **Analyzer 诊断** - 编译时校验规则

---

## 实施步骤

### Step 1: DTO Marker 接口

**目标**: 建立数据类型语义标记系统

**新增文件**:
```
LayerBase/Core/DTO/
├── ILayerDto.cs          # 根标记接口
├── IComponent.cs         # ECS 组件标记
├── IActorEvent.cs        # Actor 事件标记
├── IRequest.cs           # 请求 DTO 标记
├── IResponse.cs          # 响应 DTO 标记
├── ICommand.cs           # 命令 DTO 标记
└── ISnapshot.cs          # 快照 DTO 标记
```

**修改现有代码**:
- 更新现有组件类型实现 `IComponent`
- 更新现有事件类型实现 `IActorEvent`

**验收标准**:
- [ ] 所有 Marker 接口定义正确
- [ ] 现有组件和事件类型已更新实现
- [ ] 项目可编译通过

---

### Step 2: Query/Bring 生成器

**目标**: 实现 `[Query]` 和 `[Bring]` 属性驱动的代码生成

**新增文件**:
```
LayerBase/ECS/Query/
├── QueryAttribute.cs           # [Query] 属性
├── BringAttribute.cs           # [Bring] 属性（基础 + 泛型）
├── EntryPointAttribute.cs      # [EntryPoint] 属性
└── ProjectResult.cs            # 查询结果枚举

LayerBase.Generator/
└── QueryBringGenerator.cs      # 源生成器实现
```

**生成代码示例**:
```csharp
// 用户代码
public sealed partial class MoveService
{
    [Query]
    [Bring<MoveViewEvent>]
    private ProjectResult OnUpdateEnemyView(
        Entity entity,
        ref PositionComponent position,
        in VelocityComponent velocity,
        ref MoveViewEvent moveEvent)
    {
        position.X += velocity.X;
        position.Y += velocity.Y;
        moveEvent = new MoveViewEvent(position.X, position.Y);
        return ProjectResult.Success;
    }
}

// 生成代码
public sealed partial class MoveService
{
    public void UpdateEnemyView()
    {
        var job = new __UpdateEnemyViewJob(this);
        this.Query<PositionComponent, VelocityComponent>()
            .Bring<MoveViewEvent>()
            .ForEach(ref job);
    }

    private readonly struct __UpdateEnemyViewJob :
        IQueryJob<PositionComponent, VelocityComponent, MoveViewEvent>
    {
        private readonly MoveService _self;
        
        public __UpdateEnemyViewJob(MoveService self) => _self = self;
        
        public ProjectResult Execute(
            Entity entity,
            ref PositionComponent position,
            ref VelocityComponent velocity,
            ref MoveViewEvent moveEvent)
        {
            return _self.OnUpdateEnemyView(entity, ref position, in velocity, ref moveEvent);
        }
    }
}
```

**诊断规则**:
- `LB-ECS001`: 包含 `[Query]` 方法的类型必须是 partial
- `LB-ECS004`: 无 `[Bring]` 的 `[Query]` 方法必须返回 void
- `LB-ECS005`: 有 `[Bring]` 的 `[Query]` 方法必须返回 ProjectResult
- `LB-ECS008`: Bring 事件参数必须在方法参数末尾

**验收标准**:
- [ ] `[Query]` + void 生成 ForEach
- [ ] `[Query]` + `[Bring]` + ProjectResult 生成 Bring + Post
- [ ] ProjectResult.Fail/Touch/Success 行为正确
- [ ] 所有诊断规则实现

---

### Step 3: Bundle/Blueprint 系统

**目标**: 实现稳定的实体结构声明和扩展

**新增文件**:
```
LayerBase/ECS/Blueprint/
├── IBundle.cs                    # Bundle 接口
├── IEntityBlueprint.cs           # Blueprint 接口
├── IBlueprintUnit.cs             # 单元接口（Bundle/Blueprint 共用）
├── LayerBundleAttribute.cs       # [LayerBundle] 属性
├── LayerBlueprintAttribute.cs    # [LayerBlueprint] 属性
├── EntityBlueprintBuilder.cs     # 蓝图构建器
├── EntityBlueprint.cs            # 蓝图构建结果
├── BlueprintUnitCache.cs         # 单元缓存
├── EntityBlueprintCache.cs       # 蓝图缓存
└── Extensions/
    ├── WorldBlueprintExtensions.cs   # World 扩展
    ├── LayerBlueprintExtensions.cs   # Layer 扩展
    ├── ServiceBlueprintExtensions.cs # Service 扩展
    └── ContextBlueprintExtensions.cs # Context 扩展
```

**使用示例**:
```csharp
// Bundle 定义
[LayerBundle]
public sealed class MoveBundle : IBundle
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithComponent<PositionComponent>();
        builder.WithComponent<VelocityComponent>();
        builder.WithComponent<MoveStateComponent>();
    }
}

// Blueprint 定义
[LayerBlueprint]
public sealed class EnemyBlueprint : IEntityBlueprint
{
    public void Config(ref EntityBlueprintBuilder builder)
    {
        builder.WithBundle<MoveBundle>();
        builder.WithBundle<CombatBundle>();
        builder.WithProjectedActor<EnemyActor>();
    }
}

// 使用
var enemy = world.CreateEntity()
    .With<EnemyBlueprint>();

enemy.Set(new PositionComponent { X = 0f, Y = 0f });
```

**诊断规则**:
- `LB-BP001`: `[LayerBundle]` 类型必须是 class
- `LB-BP002`: `[LayerBundle]` 类型必须实现 IBundle
- `LB-BP003`: `[LayerBundle]` 类型必须有 public 无参构造函数
- `LB-BP004`: `[LayerBlueprint]` 类型必须是 class
- `LB-BP005`: `[LayerBlueprint]` 类型必须实现 IEntityBlueprint

**验收标准**:
- [ ] Bundle/Blueprint 使用 class
- [ ] 不使用运行时注册表
- [ ] 不使用动态 ID 分配
- [ ] Blueprint 构建结果通过缓存复用
- [ ] `CreateEntity().With<TBlueprint>()` 可用
- [ ] 新增组件只需修改 Bundle/Blueprint

---

### Step 4: Analyzer 诊断

**目标**: 实现编译时校验，确保类型安全

**新增文件**:
```
LayerBase.Generator/
├── Diagnostics/
│   ├── DiagnosticIds.cs          # 诊断 ID 常量
│   ├── DiagnosticDescriptors.cs  # 诊断描述符
│   └── ECSAnalyzer.cs            # ECS 相关分析器
│   └── BlueprintAnalyzer.cs      # Blueprint 相关分析器
│   └── DTOAnalyzer.cs            # DTO 相关分析器
```

**诊断规则汇总**:

#### ECS 诊断 (LB-ECSxxx)
| ID | 描述 | 严重级别 |
|----|------|----------|
| LB-ECS001 | [Query] 方法所在类型必须是 partial | Error |
| LB-ECS002 | [Query] 方法所在类型必须实现 IEcsWorldProvider | Error |
| LB-ECS003 | [Query] 方法不能是泛型 | Error |
| LB-ECS004 | 无 [Bring] 的 [Query] 方法必须返回 void | Error |
| LB-ECS005 | 有 [Bring] 的 [Query] 方法必须返回 ProjectResult | Error |
| LB-ECS006 | [Bring] 必须声明至少一个事件类型 | Error |
| LB-ECS007 | [Bring] 事件数量超过生成模板限制 | Error |
| LB-ECS008 | Bring 事件参数必须在参数末尾且顺序匹配 | Error |
| LB-ECS009 | Bring 事件参数必须是 ref | Error |
| LB-ECS010 | ECS 组件参数必须是 ref 或 in | Error |
| LB-ECS011 | Entity 参数最多出现一次 | Error |
| LB-ECS012 | Query 组件数量超过生成模板限制 | Error |
| LB-ECS013 | Query 组件类型必须实现 IComponent | Error |
| LB-ECS014 | Bring 事件类型必须实现 IActorEvent | Error |
| LB-ECS020 | [Query] 方法必须以 On 开头或指定 [EntryPoint] | Error |
| LB-ECS021 | [Query] 方法 'On' 无效（生成入口名为空） | Error |
| LB-ECS022 | 生成的入口点已存在 | Error |
| LB-ECS023 | 多个 [Query] 方法生成相同入口点 | Error |
| LB-ECS024 | [EntryPoint] 名称不是有效的 C# 方法名 | Error |

#### Blueprint 诊断 (LB-BPxxx)
| ID | 描述 | 严重级别 |
|----|------|----------|
| LB-BP001 | [LayerBundle] 类型必须是 class | Error |
| LB-BP002 | [LayerBundle] 类型必须实现 IBundle | Error |
| LB-BP003 | [LayerBundle] 类型必须有 public 无参构造函数 | Error |
| LB-BP004 | [LayerBlueprint] 类型必须是 class | Error |
| LB-BP005 | [LayerBlueprint] 类型必须实现 IEntityBlueprint | Error |

#### DTO 诊断 (LB-DTOxxx)
| ID | 描述 | 严重级别 |
|----|------|----------|
| LB-DTO001 | ECS 组件类型必须实现 IComponent | Error |
| LB-DTO002 | Bring 事件类型必须实现 IActorEvent | Error |
| LB-DTO003 | Actor Handler 事件类型必须实现 IActorEvent | Error |
| LB-DTO004 | Request 类型必须实现 IRequest | Error |
| LB-DTO005 | Response 类型必须实现 IResponse | Error |
| LB-DTO006 | 类型不能同时实现 IComponent 和 IActorEvent | Error |
| LB-DTO007 | 类型不能同时实现 IRequest 和 IResponse（除非明确允许） | Warning |
| LB-DTO008 | DTO 类型应是 readonly struct（除非是可变 ECS 组件） | Info |
| LB-DTO009 | IActorEvent 应是 readonly struct | Info |
| LB-DTO010 | IRequest/IResponse 应是 readonly struct | Info |

**验收标准**:
- [ ] 所有诊断规则实现
- [ ] 诊断消息清晰准确
- [ ] 支持代码修复建议（CodeFix）

---

### Step 5: Roslyn Index 和 Skill

**目标**: 为 AI Agent 提供代码索引和技能支持

**新增文件**:
```
LayerBase.Generator/
└── Index/
    ├── BundleIndexGenerator.cs     # Bundle 结构索引
    ├── BlueprintIndexGenerator.cs  # Blueprint 结构索引
    └── ComponentIndexGenerator.cs  # 组件类型索引
```

**索引输出格式**:
```json
{
  "bundles": {
    "MoveBundle": {
      "components": ["PositionComponent", "VelocityComponent", "MoveStateComponent"],
      "file": "Game.Layers.Battle.ECS.Bundles.MoveBundle"
    }
  },
  "blueprints": {
    "EnemyBlueprint": {
      "bundles": ["MoveBundle", "CombatBundle", "AoiBundle"],
      "projectedActor": "EnemyActor",
      "components": ["PositionComponent", "VelocityComponent", "HealthComponent"],
      "file": "Game.Layers.Battle.ECS.Blueprints.EnemyBlueprint"
    }
  }
}
```

**验收标准**:
- [ ] 索引生成器实现
- [ ] 索引格式正确
- [ ] AI Agent 可读取索引

---

## 文件变更汇总

### 新增文件

#### LayerBase/Core/DTO/
- `ILayerDto.cs`
- `IComponent.cs`
- `IActorEvent.cs`
- `IRequest.cs`
- `IResponse.cs`
- `ICommand.cs`
- `ISnapshot.cs`

#### LayerBase/ECS/Query/
- `QueryAttribute.cs`
- `BringAttribute.cs`
- `EntryPointAttribute.cs`
- `ProjectResult.cs`

#### LayerBase/ECS/Blueprint/
- `IBundle.cs`
- `IEntityBlueprint.cs`
- `IBlueprintUnit.cs`
- `LayerBundleAttribute.cs`
- `LayerBlueprintAttribute.cs`
- `EntityBlueprintBuilder.cs`
- `EntityBlueprint.cs`
- `BlueprintUnitCache.cs`
- `EntityBlueprintCache.cs`
- `Extensions/WorldBlueprintExtensions.cs`
- `Extensions/LayerBlueprintExtensions.cs`
- `Extensions/ServiceBlueprintExtensions.cs`
- `Extensions/ContextBlueprintExtensions.cs`

#### LayerBase.Generator/
- `QueryBringGenerator.cs`
- `Diagnostics/DiagnosticIds.cs`
- `Diagnostics/DiagnosticDescriptors.cs`
- `Diagnostics/ECSAnalyzer.cs`
- `Diagnostics/BlueprintAnalyzer.cs`
- `Diagnostics/DTOAnalyzer.cs`
- `Index/BundleIndexGenerator.cs`
- `Index/BlueprintIndexGenerator.cs`
- `Index/ComponentIndexGenerator.cs`

### 修改文件

- 现有组件类型 - 添加 `IComponent` 实现
- 现有事件类型 - 添加 `IActorEvent` 实现
- `LayerBase.Generator.csproj` - 添加新的生成器引用

---

## 测试计划

### 单元测试

1. **DTO Marker 测试**
   - 验证接口实现正确
   - 验证类型检查

2. **Query/Bring 测试**
   - 纯 Query 生成测试
   - Query + Bring 生成测试
   - ProjectResult 行为测试
   - 诊断规则测试

3. **Bundle/Blueprint 测试**
   - Bundle 配置测试
   - Blueprint 构建测试
   - 缓存复用测试
   - 实体创建测试

4. **Analyzer 测试**
   - 所有诊断规则测试
   - CodeFix 测试

### 集成测试

1. **端到端流程测试**
   - 创建 Bundle/Blueprint
   - 使用 Query/Bring 处理
   - 验证 Actor 投影

2. **性能测试**
   - 热路径性能验证
   - 内存分配验证

---

## 风险和缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 源生成器复杂度高 | 开发周期长 | 分阶段实现，先实现核心功能 |
| 与现有代码冲突 | 兼容性问题 | 保持向导兼容，新增命名空间 |
| 性能回归 | 运行时性能下降 | 严格热路径测试，避免反射 |
| 诊断规则覆盖不全 | 用户体验差 | 逐步补充，优先实现关键规则 |

---

## 时间估算

| 步骤 | 预估时间 | 依赖 |
|------|----------|------|
| Step 1: DTO Marker | 1-2 天 | 无 |
| Step 2: Query/Bring | 3-5 天 | Step 1 |
| Step 3: Bundle/Blueprint | 3-5 天 | Step 1 |
| Step 4: Analyzer | 2-3 天 | Step 1, 2, 3 |
| Step 5: Roslyn Index | 1-2 天 | Step 3, 4 |
| **总计** | **10-17 天** | |

---

## 下一步行动

1. 确认本计划
2. 开始实施 Step 1: DTO Marker 接口
3. 逐步推进后续步骤
