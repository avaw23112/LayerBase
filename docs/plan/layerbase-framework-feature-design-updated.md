# LayerBase Query-Bring Blueprint DTO Design

## 1. 文档目标

本文档定义 LayerBase 下一阶段的框架功能改造方案，覆盖三部分：

```text
Query / Bring:
  解决系统层如何批量操作 ECS，并自动提交 Actor 行为。

Bundle / Blueprint:
  解决实体结构如何稳定扩展，避免修改实体创建逻辑。

DTO 分类:
  解决 AI 和人类如何识别数据类型语义。
```

目标是让 LayerBase 的核心工作流稳定收敛为：

```text
Layer / Service / Manager:
  组织系统规则和调度入口。

ECS:
  承载高性能事实数据。

Actor:
  承载实体行为对象。

Query + Bring:
  将 ECS 计算结果自动投影为 Actor 行为事件。

Bundle / Blueprint:
  固化实体结构，使新增功能组件时不需要修改实体创建代码。

DTO Marker:
  明确 Component / ActorEvent / Request / Response 等数据类型边界，避免 AI 和开发者误用。
```

---

## 2. 总体心智模型

LayerBase 不追求成为大而全的游戏开发框架，而是一个游戏底层框架管线。

核心分工：

```text
系统层：
  Layer / Service / Manager。
  负责组织游戏规则、系统切片、调度入口。

事实层：
  ECS Component / Query。
  负责批量数据计算。

行为层：
  Actor / ActorEvent。
  负责实体行为响应。

结构层：
  Bundle / Blueprint。
  负责实体结构声明与稳定扩展。

通讯层：
  Query + Bring + ProjectResult。
  负责 ECS 数据到 Actor 行为的自动投影。
```

最终开发者可以选择三种风格：

```text
纯 ECS:
  [Query] 不带 [Bring]，只做 ECS 数据批量处理。

ECS + Actor:
  [Query] + [Bring] + ProjectResult，自动完成 ECS 到 Actor 行为事件提交。

纯 Actor:
  手动创建 Actor，缓存 ActorId，后续通过 ActorId 做单对单行为调度。
```

---

# Part A. Query / Bring 设计

## A1. 目标

Query / Bring 的目标是：

```text
让系统层的成员方法自动变成一次 ECS Query。
让 ECS 批量操作和 Actor 行为提交在同一个成员方法中表达。
隐藏 Query、Bring、Batch、Post、TouchProjectedActor 的细节。
```

最终开发者写：

```csharp
public sealed partial class EnemyViewService : IService
{
    [Query]
    [Bring<MoveViewEvent>]
    private ProjectResult OnUpdateEnemyView(
        Entity entity,
        ref PositionComponent position,
        in VelocityComponent velocity,
        in AoiComponent aoi,
        ref MoveViewEvent moveEvent)
    {
        if (!aoi.IsVisible)
        {
            return ProjectResult.Fail;
        }

        position.X += velocity.X;
        position.Y += velocity.Y;

        moveEvent = new MoveViewEvent(
            x: position.X,
            y: position.Y);

        return ProjectResult.Success;
    }
}
```

外部调用：

```csharp
enemyViewService.UpdateEnemyView();
```

等价于：

```text
Query<PositionComponent, VelocityComponent, AoiComponent>
  -> Bring<MoveViewEvent>
  -> Execute generated job
  -> ProjectResult.Fail: 不 Touch，不 Post
  -> ProjectResult.Touch: Touch，不 Post
  -> ProjectResult.Success: Touch，并 Post MoveViewEvent
```

---

## A2. QueryAttribute

```csharp
namespace LayerBase.ECS;

[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
}
```

说明：

```text
[Query]:
  标记一个单 Entity 处理方法。
  该方法不直接作为外部入口。
  源生成器会为它生成无参入口方法。
```

默认命名规则：

```text
OnXxx:
  单个 Entity 的处理逻辑。

Xxx:
  生成器生成的完整 ECS Query 入口。
```

示例：

```csharp
[Query]
private void OnUpdatePosition(...)
{
}
```

生成：

```csharp
public void UpdatePosition()
{
}
```

---

## A3. EntryPointAttribute

```csharp
namespace LayerBase.ECS;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EntryPointAttribute : Attribute
{
    public readonly string Name;

    public EntryPointAttribute(
        string name)
    {
        // name 参数作用：
        // 指定源生成器生成的 Query 入口方法名。
        // 如果标注了 EntryPoint，则 [Query] 方法本身不必以 On 开头。

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "EntryPoint name cannot be null or whitespace.",
                nameof(name));
        }

        Name = name;
    }
}
```

规则：

```text
没有 [EntryPoint]:
  [Query] 方法必须以 On 开头。
  生成入口名 = 去掉 On 前缀。

有 [EntryPoint("Name")]:
  生成入口名 = Name。
  [Query] 方法不必以 On 开头。
```

---

## A4. BringAttribute

基础版本使用 `typeof`，兼容性最好：

```csharp
namespace LayerBase.ECS;

[AttributeUsage(AttributeTargets.Method)]
public class BringAttribute : Attribute
{
    public readonly Type[] EventTypes;

    public BringAttribute(
        params Type[] eventTypes)
    {
        // eventTypes 参数作用：
        // 当前 Query 方法要输出的 Actor 事件类型集合。
        // 生成器会要求方法参数最后按顺序出现对应的 ref TEvent 参数。

        EventTypes = eventTypes;
    }
}
```

使用：

```csharp
[Query]
[Bring(typeof(MoveViewEvent))]
private ProjectResult OnUpdateEnemyView(...)
{
}
```

如果项目启用 C# 12，可以提供泛型语法糖：

```csharp
namespace LayerBase.ECS;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0> : BringAttribute
{
    public BringAttribute()
        : base(typeof(TEvent0))
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1> : BringAttribute
{
    public BringAttribute()
        : base(
            typeof(TEvent0),
            typeof(TEvent1))
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1, TEvent2> : BringAttribute
{
    public BringAttribute()
        : base(
            typeof(TEvent0),
            typeof(TEvent1),
            typeof(TEvent2))
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BringAttribute<TEvent0, TEvent1, TEvent2, TEvent3> : BringAttribute
{
    public BringAttribute()
        : base(
            typeof(TEvent0),
            typeof(TEvent1),
            typeof(TEvent2),
            typeof(TEvent3))
    {
    }
}
```

---

## A5. ProjectResult

`ProjectResult` 统一表达当前 Entity 在 Projection 中的后续动作。

```csharp
namespace LayerBase.ECS;

public readonly struct ProjectResult
{
    public static readonly ProjectResult Fail = new(
        kind: ProjectResultKind.Fail);

    public static readonly ProjectResult Touch = new(
        kind: ProjectResultKind.Touch);

    public static readonly ProjectResult Success = new(
        kind: ProjectResultKind.Success);

    public readonly ProjectResultKind Kind;

    private ProjectResult(
        ProjectResultKind kind)
    {
        // kind 参数作用：
        // 当前 Entity 的投影结果。
        // Fail 表示不保活 Actor，也不发送事件。
        // Touch 表示保活 Actor，但不发送事件。
        // Success 表示保活 Actor，并发送 Bring 事件。

        Kind = kind;
    }

    public bool ShouldTouch
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // ShouldTouch 属性作用：
            // 判断当前结果是否需要创建 / 保活 ProjectedActor。

            return Kind == ProjectResultKind.Touch
                   || Kind == ProjectResultKind.Success;
        }
    }

    public bool ShouldPost
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // ShouldPost 属性作用：
            // 判断当前结果是否需要将 Bring 事件加入 Batch 并投递给 Actor。

            return Kind == ProjectResultKind.Success;
        }
    }
}

public enum ProjectResultKind : byte
{
    Fail = 0,
    Touch = 1,
    Success = 2
}
```

语义：

```text
ProjectResult.Fail:
  不 Touch。
  不 Post。
  如果方法开头直接返回 Fail，则 ECS 数据也不会被修改。

ProjectResult.Touch:
  TouchProjectedActor。
  不 Post。
  适合 AOI 内保活但本帧无行为事件。

ProjectResult.Success:
  TouchProjectedActor。
  Post Bring 事件。
```

---

## A6. 返回值规则

### 无 Bring

无 `[Bring]` 时，方法必须返回 `void`。

```csharp
[Query]
private void OnUpdatePosition(
    ref PositionComponent position,
    in VelocityComponent velocity)
{
    position.X += velocity.X;
    position.Y += velocity.Y;
}
```

生成：

```text
Query + ForEach
```

### 有 Bring

有 `[Bring]` 时，方法必须返回 `ProjectResult`。

```csharp
[Query]
[Bring<MoveViewEvent>]
private ProjectResult OnUpdateEnemyView(
    ref PositionComponent position,
    in VelocityComponent velocity,
    ref MoveViewEvent moveEvent)
{
    return ProjectResult.Success;
}
```

生成：

```text
Query + Bring + ProjectResult + Post
```

---

## A7. 参数解析规则

```text
Entity entity:
  注入当前命中的 Entity。
  不加入 Query 组件。
  最多出现一次。

ref TComponent:
  TComponent 必须实现 IComponent。
  加入 Query All 条件。
  以可写引用传入。

in TComponent:
  TComponent 必须实现 IComponent。
  加入 Query All 条件。
  以只读引用传入。

ref TEvent:
  如果 TEvent 是 Bring 声明的事件类型，则必须位于参数末尾。
  TEvent 必须实现 IActorEvent。
  不加入 Query 组件。
```

禁止：

```text
组件值传递。
Bring 事件使用 in。
Bring 事件不在参数末尾。
Bring 事件顺序与 BringAttribute 声明不一致。
```

---

## A8. 生成代码形态

纯 ECS：

```csharp
public sealed partial class MoveService
{
    public void UpdatePosition()
    {
        var job =
            new __UpdatePositionJob(this);

        this.Query<PositionComponent, VelocityComponent>()
            .ForEach(ref job);
    }

    private readonly struct __UpdatePositionJob :
        IQueryJob<PositionComponent, VelocityComponent>
    {
        private readonly MoveService _self;

        public __UpdatePositionJob(
            MoveService self)
        {
            // self 参数作用：
            // 当前 Service 实例。
            // 生成 Job 通过它调用用户定义的 Query 成员方法。

            _self = self;
        }

        public void Execute(
            Entity entity,
            ref PositionComponent position,
            ref VelocityComponent velocity)
        {
            // entity 参数作用：
            // 当前 Query 命中的 ECS Entity。

            // position 参数作用：
            // 当前 Entity 的位置组件引用。

            // velocity 参数作用：
            // 当前 Entity 的速度组件引用。

            _self.OnUpdatePosition(
                ref position,
                in velocity);
        }
    }
}
```

Projection：

```csharp
public sealed partial class EnemyViewService
{
    public void UpdateEnemyView()
    {
        var job =
            new __UpdateEnemyViewJob(this);

            this.Query<PositionComponent, VelocityComponent, AoiComponent>()
                .Bring<MoveViewEvent>()
                .Post(ref job);
    }

    private readonly struct __UpdateEnemyViewJob :
        IProjectionJob<
            PositionComponent,
            VelocityComponent,
            AoiComponent,
            MoveViewEvent>
    {
        private readonly EnemyViewService _self;

        public __UpdateEnemyViewJob(
            EnemyViewService self)
        {
            // self 参数作用：
            // 当前 Service 实例。
            // 生成 Job 通过它调用用户定义的 Projection 成员方法。

            _self = self;
        }

        public ProjectResult Execute(
            Entity entity,
            ref PositionComponent position,
            ref VelocityComponent velocity,
            ref AoiComponent aoi,
            ref MoveViewEvent moveEvent)
        {
            // entity 参数作用：
            // 当前 Query 命中的 ECS Entity。

            // position 参数作用：
            // 当前 Entity 的位置组件引用。

            // velocity 参数作用：
            // 当前 Entity 的速度组件引用。
            // 用户方法声明为 in，因此调用用户方法时按 in 传入。

            // aoi 参数作用：
            // 当前 Entity 的 AOI 组件引用。

            // moveEvent 参数作用：
            // Bring 出来的 Actor 行为事件输出槽。

            return _self.OnUpdateEnemyView(
                entity,
                ref position,
                in velocity,
                in aoi,
                ref moveEvent);
        }
    }
}
```

---

## A9. 诊断规则

```text
LB-ECS001:
  Type containing [Query] method must be partial.

LB-ECS002:
  Type containing [Query] method must implement IEcsWorldProvider or provide a supported world source.

LB-ECS003:
  [Query] method cannot be generic.

LB-ECS004:
  [Query] method without [Bring] must return void.

LB-ECS005:
  [Query] method with [Bring] must return ProjectResult.

LB-ECS006:
  [Bring] must declare at least one event type.

LB-ECS007:
  [Bring] event count exceeds generated projection template limit.

LB-ECS008:
  [Bring] event parameters must appear at the end of the method parameter list and match the [Bring] event type order.

LB-ECS009:
  Bring event parameter must be ref.

LB-ECS010:
  ECS component parameter must be ref or in.

LB-ECS011:
  Entity parameter can appear at most once.

LB-ECS012:
  Query component count exceeds generated query template limit.

LB-ECS013:
  Query component type must implement IComponent.

LB-ECS014:
  Bring event type must implement IActorEvent.

LB-ECS020:
  [Query] method must start with On or specify [EntryPoint("Name")].

LB-ECS021:
  [Query] method 'On' is invalid because generated entry point name would be empty.

LB-ECS022:
  Generated entry point already exists.

LB-ECS023:
  Multiple [Query] methods generate the same entry point.

LB-ECS024:
  [EntryPoint] name is not a valid C# method name.
```

---

# Part B. Bundle / Blueprint 设计

## B1. 目标

Bundle / Blueprint 的目标是：

```text
稳定实体结构声明。
避免新增功能组件时修改实体创建逻辑。
让 AI 和开发者只需要改 Bundle / Blueprint，即可扩展实体能力。
```

最终创建代码：

```csharp
var enemy = world.CreateEntity()
    .With<EnemyBlueprint>();
```

新增功能时不修改实体创建代码，只修改：

```text
Component
Bundle
Blueprint
Query
Actor Event
Actor Handler
```

---

## B2. 设计原则

Bundle / Blueprint 使用 `class`，因为它们只在实体结构构建冷路径中使用，不参与 Query / Post / Actor 邮箱等热路径。

核心原则：

```text
Bundle / Blueprint 使用 class。
Bundle / Blueprint 实现实例 Config(ref EntityBlueprintBuilder builder)。
不使用 static abstract interface member。
不使用运行时注册表。
不使用运行时动态 ID 分配。
不使用 List<IBlueprintUnit> 管理 Bundle / Blueprint。
每个 Bundle / Blueprint 类型通过泛型静态缓存持有单例实例。
每个 Blueprint 类型通过泛型静态缓存持有构建后的 EntityBlueprint。
EntityBlueprintBuilder 显式区分 WithComponent 和 WithBundle。
```

推荐核心结构：

```text
IBlueprintUnit
IBundle
IEntityBlueprint
BlueprintUnitCache<TUnit>
EntityBlueprintCache<TBlueprint>
EntityBlueprintBuilder
EntityCreateBuilder
World.CreateEntity().With<TBlueprint>()
```

---

## B3. 核心接口

```csharp
namespace LayerBase.ECS;

/// <summary>
/// Blueprint 和 Bundle 的共同配置单元。
/// </summary>
public interface IBlueprintUnit
{
    void Config(
        ref EntityBlueprintBuilder builder);
}

/// <summary>
/// Bundle 表示一组可复用的实体结构切片。
/// </summary>
public interface IBundle : IBlueprintUnit
{
}

/// <summary>
/// Blueprint 表示完整实体结构模板。
/// </summary>
public interface IEntityBlueprint : IBlueprintUnit
{
}
```

说明：

```text
IBlueprintUnit:
  Bundle 和 Blueprint 的共同配置接口。

IBundle:
  表示实体结构切片。
  例如移动能力、战斗能力、AOI 能力。

IEntityBlueprint:
  表示完整实体结构模板。
  例如 EnemyBlueprint、ProjectileBlueprint、PlayerBlueprint。
```

---

## B4. 标记特性

```csharp
namespace LayerBase.ECS;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LayerBundleAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class LayerBlueprintAttribute : Attribute
{
}
```

说明：

```text
[LayerBundle]:
  标记一个 class 是 Bundle。
  供 Roslyn 分析器、源生成器、Agent 索引器识别。

[LayerBlueprint]:
  标记一个 class 是 Blueprint。
  供 Roslyn 分析器、源生成器、Agent 索引器识别。
```

---

## B5. BlueprintUnitCache

```csharp
namespace LayerBase.ECS;

/// <summary>
/// Blueprint / Bundle 的泛型实例缓存。
/// </summary>
/// <typeparam name="TUnit">
/// TUnit 参数作用：
/// 当前要缓存的 BlueprintUnit 类型。
/// 它可以是 Bundle，也可以是 Blueprint。
/// </typeparam>
internal static class BlueprintUnitCache<TUnit>
    where TUnit : class, IBlueprintUnit, new()
{
    public static readonly TUnit Instance = new();

    public static void Config(
        ref EntityBlueprintBuilder builder)
    {
        // builder 参数作用：
        // 当前实体蓝图构建器。
        // 这里会调用 TUnit.Config，把 TUnit 声明的组件、Bundle、Actor 投影信息写入 builder。

        Instance.Config(
            ref builder);
    }
}
```

设计说明：

```text
每个 Bundle / Blueprint 类型只创建一个实例。
不需要注册表。
不需要运行时 ID。
不需要字典。
不需要反射。
不需要 Interlocked。
```

---

## B6. EntityBlueprintCache

```csharp
namespace LayerBase.ECS;

/// <summary>
/// Blueprint 构建结果缓存。
/// </summary>
/// <typeparam name="TBlueprint">
/// TBlueprint 参数作用：
/// 当前实体使用的 Blueprint 类型。
/// </typeparam>
internal static class EntityBlueprintCache<TBlueprint>
    where TBlueprint : class, IEntityBlueprint, new()
{
    private static readonly EntityBlueprint s_blueprint =
        Build();

    public static EntityBlueprint GetOrBuild()
    {
        // GetOrBuild 方法作用：
        // 返回当前 Blueprint 类型对应的实体结构缓存。
        // 泛型静态字段保证每个 TBlueprint 只构建一次。

        return s_blueprint;
    }

    private static EntityBlueprint Build()
    {
        // Build 方法作用：
        // 调用 Blueprint.Config 构建实体结构。
        // 该方法属于冷路径，不在每帧 Query / Post 热路径执行。

        var builder =
            new EntityBlueprintBuilder();

        BlueprintUnitCache<TBlueprint>.Config(
            ref builder);

        return builder.Build();
    }
}
```

设计说明：

```text
EntityBlueprint 是 Blueprint 的构建结果。
TBlueprint.Config 只在 EntityBlueprintCache<TBlueprint> 初始化时执行一次。
后续创建实体时直接复用 EntityBlueprint。
```

---

## B7. EntityBlueprintBuilder

```csharp
namespace LayerBase.ECS;

/// <summary>
/// 实体结构构建器。
/// </summary>
public ref struct EntityBlueprintBuilder
{
    private ComponentSignatureBuilder _components;

    private ActorProjectionMetaData _actorProjection;

    public void WithComponent<TComponent>()
        where TComponent : struct, IComponent
    {
        // TComponent 参数作用：
        // 要加入当前实体结构的 ECS 组件类型。
        // 这里只登记组件类型，不写入组件实例值。

        _components.Add(
            Component<TComponent>.Id);
    }

    public void WithBundle<TBundle>()
        where TBundle : class, IBundle, new()
    {
        // TBundle 参数作用：
        // 要展开的实体结构切片。
        // Bundle 内部可以继续声明 Component、Bundle 或 Actor 投影信息。

        BlueprintUnitCache<TBundle>.Config(
            ref this);
    }

    public void WithProjectedActor<TActor>(
        ProjectedActorOptions options = default)
        where TActor : class, IActor, new()
    {
        // TActor 参数作用：
        // 当前 Entity 可以延迟投影成的 Actor 类型。
        // Entity 创建时 ActorId 无效。
        // Query + Bring 或 Touch 命中后才创建或保活 Actor。

        // options 参数作用：
        // ProjectedActor 的保活帧数、释放策略、池化策略等配置。

        _actorProjection =
            ActorProjectionMetaData.Projected<TActor>(
                options);
    }

    public void WithActor<TActor>()
        where TActor : class, IActor, new()
    {
        // TActor 参数作用：
        // 当前 Entity 创建时立即绑定的 Actor 类型。
        // 适合必须从创建开始就具备行为对象的实体。

        _actorProjection =
            ActorProjectionMetaData.Immediate<TActor>();
    }

    public EntityBlueprint Build()
    {
        // Build 方法作用：
        // 将当前 Builder 收集到的组件签名和 Actor 投影信息构造成 EntityBlueprint。

        return new EntityBlueprint(
            componentSignature: _components.Build(),
            actorProjection: _actorProjection);
    }
}
```

说明：

```text
WithComponent<TComponent>:
  明确声明 ECS 组件。

WithBundle<TBundle>:
  明确展开结构切片。

WithProjectedActor<TActor>:
  声明延迟 Actor 投影。

WithActor<TActor>:
  声明创建实体时立即绑定 Actor。
```

第一版不建议使用 `builder.With<T>()` 同时猜测 Component 和 Bundle，原因是：

```text
语义清楚。
AI 不容易误用。
Analyzer 更容易校验。
泛型约束更准确。
不需要运行时判断 T 的语义。
```

---

## B8. Bundle 示例

```csharp
namespace Game.Layers.Battle.ECS.Bundles;

[LayerBundle]
public sealed class MoveBundle : IBundle
{
    public void Config(
        ref EntityBlueprintBuilder builder)
    {
        // builder 参数作用：
        // 当前实体蓝图构建器。
        // MoveBundle 用于声明移动能力需要的 ECS 组件。

        builder.WithComponent<PositionComponent>();
        builder.WithComponent<VelocityComponent>();
        builder.WithComponent<MoveStateComponent>();
    }
}
```

Bundle 代表一个功能切片：

```text
MoveBundle:
  移动能力。

CombatBundle:
  战斗能力。

AoiBundle:
  可见性 / AOI 能力。

ViewProjectionBundle:
  表现投影能力。
```

---

## B9. Blueprint 示例

```csharp
namespace Game.Layers.Battle.ECS.Blueprints;

[LayerBlueprint]
public sealed class EnemyBlueprint : IEntityBlueprint
{
    public void Config(
        ref EntityBlueprintBuilder builder)
    {
        // builder 参数作用：
        // 当前实体蓝图构建器。
        // EnemyBlueprint 用于声明敌人实体的完整结构。

        builder.WithBundle<MoveBundle>();
        builder.WithBundle<CombatBundle>();
        builder.WithBundle<AoiBundle>();

        builder.WithProjectedActor<EnemyActor>();
    }
}
```

Blueprint 表示完整实体结构：

```text
EnemyBlueprint:
  敌人实体结构。

ProjectileBlueprint:
  投射物实体结构。

PlayerBlueprint:
  玩家实体结构。
```

---

## B10. EntityBlueprint

```csharp
namespace LayerBase.ECS;

/// <summary>
/// Blueprint 构建后的实体结构缓存。
/// </summary>
public readonly struct EntityBlueprint
{
    public readonly ComponentSignature ComponentSignature;

    public readonly ActorProjectionMetaData ActorProjection;

    public EntityBlueprint(
        ComponentSignature componentSignature,
        ActorProjectionMetaData actorProjection)
    {
        // componentSignature 参数作用：
        // 当前实体结构包含的 ECS 组件签名。

        // actorProjection 参数作用：
        // 当前实体与 Actor 的绑定 / 投影策略。

        ComponentSignature = componentSignature;
        ActorProjection = actorProjection;
    }
}
```

---

## B11. Entity 创建入口

```csharp
namespace LayerBase.ECS;

public ref struct EntityCreateBuilder
{
    private readonly World _world;

    public EntityCreateBuilder(
        World world)
    {
        // world 参数作用：
        // 当前 ECS World。
        // With<TBlueprint>() 会基于该 World 创建实体。

        _world = world;
    }

    public Entity With<TBlueprint>()
        where TBlueprint : class, IEntityBlueprint, new()
    {
        // TBlueprint 参数作用：
        // 当前实体使用的结构模板。
        // Blueprint 决定组件结构、Actor 投影策略和初始化约束。

        EntityBlueprint blueprint =
            EntityBlueprintCache<TBlueprint>.GetOrBuild();

        return _world.CreateFromBlueprint(
            blueprint);
    }
}
```

使用：

```csharp
var enemy = world.CreateEntity()
    .With<EnemyBlueprint>();

enemy.Set(new PositionComponent(...));
enemy.Set(new VelocityComponent(...));
```

约束：

```text
With<TBlueprint>() 只创建结构。
不注入组件实例。
初始化数据通过 Entity.Set<TComponent>() 写入。
Set<TComponent>() 只能写 Blueprint 已声明组件。
```

---

## B12. World 创建扩展

```csharp
namespace LayerBase.ECS;

public static class WorldEntityCreateExtensions
{
    public static EntityCreateBuilder CreateEntity(
        this World world)
    {
        return new EntityCreateBuilder(
            world);
    }
}
```

---

## B13. Service / Layer / Context 创建扩展约束

所有 Blueprint 创建入口统一使用：

```csharp
where TBlueprint : class, IEntityBlueprint, new()
```

示例：

```csharp
namespace LayerBase.ECS;
public static class LayerEcsExtensions
{
    public static EntityCreateBuilder CreateEntity(
        this Layer layer)
    {
        // world 参数作用：
        // 当前 ECS World。
        // 返回实体创建构建器，用于继续指定 Blueprint。

        return new EntityCreateBuilder(
            ECSWorld(layer));
    }
}
public static class ILayerContextEcsExtensions
{
    public static EntityCreateBuilder CreateEntity(
        this ILayerContext context)
    {
        // world 参数作用：
        // 当前 ECS World。
        // 返回实体创建构建器，用于继续指定 Blueprint。

        return new EntityCreateBuilder(
            ECSWorld(context));
    }
}
public static class ServiceEcsExtensions
{
    public static EntityCreateBuilder CreateEntity(
        this Service service)
    {
        // world 参数作用：
        // 当前 ECS World。
        // 返回实体创建构建器，用于继续指定 Blueprint。

        return new EntityCreateBuilder(
            ECSWorld(service));
    }
}
```

---

## B14. 初始化数据规则

Blueprint 只声明结构，不写实例数据。

```csharp
var enemy = world.CreateEntity()
    .With<EnemyBlueprint>();

enemy.Set(new PositionComponent
{
    X = 0f,
    Y = 0f
});
```

规则：

```text
CreateEntity().With<TBlueprint>:
  只创建结构。

Entity.Set<TComponent>:
  写入组件实例数据。

Entity.Set<TComponent>:
  只能写 Blueprint 已声明组件。
```

Debug 错误示例：

```text
Entity was created from EnemyBlueprint, but component HealthComponent was not declared.
```

---

## B15. AI 友好的扩展路线

新增敌人功能时，Agent 应按固定路线修改：

```text
1. 新增 Component，实现 IComponent。
2. 如果该功能是可复用切片，新增或修改 Bundle。
3. 将 Bundle 加入对应 Blueprint。
4. 新增 [Query] 方法处理 ECS 数据。
5. 如需表现，新增 IActorEvent 并用 [Bring] 投递。
6. 在目标 Actor 中新增 Handler。
7. 补 Blueprint 结构测试、Query 测试、Projection 测试。
```

---

## B16. Analyzer 诊断

```text
LB-BP001:
  [LayerBundle] 类型必须是 class。

LB-BP002:
  [LayerBundle] 类型必须实现 IBundle。

LB-BP003:
  [LayerBundle] 类型必须有 public parameterless constructor。

LB-BP004:
  [LayerBlueprint] 类型必须是 class。

LB-BP005:
  [LayerBlueprint] 类型必须实现 IEntityBlueprint。

LB-BP006:
  [LayerBlueprint] 类型必须有 public parameterless constructor。

LB-BP007:
  Bundle / Blueprint 的 Config 方法必须是 public void Config(ref EntityBlueprintBuilder builder)。

LB-BP008:
  WithComponent<TComponent>() 的 TComponent 必须实现 IComponent。

LB-BP009:
  WithBundle<TBundle>() 的 TBundle 必须实现 IBundle。

LB-BP010:
  Blueprint 必须至少声明一个 Component 或 Bundle。

LB-BP011:
  同一个 Blueprint 中重复声明同一 Component 时给出 warning。

LB-BP012:
  WithProjectedActor<TActor>() 的 TActor 必须实现 IActor。

LB-BP013:
  WithActor<TActor>() 的 TActor 必须实现 IActor。

LB-BP014:
  Entity.Set<TComponent>() 的 TComponent 不在 Blueprint 中时，Debug 模式报错。
```

---

## B17. 测试计划

```text
MoveBundle_Config_AddsExpectedComponents
NestedBundle_Config_ExpandsCorrectly
Bundle_Config_DoesNotWriteComponentValues

EnemyBlueprint_Build_ContainsMoveCombatAoiComponents
EnemyBlueprint_Build_HasProjectedActor
BlueprintCache_ReturnsSameBlueprintInstance
BlueprintCache_BuildsOnlyOnce

CreateEntity_WithEnemyBlueprint_CreatesEntityWithExpectedComponents
CreateEntity_WithEnemyBlueprint_AllowsSetDeclaredComponent
CreateEntity_WithEnemyBlueprint_RejectsUndeclaredComponentInDebug
CreateEntity_WithProjectedActor_InitialActorIdInvalid

AddingComponentToBundle_ChangesBlueprintStructure
AddingBundleToBlueprint_ChangesCreatedEntityStructure
BlueprintIndex_ReportsExpectedBundlesComponentsAndProjectedActor
```

---

## B18. 实施顺序

```text
Step 1:
  新增 IBlueprintUnit、IBundle、IEntityBlueprint、LayerBundleAttribute、LayerBlueprintAttribute。

Step 2:
  新增 BlueprintUnitCache<TUnit>、EntityBlueprintCache<TBlueprint>。

Step 3:
  新增 EntityBlueprintBuilder：
    WithComponent<TComponent>()
    WithBundle<TBundle>()
    WithProjectedActor<TActor>()
    WithActor<TActor>()
    Build()

Step 4:
  新增 EntityCreateBuilder 和 World / Service / Context / Layer 创建扩展。

Step 5:
  新增 Analyzer 诊断。

Step 6:
  更新 Roslyn Index 和 Skill：
    Bundle -> Components
    Blueprint -> Bundles / Components / ProjectedActor
```

---

## B19. 完成标准

完成后应满足：

```text
Bundle / Blueprint 使用 class。
Bundle / Blueprint 不使用运行时注册表。
Bundle / Blueprint 不使用动态 ID 分配。
Blueprint 构建结果通过 EntityBlueprintCache<TBlueprint> 缓存。
Bundle 实例通过 BlueprintUnitCache<TBundle> 缓存。
Blueprint 实例通过 BlueprintUnitCache<TBlueprint> 缓存。
EntityBlueprintBuilder 明确区分 WithComponent 和 WithBundle。
CreateEntity().With<TBlueprint>() 可以创建稳定结构实体。
新增组件能力时只需要修改 Bundle / Blueprint，不需要修改实体创建代码。
Analyzer 可以校验 Bundle / Blueprint 的结构合法性。
Roslyn Index 可以输出 Blueprint 最终结构。
AI Agent 可以按固定路线扩展实体能力。
```

---

# Part C. DTO 分类设计

## C1. 目标

DTO 分类的目标是：

```text
让人类和 AI 明确识别数据类型语义。
让源生成器和 Analyzer 可以做强校验。
避免 Component、ActorEvent、Request、Response 混用。
```

建议使用空接口作为 Marker。

---

## C2. Marker 接口

```csharp
namespace LayerBase.Core;

public interface ILayerDto
{
}

public interface IComponent : ILayerDto
{
}

public interface IActorEvent : ILayerDto
{
}

public interface IRequest : ILayerDto
{
}

public interface IResponse : ILayerDto
{
}

public interface ICommand : ILayerDto
{
}

public interface ISnapshot : ILayerDto
{
}
```

说明：

```text
ILayerDto:
  所有 LayerBase 数据对象根标记。

IComponent:
  ECS 事实数据。
  只能作为 Query / Bundle / Blueprint 的组件。

IActorEvent:
  Actor 行为事件。
  只能用于 Bring / Actor Handler / ActorWorld.PostTo。

IRequest:
  系统间请求 DTO。

IResponse:
  系统间响应 DTO。

ICommand:
  外部输入或高层命令。

ISnapshot:
  快照数据，用于调试、存档、网络同步、状态导出。
```

---

## C3. 示例

### Component

```csharp
public struct PositionComponent : IComponent
{
    public float X;
    public float Y;
}
```

### Actor Event

```csharp
public readonly struct MoveViewEvent : IActorEvent
{
    public readonly float X;

    public readonly float Y;

    public MoveViewEvent(
        float x,
        float y)
    {
        // x 参数作用：
        // Actor 表现目标位置的 X 坐标。

        // y 参数作用：
        // Actor 表现目标位置的 Y 坐标。

        X = x;
        Y = y;
    }
}
```

### Request / Response

```csharp
public readonly struct DamageRequest : IRequest
{
    public readonly int TargetId;
    public readonly int Damage;

    public DamageRequest(
        int targetId,
        int damage)
    {
        // targetId 参数作用：
        // 受击目标的业务 ID。

        // damage 参数作用：
        // 本次请求造成的伤害数值。

        TargetId = targetId;
        Damage = damage;
    }
}
```

```csharp
public readonly struct DamageResponse : IResponse
{
    public readonly bool Applied;
    public readonly int FinalDamage;

    public DamageResponse(
        bool applied,
        int finalDamage)
    {
        // applied 参数作用：
        // 伤害是否成功应用。

        // finalDamage 参数作用：
        // 最终结算后的伤害值。

        Applied = applied;
        FinalDamage = finalDamage;
    }
}
```

---

## C4. 强制规则

关键链路必须强制：

```text
Query 组件参数:
  必须实现 IComponent。

Bundle.With<TComponent>:
  TComponent 必须实现 IComponent。

Blueprint 组件:
  必须实现 IComponent。

Bring 事件类型:
  必须实现 IActorEvent。

Actor Handler 事件参数:
  必须实现 IActorEvent。

Layer Call Request:
  必须实现 IRequest。

Layer Call Response:
  必须实现 IResponse。
```

推荐规则：

```text
Command:
  推荐实现 ICommand。

Snapshot:
  推荐实现 ISnapshot。
```

---

## C5. Analyzer 诊断

```text
LB-DTO001:
  ECS component type must implement IComponent.

LB-DTO002:
  Bring event type must implement IActorEvent.

LB-DTO003:
  Actor handler event type must implement IActorEvent.

LB-DTO004:
  Request type must implement IRequest.

LB-DTO005:
  Response type must implement IResponse.

LB-DTO006:
  Type cannot implement both IComponent and IActorEvent.

LB-DTO007:
  Type cannot implement both IRequest and IResponse unless explicitly allowed.

LB-DTO008:
  DTO type should be readonly struct unless it is a mutable ECS component.

LB-DTO009:
  IActorEvent should be readonly struct.

LB-DTO010:
  IRequest / IResponse should be readonly struct.
```

---

# Part D. 实施顺序

## D1. Step 1：DTO Marker

新增：

```text
ILayerDto
IComponent
IActorEvent
IRequest
IResponse
ICommand
ISnapshot
```

修改：

```text
现有 Component 实现 IComponent。
现有 Actor Event 实现 IActorEvent。
现有 Request / Response 实现对应接口。
```

验收：

```text
现有代码可编译。
Query / Bring 可基于 Marker 做校验。
```

---

## D2. Step 2：Query / Bring 生成器

新增：

```text
QueryAttribute
BringAttribute
BringAttribute<T...>
EntryPointAttribute
ProjectResult
ProjectResultKind
```

生成：

```text
纯 Query 入口。
Bring Projection 入口。
Job struct。
```

验收：

```text
[Query] + void 可生成 ForEach。
[Query] + [Bring] + ProjectResult 可生成 Bring + Post。
ProjectResult.Fail / Touch / Success 行为正确。
```

---

## D3. Step 3：Bundle / Blueprint

新增：

```text
IBundle
IEntityBlueprint
EntityBlueprintBuilder
EntityBlueprintCache
GeneratedBundleDispatcher
GeneratedBlueprintDispatcher
CreateEntity().With<TBlueprint>()
```

验收：

```text
可以通过 Blueprint 创建实体。
新增组件只需修改 Bundle / Blueprint，不改实体创建代码。
Set 未声明组件时给出明确错误。
```

---

## D4. Step 4：Analyzer

新增诊断：

```text
Query 参数校验。
Bring 类型校验。
DTO Marker 冲突校验。
Bundle / Blueprint 类型校验。
EntryPoint / Query 命名校验。
```

验收：

```text
错误写法在编译期报错。
错误信息能指导 AI 和人类修复。
```

---

## 5. 完成标准

完成后应满足：

```text
系统层可以通过 [Query] 成员方法完成 ECS 批量操作。
系统层可以通过 [Query] + [Bring] 自动提交 Actor 行为。
实体结构通过 Bundle / Blueprint 稳定扩展。
新增功能组件不需要修改实体创建逻辑。
Component / ActorEvent / Request / Response 有明确 Marker。
生成器和 Analyzer 能避免 AI 认错 DTO。
核心热路径不使用反射。
生成代码不使用 delegate / closure。
开发者和 AI 都能按固定范式扩展项目。
```
