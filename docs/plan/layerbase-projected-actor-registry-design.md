# LayerBase Embedded ArchECS Query Flow Projection Design

## 1. 设计目标

本文档定义 LayerBase 内置 ArchECS 后的 Query Flow Projection 基础设计。

核心目标：

```text
LayerRuntime 持有 ActorWorld。
LayerRuntime 持有 EcsWorld。
LayerRuntime 持有 ProjectedActorTypeRegistry。
ProjectedActorTypeRegistry 是每个 LayerRuntime 独立的运行时数组表。
源生成器只负责生成注册代码，把 Type 和工厂委托直存进 Registry。
Projection Query Flow 保持干净 API。
Post() 只负责 Batch 投递，不参与 RuntimeFrameBudget。
ActorWorld.Pump 继续负责按 RuntimeFrameBudget 消费 Actor 邮箱。
ProjectedActor 回收只使用 Stopwatch 时间戳。
```

明确不做：

```text
不引入 IProjectionJob。
不引入 ITouchProjectedActorJob。
不引入 Projection 系统标注。
不引入 WithProjectedActor 生成器输出。
不引入全局静态 ActorTypeRegistry。
不使用逻辑帧号回收 ProjectedActor。
不让 Post() 接收 ActorWorld。
不让 Post() 接收 RuntimeFrameBudget。
不让 Post() 接收 currentFrame。
```

最终业务 API：

```csharp
world.Query<PositionComponent, VelocityComponent>()
    .Where(static (
        in Entity entity,
        in PositionComponent position,
        in VelocityComponent velocity) =>
    {
        // entity 参数作用：
        // 当前 Query 命中的 ECS Entity。

        // position 参数作用：
        // 当前 Entity 的位置事实数据。
        // Where 阶段只读，不修改。

        // velocity 参数作用：
        // 当前 Entity 的速度事实数据。
        // Where 阶段只读，不修改。

        return velocity.X != 0f || velocity.Y != 0f;
    })
    .Bring<MoveViewEvent>()
    .ForEach(static (
        in Entity entity,
        ref PositionComponent position,
        ref VelocityComponent velocity,
        ref MoveViewEvent output) =>
    {
        // entity 参数作用：
        // 当前 Query 命中的 ECS Entity。

        // position 参数作用：
        // 当前 Entity 的位置组件。
        // ForEach 阶段允许修改它，因为它是 ECS 事实数据。

        // velocity 参数作用：
        // 当前 Entity 的速度组件。

        // output 参数作用：
        // 输出给 Actor 的一次性行为事件。
        // 它不是 ECS 组件，不进入 Chunk，不长期保存。

        position.X += velocity.X;
        position.Y += velocity.Y;

        output = new MoveViewEvent(
            x: position.X,
            y: position.Y);
    })
    .Batch()
    .Post();
```

Touch API：

```csharp
world.Query<PositionComponent, AoiComponent>()
    .Where(static (
        in Entity entity,
        in PositionComponent position,
        in AoiComponent aoi) =>
    {
        // entity 参数作用：
        // 当前 Query 命中的 ECS Entity。

        // position 参数作用：
        // 当前 Entity 的位置事实数据。

        // aoi 参数作用：
        // 当前 Entity 的 AOI 状态。
        // AOI 是 Area Of Interest，表示实体是否处于玩家关心范围内。

        return aoi.IsVisible;
    })
    .TouchProjectedActor();
```

---

## 2. 总体结构

推荐目录：

```text
LayerBase/
  Application/
    LayerRuntime.ECS.cs

  Actor/
    Core/
      IPooledActor.cs
      ActorId.cs

    Storage/
      ActorWorld.ProjectedActor.cs

  ECS/
    Projection/
      ProjectedActorMeta.cs
      ProjectedActorHandle.cs
      ProjectedActorTime.cs
      ProjectedActorTypeRegistry.cs
      ProjectedActorWorldExtensions.cs
      ProjectedActorBinding.cs
      ActiveProjectedActorList.cs

    Projection/Generated/
      GeneratedProjectedActorTypes.g.cs

    Projection/Flow/
      ProjectionBatchBuffer.cs
      ProjectionDelegates.g.cs
      ProjectionQueryFlow.g.cs
      ProjectionExecutor.g.cs
      ProjectionWorldExtensions.g.cs

    Projection/Templates/
      Helpers.ttinclude
      ProjectionDelegates.tt
      ProjectionQueryFlow.tt
      ProjectionExecutor.tt
      ProjectionWorldExtensions.tt

  Arch/Core/
    Chunk.Projection.cs
    World.Projection.cs
```

模块职责：

```text
LayerRuntime:
  拥有 ActorWorld、EcsWorld、ProjectedActorTypeRegistry。

ProjectedActorTypeRegistry:
  当前 Runtime 内的 ActorTypeId -> Type / Factory 数组表。

GeneratedProjectedActorTypes:
  源生成器输出。
  负责把 Type 和 Factory 注册进 Runtime 的 Registry。
  负责生成 GetId<TActor>()。

ProjectedActorBinding:
  无状态工具。
  负责 Ensure Actor、刷新 RecycleDeadlineTicks。

Query Flow:
  负责 Query -> Where -> Bring -> ForEach -> Batch -> Post。

ActorWorld:
  负责 Actor 创建、Actor 邮箱、Actor Pump、Actor 生命周期。

Arch Chunk:
  原生保存 ProjectedActorMeta[]。
```

---

## 3. 多世界安全原则

每个 `LayerRuntime` 拥有独立对象：

```text
LayerRuntime A
  Actors A
  EcsWorld A
  ProjectedActorTypeRegistry A

LayerRuntime B
  Actors B
  EcsWorld B
  ProjectedActorTypeRegistry B
```

`ProjectedActorMeta.ActorTypeId` 只在所属 `LayerRuntime.ProjectedActorTypeRegistry` 内有效。

禁止：

```text
全局静态 ActorTypeRegistry。
全局静态 ActorType<TActor>.Id。
跨 Runtime 共享可变类型注册表。
Entity 从 Runtime A 创建 Runtime B 的 Actor。
```

允许：

```text
源生成器生成静态注册方法。
静态注册方法本身不保存状态。
每个 Runtime 初始化时把生成表注册到自己的 Registry。
```

---

## 4. IPooledActor 修改

文件：

```text
LayerBase/Actor/Core/IPooledActor.cs
```

代码：

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 显式允许对象池复用的 Actor。
/// </summary>
public interface IPooledActor : IActor
{
    long RecycleDeadlineTicks { get; set; }

    void OnRent();

    void OnReturn();
}
```

字段说明：

```text
RecycleDeadlineTicks:
  ProjectedActor 最早允许被回收的 Stopwatch 时间戳。
  Projection Post 或 Touch 命中时刷新。
  SweepProjectedActors 比较 nowTicks 和 RecycleDeadlineTicks。
```

设计约束：

```text
WithProjectedActor<TActor>() 要求 TActor : class, IPooledActor, new()。
普通 Actor 创建仍然可以只依赖 IActor。
ProjectedActor 语义与对象池语义绑定。
```

---

## 5. ActorId 补齐

文件：

```text
LayerBase/Actor/Core/ActorId.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;

namespace LayerBase.Actor;

public readonly struct ActorId : IEquatable<ActorId>
{
    public static readonly ActorId Invalid = new(
        archetypeId: -1,
        slotIndex: -1,
        generation: -1);

    public readonly int ArchetypeId;

    public readonly int SlotIndex;

    public readonly int Generation;

    public ActorId(
        int archetypeId,
        int slotIndex,
        int generation)
    {
        // archetypeId 参数作用：
        // Actor 所属 BehaviourArchetype 的物理下标。
        // ActorWorld.PostTo 会用它定位 Actor 邮箱行。

        // slotIndex 参数作用：
        // Actor 在 TypedActorStorage 中的物理槽位。
        // ActorWorld.PostTo 会用它定位具体 mailbox slot。

        // generation 参数作用：
        // Actor 槽位版本号。
        // 用于判断旧 ActorId 是否已经失效。

        ArchetypeId = archetypeId;
        SlotIndex = slotIndex;
        Generation = generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 逻辑说明：
            // 只判断 ActorId 是否具备进入 ActorWorld 物理定位的基本条件。
            // generation 是否匹配由 ActorWorld.PostTo 内部继续校验。

            return ArchetypeId >= 0
                   && SlotIndex >= 0;
        }
    }

    public bool Equals(
        ActorId other)
    {
        // other 参数作用：
        // 要比较的另一个 ActorId。

        return ArchetypeId == other.ArchetypeId
               && SlotIndex == other.SlotIndex
               && Generation == other.Generation;
    }

    public override bool Equals(
        object? obj)
    {
        // obj 参数作用：
        // object 形式的待比较对象。
        // 该重写主要服务于非热路径调试和集合场景。

        return obj is ActorId other && Equals(other);
    }

    public override int GetHashCode()
    {
        // 逻辑说明：
        // HashCode 只用于非热路径集合场景。
        // Projection 热路径不通过 ActorId 做 Dictionary 查询。

        return HashCode.Combine(
            ArchetypeId,
            SlotIndex,
            Generation);
    }
}
```

---

## 6. ProjectedActorMeta

文件：

```text
LayerBase/ECS/Projection/ProjectedActorMeta.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal struct ProjectedActorMeta
{
    public ActorId ActorId;

    public int ActorTypeId;

    public int ActiveListIndex;

    public ProjectedActorState State;

    public ProjectedActorReleasePolicy ReleasePolicy;

    public long KeepAliveTicks;

    public static ProjectedActorMeta None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 逻辑说明：
            // None 表示当前 Entity 没有 Actor 投影绑定。
            // Chunk 初始化、Entity 删除、Actor 释放后都可以使用该默认状态。

            return new ProjectedActorMeta
            {
                ActorId = ActorId.Invalid,
                ActorTypeId = -1,
                ActiveListIndex = -1,
                State = ProjectedActorState.None,
                ReleasePolicy = ProjectedActorReleasePolicy.ReturnToPool,
                KeepAliveTicks = 0
            };
        }
    }

    public bool HasActor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // 逻辑说明：
            // 只判断 ActorId 是否能进入 ActorWorld.PostTo。
            // ActorId 的 generation 是否仍然匹配，由 ActorWorld.PostTo 内部校验。

            return ActorId.IsValid;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkProjected(
        int actorTypeId,
        long keepAliveTicks,
        ProjectedActorReleasePolicy releasePolicy)
    {
        // actorTypeId 参数作用：
        // Actor 类型编号。
        // 由源生成器生成的 GeneratedProjectedActorTypes.GetId<TActor>() 提供。

        // keepAliveTicks 参数作用：
        // Actor 未被 Touch 或 Post 命中后仍保留的 Stopwatch ticks 数量。

        // releasePolicy 参数作用：
        // Actor 超过回收时间戳后的释放策略。

        ActorTypeId = actorTypeId;
        KeepAliveTicks = keepAliveTicks < 0 ? 0 : keepAliveTicks;
        ReleasePolicy = releasePolicy;
        State = ProjectedActorState.Projectable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindActor(
        ActorId actorId)
    {
        // actorId 参数作用：
        // ActorWorld 创建或租用 Actor 后返回的 ActorId。

        ActorId = actorId;
        State = ProjectedActorState.Active;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearActor()
    {
        // 逻辑说明：
        // 清理当前绑定 Actor。
        // 保留 ActorTypeId 和 KeepAliveTicks，保证 Entity 后续仍然可以再次延迟投影 Actor。

        ActorId = ActorId.Invalid;
        State = ActorTypeId >= 0
            ? ProjectedActorState.Projectable
            : ProjectedActorState.None;
    }
}

internal enum ProjectedActorState : byte
{
    None = 0,
    Projectable = 1,
    Active = 2,
    PendingRelease = 3
}

internal enum ProjectedActorReleasePolicy : byte
{
    DestroyImmediately = 0,
    ReturnToPool = 1,
    DetachAndLetActorFinish = 2
}
```

---

## 7. ProjectedActorTime

文件：

```text
LayerBase/ECS/Projection/ProjectedActorTime.cs
```

代码：

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorTime
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SecondsToTicks(
        float seconds)
    {
        // seconds 参数作用：
        // Actor 的保活秒数。

        // 逻辑说明：
        // Stopwatch.Frequency 表示每秒包含多少 Stopwatch ticks。
        // 小于等于 0 的保活时间表示不额外保活。

        if (seconds <= 0f)
        {
            return 0;
        }

        return (long)(Stopwatch.Frequency * seconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BuildDeadline(
        long nowTicks,
        long keepAliveTicks)
    {
        // nowTicks 参数作用：
        // 本次 Projection 或 Sweep 开始时取到的 Stopwatch.GetTimestamp()。

        // keepAliveTicks 参数作用：
        // Actor 保活时长。

        // 逻辑说明：
        // RecycleDeadlineTicks 表示 Actor 最早允许被回收的时间点。

        return nowTicks + keepAliveTicks;
    }
}
```

---

## 8. ProjectedActorHandle

文件：

```text
LayerBase/ECS/Projection/ProjectedActorHandle.cs
```

代码：

```csharp
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal readonly struct ProjectedActorHandle
{
    public readonly ActorId ActorId;

    public readonly IPooledActor Actor;

    public ProjectedActorHandle(
        ActorId actorId,
        IPooledActor actor)
    {
        // actorId 参数作用：
        // 新建或租用 Actor 的物理句柄。

        // actor 参数作用：
        // 新建或租用的池化 Actor 实例。
        // Projection 会写入它的 RecycleDeadlineTicks。

        ActorId = actorId;
        Actor = actor;
    }

    public bool IsValid => ActorId.IsValid;
}
```

---

## 9. ActorWorld ProjectedActor 接口

文件：

```text
LayerBase/Actor/Storage/ActorWorld.ProjectedActor.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.ECS.Projection;

namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal ProjectedActorHandle CreateProjectedActor<TActor>()
        where TActor : class, IPooledActor, new()
    {
        // 逻辑说明：
        // ProjectedActor 必须是 IPooledActor。
        // usePool 固定为 true，确保它走对象池租用路径。
        // CreateActor<TActor>() 当前真实实现会执行 ActorInit、生命周期注册和 storage 注册。

        TActor actor =
            CreateActor<TActor>(
                usePool: true);

        IGeneratedActorMeta generated =
            ActorGeneratedAccess.RequireGenerated(actor);

        return new ProjectedActorHandle(
            generated.GetId(),
            actor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetPooledActor(
        ActorId actorId,
        out IPooledActor pooledActor)
    {
        // actorId 参数作用：
        // 要读取的 Actor 物理句柄。

        // pooledActor 参数作用：
        // 成功时返回对应 IPooledActor 实例。

        if (!TryGetActor(
                actorId,
                out IActor? actor))
        {
            pooledActor = null!;
            return false;
        }

        pooledActor = actor as IPooledActor;

        return pooledActor != null;
    }
}
```

需要补充 ActorWorld 读取 Actor 实例：

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    internal bool TryGetActor(
        ActorId actorId,
        out IActor? actor)
    {
        // actorId 参数作用：
        // 目标 Actor 的物理句柄。

        // actor 参数作用：
        // 成功时返回 Actor 实例。

        actor = null;

        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        BehaviourArchetype? archetype =
            _archetypes[actorId.ArchetypeId];

        if (archetype == null)
        {
            return false;
        }

        return archetype.TryGetActor(
            actorId,
            out actor);
    }
}
```

`TypedStorageRuntime` 增加抽象方法：

```csharp
namespace LayerBase.Actor;

internal abstract class TypedStorageRuntime
{
    internal abstract bool TryGetActor(
        ActorId actorId,
        out IActor? actor);
}
```

`TypedActorStorage<TActor>` 实现：

```csharp
namespace LayerBase.Actor;

internal sealed partial class TypedActorStorage<TActor>
    where TActor : class, IActor
{
    internal override bool TryGetActor(
        ActorId actorId,
        out IActor? actor)
    {
        // actorId 参数作用：
        // 目标 Actor 的物理句柄。

        // actor 参数作用：
        // 成功时返回 Actor 实例。

        int slotIndex =
            actorId.SlotIndex;

        if ((uint)slotIndex >= (uint)_actors.Length)
        {
            actor = null;
            return false;
        }

        if (_generations[slotIndex] != actorId.Generation)
        {
            actor = null;
            return false;
        }

        TActor? typedActor =
            _actors[slotIndex];

        if (typedActor == null)
        {
            actor = null;
            return false;
        }

        actor = typedActor;
        return true;
    }
}
```

---

## 10. ProjectedActorTypeRegistry

文件：

```text
LayerBase/ECS/Projection/ProjectedActorTypeRegistry.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal delegate ProjectedActorHandle ProjectedActorFactory(
    ActorWorld actorWorld);

internal sealed class ProjectedActorTypeRegistry
{
    private Type?[] _typesById = new Type?[64];

    private ProjectedActorFactory?[] _factoriesById = new ProjectedActorFactory?[64];

    public void RegisterGenerated(
        int actorTypeId,
        Type actorType,
        ProjectedActorFactory factory)
    {
        // actorTypeId 参数作用：
        // 源生成器分配的 Actor 类型编号。
        // 它在同一次编译产物内保持稳定。

        // actorType 参数作用：
        // 当前 ActorTypeId 对应的 Actor 类型。
        // 这里直存 Type，只用于调试、校验和错误信息，不参与热路径投递。

        // factory 参数作用：
        // 当前 ActorTypeId 对应的创建函数。
        // 它不捕获具体 ActorWorld，而是在调用时接收当前 Runtime 的 ActorWorld。

        EnsureCapacity(
            actorTypeId);

        _typesById[actorTypeId] =
            actorType;

        _factoriesById[actorTypeId] =
            factory;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ProjectedActorHandle CreateActorByTypeId(
        ActorWorld actorWorld,
        int actorTypeId)
    {
        // actorWorld 参数作用：
        // 当前 LayerRuntime.Actors。
        // 创建出的 Actor 必须进入这个 ActorWorld。

        // actorTypeId 参数作用：
        // ProjectedActorMeta 中保存的 Actor 类型编号。

        // 逻辑说明：
        // 这里是 Actor 缺失时的冷路径。
        // 只通过数组下标取 factory，不使用 Dictionary，不使用反射。

        if ((uint)actorTypeId >= (uint)_factoriesById.Length)
        {
            return default;
        }

        ProjectedActorFactory? factory =
            _factoriesById[actorTypeId];

        if (factory == null)
        {
            return default;
        }

        return factory(
            actorWorld);
    }

    public Type? GetActorType(
        int actorTypeId)
    {
        // actorTypeId 参数作用：
        // 要读取调试信息的 Actor 类型编号。

        // 逻辑说明：
        // 该方法只用于调试和错误信息，不参与 Projection 热路径。

        if ((uint)actorTypeId >= (uint)_typesById.Length)
        {
            return null;
        }

        return _typesById[actorTypeId];
    }

    private void EnsureCapacity(
        int actorTypeId)
    {
        // actorTypeId 参数作用：
        // 本次注册需要容纳的 Actor 类型编号。

        // 逻辑说明：
        // 注册发生在 Runtime 初始化阶段，不在 Query 行循环中执行。

        if ((uint)actorTypeId < (uint)_factoriesById.Length)
        {
            return;
        }

        int newLength =
            _factoriesById.Length;

        while ((uint)actorTypeId >= (uint)newLength)
        {
            newLength <<= 1;
        }

        Array.Resize(
            ref _typesById,
            newLength);

        Array.Resize(
            ref _factoriesById,
            newLength);
    }
}
```

设计说明：

```text
ProjectedActorTypeRegistry 是实例对象。
每个 LayerRuntime 拥有一份。
Registry 内部只维护数组表。
Registry 不做扫描。
Registry 不动态分配 ActorTypeId。
Registry 不使用反射创建 Actor。
```

---

## 11. 源生成器输出：GeneratedProjectedActorTypes

文件：

```text
LayerBase/ECS/Projection/Generated/GeneratedProjectedActorTypes.g.cs
```

生成代码形态：

```csharp
using System;
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Generated;

internal static partial class GeneratedProjectedActorTypes
{
    public const int BirdViewActor = 0;

    public static void RegisterTo(
        ProjectedActorTypeRegistry registry)
    {
        // registry 参数作用：
        // 当前 LayerRuntime 独立持有的 ProjectedActorTypeRegistry。
        // 生成代码会把 Type 和 factory 直接写入该 registry。

        registry.RegisterGenerated(
            actorTypeId: BirdViewActor,
            actorType: typeof(Game.Actors.BirdViewActor),
            factory: static actorWorld =>
                actorWorld.CreateProjectedActor<Game.Actors.BirdViewActor>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetId<TActor>()
        where TActor : class, IPooledActor, new()
    {
        // 逻辑说明：
        // WithProjectedActor<TActor>() 是 Entity 初始化 / 配置路径，不是 Projection 行循环热路径。
        // 这里允许 typeof(TActor) 判断。
        // 不使用 Dictionary，不使用反射创建 Actor。

        Type type =
            typeof(TActor);

        if (type == typeof(Game.Actors.BirdViewActor))
        {
            return BirdViewActor;
        }

        return -1;
    }
}
```

生成器规则：

```text
扫描实现 IPooledActor 的 Actor 类型。
为每个 ProjectedActor 分配稳定 int ID。
生成 const int。
生成 RegisterTo(ProjectedActorTypeRegistry registry)。
生成 GetId<TActor>()。
生成 factory: static actorWorld => actorWorld.CreateProjectedActor<TActor>()。
不生成全局 Registry。
不生成静态可变数组。
不使用 Activator.CreateInstance。
不使用 MethodInfo.Invoke。
```

---

## 12. LayerRuntime 接入

文件：

```text
LayerBase/Application/LayerRuntime.ECS.cs
```

代码：

```csharp
using Arch.Core;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public World EcsWorld { get; private set; } = null!;

    internal ProjectedActorTypeRegistry ProjectedActorTypes { get; private set; } = null!;

    internal void InitializeEcsWorld()
    {
        // 逻辑说明：
        // 每个 LayerRuntime 都有独立的 EcsWorld 和 ProjectedActorTypeRegistry。
        // 生成代码只把类型注册到当前 Runtime 的 registry 中。

        EcsWorld =
            World.Create();

        EcsWorld.BindRuntime(
            this);

        ProjectedActorTypes =
            new ProjectedActorTypeRegistry();

        GeneratedProjectedActorTypes.RegisterTo(
            ProjectedActorTypes);
    }
}
```

在 `LayerRuntime` 构造函数中调用：

```csharp
InitializeEcsWorld();
```

运行时关系：

```text
LayerRuntime
  -> Actors
  -> EcsWorld
  -> ProjectedActorTypes
```

---

## 13. ProjectedActorWorldExtensions

文件：

```text
LayerBase/ECS/Projection/ProjectedActorWorldExtensions.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorWorldExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WithProjectedActor<TActor>(
        this World world,
        Entity entity,
        float keepAliveSeconds = 0.2f,
        ProjectedActorReleasePolicy releasePolicy = ProjectedActorReleasePolicy.ReturnToPool)
        where TActor : class, IPooledActor, new()
    {
        // world 参数作用：
        // 当前 ECS World。
        // 它已经绑定到唯一 LayerRuntime。

        // entity 参数作用：
        // 要启用延迟投影 Actor 的 ECS Entity。

        // keepAliveSeconds 参数作用：
        // Actor 未被 Touch 或 Post 命中后仍保留的秒数。

        // releasePolicy 参数作用：
        // Actor 超时后的释放策略。

        int actorTypeId =
            GeneratedProjectedActorTypes.GetId<TActor>();

        if (actorTypeId < 0)
        {
            throw new InvalidOperationException(
                $"ProjectedActor type {typeof(TActor).Name} was not generated. Make sure it implements IPooledActor and is visible to the generator.");
        }

        ref ProjectedActorMeta meta =
            ref world.GetProjectionMeta(entity);

        meta.MarkProjected(
            actorTypeId: actorTypeId,
            keepAliveTicks: ProjectedActorTime.SecondsToTicks(keepAliveSeconds),
            releasePolicy: releasePolicy);
    }
}
```

说明：

```text
WithProjectedActor<TActor>() 不动态注册。
它只读取源生成器生成的 ActorTypeId。
ActorTypeId 写入当前 Entity 的 ProjectedActorMeta。
```

---

## 14. ProjectedActorBinding

文件：

```text
LayerBase/ECS/Projection/ProjectedActorBinding.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal static class ProjectedActorBinding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ActorId EnsureProjectedActor(
        World world,
        ActorWorld actorWorld,
        Entity entity,
        ref ProjectedActorMeta meta,
        long nowTicks)
    {
        // world 参数作用：
        // 当前 ECS World。
        // 通过它取得所属 LayerRuntime 和 runtime-local ProjectedActorTypeRegistry。

        // actorWorld 参数作用：
        // 当前 LayerRuntime.Actors。
        // 创建出的 Actor 必须进入这个 ActorWorld。

        // entity 参数作用：
        // 当前投影命中的 ECS Entity。

        // meta 参数作用：
        // 当前 Entity 的 ProjectedActorMeta 引用。

        // nowTicks 参数作用：
        // 本次 Projection 开始时取到的 Stopwatch 时间戳。

        ProjectedActorHandle handle =
            world.Runtime.ProjectedActorTypes.CreateActorByTypeId(
                actorWorld,
                meta.ActorTypeId);

        if (!handle.IsValid)
        {
            return ActorId.Invalid;
        }

        handle.Actor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                meta.KeepAliveTicks);

        meta.BindActor(
            handle.ActorId);

        world.AddActiveProjectedActor(
            entity,
            ref meta);

        return handle.ActorId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TouchProjectedActor(
        ActorWorld actorWorld,
        ref ProjectedActorMeta meta,
        long nowTicks)
    {
        // actorWorld 参数作用：
        // 当前 LayerRuntime.Actors。

        // meta 参数作用：
        // 当前 Entity 的 ProjectedActorMeta 引用。

        // nowTicks 参数作用：
        // 本次 Projection 开始时取到的 Stopwatch 时间戳。

        if (!meta.ActorId.IsValid)
        {
            return;
        }

        if (!actorWorld.TryGetPooledActor(
                meta.ActorId,
                out IPooledActor pooledActor))
        {
            meta.ClearActor();
            return;
        }

        pooledActor.RecycleDeadlineTicks =
            ProjectedActorTime.BuildDeadline(
                nowTicks,
                meta.KeepAliveTicks);
    }
}
```

---

## 15. Arch World 投影扩展

文件：

```text
Arch/Core/World.Projection.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using LayerBase;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial class World
{
    private readonly ActiveProjectedActorList _activeProjectedActors = new();

    internal LayerRuntime Runtime { get; private set; } = null!;

    internal void BindRuntime(
        LayerRuntime runtime)
    {
        // runtime 参数作用：
        // 当前 Arch World 所属的 LayerRuntime。
        // Query Flow 的 Post() 和 TouchProjectedActor() 会通过它访问 Actors 和 ProjectedActorTypes。

        Runtime = runtime;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ProjectedActorMeta GetProjectionMeta(
        Entity entity)
    {
        // entity 参数作用：
        // 要定位投影元数据的 ECS Entity。

        ref EntityData data =
            ref EntityInfo.GetEntityData(entity.Id);

        ref Chunk chunk =
            ref data.Archetype.GetChunk(data.Slot.ChunkIndex);

        return ref chunk.ProjectionAt(
            data.Slot.Index);
    }

    internal bool TryGetProjectionMeta(
        Entity entity,
        out ProjectedActorMetaRef metaRef)
    {
        // entity 参数作用：
        // 要尝试定位投影元数据的 ECS Entity。

        // metaRef 参数作用：
        // 成功时返回 ProjectedActorMeta 引用包装。

        if (!EntityInfo.Has(entity.Id))
        {
            metaRef = default;
            return false;
        }

        ref EntityData data =
            ref EntityInfo.GetEntityData(entity.Id);

        if (data.Version != entity.Version)
        {
            metaRef = default;
            return false;
        }

        ref Chunk chunk =
            ref data.Archetype.GetChunk(data.Slot.ChunkIndex);

        metaRef = new ProjectedActorMetaRef(
            ref chunk.ProjectionAt(data.Slot.Index));

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddActiveProjectedActor(
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        // entity 参数作用：
        // 当前刚刚绑定 Actor 的 ECS Entity。

        // meta 参数作用：
        // 当前 Entity 的投影元数据引用。

        _activeProjectedActors.Add(
            entity,
            ref meta);
    }

    internal void SweepProjectedActors()
    {
        // 逻辑说明：
        // Sweep 开始只取一次 Stopwatch.GetTimestamp()。
        // 后续所有 Active 判定都复用同一个 nowTicks。

        _activeProjectedActors.Sweep(
            this,
            Runtime.Actors);
    }
}
```

如果当前 `EntityInfo` 没有 `Has(int id)`，需要补充数组边界和版本校验方法。

---

## 16. Arch Chunk 投影列

文件：

```text
Arch/Core/Chunk.Projection.cs
```

代码：

```csharp
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial struct Chunk
{
    internal ProjectedActorMeta[] ProjectedActors { get; private set; }

    internal void InitializeProjectionStorage(
        int capacity)
    {
        // capacity 参数作用：
        // 当前 Chunk 的最大 Entity 容量。
        // ProjectedActors 必须和 Entities 行容量一致。

        ProjectedActors = new ProjectedActorMeta[capacity];

        for (int i = 0; i < ProjectedActors.Length; i++)
        {
            // 逻辑说明：
            // Chunk 创建是冷路径。
            // 每个 Entity 行默认没有 Actor 投影绑定。

            ProjectedActors[i] = ProjectedActorMeta.None;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ProjectedActorMeta ProjectionAt(
        int row)
    {
        // row 参数作用：
        // Entity 在当前 Chunk 内的行号。

        return ref ProjectedActors.DangerousGetReferenceAt(
            row);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ProjectedActorMeta FirstProjection()
    {
        // 逻辑说明：
        // 返回 ProjectedActors[0] 引用。
        // 行循环使用 Unsafe.Add(ref first, row) 前进。

        return ref ProjectedActors.DangerousGetReference();
    }
}
```

需要在 `Chunk` 构造函数末尾调用：

```csharp
InitializeProjectionStorage(capacity);
```

并在以下结构变更逻辑中同步移动：

```text
Chunk.Remove
Chunk.Copy
Chunk.Transfer
World.Move
```

同步规则：

```text
Entity 行移动，ProjectedActorMeta 必须跟着移动。
Entity 行删除，尾部 ProjectedActorMeta 必须清理为 None。
Archetype 迁移，ProjectedActorMeta 必须复制到目标 Chunk。
```

---

## 17. ActiveProjectedActorList

文件：

```text
LayerBase/ECS/Projection/ActiveProjectedActorList.cs
```

代码：

```csharp
using System.Diagnostics;
using Arch.Core;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection;

internal sealed class ActiveProjectedActorList
{
    private ProjectedEntityRef[] _items =
        new ProjectedEntityRef[64];

    private int _count;

    public void Add(
        Entity entity,
        ref ProjectedActorMeta meta)
    {
        // entity 参数作用：
        // 当前刚刚绑定 Actor 的 Entity。

        // meta 参数作用：
        // 当前 Entity 的投影元数据引用。
        // Add 会写入 ActiveListIndex。

        if (meta.ActiveListIndex >= 0)
        {
            return;
        }

        int index =
            _count;

        if ((uint)index >= (uint)_items.Length)
        {
            Grow();
        }

        _items[index] =
            new ProjectedEntityRef(entity);

        _count =
            index + 1;

        meta.ActiveListIndex =
            index;
    }

    private void Grow()
    {
        // 逻辑说明：
        // 扩容属于低频路径。
        // 当前阶段只保证功能正确。

        Array.Resize(
            ref _items,
            _items.Length << 1);
    }

    public void Sweep(
        World world,
        ActorWorld actorWorld)
    {
        // world 参数作用：
        // 当前 ECS World。

        // actorWorld 参数作用：
        // 当前 LayerRuntime.Actors。

        // 逻辑说明：
        // 整个 Sweep 只取一次当前时间戳。
        // 所有 ProjectedActor 的回收判定复用同一个 nowTicks。

        long nowTicks =
            Stopwatch.GetTimestamp();

        for (int i = _count - 1; i >= 0; i--)
        {
            Entity entity =
                _items[i].Entity;

            if (!world.TryGetProjectionMeta(
                    entity,
                    out ProjectedActorMetaRef metaRef))
            {
                RemoveDeadAt(
                    world,
                    i);

                continue;
            }

            ref ProjectedActorMeta meta =
                ref metaRef.Value;

            if (!meta.ActorId.IsValid)
            {
                RemoveAt(
                    world,
                    i,
                    ref meta);

                continue;
            }

            if (!actorWorld.TryGetPooledActor(
                    meta.ActorId,
                    out IPooledActor pooledActor))
            {
                meta.ClearActor();

                RemoveAt(
                    world,
                    i,
                    ref meta);

                continue;
            }

            if (nowTicks < pooledActor.RecycleDeadlineTicks)
            {
                continue;
            }

            actorWorld.ReleaseProjectedActor(
                meta.ActorId,
                meta.ReleasePolicy);

            meta.ClearActor();

            RemoveAt(
                world,
                i,
                ref meta);
        }
    }

    private void RemoveAt(
        World world,
        int index,
        ref ProjectedActorMeta meta)
    {
        // world 参数作用：
        // 当前 ECS World。

        // index 参数作用：
        // 要移除的活跃投影列表下标。

        // meta 参数作用：
        // 被移除 Entity 的投影元数据。

        int lastIndex =
            _count - 1;

        ProjectedEntityRef moved =
            _items[lastIndex];

        _items[index] =
            moved;

        _items[lastIndex] =
            default;

        _count =
            lastIndex;

        meta.ActiveListIndex =
            -1;

        if (index != lastIndex
            && world.TryGetProjectionMeta(
                moved.Entity,
                out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex =
                index;
        }
    }

    private void RemoveDeadAt(
        World world,
        int index)
    {
        // world 参数作用：
        // 当前 ECS World。

        // index 参数作用：
        // 已经无法定位 Entity 的活跃列表下标。

        int lastIndex =
            _count - 1;

        ProjectedEntityRef moved =
            _items[lastIndex];

        _items[index] =
            moved;

        _items[lastIndex] =
            default;

        _count =
            lastIndex;

        if (index != lastIndex
            && world.TryGetProjectionMeta(
                moved.Entity,
                out ProjectedActorMetaRef movedMetaRef))
        {
            movedMetaRef.Value.ActiveListIndex =
                index;
        }
    }
}

internal readonly struct ProjectedEntityRef
{
    public readonly Entity Entity;

    public ProjectedEntityRef(
        Entity entity)
    {
        // entity 参数作用：
        // 已经创建 Actor 的 ECS Entity。

        Entity = entity;
    }
}

internal readonly ref struct ProjectedActorMetaRef
{
    public readonly ref ProjectedActorMeta Value;

    public ProjectedActorMetaRef(
        ref ProjectedActorMeta value)
    {
        // value 参数作用：
        // 当前 Entity 所在 Chunk 行中的 ProjectedActorMeta 引用。

        Value = ref value;
    }
}
```

---

## 18. Projection Batch

文件：

```text
LayerBase/ECS/Projection/Flow/ProjectionBatchBuffer.cs
```

代码：

```csharp
using System.Buffers;
using System.Runtime.CompilerServices;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Flow;

internal struct ProjectionBatchBuffer<TEvent> : IDisposable
    where TEvent : struct
{
    private ActorId[] _actorIds;

    private TEvent[] _events;

    public int Count { get; private set; }

    private ProjectionBatchBuffer(
        ActorId[] actorIds,
        TEvent[] events)
    {
        // actorIds 参数作用：
        // 保存本次 Batch 中每条事件的目标 ActorId。

        // events 参数作用：
        // 保存本次 Batch 中每条事件的事件值。

        _actorIds = actorIds;
        _events = events;
        Count = 0;
    }

    public static ProjectionBatchBuffer<TEvent> Rent(
        int initialCapacity = 64)
    {
        // initialCapacity 参数作用：
        // 初始批量容量。
        // 当前基础实现默认 64，后续可以根据 Query 规模调优。

        return new ProjectionBatchBuffer<TEvent>(
            ArrayPool<ActorId>.Shared.Rent(initialCapacity),
            ArrayPool<TEvent>.Shared.Rent(initialCapacity));
    }

    public void Add(
        ActorId actorId,
        in TEvent value)
    {
        // actorId 参数作用：
        // 当前事件目标 ActorId。

        // value 参数作用：
        // 当前事件值。

        int index =
            Count;

        if ((uint)index >= (uint)_actorIds.Length)
        {
            Grow();
        }

        _actorIds[index] =
            actorId;

        _events[index] =
            value;

        Count =
            index + 1;
    }

    private void Grow()
    {
        // 逻辑说明：
        // 扩容只在本次 Batch 容量不足时发生。

        int oldLength =
            _actorIds.Length;

        int newLength =
            oldLength << 1;

        ActorId[] newActorIds =
            ArrayPool<ActorId>.Shared.Rent(newLength);

        TEvent[] newEvents =
            ArrayPool<TEvent>.Shared.Rent(newLength);

        Array.Copy(
            _actorIds,
            newActorIds,
            Count);

        Array.Copy(
            _events,
            newEvents,
            Count);

        ArrayPool<ActorId>.Shared.Return(
            _actorIds,
            clearArray: false);

        ArrayPool<TEvent>.Shared.Return(
            _events,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());

        _actorIds =
            newActorIds;

        _events =
            newEvents;
    }

    public void PostTo(
        ActorWorld actorWorld)
    {
        // actorWorld 参数作用：
        // 当前 LayerRuntime.Actors。
        // PostTo 会把 batch 中的事件写入 Actor 邮箱。

        for (int i = 0; i < Count; i++)
        {
            ActorId actorId =
                _actorIds[i];

            _ = actorWorld.PostTo(
                actorId,
                in _events[i]);
        }
    }

    public void Dispose()
    {
        // 逻辑说明：
        // 归还数组池。
        // 如果 TEvent 包含引用字段，需要清理事件数组，避免引用滞留。

        ArrayPool<ActorId>.Shared.Return(
            _actorIds,
            clearArray: false);

        ArrayPool<TEvent>.Shared.Return(
            _events,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());

        _actorIds =
            Array.Empty<ActorId>();

        _events =
            Array.Empty<TEvent>();

        Count =
            0;
    }
}
```

---

## 19. Query Flow 模板生成

Projection Query Flow 的多泛型版本使用 T4 模板生成普通 `.cs` 文件，风格对齐 Arch 模板。

模板文件：

```text
LayerBase/ECS/Projection/Templates/
  Helpers.ttinclude
  ProjectionDelegates.tt
  ProjectionQueryFlow.tt
  ProjectionExecutor.tt
  ProjectionWorldExtensions.tt
```

生成范围：

```text
Query<T0>
Query<T0,T1>
Query<T0,T1,T2>
Query<T0,T1,T2,T3>
Query<T0,T1,T2,T3,T4>
Query<T0,T1,T2,T3,T4,T5>
Query<T0,T1,T2,T3,T4,T5,T6>
Query<T0,T1,T2,T3,T4,T5,T6,T7>
```

### 19.1 ProjectionDelegates.tt

```csharp
<#@ template language="C#" #>
<#@ output extension=".cs" #>
<#@ include file="Helpers.ttinclude" #>

using Arch.Core;

namespace LayerBase.ECS.Projection.Flow;

<#
for (var index = 1; index <= Amount; index++)
{
    var generics = AppendGenerics(index);
    var predicateParams = AppendPredicateParams(index);
    var forEachParams = AppendForEachParams(index);
#>
internal delegate bool ProjectionPredicate<<#= generics #>>(
    <#= predicateParams #>);

internal delegate void ProjectionForEach<<#= generics #>, TEvent>(
    <#= forEachParams #>)
    where TEvent : struct;

<#
}
#>
```

### 19.2 ProjectionExecutor.tt 核心生成形态

```csharp
<#@ template language="C#" #>
<#@ output extension=".cs" #>
<#@ import namespace="System.Text" #>
<#@ include file="Helpers.ttinclude" #>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Flow;

<#
for (var index = 1; index <= Amount; index++)
{
    var generics = AppendGenerics(index);
    var getFirst = AppendGetFirstComponents(index);
    var getRows = AppendGetRowComponents(index);
    var predicateArgs = InsertPredicateArgs(index);
    var forEachArgs = InsertForEachArgs(index);
#>
internal static class ProjectionExecutor<#= index #><<#= generics #>>
{
    public static void Post<TEvent>(
        World world,
        Query query,
        ProjectionPredicate<<#= generics #>>? predicate,
        ProjectionForEach<<#= generics #>, TEvent> forEach)
        where TEvent : struct
    {
        ActorWorld actorWorld =
            world.Runtime.Actors;

        long nowTicks =
            Stopwatch.GetTimestamp();

        using ProjectionBatchBuffer<TEvent> batch =
            ProjectionBatchBuffer<TEvent>.Rent();

        foreach (ref Chunk chunk in query.GetChunkIterator())
        {
            CollectPostChunk(
                world,
                actorWorld,
                ref chunk,
                predicate,
                forEach,
                nowTicks,
                ref batch);
        }

        batch.PostTo(
            actorWorld);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CollectPostChunk<TEvent>(
        World world,
        ActorWorld actorWorld,
        ref Chunk chunk,
        ProjectionPredicate<<#= generics #>>? predicate,
        ProjectionForEach<<#= generics #>, TEvent> forEach,
        long nowTicks,
        ref ProjectionBatchBuffer<TEvent> batch)
        where TEvent : struct
    {
<#= getFirst #>
        ref ProjectedActorMeta firstMeta =
            ref chunk.FirstProjection();

        ref Entity firstEntity =
            ref chunk.Entities.DangerousGetReference();

        int count =
            chunk.Count;

        for (int row = 0; row < count; row++)
        {
<#= getRows #>
            Entity entity =
                Unsafe.Add(
                    ref firstEntity,
                    row);

            if (predicate != null
                && !predicate(<#= predicateArgs #>))
            {
                continue;
            }

            ref ProjectedActorMeta meta =
                ref Unsafe.Add(
                    ref firstMeta,
                    row);

            TEvent output =
                default;

            forEach(<#= forEachArgs #>);

            ActorId actorId =
                meta.ActorId;

            if (!actorId.IsValid)
            {
                actorId =
                    ProjectedActorBinding.EnsureProjectedActor(
                        world,
                        actorWorld,
                        entity,
                        ref meta,
                        nowTicks);

                if (!actorId.IsValid)
                {
                    continue;
                }
            }
            else
            {
                ProjectedActorBinding.TouchProjectedActor(
                    actorWorld,
                    ref meta,
                    nowTicks);
            }

            batch.Add(
                actorId,
                in output);
        }
    }
}

<#
}
#>
```

模板说明：

```text
T4 生成普通 .cs 文件。
不是运行时 Source Generator。
不是 ECS 内核依赖。
用于消除多泛型 Query Flow 手写重复。
```

---

## 20. Actor 类型源生成器设计

### 20.1 输入

扫描条件：

```text
类型是 class。
类型实现 IPooledActor。
类型有 new() 约束可满足。
类型不是 abstract。
类型对生成器可见。
```

### 20.2 输出

输出文件：

```text
GeneratedProjectedActorTypes.g.cs
```

输出内容：

```text
const int 类型 ID。
RegisterTo(ProjectedActorTypeRegistry registry)。
GetId<TActor>()。
```

### 20.3 禁止事项

生成器禁止：

```text
生成全局可变 Registry。
生成静态 Type -> Id Dictionary。
生成运行时反射创建。
生成 Activator.CreateInstance。
生成 MethodInfo.Invoke。
```

### 20.4 允许事项

生成器允许：

```text
typeof(TActor) 比较。
static actorWorld => actorWorld.CreateProjectedActor<TActor>()。
注册 Type 到 Registry，用于调试。
注册 Factory 到 Registry，用于创建 ProjectedActor。
```

---

## 21. Projection 执行语义

Post 语义：

```text
Query 遍历 Chunk。
Where 负责过滤。
ForEach 负责修改组件并写 output。
EnsureProjectedActor 负责缺失 Actor 创建。
TouchProjectedActor 负责刷新 RecycleDeadlineTicks。
Batch 收集 actorId[] 和 event[]。
PostTo 把 Batch 投递给 ActorWorld.PostTo。
```

TouchProjectedActor 语义：

```text
Query 遍历 Chunk。
Where 负责过滤。
缺失 Actor 则创建。
已有 Actor 则刷新 RecycleDeadlineTicks。
不生成事件。
不投递 ActorWorld.PostTo。
不参与 RuntimeFrameBudget。
```

ActorWorld.Pump 语义：

```text
消费 Actor 邮箱。
按 RuntimeFrameBudget 限制事件处理。
推进 Actor 生命周期。
```

ProjectedActor Sweep 语义：

```text
Sweep 开始取一次 Stopwatch.GetTimestamp()。
读取 ActiveProjectedActorList。
通过 ActorId 找到 IPooledActor。
比较 nowTicks 和 RecycleDeadlineTicks。
超过时间后按 ReleasePolicy 回收。
```

---

## 22. 测试要求

### 22.1 多世界测试

覆盖：

```text
Runtime A 和 Runtime B 各自持有 ProjectedActorTypeRegistry。
Runtime A 的 Entity 创建 Actor 到 Actors A。
Runtime B 的 Entity 创建 Actor 到 Actors B。
Runtime A 的 registry 修改不影响 Runtime B。
GeneratedProjectedActorTypes.RegisterTo 可以对多个 registry 分别注册。
```

### 22.2 生成器注册测试

覆盖：

```text
实现 IPooledActor 的 Actor 类型会生成 ID。
GeneratedProjectedActorTypes.GetId<TActor>() 返回正确 ID。
GeneratedProjectedActorTypes.RegisterTo(registry) 写入 Type。
GeneratedProjectedActorTypes.RegisterTo(registry) 写入 Factory。
Factory 创建 Actor 时使用传入的 ActorWorld。
```

### 22.3 Query Flow 测试

覆盖：

```text
Where 返回 false 时不执行 ForEach。
Where 返回 false 时不创建 Actor。
ForEach 无返回值。
ForEach 执行后加入 Batch。
Post 只调用 ActorWorld.PostTo。
Post 不读取 RuntimeFrameBudget。
TouchProjectedActor 不读取 RuntimeFrameBudget。
```

### 22.4 时间戳回收测试

覆盖：

```text
Post 命中后刷新 RecycleDeadlineTicks。
TouchProjectedActor 命中后刷新 RecycleDeadlineTicks。
Sweep 只取一次 Stopwatch 时间戳。
nowTicks < RecycleDeadlineTicks 时不回收。
nowTicks >= RecycleDeadlineTicks 时按 ReleasePolicy 回收。
```

### 22.5 Arch 结构变更测试

覆盖：

```text
Chunk.Remove 后 ProjectedActorMeta 跟随 Entity 行移动。
Chunk.Copy 后 ProjectedActorMeta 复制到目标 Chunk。
Chunk.Transfer 后 ProjectedActorMeta 转移到目标行。
Entity Archetype 迁移后 ActorId 不错位。
```

---

## 23. 落地顺序

第一步：

```text
IPooledActor 增加 RecycleDeadlineTicks。
ActorId 补齐 Invalid / IsValid。
```

第二步：

```text
ActorWorld 增加 CreateProjectedActor<TActor>()。
ActorWorld 增加 TryGetActor / TryGetPooledActor。
```

第三步：

```text
实现 ProjectedActorMeta。
实现 ProjectedActorTime。
实现 ProjectedActorHandle。
实现 ProjectedActorTypeRegistry。
```

第四步：

```text
实现 Actor 类型源生成器。
生成 GeneratedProjectedActorTypes.g.cs。
LayerRuntime 初始化时调用 RegisterTo。
```

第五步：

```text
Arch Chunk 增加 ProjectedActorMeta[]。
同步修改 Chunk.Remove / Chunk.Copy / Chunk.Transfer。
World 增加 BindRuntime / GetProjectionMeta / TryGetProjectionMeta。
```

第六步：

```text
实现 ProjectedActorWorldExtensions。
实现 ProjectedActorBinding。
实现 ActiveProjectedActorList。
```

第七步：

```text
实现 ProjectionBatchBuffer。
用手写 Query2 版本验证 Query Flow。
```

第八步：

```text
用 T4 模板生成 Query1 到 Query8。
删除手写重复版本。
```

第九步：

```text
调整 LayerRuntime.Pump 顺序：
LayerChain.Pump 在 Actors.Pump 前。
EcsWorld.SweepProjectedActors 在 Actors.Pump 后。
```

---

## 24. 最终结论

最终设计应保持：

```text
Registry 是 Runtime-owned 数组表。
源生成器只负责生成注册代码。
ProjectedActorBinding 是无状态工具。
Query Flow API 干净。
Post() 只投递 Batch。
RuntimeFrameBudget 只在 ActorWorld.Pump 中消费。
ProjectedActor 回收只依赖 IPooledActor.RecycleDeadlineTicks。
ProjectionQueryFlow 多泛型代码用 Arch 风格 T4 模板生成。
```

核心运行链路：

```text
WithProjectedActor<TActor>
  -> GeneratedProjectedActorTypes.GetId<TActor>()
  -> meta.ActorTypeId

Projection.Post()
  -> Query chunks
  -> Where
  -> ForEach output
  -> EnsureProjectedActor if missing
  -> refresh RecycleDeadlineTicks
  -> Batch actorId[] + event[]
  -> ActorWorld.PostTo

ActorWorld.Pump
  -> consume mailbox by RuntimeFrameBudget

EcsWorld.SweepProjectedActors
  -> one Stopwatch.GetTimestamp()
  -> compare IPooledActor.RecycleDeadlineTicks
  -> release expired actors
```
