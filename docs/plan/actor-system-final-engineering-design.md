# LayerBase Actor System Final Engineering Design

> 文件名：`actor-system-final-engineering-design.md`  
> 适用仓库：`avaw23112/LayerBase`  
> 目标：将 Actor 系统推进到可覆盖游戏工程常见业务场景的工程化版本。  
> 核心范围：泛型 Tag / Group 特性、Query v2、QueryResult 遍历、PostAll 多事件优化、可选 Actor 池化、Benchmark 与测试。

---

## 1. 总体结论

Actor 系统应定位为 LayerBase 的游戏业务对象层。

它负责：

- 创建和销毁游戏业务对象。
- 为 Actor 注入 `ActorContext`。
- 管理 Actor 生命周期。
- 管理 Actor 邮箱与事件投递。
- 按行为、Tag、Group 查询 Actor。
- 对 Query 命中的 Actor 批量 Post 或 ForEach。
- 为高频 Actor 提供可选池化路径。
- 通过 Benchmark 确认热路径分配和耗时边界。

Actor 系统不负责：

- 物理空间查询。
- AOI。
- 坐标范围搜索。
- 最近目标搜索。
- 大规模物理计算。
- 大规模 ECS 数据批处理。
- 引擎对象生命周期绑定。
- 网络同步协议内建。

Actor 系统与其他模块的关系应保持清晰：

```text
LayerBase Actor = 游戏业务对象 / 消息对象 / 生命周期对象
ECS             = 大规模连续数据计算
Layer           = 系统级模块边界
Service         = 稳定服务能力
Spatial Module  = 空间查询与范围索引
```

---

## 2. 术语解释

### 2.1 Actor

Actor 是一个具备身份、上下文、生命周期和消息接收能力的业务对象。

典型 Actor：

- 敌人。
- 技能。
- Buff。
- 任务节点。
- 交互物。
- UI 逻辑对象。
- 网络房间内的轻量逻辑对象。
- 临时触发器。
- 投射物逻辑对象。

Actor 不等于 Unity 的 `GameObject`。  
Actor 偏逻辑层，`GameObject` 偏引擎表现层。

### 2.2 ActorWorld

`ActorWorld` 是 Actor 系统所在的世界容器。

它负责：

- 创建 Actor。
- 销毁 Actor。
- 管理 Archetype。
- 管理 TypedActorStorage。
- 管理 QueryCache。
- Pump Actor 邮箱。
- Pump Actor 生命周期。
- 处理 Actor 池化归还。

### 2.3 Archetype

Archetype 是一组具有相同结构元数据的 Actor 存储集合。

本设计中，Archetype 由三部分共同决定：

```text
BehaviourSignature + ActorTagSignature + ActorGroupSignature
```

也就是说，只有行为签名、Tag 签名、Group 签名都相同，两个 Actor 类型才应进入同一个 Archetype。

### 2.4 BehaviourSignature

`BehaviourSignature` 是行为签名。

它表示某个 Actor 类型支持哪些 ActorBehaviour 事件。

例如：

```text
DamageEvent
MoveEvent
BurnEvent
```

会被映射为：

```text
[DamageEventId, MoveEventId, BurnEventId]
```

运行时 Query 不比较事件类型本身，而是比较整数 ID。

### 2.5 Tag

Tag 是 Actor 的类型级静态标签。

它用于表达 Actor “是什么”。

例如：

```text
EnemyTag
BossTag
DamageableTag
ProjectileTag
InteractableTag
DeadTag
```

Tag 不保存运行时数据。  
Tag 默认由泛型特性声明：

```csharp
[Tag<EnemyTag>]
```

### 2.6 Group

Group 是 Actor 的类型级静态业务分组。

它用于表达 Actor “属于哪个业务域”。

例如：

```text
BattleActorGroup
QuestActorGroup
UIActorGroup
NetworkActorGroup
SkillActorGroup
```

Group 默认由泛型特性声明：

```csharp
[Group<BattleActorGroup>]
```

### 2.7 Query

Query 是 Actor 查询入口。

Query 可以表达：

```text
必须拥有某些 Behaviour
不能拥有某些 Behaviour
必须拥有某些 Tag
不能拥有某些 Tag
必须属于某些 Group
不能属于某些 Group
```

Query 不表达空间条件。

### 2.8 QueryCache

QueryCache 是 Query 的缓存结果。

第一次执行某个 Query 时，ActorWorld 扫描 Archetype 并构建缓存。  
之后相同 Query 可以直接使用缓存，避免重复扫描 Archetype。

### 2.9 Dirty Slot

Dirty Slot 是有待处理消息的 Actor 槽位。

Slot 是 Storage 中的数组下标。  
Dirty 表示这个槽位有邮件需要 Pump。

Actor 邮箱使用 Dirty Slot 后，不需要每帧扫描所有 Actor。

### 2.10 Frame Budget

Frame Budget 是帧预算。

它限制一帧内最多处理多少工作，避免主线程被 Actor 消息或生命周期拖死。

在 Actor 系统中，一次 Actor 邮件处理或一次生命周期调用都可以视为一个 Work Unit。

Work Unit 是工作单元，表示可被预算统计的一次执行动作。

---

## 3. 当前能力目标

Actor 系统应覆盖以下工程能力：

| 能力 | 目标 |
|---|---|
| Runtime 接入 | ActorWorld 接入 LayerRuntime.Pump |
| 创建入口 | `CreateActor<TActor>(bool usePool = false)` |
| 上下文注入 | 源生成器注入 `ActorContext` |
| 销毁机制 | `DestroyActor` + `PendingDestroy` + `SweepPendingDestroy` |
| 生命周期 | `IStart / IUpdate / ILateUpdate / IFixedUpdate / IDestroy` |
| Enable 控制 | `SetEnable / IsEnable` |
| 邮箱系统 | EventColumn + DirtySlot |
| Query v2 | Behaviour + Tag + Group 查询 |
| ForEach | QueryResult 遍历 Actor |
| PostAll | QueryResult 批量投递事件 |
| 多事件优化 | 同一 Storage 扫描一次投递多个事件 |
| 可选池化 | 创建时由 `usePool` 决定是否走池 |
| Benchmark | Query / PostAll / Pump / Create / Destroy 压测 |

---

## 4. 非目标

本设计不实现：

- 空间索引。
- AOI。
- 半径查询。
- 最近目标查询。
- 坐标范围查询。
- 物理碰撞查询。
- 低频生命周期调度接口。
- Actor 多线程并发执行。
- Actor 跨 Runtime 自动迁移。
- 引擎对象生命周期绑定。
- 网络同步协议内建。

生命周期只保留当前常规生命周期：

```text
IStart
IUpdate
ILateUpdate
IFixedUpdate
IDestroy
```

---

## 5. 泛型 Tag / Group 特性设计

### 5.1 IActorTag

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 类型级静态标签接口。
/// </summary>
public interface IActorTag
{
    // 该接口不需要成员。
    // 它只用于限制 [Tag<TTag>] 的 TTag 类型范围。
}
```

### 5.2 IActorGroup

```csharp
namespace LayerBase.Actor;

/// <summary>
/// Actor 类型级静态分组接口。
/// </summary>
public interface IActorGroup
{
    // 该接口不需要成员。
    // 它只用于限制 [Group<TGroup>] 的 TGroup 类型范围。
}
```

### 5.3 TagAttribute<TTag>

```csharp
using System;

namespace LayerBase.Actor;

/// <summary>
/// Actor Tag 泛型特性。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = true,
    Inherited = false)]
public sealed class TagAttribute<TTag> : Attribute
    where TTag : struct, IActorTag
{
    // TTag 参数：
    // 当前 Actor 类型声明的静态 Tag 类型。
    //
    // AttributeTargets.Class：
    // 限制该特性只能标记在 class 上。
    //
    // AllowMultiple = true：
    // 允许一个 Actor 类型声明多个 Tag。
    //
    // Inherited = false：
    // 子类不会自动继承父类 Tag，避免隐式分类造成误判。
    //
    // where TTag : struct, IActorTag：
    // 要求 Tag 是值类型，并且实现 IActorTag。
    // 这样可以在编译期阻止错误类型被当作 Tag。
}
```

### 5.4 GroupAttribute<TGroup>

```csharp
using System;

namespace LayerBase.Actor;

/// <summary>
/// Actor Group 泛型特性。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = true,
    Inherited = false)]
public sealed class GroupAttribute<TGroup> : Attribute
    where TGroup : struct, IActorGroup
{
    // TGroup 参数：
    // 当前 Actor 类型声明的静态 Group 类型。
    //
    // AttributeTargets.Class：
    // 限制该特性只能标记在 class 上。
    //
    // AllowMultiple = true：
    // 允许一个 Actor 类型声明多个 Group。
    //
    // Inherited = false：
    // 子类不会自动继承父类 Group，避免隐式业务域污染。
    //
    // where TGroup : struct, IActorGroup：
    // 要求 Group 是值类型，并且实现 IActorGroup。
    // 这样可以在编译期阻止错误类型被当作 Group。
}
```

### 5.5 使用示例

```csharp
using LayerBase.Actor;

public readonly struct EnemyTag : IActorTag
{
    // EnemyTag 表示 Actor 类型属于敌人。
}

public readonly struct DamageableTag : IActorTag
{
    // DamageableTag 表示 Actor 类型可以被伤害。
}

public readonly struct BattleActorGroup : IActorGroup
{
    // BattleActorGroup 表示 Actor 类型属于战斗业务域。
}

[Tag<EnemyTag>]
[Tag<DamageableTag>]
[Group<BattleActorGroup>]
public sealed partial class EnemyActor : IActor
{
    // EnemyActor 同时拥有 EnemyTag 和 DamageableTag。
    // EnemyActor 属于 BattleActorGroup。
    // partial 允许源生成器为该类型生成 ActorContext 注入和元数据代码。
}
```

---

## 6. TagId / GroupId 设计

运行时 Query 不比较字符串，不比较 `Type`，只比较整数 ID。

### 6.1 ActorTagId<TTag>

```csharp
namespace LayerBase.Actor;

internal static class ActorTagId<TTag>
    where TTag : struct, IActorTag
{
    // Id：
    // 当前 Tag 类型对应的运行时整数 ID。
    //
    // 必要逻辑：
    // 泛型静态字段会针对每个 TTag 单独初始化一次。
    // Query 热路径只读取 int，不做字符串比较，也不做 Type 比较。
    public static readonly int Id = ActorTagIdAllocator.GetOrCreate(typeof(TTag));
}
```

### 6.2 ActorGroupId<TGroup>

```csharp
namespace LayerBase.Actor;

internal static class ActorGroupId<TGroup>
    where TGroup : struct, IActorGroup
{
    // Id：
    // 当前 Group 类型对应的运行时整数 ID。
    //
    // 必要逻辑：
    // 泛型静态字段会针对每个 TGroup 单独初始化一次。
    // Query 热路径只读取 int，不做字符串比较，也不做 Type 比较。
    public static readonly int Id = ActorGroupIdAllocator.GetOrCreate(typeof(TGroup));
}
```

### 6.3 ActorTagIdAllocator

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace LayerBase.Actor;

internal static class ActorTagIdAllocator
{
    private static int s_nextId;

    private static readonly Dictionary<Type, int> s_typeToId = new();

    private static readonly object s_lock = new();

    public static int GetOrCreate(Type type)
    {
        // type 参数：
        // 需要获取整数 ID 的 Tag 类型。
        //
        // 必要逻辑：
        // 该方法只在泛型静态字段首次初始化时调用，不是 Query 热路径。
        // 使用 lock 是为了保证多线程首次初始化时不会重复分配 ID。
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        lock (s_lock)
        {
            if (s_typeToId.TryGetValue(type, out int existing))
            {
                return existing;
            }

            int id = Interlocked.Increment(ref s_nextId);
            s_typeToId.Add(type, id);
            return id;
        }
    }
}
```

### 6.4 ActorGroupIdAllocator

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace LayerBase.Actor;

internal static class ActorGroupIdAllocator
{
    private static int s_nextId;

    private static readonly Dictionary<Type, int> s_typeToId = new();

    private static readonly object s_lock = new();

    public static int GetOrCreate(Type type)
    {
        // type 参数：
        // 需要获取整数 ID 的 Group 类型。
        //
        // 必要逻辑：
        // Group 和 Tag 使用不同 ID 空间。
        // 这样调试时可以清晰区分 TagId 与 GroupId。
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        lock (s_lock)
        {
            if (s_typeToId.TryGetValue(type, out int existing))
            {
                return existing;
            }

            int id = Interlocked.Increment(ref s_nextId);
            s_typeToId.Add(type, id);
            return id;
        }
    }
}
```

---

## 7. Signature 设计

### 7.1 ActorSignatureUtility

```csharp
namespace LayerBase.Actor;

internal static class ActorSignatureUtility
{
    public static int[] Normalize(int[] ids)
    {
        // ids 参数：
        // 待标准化的 ID 集合。
        //
        // 必要逻辑：
        // Query 匹配依赖稳定顺序。
        // 因此需要复制、排序、去重。
        // 复制是为了避免修改外部传入数组。
        if (ids == null || ids.Length == 0)
        {
            return Array.Empty<int>();
        }

        int[] copy = new int[ids.Length];
        Array.Copy(ids, copy, ids.Length);
        Array.Sort(copy);

        int uniqueCount = 0;

        for (int i = 0; i < copy.Length; i++)
        {
            if (i == 0 || copy[i] != copy[i - 1])
            {
                copy[uniqueCount] = copy[i];
                uniqueCount++;
            }
        }

        if (uniqueCount == copy.Length)
        {
            return copy;
        }

        Array.Resize(ref copy, uniqueCount);
        return copy;
    }

    public static bool ContainsAll(int[] source, int[] query)
    {
        // source 参数：
        // 当前 Actor 或 Archetype 拥有的有序 ID 集合。
        //
        // query 参数：
        // Query 要求必须全部拥有的有序 ID 集合。
        //
        // 必要逻辑：
        // 使用双指针扫描，避免构造 HashSet。
        if (query.Length == 0)
        {
            return true;
        }

        int sourceIndex = 0;
        int queryIndex = 0;

        while (sourceIndex < source.Length && queryIndex < query.Length)
        {
            int sourceValue = source[sourceIndex];
            int queryValue = query[queryIndex];

            if (sourceValue == queryValue)
            {
                sourceIndex++;
                queryIndex++;
                continue;
            }

            if (sourceValue < queryValue)
            {
                sourceIndex++;
                continue;
            }

            return false;
        }

        return queryIndex == query.Length;
    }

    public static bool ContainsAny(int[] source, int[] query)
    {
        // source 参数：
        // 当前 Actor 或 Archetype 拥有的有序 ID 集合。
        //
        // query 参数：
        // Query 要检查是否命中的有序 ID 集合。
        //
        // 必要逻辑：
        // 任意一个 ID 相同就返回 true。
        if (source.Length == 0 || query.Length == 0)
        {
            return false;
        }

        int sourceIndex = 0;
        int queryIndex = 0;

        while (sourceIndex < source.Length && queryIndex < query.Length)
        {
            int sourceValue = source[sourceIndex];
            int queryValue = query[queryIndex];

            if (sourceValue == queryValue)
            {
                return true;
            }

            if (sourceValue < queryValue)
            {
                sourceIndex++;
            }
            else
            {
                queryIndex++;
            }
        }

        return false;
    }
}
```

### 7.2 ActorTagSignature

```csharp
namespace LayerBase.Actor;

public readonly struct ActorTagSignature : IEquatable<ActorTagSignature>
{
    private readonly int[] _ids;

    public ActorTagSignature(int[] ids)
    {
        // ids 参数：
        // Tag ID 集合。
        //
        // 必要逻辑：
        // 构造时统一排序和去重。
        _ids = ActorSignatureUtility.Normalize(ids);
    }

    public bool ContainsAll(ActorTagSignature query)
    {
        // query 参数：
        // 查询要求必须拥有的 Tag 签名。
        return ActorSignatureUtility.ContainsAll(_ids, query._ids);
    }

    public bool ContainsAny(ActorTagSignature query)
    {
        // query 参数：
        // 查询要求检查是否存在交集的 Tag 签名。
        return ActorSignatureUtility.ContainsAny(_ids, query._ids);
    }

    public bool Equals(ActorTagSignature other)
    {
        // other 参数：
        // 另一个 Tag 签名。
        return _ids.AsSpan().SequenceEqual(other._ids);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorTagSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        // 必要逻辑：
        // HashCode 需要基于所有 ID 计算。
        // 该方法主要用于字典 key，不应在每帧热循环中频繁调用。
        var hash = new HashCode();

        foreach (int id in _ids)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
```

### 7.3 ActorGroupSignature

```csharp
namespace LayerBase.Actor;

public readonly struct ActorGroupSignature : IEquatable<ActorGroupSignature>
{
    private readonly int[] _ids;

    public ActorGroupSignature(int[] ids)
    {
        // ids 参数：
        // Group ID 集合。
        //
        // 必要逻辑：
        // 构造时统一排序和去重。
        _ids = ActorSignatureUtility.Normalize(ids);
    }

    public bool ContainsAll(ActorGroupSignature query)
    {
        // query 参数：
        // 查询要求必须拥有的 Group 签名。
        return ActorSignatureUtility.ContainsAll(_ids, query._ids);
    }

    public bool ContainsAny(ActorGroupSignature query)
    {
        // query 参数：
        // 查询要求检查是否存在交集的 Group 签名。
        return ActorSignatureUtility.ContainsAny(_ids, query._ids);
    }

    public bool Equals(ActorGroupSignature other)
    {
        // other 参数：
        // 另一个 Group 签名。
        return _ids.AsSpan().SequenceEqual(other._ids);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorGroupSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        // 必要逻辑：
        // HashCode 需要基于所有 ID 计算。
        var hash = new HashCode();

        foreach (int id in _ids)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
```

---

## 8. ActorTypeMeta 设计

`ActorTypeMeta<TActor>` 保存 Actor 类型完整元数据。

```csharp
namespace LayerBase.Actor;

public sealed class ActorTypeMeta<TActor>
    where TActor : class, IActor
{
    public BehaviourSignature Signature { get; }

    public ActorBehaviourEntry[] Behaviours { get; }

    public int[] TagIds { get; }

    public int[] GroupIds { get; }

    public ActorTypeMeta(
        BehaviourSignature signature,
        ActorBehaviourEntry[] behaviours,
        int[] tagIds,
        int[] groupIds)
    {
        // signature 参数：
        // 当前 Actor 类型支持的行为事件签名。
        //
        // behaviours 参数：
        // 当前 Actor 类型的行为入口集合。
        //
        // tagIds 参数：
        // 源生成器根据 [Tag<TTag>] 生成的 Tag ID 集合。
        //
        // groupIds 参数：
        // 源生成器根据 [Group<TGroup>] 生成的 Group ID 集合。
        //
        // 必要逻辑：
        // TagIds 和 GroupIds 应在生成阶段完成排序和去重。
        Signature = signature;
        Behaviours = behaviours;
        TagIds = tagIds;
        GroupIds = groupIds;
    }
}
```

---

## 9. Archetype Key 设计

### 9.1 ActorArchetypeKey

```csharp
namespace LayerBase.Actor;

public readonly struct ActorArchetypeKey : IEquatable<ActorArchetypeKey>
{
    public readonly BehaviourSignature Behaviour;
    public readonly ActorTagSignature Tags;
    public readonly ActorGroupSignature Groups;

    public ActorArchetypeKey(
        BehaviourSignature behaviour,
        ActorTagSignature tags,
        ActorGroupSignature groups)
    {
        // behaviour 参数：
        // Actor 类型支持的行为事件签名。
        //
        // tags 参数：
        // Actor 类型声明的 Tag 签名。
        //
        // groups 参数：
        // Actor 类型声明的 Group 签名。
        //
        // 必要逻辑：
        // 三者共同决定 Actor 类型应该进入哪个 Archetype。
        Behaviour = behaviour;
        Tags = tags;
        Groups = groups;
    }

    public bool Equals(ActorArchetypeKey other)
    {
        // other 参数：
        // 另一个 Archetype Key。
        //
        // 必要逻辑：
        // 行为、Tag、Group 都相等时，才属于同一个 Archetype。
        return Behaviour.Equals(other.Behaviour)
               && Tags.Equals(other.Tags)
               && Groups.Equals(other.Groups);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorArchetypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Behaviour, Tags, Groups);
    }
}
```

### 9.2 BehaviourArchetype

```csharp
namespace LayerBase.Actor;

internal sealed class BehaviourArchetype
{
    public int ArchetypeId { get; }

    public BehaviourSignature Signature { get; }

    public ActorTagSignature Tags { get; }

    public ActorGroupSignature Groups { get; }

    public BehaviourArchetype(
        int archetypeId,
        BehaviourSignature signature,
        ActorTagSignature tags,
        ActorGroupSignature groups)
    {
        // archetypeId 参数：
        // 当前 Archetype 在 ActorWorld 中的编号。
        //
        // signature 参数：
        // 当前 Archetype 的行为事件签名。
        //
        // tags 参数：
        // 当前 Archetype 的 Tag 签名。
        //
        // groups 参数：
        // 当前 Archetype 的 Group 签名。
        ArchetypeId = archetypeId;
        Signature = signature;
        Tags = tags;
        Groups = groups;
    }
}
```

---

## 10. CreateActor 可选池化设计

### 10.1 目标

Actor 创建入口保持简单：

```csharp
CreateActor<TActor>(bool usePool = false)
```

默认不池化。

这意味着：

```csharp
runtime.Actors.CreateActor<EnemyActor>();
```

等价于：

```csharp
runtime.Actors.CreateActor<EnemyActor>(usePool: false);
```

只有调用方明确传入 `usePool: true` 时，才启用 Actor 池。

### 10.2 IPooledActor

```csharp
namespace LayerBase.Actor;

/// <summary>
/// 可池化 Actor 接口。
/// </summary>
public interface IPooledActor : IActor
{
    void OnRent();

    void OnReturn();
}
```

说明：

- `OnRent`：Actor 从池中取出后调用，用于重置运行状态。
- `OnReturn`：Actor 归还池前调用，用于清理引用和临时状态。

### 10.3 ActorPool

```csharp
namespace LayerBase.Actor;

internal sealed class ActorPool<TActor>
    where TActor : class, IActor, new()
{
    private readonly Stack<TActor> _items = new();

    public TActor Rent()
    {
        // Rent 表示从池中取出 Actor。
        // 如果池为空，则创建新实例。
        TActor actor = _items.Count > 0
            ? _items.Pop()
            : new TActor();

        if (actor is IPooledActor pooled)
        {
            // pooled 参数：
            // 当前 Actor 支持池化生命周期回调。
            //
            // 必要逻辑：
            // Actor 每次从池中取出时都必须重置状态，
            // 否则上一次使用留下的数据可能污染本次逻辑。
            pooled.OnRent();
        }

        return actor;
    }

    public void Return(TActor actor)
    {
        // actor 参数：
        // 即将归还对象池的 Actor。
        //
        // 必要逻辑：
        // 归还前先执行 OnReturn 清理引用，
        // 再把 Actor 压回池中等待下次复用。
        if (actor == null)
        {
            return;
        }

        if (actor is IPooledActor pooled)
        {
            pooled.OnReturn();
        }

        _items.Push(actor);
    }
}
```

### 10.4 CreateActor 主入口

```csharp
public TActor CreateActor<TActor>(bool usePool = false)
    where TActor : class, IActor, new()
{
    // usePool 参数：
    // false 表示使用 new TActor() 创建 Actor。
    // true 表示尝试从 ActorPool<TActor> 中租用 Actor。
    //
    // 默认值：
    // usePool 默认为 false，保证普通 Actor 创建路径不改变。
    //
    // 必要逻辑：
    // 池化是显式选择，不应该默认启用。
    // 默认不池化可以避免 Actor 状态清理不完整造成脏数据复用。

    TActor actor = usePool
        ? RentActorFromPool<TActor>()
        : new TActor();

    return RegisterActor(
        actor: actor,
        createdFromPool: usePool);
}
```

### 10.5 RentActorFromPool

```csharp
private TActor RentActorFromPool<TActor>()
    where TActor : class, IActor, new()
{
    // TActor 参数：
    // 要从池中租用的 Actor 类型。
    //
    // 必要逻辑：
    // usePool: true 时，建议要求 TActor 实现 IPooledActor。
    // 如果不做这个限制，Actor 可能没有清理逻辑，复用时容易残留脏状态。
    if (!typeof(IPooledActor).IsAssignableFrom(typeof(TActor)))
    {
        throw new InvalidOperationException(
            $"Actor type {typeof(TActor).Name} must implement IPooledActor when usePool is true.");
    }

    return ActorPoolCache<TActor>.Pool.Rent();
}
```

### 10.6 RegisterActor

```csharp
private TActor RegisterActor<TActor>(
    TActor actor,
    bool createdFromPool)
    where TActor : class, IActor, new()
{
    // actor 参数：
    // 已创建或已从池中租用的 Actor 实例。
    //
    // createdFromPool 参数：
    // true 表示该 Actor 来自 ActorPool<TActor>。
    // false 表示该 Actor 来自 new TActor()。
    //
    // 必要逻辑：
    // DestroyNow 时需要知道 Actor 是否来自池。
    // 来自池的 Actor 应归还池。
    // 非池化 Actor 应正常释放引用并等待 GC。

    IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);

    ActorTypeMeta<TActor> meta = ActorTypeMetaCache.GetOrBuild<TActor>(generated);

    var key = new ActorArchetypeKey(
        behaviour: meta.Signature,
        tags: new ActorTagSignature(meta.TagIds),
        groups: new ActorGroupSignature(meta.GroupIds));

    BehaviourArchetype archetype = GetOrCreateArchetype(key);

    TypedActorStorage<TActor> storage = archetype.GetOrCreateStorage(meta, this);

    int slotIndex = storage.AllocateSlot(
        actor: actor,
        createdFromPool: createdFromPool);

    ActorId actorId = new(
        archetypeId: archetype.ArchetypeId,
        typeStorageIndex: storage.TypeStorageIndex,
        slotIndex: slotIndex,
        generation: storage.GetGeneration(slotIndex));

    generated.ActorInit(new ActorContext(this, actorId));

    storage.RegisterLifecycleInterfaces(actor, actorId, slotIndex, this);

    return actor;
}
```

### 10.7 Storage 记录池化来源

`TypedActorStorage<TActor>` 需要新增一个数组保存每个 Slot 是否来自池。

```csharp
private bool[] _createdFromPool;
```

分配 Slot 时写入：

```csharp
public int AllocateSlot(
    TActor actor,
    bool createdFromPool)
{
    // actor 参数：
    // 要写入 Storage 的 Actor 实例。
    //
    // createdFromPool 参数：
    // true 表示该实例来自 ActorPool<TActor>。
    // false 表示该实例来自 new TActor()。
    //
    // 必要逻辑：
    // DestroyNow 时需要根据该标记决定是否归还池。

    int slotIndex = _freeList.TryPop(out int freeSlot)
        ? freeSlot
        : AllocateNewSlot();

    _actors[slotIndex] = actor;
    _states[slotIndex] = ActorSlotState.Alive;
    _enabled[slotIndex] = true;
    _createdFromPool[slotIndex] = createdFromPool;
    _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

    EnsureColumnCapacity(slotIndex);

    return slotIndex;
}
```

销毁时归还：

```csharp
private bool DestroyNow(
    int slotIndex,
    int generation,
    ActorWorld world)
{
    // slotIndex 参数：
    // 要销毁的 Actor 槽位。
    //
    // generation 参数：
    // ActorId 中保存的代数，用于避免旧 ActorId 命中新 Actor。
    //
    // world 参数：
    // 当前 Actor 所属 ActorWorld。
    //
    // 必要逻辑：
    // 先执行 IDestroy，再注销生命周期，再清理邮件。
    // 如果 Actor 来自池，则归还池。
    // 最后释放 Slot 并递增 generation。

    if ((uint)slotIndex >= (uint)_actors.Length)
    {
        return false;
    }

    if (_generations[slotIndex] != generation)
    {
        return false;
    }

    TActor? actor = _actors[slotIndex];

    if (actor == null)
    {
        return false;
    }

    if (actor is IDestroy destroy)
    {
        destroy.Destroy();
    }

    UnregisterLifecycleInterfaces(slotIndex, world);
    ClearAllMails(slotIndex);

    bool returnToPool = _createdFromPool[slotIndex];

    _actors[slotIndex] = null;
    _enabled[slotIndex] = false;
    _states[slotIndex] = ActorSlotState.Empty;
    _createdFromPool[slotIndex] = false;
    _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

    unchecked
    {
        _generations[slotIndex]++;
    }

    _freeList.Push(slotIndex);

    if (returnToPool)
    {
        ActorPoolCache<TActor>.Pool.Return(actor);
    }

    return true;
}
```

### 10.8 ActorPoolCache

```csharp
namespace LayerBase.Actor;

internal static class ActorPoolCache<TActor>
    where TActor : class, IActor, new()
{
    // Pool：
    // 每个 Actor 类型一个静态池。
    //
    // 必要逻辑：
    // 泛型静态字段能让不同 Actor 类型自然隔离。
    // EnemyActor 和 BulletActor 不会进入同一个池。
    public static readonly ActorPool<TActor> Pool = new();
}
```

---

## 11. Query v2 设计

### 11.1 ActorQueryDescriptor

```csharp
namespace LayerBase.Actor;

public readonly struct ActorQueryDescriptor : IEquatable<ActorQueryDescriptor>
{
    public readonly BehaviourSignature AllBehaviours;
    public readonly BehaviourSignature NoneBehaviours;

    public readonly ActorTagSignature AllTags;
    public readonly ActorTagSignature NoneTags;

    public readonly ActorGroupSignature AllGroups;
    public readonly ActorGroupSignature NoneGroups;

    public ActorQueryDescriptor(
        BehaviourSignature allBehaviours,
        BehaviourSignature noneBehaviours,
        ActorTagSignature allTags,
        ActorTagSignature noneTags,
        ActorGroupSignature allGroups,
        ActorGroupSignature noneGroups)
    {
        // allBehaviours 参数：
        // Actor 必须全部拥有的行为事件集合。
        //
        // noneBehaviours 参数：
        // Actor 必须全部不拥有的行为事件集合。
        //
        // allTags 参数：
        // Actor 必须全部拥有的 Tag 集合。
        //
        // noneTags 参数：
        // Actor 必须全部不拥有的 Tag 集合。
        //
        // allGroups 参数：
        // Actor 必须全部拥有的 Group 集合。
        //
        // noneGroups 参数：
        // Actor 必须全部不拥有的 Group 集合。
        AllBehaviours = allBehaviours;
        NoneBehaviours = noneBehaviours;
        AllTags = allTags;
        NoneTags = noneTags;
        AllGroups = allGroups;
        NoneGroups = noneGroups;
    }

    public bool Equals(ActorQueryDescriptor other)
    {
        // other 参数：
        // 另一个 QueryDescriptor。
        //
        // 必要逻辑：
        // QueryCache 会以 Descriptor 作为 key。
        // 因此所有查询条件都必须参与相等判断。
        return AllBehaviours.Equals(other.AllBehaviours)
               && NoneBehaviours.Equals(other.NoneBehaviours)
               && AllTags.Equals(other.AllTags)
               && NoneTags.Equals(other.NoneTags)
               && AllGroups.Equals(other.AllGroups)
               && NoneGroups.Equals(other.NoneGroups);
    }

    public override bool Equals(object? obj)
    {
        return obj is ActorQueryDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            AllBehaviours,
            NoneBehaviours,
            AllTags,
            NoneTags,
            AllGroups,
            NoneGroups);
    }
}
```

### 11.2 ActorQueryBuilder

```csharp
namespace LayerBase.Actor;

public sealed class ActorQueryBuilder
{
    private readonly ActorWorld _world;

    private BehaviourSignature _allBehaviours;
    private BehaviourSignature _noneBehaviours;

    private ActorTagSignature _allTags;
    private ActorTagSignature _noneTags;

    private ActorGroupSignature _allGroups;
    private ActorGroupSignature _noneGroups;

    public ActorQueryBuilder(ActorWorld world)
    {
        // world 参数：
        // 当前 QueryBuilder 所属的 ActorWorld。
        //
        // 必要逻辑：
        // QueryBuilder 只收集查询条件。
        // 它不直接扫描 Actor，也不直接扫描 Archetype。
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public ActorQueryBuilder AllTags<TTag>()
        where TTag : struct, IActorTag
    {
        // TTag 参数：
        // Actor 必须拥有的 Tag。
        //
        // 必要逻辑：
        // 使用泛型静态缓存可以避免每次 Query 都创建数组。
        _allTags = ActorTagQuerySignature<TTag>.Value;
        return this;
    }

    public ActorQueryBuilder AllTags<TTag1, TTag2>()
        where TTag1 : struct, IActorTag
        where TTag2 : struct, IActorTag
    {
        // TTag1 / TTag2 参数：
        // Actor 必须同时拥有的两个 Tag。
        //
        // 必要逻辑：
        // ActorTagQuerySignature 会提供已标准化的 Tag 签名。
        _allTags = ActorTagQuerySignature<TTag1, TTag2>.Value;
        return this;
    }

    public ActorQueryBuilder NoneTags<TTag>()
        where TTag : struct, IActorTag
    {
        // TTag 参数：
        // Actor 不能拥有的 Tag。
        //
        // 示例：
        // NoneTags<DeadTag>() 表示排除 DeadTag。
        _noneTags = ActorTagQuerySignature<TTag>.Value;
        return this;
    }

    public ActorQueryBuilder AllGroups<TGroup>()
        where TGroup : struct, IActorGroup
    {
        // TGroup 参数：
        // Actor 必须属于的 Group。
        //
        // 示例：
        // AllGroups<BattleActorGroup>() 表示只查询战斗业务域 Actor。
        _allGroups = ActorGroupQuerySignature<TGroup>.Value;
        return this;
    }

    public ActorQueryBuilder NoneGroups<TGroup>()
        where TGroup : struct, IActorGroup
    {
        // TGroup 参数：
        // Actor 不能属于的 Group。
        //
        // 示例：
        // NoneGroups<UIActorGroup>() 表示排除 UI 业务域 Actor。
        _noneGroups = ActorGroupQuerySignature<TGroup>.Value;
        return this;
    }

    public ActorQueryResult Build()
    {
        // Build 的作用：
        // 将当前查询条件固化为 ActorQueryDescriptor。
        // ActorWorld 根据 Descriptor 获取或构建 QueryCache。
        var descriptor = new ActorQueryDescriptor(
            allBehaviours: _allBehaviours,
            noneBehaviours: _noneBehaviours,
            allTags: _allTags,
            noneTags: _noneTags,
            allGroups: _allGroups,
            noneGroups: _noneGroups);

        return _world.GetOrBuildQuery(descriptor);
    }
}
```

### 11.3 QueryResult 版本校验

```csharp
public readonly struct ActorQueryResult
{
    private readonly ActorWorld _world;
    private readonly ActorQueryCache _cache;
    private readonly int _version;

    public ActorQueryResult(
        ActorWorld world,
        ActorQueryCache cache,
        int version)
    {
        // world 参数：
        // 查询结果所属 ActorWorld。
        //
        // cache 参数：
        // 本次查询命中的 Archetype 缓存。
        //
        // version 参数：
        // 构建 QueryResult 时的 ActorWorld 查询版本。
        _world = world;
        _cache = cache;
        _version = version;
    }

    public bool IsValid
    {
        get
        {
            // IsValid 用于判断 QueryResult 是否仍然匹配当前 ActorWorld 结构。
            // 如果 ActorWorld 新增了 Archetype，旧 QueryResult 可能漏掉新对象。
            return _world.QueryVersion == _version;
        }
    }

    public ActorQueryResult RefreshIfNeeded()
    {
        // 必要逻辑：
        // 如果版本没变化，直接复用当前 QueryResult。
        // 如果版本变化，则让 ActorWorld 根据 Descriptor 重新构建 Query。
        if (IsValid)
        {
            return this;
        }

        return _world.RebuildQuery(_cache.Descriptor);
    }
}
```

### 11.4 Query 匹配逻辑

```csharp
private static bool IsMatch(
    BehaviourArchetype archetype,
    ActorQueryDescriptor descriptor)
{
    // archetype 参数：
    // 当前正在检查的 Archetype。
    //
    // descriptor 参数：
    // QueryBuilder.Build() 生成的查询条件。
    //
    // 返回值：
    // true 表示该 Archetype 满足全部查询条件。

    if (!archetype.Signature.ContainsAll(descriptor.AllBehaviours))
    {
        return false;
    }

    if (archetype.Signature.ContainsAny(descriptor.NoneBehaviours))
    {
        return false;
    }

    if (!archetype.Tags.ContainsAll(descriptor.AllTags))
    {
        return false;
    }

    if (archetype.Tags.ContainsAny(descriptor.NoneTags))
    {
        return false;
    }

    if (!archetype.Groups.ContainsAll(descriptor.AllGroups))
    {
        return false;
    }

    if (archetype.Groups.ContainsAny(descriptor.NoneGroups))
    {
        return false;
    }

    return true;
}
```

---

## 12. Query Signature 缓存

### 12.1 ActorTagQuerySignature

```csharp
namespace LayerBase.Actor;

internal static class ActorTagQuerySignature<TTag>
    where TTag : struct, IActorTag
{
    // Value：
    // 只包含一个 Tag 的查询签名。
    //
    // 必要逻辑：
    // 泛型静态字段每个 TTag 只初始化一次。
    // QueryBuilder 直接复用该值，避免重复构造数组。
    public static readonly ActorTagSignature Value = new(new[]
    {
        ActorTagId<TTag>.Id
    });
}

internal static class ActorTagQuerySignature<TTag1, TTag2>
    where TTag1 : struct, IActorTag
    where TTag2 : struct, IActorTag
{
    // Value：
    // 包含两个 Tag 的查询签名。
    //
    // 必要逻辑：
    // ActorTagSignature 内部会排序和去重。
    public static readonly ActorTagSignature Value = new(new[]
    {
        ActorTagId<TTag1>.Id,
        ActorTagId<TTag2>.Id
    });
}
```

### 12.2 ActorGroupQuerySignature

```csharp
namespace LayerBase.Actor;

internal static class ActorGroupQuerySignature<TGroup>
    where TGroup : struct, IActorGroup
{
    // Value：
    // 只包含一个 Group 的查询签名。
    //
    // 必要逻辑：
    // 泛型静态字段每个 TGroup 只初始化一次。
    public static readonly ActorGroupSignature Value = new(new[]
    {
        ActorGroupId<TGroup>.Id
    });
}

internal static class ActorGroupQuerySignature<TGroup1, TGroup2>
    where TGroup1 : struct, IActorGroup
    where TGroup2 : struct, IActorGroup
{
    // Value：
    // 包含两个 Group 的查询签名。
    //
    // 必要逻辑：
    // ActorGroupSignature 内部会排序和去重。
    public static readonly ActorGroupSignature Value = new(new[]
    {
        ActorGroupId<TGroup1>.Id,
        ActorGroupId<TGroup2>.Id
    });
}
```

---

## 13. Query ForEach 设计

### 13.1 业务友好 ForEachActor

```csharp
public static class ActorQueryForEachExtensions
{
    public static void ForEachActor<TActor>(
        this ActorQueryResult query,
        Action<TActor> action)
        where TActor : class, IActor
    {
        // query 参数：
        // QueryBuilder.Build() 得到的查询结果。
        //
        // action 参数：
        // 对每个命中的 TActor 执行的业务逻辑。
        //
        // TActor 参数：
        // 目标 Actor 类型。
        //
        // 必要逻辑：
        // 如果 QueryResult 过期，先刷新，避免漏掉新 Archetype。
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        query = query.RefreshIfNeeded();

        foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
        {
            archetype.ForEachActor(action);
        }
    }
}
```

### 13.2 性能友好 ForEachActor with state

```csharp
public delegate void ActorForEachAction<TActor, TState>(
    TActor actor,
    ref TState state)
    where TActor : class, IActor;

public static void ForEachActor<TActor, TState>(
    this ActorQueryResult query,
    ref TState state,
    ActorForEachAction<TActor, TState> action)
    where TActor : class, IActor
{
    // query 参数：
    // QueryBuilder.Build() 得到的查询结果。
    //
    // state 参数：
    // 外部传入的可变状态，用 ref 传递避免闭包分配。
    //
    // action 参数：
    // 处理每个 Actor 的静态委托。
    //
    // TActor 参数：
    // 目标 Actor 类型。
    //
    // TState 参数：
    // 遍历过程中需要共享的状态类型。
    //
    // 必要逻辑：
    // 使用 static lambda + ref state 可以减少 GC 分配。
    if (action == null)
    {
        throw new ArgumentNullException(nameof(action));
    }

    query = query.RefreshIfNeeded();

    foreach (BehaviourArchetype archetype in query.Cache.Archetypes)
    {
        archetype.ForEachActor(ref state, action);
    }
}
```

### 13.3 Storage 级 ForEach

```csharp
public delegate void ActorStorageForEachAction<TActor, TState>(
    TActor[] actors,
    ActorSlotState[] states,
    bool[] enabled,
    int maxSlot,
    ref TState state)
    where TActor : class, IActor;

public void ForEachStorage<TActor, TState>(
    ref TState state,
    ActorStorageForEachAction<TActor, TState> action)
    where TActor : class, IActor
{
    // actors 参数：
    // Actor 连续数组。
    //
    // states 参数：
    // Actor 槽位状态数组。
    //
    // enabled 参数：
    // Actor 是否启用的数组。
    //
    // maxSlot 参数：
    // 当前有效扫描上界。
    //
    // state 参数：
    // 外部共享状态。
    //
    // action 参数：
    // 调用方提供的批处理逻辑。
    //
    // 必要逻辑：
    // Storage 级 ForEach 暴露更底层的数据结构，
    // 主要用于性能敏感系统，不建议普通业务层滥用。
    foreach (var storage in FindStorages<TActor>())
    {
        action(
            storage.Actors,
            storage.States,
            storage.Enabled,
            storage.MaxSlot,
            ref state);
    }
}
```

---

## 14. PostAll 多事件优化

### 14.1 目标

多事件 `PostAll<T1...T12>()` 应避免重复扫描同一个 Storage。

目标路径：

```text
扫描一次 alive actor
同时投递多个事件
```

避免路径：

```text
扫描一次投递 Event1
再扫描一次投递 Event2
再扫描一次投递 Event3
```

### 14.2 示例设计

```csharp
public override void PostManyToAliveActors<TEvent1, TEvent2>(
    in TEvent1 value1,
    in TEvent2 value2,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // value1 参数：
    // 第一个要投递的事件值。
    //
    // value2 参数：
    // 第二个要投递的事件值。
    //
    // postPolicy 参数：
    // 可选 Actor 投递策略。
    //
    // fullPolicy 参数：
    // 可选邮箱满载策略。
    //
    // 必要逻辑：
    // 对同一个 Storage 只扫描一次 alive slot，
    // 在扫描过程中同时向可接收事件的列投递多个事件。

    int eventId1 = EventTypeId<TEvent1>.Id;
    int eventId2 = EventTypeId<TEvent2>.Id;

    if (_columnsByEventId[eventId1] is not EventColumn<TActor, TEvent1> column1)
    {
        return;
    }

    if (_columnsByEventId[eventId2] is not EventColumn<TActor, TEvent2> column2)
    {
        return;
    }

    int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        // slotIndex 参数：
        // 当前遍历的 Actor 槽位。
        //
        // 必要逻辑：
        // 只有 Alive 且 Actor 实例非空时才投递事件。
        if (_states[slotIndex] != ActorSlotState.Alive)
        {
            continue;
        }

        if (_actors[slotIndex] == null)
        {
            continue;
        }

        _ = column1.Post(slotIndex, in value1, postPolicy, fullPolicy);
        _ = column2.Post(slotIndex, in value2, postPolicy, fullPolicy);
    }
}
```

### 14.3 推进方式

至少12个泛型


---

## 15. 源生成器设计

### 15.1 生成器职责

Actor 源生成器需要读取：

```text
[Tag<TTag>]
[Group<TGroup>]
[ActorBehaviour]
```

并生成：

```text
ActorContext 注入逻辑
Actor 行为元数据
Actor TagIds
Actor GroupIds
ActorTypeMeta<TActor>
```

### 15.2 读取泛型 Tag

```csharp
private static bool TryGetGenericTagType(
    AttributeData attribute,
    INamedTypeSymbol tagAttributeDefinition,
    out INamedTypeSymbol? tagType)
{
    // attribute 参数：
    // Roslyn 提供的特性语义数据。
    //
    // tagAttributeDefinition 参数：
    // TagAttribute<TTag> 的泛型定义符号。
    //
    // tagType 参数：
    // 如果读取成功，输出 TTag 的具体类型。
    //
    // 必要逻辑：
    // 泛型特性的类型参数不在 ConstructorArguments 中。
    // 它在 AttributeClass.TypeArguments 中。
    tagType = null;

    if (attribute.AttributeClass is not INamedTypeSymbol attrClass)
    {
        return false;
    }

    if (!SymbolEqualityComparer.Default.Equals(
            attrClass.OriginalDefinition,
            tagAttributeDefinition))
    {
        return false;
    }

    if (attrClass.TypeArguments.Length != 1)
    {
        return false;
    }

    tagType = attrClass.TypeArguments[0] as INamedTypeSymbol;
    return tagType != null;
}
```

### 15.3 读取泛型 Group

```csharp
private static bool TryGetGenericGroupType(
    AttributeData attribute,
    INamedTypeSymbol groupAttributeDefinition,
    out INamedTypeSymbol? groupType)
{
    // attribute 参数：
    // Roslyn 提供的特性语义数据。
    //
    // groupAttributeDefinition 参数：
    // GroupAttribute<TGroup> 的泛型定义符号。
    //
    // groupType 参数：
    // 如果读取成功，输出 TGroup 的具体类型。
    //
    // 必要逻辑：
    // 泛型 Group 类型从 AttributeClass.TypeArguments 中读取。
    groupType = null;

    if (attribute.AttributeClass is not INamedTypeSymbol attrClass)
    {
        return false;
    }

    if (!SymbolEqualityComparer.Default.Equals(
            attrClass.OriginalDefinition,
            groupAttributeDefinition))
    {
        return false;
    }

    if (attrClass.TypeArguments.Length != 1)
    {
        return false;
    }

    groupType = attrClass.TypeArguments[0] as INamedTypeSymbol;
    return groupType != null;
}
```

### 15.4 生成代码示例

```csharp
public sealed partial class EnemyActor : IGeneratedActorMeta
{
    public static ActorTypeMeta<EnemyActor> CreateActorMeta()
    {
        // CreateActorMeta 的作用：
        // 返回 EnemyActor 的完整 Actor 元数据。
        //
        // 必要逻辑：
        // ActorWorld.CreateActor<EnemyActor>() 会读取这份元数据，
        // 并根据 Behaviour、Tag、Group 建立正确的 Archetype。
        return new ActorTypeMeta<EnemyActor>(
            signature: EnemyActor_ActorMetaGenerated.CreateBehaviourSignature(),
            behaviours: EnemyActor_ActorMetaGenerated.CreateBehaviours(),
            tagIds: new[]
            {
                ActorTagId<EnemyTag>.Id,
                ActorTagId<DamageableTag>.Id
            },
            groupIds: new[]
            {
                ActorGroupId<BattleActorGroup>.Id
            });
    }
}
```

---

## 16. Actor Benchmark 方案

### 16.1 Benchmark 目标

Actor Benchmark 需要覆盖：

1. 创建 Actor。
2. 池化创建 Actor。
3. 销毁 Actor。
4. 池化销毁 Actor。
5. Query 首次构建。
6. Query 缓存命中。
7. Tag / Group Query。
8. PostAll 单事件。
9. PostAll 多事件。
10. Actor 邮箱 Pump。
11. 生命周期 Pump。
12. ForEachActor。
13. ForEachStorage。

### 16.2 Benchmark 项目结构

```text
LayerBase.BenchMark/
  Actor/
    ActorCreateBenchmarks.cs
    ActorDestroyBenchmarks.cs
    ActorPoolBenchmarks.cs
    ActorQueryBenchmarks.cs
    ActorPostAllBenchmarks.cs
    ActorPumpBenchmarks.cs
    ActorLifecycleBenchmarks.cs
    ActorForEachBenchmarks.cs
```

### 16.3 Benchmark 示例

```csharp
[MemoryDiagnoser]
public class ActorCreateBenchmarks
{
    private LayerRuntime _runtime = null!;

    [Params(100, 1000, 10000)]
    public int ActorCount;

    [GlobalSetup]
    public void Setup()
    {
        // Setup 用于构造基准测试前置环境。
        // ActorCount 参数来自 Params，表示本轮测试创建多少个 Actor。
        _runtime = LayerRuntime.Create()
            .Push(new GameLayer())
            .Build();
    }

    [Benchmark]
    public void CreateWithoutPool()
    {
        // CreateWithoutPool 表示默认非池化创建路径。
        // usePool 参数为 false，等价于不传该参数。
        for (int i = 0; i < ActorCount; i++)
        {
            _runtime.Actors.CreateActor<EnemyActor>(usePool: false);
        }
    }

    [Benchmark]
    public void CreateWithPool()
    {
        // CreateWithPool 表示显式池化创建路径。
        // usePool 参数为 true，要求 EnemyActor 实现 IPooledActor。
        for (int i = 0; i < ActorCount; i++)
        {
            _runtime.Actors.CreateActor<PooledEnemyActor>(usePool: true);
        }
    }
}
```

---

## 17. 与 ECS 的分工

Actor 不替代 ECS。

推荐关系：

```text
Actor 持有业务身份和上下文
ECS 持有连续数据和批处理逻辑
Actor 通过 ActorId / EntityId 关联 ECS 数据
```

示例：

```csharp
public sealed partial class EnemyActor : IActor
{
    private ActorContext _ctx;

    public int EntityId { get; private set; }

    public void BindEntity(int entityId)
    {
        // entityId 参数：
        // ECS 世界中的实体编号。
        //
        // 必要逻辑：
        // Actor 不直接保存所有运动数据。
        // Actor 通过 EntityId 找到 ECS 侧连续数据。
        EntityId = entityId;
    }
}
```

这种方式下：

- Actor 负责业务消息。
- ECS 负责批量移动和计算。
- Layer 负责系统边界。
- Service 负责稳定服务。

---

## 18. 实施阶段

### Phase 1：工程安全性与 Benchmark

目标：

- 补齐 Actor Benchmark。
- 稳定 QueryResult 版本语义。
- 确认热路径分配情况。

任务：

1. 新增 Actor Benchmark。
2. QueryResult 增加版本号。
3. QueryCache 增加 Descriptor。
4. QueryActor 高频泛型签名缓存。
5. PostAll 单事件路径确认零分配。
6. Actor Pump 确认零分配。

验收：

```text
dotnet test 通过
dotnet build -c Release 通过
Benchmark 可运行
QueryHot 不产生 Gen0 分配
Actor Pump 不产生 Gen0 分配
```

### Phase 2：泛型 Tag / Group 元数据

目标：

- Actor 类型可通过泛型特性声明 Tag / Group。
- 源生成器将 Tag / Group 写入 ActorTypeMeta。

任务：

1. 新增 `IActorTag`。
2. 新增 `IActorGroup`。
3. 新增 `TagAttribute<TTag>`。
4. 新增 `GroupAttribute<TGroup>`.
5. 新增 `ActorTagId<TTag>`。
6. 新增 `ActorGroupId<TGroup>`。
7. 扩展 `ActorTypeMeta<TActor>`。
8. 扩展 Actor 源生成器。
9. 增加生成器测试。

验收：

```text
[Tag<EnemyTag>] 可用
[Group<BattleActorGroup>] 可用
ActorTypeMeta<TActor> 生成 TagIds
ActorTypeMeta<TActor> 生成 GroupIds
重复 Tag / Group 自动去重
```

### Phase 3：Archetype Key 扩展

目标：

- Behaviour + Tag + Group 共同决定 Archetype。

任务：

1. 新增 `ActorTagSignature`。
2. 新增 `ActorGroupSignature`。
3. 新增 `ActorArchetypeKey`。
4. `BehaviourArchetype` 保存 Tags / Groups。
5. `ActorWorld.GetOrCreateArchetype` 使用完整 Key。

验收：

```text
行为相同但 Tag 不同的 Actor 不混入同一个 Archetype
行为相同但 Group 不同的 Actor 不混入同一个 Archetype
行为、Tag、Group 都相同的 Actor 才复用同一个 Archetype
```

### Phase 4：Query v2

目标：

- Query 从广播工具升级为业务筛选入口。

任务：

1. 新增 `ActorQueryBuilder`。
2. 新增 `ActorQueryDescriptor`。
3. 支持 `AllBehaviours<T...>()`。
4. 支持 `NoneBehaviours<T...>()`。
5. 支持 `AnyBehaviours<T...>()`。
6. 支持 `AllTags<T...>()`。
7. 支持 `NoneTags<T...>()`。
8. 支持 `AllGroups<T...>()`。
9. 支持 `NoneGroups<T...>()`。
10. 保留旧 `QueryActor<T...>()` API。
11. 旧 API 内部复用 Query v2。

验收：

```text
可表达有 A/B 且排除 C/D 的查询
可按 Tag 查询
可按 Group 查询
可组合 Behaviour + Tag + Group 查询
旧 API 不破坏
```

### Phase 5：ForEach 与 Storage 遍历

目标：

- QueryResult 支持直接遍历 Actor。
- 性能敏感场景支持 Storage 级遍历。

任务：

1. 新增 `ForEachActor<TActor>()`。
2. 新增 `ForEachActor<TActor, TState>()`。
3. 新增 `ForEachStorage<TActor, TState>()`。
4. 支持 `static lambda + ref state`。
5. 增加 ForEach Benchmark。

验收：

```text
可遍历指定 Actor 类型
可用 static lambda + ref state 避免闭包分配
Destroyed Actor 不会被遍历
PendingDestroy Actor 不会被遍历
```

### Phase 6：PostAll 多事件融合

目标：

- 减少多事件广播重复扫描 Storage。

任务：

1. 新增 `PostManyToAliveActors<T1,T2>()`。
2. 扩展到 `T1...T12`。
3. `ActorQueryPostExtensions` 改为调用融合路径。
4. Benchmark 对比旧路径和新路径。

验收：

```text
多事件 PostAll 对同一 Storage 只扫描一次
性能优于旧路径
不产生额外 GC
```

### Phase 7：Actor Pool

目标：

- 支持高频 Actor 创建销毁场景。
- `CreateActor<TActor>()` 默认不池化。
- 调用方可通过 `usePool: true` 显式启用池化。

任务：

1. 新增 `IPooledActor`。
2. 新增 `ActorPool<TActor>`。
3. 新增 `ActorPoolCache<TActor>`。
4. `CreateActor<TActor>(bool usePool = false)` 接入池化。
5. `TypedActorStorage<TActor>` 记录 Slot 是否来自池。
6. `DestroyNow` 根据 Slot 标记决定是否 Return Pool。
7. Benchmark 对比池化前后。

验收：

```text
CreateActor<TActor>() 默认不池化
CreateActor<TActor>(usePool: false) 不池化
CreateActor<TActor>(usePool: true) 走池化
usePool: true 时 TActor 必须实现 IPooledActor
池化 Actor 销毁后归还 ActorPool
普通 Actor 销毁后正常释放引用
```

---

## 19. DoD

DoD 是 Definition of Done，意思是完成标准。

本设计完成标准：

1. 所有新增 API 有单元测试。
2. 所有热路径 Benchmark 有结果。
3. `QueryHot` 不产生 GC。
4. `PostAll` 单事件不产生 GC。
5. `PostAll` 多事件融合路径不重复扫描 Storage。
6. `DestroyActor` 后不会再收到消息。
7. Disabled Actor 不执行生命周期。
8. PendingDestroy Actor 不执行生命周期。
9. QueryResult 过期后能刷新。
10. 旧 `QueryActor<T...>()` API 保持兼容。
11. `CreateActor<TActor>()` 默认不池化。
12. `CreateActor<TActor>(usePool: true)` 可显式启用池化。
13. 文档更新：
    - Query v2 使用说明。
    - 泛型 Tag / Group 使用说明。
    - Actor Pool 使用说明。
    - Benchmark 运行说明。

---

## 20. Agent 执行指令

```text
You are working on the LayerBase repository.

Goal:
Upgrade the Actor system to an engineering-ready gameplay Actor runtime.

Do not redesign the whole framework.
Do not replace the existing Layer / Event / Post / Scheduler architecture.
Do not remove existing public APIs.

Must implement:
1. Actor benchmarks.
2. ActorQueryResult version validation.
3. Query v2 builder.
4. Generic Tag and Group attributes:
   - [Tag<EnemyTag>]
   - [Group<BattleActorGroup>]
5. Source generator support for generic Tag / Group attributes.
6. ActorTypeMeta TagIds and GroupIds.
7. ActorArchetypeKey = BehaviourSignature + ActorTagSignature + ActorGroupSignature.
8. ForEachActor APIs with ref state support.
9. ForEachStorage API for performance-sensitive cases.
10. PostAll multi-event fused traversal.
11. Optional Actor pooling:
    - CreateActor<TActor>(bool usePool = false)
    - default usePool is false
    - usePool true requires IPooledActor

Must not implement:
- AOI
- SpatialCellId
- BVH
- quadtree
- grid index
- radius query
- nearest query
- position based query
- low-frequency lifecycle scheduling interfaces

Required checks:
- dotnet test
- dotnet build -c Release
- dotnet run -c Release --project LayerBase.BenchMark

Constraints:
- Keep hot paths allocation-free where practical.
- Prefer generic static caches.
- Use source generators only for repetitive overload boilerplate.
- Keep old QueryActor<T...>() and PostAll<T...>() APIs compatible.
- Do not introduce Unity or Godot dependencies into core LayerBase.
```

---

## 21. 最终推荐形态

Actor 声明：

```csharp
[Tag<EnemyTag>]
[Tag<DamageableTag>]
[Group<BattleActorGroup>]
public sealed partial class EnemyActor : IActor
{
}
```

默认创建：

```csharp
EnemyActor enemy = runtime.Actors.CreateActor<EnemyActor>();
```

等价于：

```csharp
EnemyActor enemy = runtime.Actors.CreateActor<EnemyActor>(usePool: false);
```

池化创建：

```csharp
PooledBulletActor bullet = runtime.Actors.CreateActor<PooledBulletActor>(usePool: true);
```

Query：

```csharp
ActorQueryResult query = runtime.Actors
    .Query()
    .AllTags<EnemyTag, DamageableTag>()
    .NoneTags<DeadTag>()
    .AllGroups<BattleActorGroup>()
    .Build();
```

批量 Post：

```csharp
query.PostAll(new DamageEvent(value: 100));
```

遍历处理：

```csharp
var state = new DamageState
{
    // Damage 字段表示本次处理要使用的伤害值。
    Damage = 100
};

query.ForEachActor<EnemyActor, DamageState>(
    ref state,
    static (enemy, ref DamageState state) =>
    {
        // enemy 参数：
        // 当前遍历到的 EnemyActor。
        //
        // state 参数：
        // 外部传入的处理状态。
        enemy.TakeDamage(state.Damage);
    });
```

最终原则：

```text
Tag / Group 使用泛型特性声明。
Query 负责业务分类查询。
Actor Pool 由 CreateActor<TActor>(bool usePool = false) 显式控制。
默认不池化。
PostAll 和 ForEach 都需要保留。
空间查询和低频生命周期调度不进入本设计。
```
