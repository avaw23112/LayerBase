# LayerBase Actor Behaviour Runtime 完整设计文档

文件建议路径：

```text
docs/actor/actor-runtime-design.md
```

本文档用于指导在 `avaw23112/LayerBase` 中加入 Actor Behaviour Runtime。  
该功能来自 Actor Behaviour ECS 方案，但落地时不应被设计成完整传统 ECS，而应被设计成 LayerBase 内部的“实体行为运行时”。

---

## 0. 文档目标

本文档解决四个问题：

```text
1. Actor Behaviour Runtime 在 LayerBase 中负责什么。
2. 它和现有 LayerRuntime、PostScheduler、EventTypeId、EventMetaData、Source Generator 如何连接。
3. 每个模块应该新增哪些文件、承担哪些职责。
4. 每个阶段怎么拆 PR、怎么验收、哪些内容不能提前做。
```

---

## 1. 名词说明

### 1.1 Actor

Actor 是游戏实体的逻辑行为对象。

例如：

```text
EnemyActor
BossActor
NpcActor
ProjectileActor
```

它不是 Unity 的 `GameObject`，也不是 Godot 的 `Node`。  
它只承载实体的行为逻辑。

### 1.2 Behaviour

Behaviour 指 Actor 对某种事件的响应方法。

例如：

```csharp
[ActorBehaviour]
private void OnDamage(in DamageEvent e)
{
}
```

其中 `DamageEvent` 是事件类型，`OnDamage` 是行为方法。

### 1.3 Runtime

Runtime 是运行时系统。

这里的 Actor Runtime 指：

```text
ActorWorld
BehaviourArchetype
TypedActorStorage
EventColumn
EventMail
ActorEventBucket
```

它们负责 Actor 创建、ActorId 定位、事件投递、邮箱存储和帧内 Pump。

### 1.4 Pump

Pump 是“每帧推进一部分任务”的执行过程。

在 LayerBase 中，`LayerRuntime.Pump(deltaTime)` 会推进 Timer、Delay、PostScheduler 和 Layer Update。  
Actor Runtime 应插入这个 Pump 流程，而不是自己开一个独立循环。

### 1.5 SOA

SOA 是 Structure of Arrays，意思是“数组化结构”。

它和普通 OOP 对象存储不同：

```text
普通 OOP：
  EnemyActor 对象里放各种字段。

SOA：
  同类数据放进连续数组。
```

Actor Runtime 不要求用户把所有业务字段都改成纯 ECS 数据，但内部存储必须尽量用数组索引，避免热路径查字典。

### 1.6 热路径

热路径是高频执行路径。

例如：

```text
Post<TEvent>
PumpOne
EventMail.Enqueue
EventMail.Dequeue
ActorBehaviourInvoker 调用
```

热路径禁止使用：

```text
Dictionary
反射
字符串查找
Type 查找
GetComponent
ActorId -> Actor 对象字典
```

### 1.7 冷路径

冷路径是低频执行路径。

例如：

```text
CreateActor<TActor>()
首次构建 ActorTypeMeta
首次创建 BehaviourArchetype
首次创建 TypedActorStorage<TActor>
生成 EventColumn
```

冷路径可以使用字典，但结果必须写入数组，供热路径直接索引。

### 1.8 BehaviourSignature

BehaviourSignature 是 Actor 支持的事件集合。

例如：

```text
EnemyActor:
  DamageEvent
  DeadEvent

BossActor:
  DamageEvent
  DeadEvent
```

这两个 Actor 的 BehaviourSignature 相同，因此可以进入同一个 BehaviourArchetype。

### 1.9 BehaviourArchetype

BehaviourArchetype 是按 BehaviourSignature 分组的运行时存储块。

它不是传统 ECS 的 Component Archetype。  
这里的 Archetype 只表示“支持同一组 ActorBehaviour 事件”的 Actor 分组。

### 1.10 EventColumn

EventColumn 是某个 Actor 类型针对某个事件类型的邮箱列。

例如：

```text
EventColumn<EnemyActor, DamageEvent>
EventColumn<BossActor, DamageEvent>
EventColumn<EnemyActor, DeadEvent>
```

EventColumn 内部保存：

```text
EventMail<TEvent>[]
DirtySlotList
RingQueueBuffer<TEvent>
ActorBehaviourInvoker<TActor, TEvent>
```

---

## 2. 总体定位

Actor Behaviour Runtime 是 LayerBase 的实体行为层。

它的职责是：

```text
1. 允许用户用 OOP 写 Actor 行为。
2. 用 Source Generator 自动补运行时能力。
3. 用 ActorWorld 管理 Actor 创建和存储。
4. 用 ActorId 直接定位 Actor 所在数组位置。
5. 用 EventColumn 和 EventMail 管理单 Actor 单事件邮箱。
6. 用 ActorEventBucket 按事件类型轮询执行行为。
7. 用 LayerRuntime 剩余帧预算控制执行量。
```

它不负责：

```text
1. 物理检测。
2. AABB 查询。
3. 空间索引。
4. 阵营过滤。
5. 技能目标选择。
6. Unity GameObject 生命周期。
7. Godot Node 生命周期。
8. 网络同步。
9. 传统 ECS Component 数据查询。
```

这些应由业务系统或引擎适配层处理，然后把得到的 `ActorId` 交给 ActorWorld 投递事件。

---

## 3. 与 LayerBase 现有机制的对齐

当前 LayerBase 已经具备这些基础：

```text
1. LayerRuntime 是每个世界的运行时入口。
2. PostScheduler 已经负责异步 Post 队列和帧预算。
3. EventTypeId<T>.Id 已经提供热路径事件类型 ID。
4. EventMetaData<TEvent> 已经负责事件元数据。
5. ManagerAutoSubscribeGenerator 已经负责 partial class 源生成。
6. LayerBase 本身已经以 DOD / SOA / Source Generator 为核心方向。
```

Actor Behaviour Runtime 必须复用这些方向。

### 3.1 生成器对齐

现有 Manager 自动订阅模式：

```text
用户写 partial class。
用户写 [Subscribe] 方法。
生成器生成同名 partial class。
生成器让类实现隐藏接口。
运行时识别隐藏接口并完成绑定。
```

Actor 对应模式：

```text
用户写 partial Actor。
用户写 [ActorBehaviour] 方法。
生成器生成同名 partial class。
生成器让 Actor 实现 IGeneratedActorMeta。
ActorWorld.CreateActor<TActor>() 识别 IGeneratedActorMeta。
ActorWorld 调用 __BuildActorMeta 构建 ActorTypeMeta。
```

禁止：

```text
全局 ActorGeneratedRegistry。
ModuleInitializer 自动注册。
EnemyActor_ActorMetaGenerated.Register。
用户手动 ActorInit。
```

### 3.2 Runtime 对齐

ActorWorld 应挂到 `LayerRuntime`：

```text
LayerRuntime
  -> EventCenter
  -> PostScheduler
  -> Timer
  -> Delay
  -> LayerChain
  -> ActorWorld
```

推荐公开入口：

```csharp
runtime.Actors.CreateActor<EnemyActor>();
```

不推荐：

```csharp
Actor.Create<EnemyActor>();
ActorHubFacade.Create<EnemyActor>();
GlobalActorWorld.Create<EnemyActor>();
```

原因：

```text
1. Actor 属于某个 Runtime 世界。
2. ActorId 只在所属 ActorWorld 内有效。
3. 多世界场景下全局入口容易混淆。
```

### 3.3 EventTypeId 对齐

Actor 热路径必须复用：

```csharp
EventTypeId<TEvent>.Id
```

禁止在 Post / Pump 热路径使用：

```csharp
typeof(TEvent)
Dictionary<Type, int>
Type.GetHashCode()
反射扫描
```

---

## 4. 顶层架构

最终存储结构如下：

```text
LayerRuntime
  -> ActorWorld Actors

ActorWorld
  -> BehaviourArchetype[] _archetypes
  -> IActorEventBucket[] _eventBucketsByEventId
  -> Dictionary<BehaviourSignature, BehaviourArchetype> _archetypeMap      冷路径
  -> int _bucketCursor

BehaviourArchetype
  -> BehaviourSignature Signature
  -> TypedStorageRuntime[] _storages
  -> Dictionary<Type, ushort> _storageIndexByType                          冷路径

TypedActorStorage<TActor>
  -> TActor[] Actors
  -> int[] Generations
  -> ActorSlotFreeList FreeList
  -> ActorEventColumnRuntime[] _columnsByEventId

EventColumn<TActor, TEvent>
  -> EventMail<TEvent>[] _mails
  -> DirtySlotList _dirtySlots
  -> RingQueueBuffer<TEvent> _bufferPool
  -> ActorBehaviourInvoker<TActor, TEvent> _invoker
  -> ActorMailOptions _options

ActorEventBucket<TEvent>
  -> IActorEventColumn<TEvent>[] _columns
  -> int _cursor
```

Post 路径：

```text
ActorWorld.Post<TEvent>(ActorId, in TEvent)
  -> _archetypes[ActorId.ArchetypeId]
  -> _storages[ActorId.TypeStorageIndex]
  -> _columnsByEventId[EventTypeId<TEvent>.Id]
  -> EventMail<TEvent>[ActorId.SlotIndex]
```

Pump 路径：

```text
ActorWorld.Pump(ref RuntimeFrameBudget)
  -> ActorEventBucket<TEvent>
  -> EventColumn<TActor, TEvent>
  -> DirtySlotList
  -> EventMail<TEvent>
  -> TActor[] Actors
  -> ActorBehaviourInvoker<TActor, TEvent>
```

---

## 5. 推荐目录结构

第一阶段不建议新建独立 `LayerBase.Actor.csproj`。  
先放进主工程 `LayerBase` 下，降低项目引用和 NuGet 包拆分成本。

```text
LayerBase/
  Actor/
    Core/
      IActor.cs
      IGeneratedActorMeta.cs
      ActorBehaviourAttribute.cs
      ActorId.cs
      ActorContext.cs
      ActorExtensions.cs
      ActorGeneratedAccess.cs

    Meta/
      ActorBehaviourInvoker.cs
      ActorBehaviourEntry.cs
      ActorTypeMeta.cs
      ActorTypeMetaBuilder.cs
      ActorTypeMetaCache.cs
      BehaviourSignature.cs

    Storage/
      ActorWorld.cs
      ActorWorld.Create.cs
      ActorWorld.Post.cs
      BehaviourArchetype.cs
      TypedStorageRuntime.cs
      TypedActorStorage.cs
      ActorSlotFreeList.cs

    Mail/
      ActorEventBucket.cs
      ActorEventColumnRuntime.cs
      IActorEventBucket.cs
      IActorEventColumn.cs
      EventColumn.cs
      EventMail.cs
      EventMailWriter.cs
      EventMailReader.cs
      RingQueueBuffer.cs
      DirtySlotList.cs

    Options/
      ActorMailOptions.cs
      ActorPostPolicy.cs
      ActorMailFullPolicy.cs

    Pump/
      RuntimeFrameBudget.cs

    Query/
      ActorQuery.cs
      ActorQueryCache.cs
      ActorQueryResult.cs
      ActorQueryPostExtensions.cs

LayerBase.Generator/
  LayerBase.Generator/
    ActorBehaviourGenerator.cs
    ActorBehaviourDiagnostics.cs

LayerBase.Test/
  ActorCoreTests.cs
  ActorGeneratorTests.cs
  ActorPostPumpTests.cs
  ActorMailPolicyTests.cs
  ActorRuntimeIntegrationTests.cs
  ActorQueryTests.cs

LayerBase.BenchMark/
  ActorWorldBenchmarks.cs
```

---

## 6. 模块设计：Actor/Core

### 6.1 IActor

```csharp
namespace LayerBase.Actor;

public interface IActor
{
}
```

设计说明：

```text
IActor 是空标记接口。
它只表示该对象是 Actor。
它不承载 GetId、Post、ActorInit。
```

原因：

```text
1. 用户类保持纯净。
2. 用户不需要手写运行时能力。
3. 运行时能力由 Source Generator 生成。
4. 与 LayerBase 中 ILayerContext / IService 的扩展方法风格一致。
```

### 6.2 ActorBehaviourAttribute

```csharp
namespace LayerBase.Actor;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ActorBehaviourAttribute : Attribute
{
}
```

### 6.3 IGeneratedActorMeta

```csharp
namespace LayerBase.Actor;

using System.ComponentModel;
using LayerBase.Core.Event;

public interface IGeneratedActorMeta
{
    void __BuildActorMeta(ActorTypeMetaBuilder builder);

    ActorId GetId();

    void ActorInit(ActorContext context);

    PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct;

    PostResult TryPost<TEvent>(in TEvent value)
        where TEvent : struct;
}
```

关键点：

```text
1. 该接口必须 public。
2. 用户 Actor 往往在用户程序集。
3. 生成器生成的 partial class 也在用户程序集。
4. 如果接口是 internal，用户程序集无法实现。
```

### 6.4 ActorId

```csharp
namespace LayerBase.Actor;

public readonly struct ActorId
{
    public readonly int ArchetypeId;
    public readonly ushort TypeStorageIndex;
    public readonly int SlotIndex;
    public readonly int Generation;

    public ActorId(
        int archetypeId,
        ushort typeStorageIndex,
        int slotIndex,
        int generation)
    {
        // archetypeId 参数表示 Actor 所属 BehaviourArchetype 的数组下标。
        // ActorWorld.Post 会先通过该值定位 _archetypes。
        ArchetypeId = archetypeId;

        // typeStorageIndex 参数表示 Actor 所属 BehaviourArchetype 内的具体类型存储下标。
        // 同一个 BehaviourArchetype 可以包含 EnemyActor、BossActor 等多个具体 Actor 类型。
        TypeStorageIndex = typeStorageIndex;

        // slotIndex 参数表示 Actor 在 TypedActorStorage<TActor>.Actors 数组中的下标。
        // EventMail<TEvent>[] 也使用同一个 slotIndex 定位目标邮箱。
        SlotIndex = slotIndex;

        // generation 参数表示 slot 当前代数。
        // slot 被销毁后再次复用时必须递增 generation，避免旧 ActorId 命中新 Actor。
        Generation = generation;
    }
}
```

### 6.5 ActorContext

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

public readonly struct ActorContext
{
    public ActorId ActorId { get; }

    internal ActorWorld World { get; }

    internal ActorContext(ActorWorld world, ActorId actorId)
    {
        // world 参数表示当前 Actor 所属的 ActorWorld。
        // ActorWorld 负责 Actor 创建、Post 路由、Query 和 Pump。
        World = world;

        // actorId 参数表示当前 Actor 在 ActorWorld 内的定位信息。
        // 该值会被生成代码返回给用户侧扩展方法。
        ActorId = actorId;
    }

    public PostResult Post<TEvent>(in TEvent value)
        where TEvent : struct
    {
        // value 参数表示要投递给当前 Actor 自己的事件。
        // 这里只写入当前 Actor 的 EventMail，不直接调用 ActorBehaviour 方法。
        return World.Post(ActorId, in value);
    }

    public PostResult TryPost<TEvent>(in TEvent value)
        where TEvent : struct
    {
        // value 参数表示要尝试投递给当前 Actor 自己的事件。
        // TryPost 保留失败结果，方便业务层决定是否降级处理。
        return World.TryPost(ActorId, in value);
    }
}
```

### 6.6 ActorExtensions

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

public static class ActorExtensions
{
    public static ActorId GetActorId(this IActor actor)
    {
        // actor 参数是任意 IActor 实例。
        // 真正的运行时能力由生成器生成的 IGeneratedActorMeta 提供。
        return ActorGeneratedAccess.RequireGenerated(actor).GetId();
    }

    public static PostResult Post<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        // actor 参数是目标 Actor。
        // value 参数是要投递给该 Actor 的事件。
        // 这里只写入邮箱，不直接执行行为方法。
        return ActorGeneratedAccess.RequireGenerated(actor).Post(in value);
    }

    public static PostResult TryPost<TEvent>(this IActor actor, in TEvent value)
        where TEvent : struct
    {
        // actor 参数是目标 Actor。
        // value 参数是要尝试投递给该 Actor 的事件。
        // 返回值包含成功、失败、丢弃等投递结果。
        return ActorGeneratedAccess.RequireGenerated(actor).TryPost(in value);
    }
}
```

### 6.7 ActorGeneratedAccess

```csharp
namespace LayerBase.Actor;

internal static class ActorGeneratedAccess
{
    public static IGeneratedActorMeta RequireGenerated(IActor actor)
    {
        // actor 参数是任意 IActor 实例。
        // ActorBehaviourGenerator 会为合法 Actor 生成 IGeneratedActorMeta 实现。
        if (actor is IGeneratedActorMeta generated)
        {
            return generated;
        }

        throw new InvalidOperationException(
            $"Actor type {actor.GetType().Name} does not provide generated actor metadata.");
    }
}
```

---

## 7. 模块设计：Actor/Meta

### 7.1 ActorBehaviourInvoker

```csharp
namespace LayerBase.Actor;

internal delegate void ActorBehaviourInvoker<TActor, TEvent>(
    TActor actor,
    in TEvent value)
    where TActor : class, IActor
    where TEvent : struct;
```

参数说明：

```text
actor：
  从 TypedActorStorage<TActor>.Actors[slotIndex] 取出的强类型 Actor 实例。

value：
  从 EventMail<TEvent> 出队得到的事件值。
```

### 7.2 ActorBehaviourEntry

```csharp
namespace LayerBase.Actor;

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;

    public ActorBehaviourEntry(
        int eventTypeId,
        Type eventType,
        object invoker)
    {
        // eventTypeId 参数表示 TEvent 对应的 EventTypeId<TEvent>.Id。
        // 它用于构建 BehaviourSignature 和 EventColumn 索引。
        EventTypeId = eventTypeId;

        // eventType 参数只在冷路径构建和诊断中使用。
        // Post/Pump 热路径禁止使用 Type 查找。
        EventType = eventType;

        // invoker 参数保存 ActorBehaviourInvoker<TActor, TEvent>。
        // 因为不同 TEvent 泛型类型不同，这里在冷路径用 object 保存。
        Invoker = invoker;
    }
}
```

### 7.3 ActorTypeMeta

```csharp
namespace LayerBase.Actor;

internal sealed class ActorTypeMeta<TActor>
    where TActor : class, IActor
{
    public BehaviourSignature Signature { get; }

    public ActorBehaviourEntry[] Behaviours { get; }

    public ActorTypeMeta(
        BehaviourSignature signature,
        ActorBehaviourEntry[] behaviours)
    {
        // signature 参数表示当前 TActor 支持的事件集合。
        // ActorWorld 会用它选择 BehaviourArchetype。
        Signature = signature;

        // behaviours 参数表示当前 TActor 的所有 ActorBehaviour 方法元数据。
        // TypedActorStorage 创建 EventColumn 时会读取它。
        Behaviours = behaviours;
    }
}
```

### 7.4 ActorTypeMetaBuilder

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

public sealed class ActorTypeMetaBuilder
{
    private readonly List<ActorBehaviourEntry> _entries = new();

    private readonly HashSet<int> _eventIds = new();

    public void AddBehaviour<TActor, TEvent>(
        ActorBehaviourInvoker<TActor, TEvent> invoker)
        where TActor : class, IActor
        where TEvent : struct
    {
        // invoker 参数是 Source Generator 生成的强类型行为调用器。
        // 它会在 Pump 时直接调用用户写的 ActorBehaviour 方法。
        int eventTypeId = EventTypeId<TEvent>.Id;

        if (!_eventIds.Add(eventTypeId))
        {
            throw new InvalidOperationException(
                $"Actor type {typeof(TActor).Name} already has behaviour for event {typeof(TEvent).Name}.");
        }

        _entries.Add(new ActorBehaviourEntry(
            eventTypeId: eventTypeId,
            eventType: typeof(TEvent),
            invoker: invoker));
    }

    internal ActorTypeMeta<TActor> Build<TActor>()
        where TActor : class, IActor
    {
        // Build 只在 ActorTypeMeta 冷路径构建时调用。
        // 它会对 EventTypeId 排序，确保 BehaviourSignature 稳定。
        ActorBehaviourEntry[] entries = _entries
            .OrderBy(static entry => entry.EventTypeId)
            .ToArray();

        int[] eventIds = entries
            .Select(static entry => entry.EventTypeId)
            .ToArray();

        BehaviourSignature signature = new BehaviourSignature(eventIds);

        return new ActorTypeMeta<TActor>(
            signature: signature,
            behaviours: entries);
    }
}
```

### 7.5 BehaviourMask 与 BehaviourSignature

Query 必须基于 Mask 做集合包含判断。

这里的 Mask 不是 64 位固定上限设计，而是 `ulong[] words` 形式的可扩展位集。

```text
wordIndex = eventTypeId / 64
bitIndex  = eventTypeId % 64
words[wordIndex] |= 1UL << bitIndex
```

这样不会把 ActorBehaviour 事件类型限制在 64 个以内。  
64 只是每个 `ulong` word 可以保存的 bit 数量，不是系统事件类型上限。

```csharp
namespace LayerBase.Actor;

internal readonly struct BehaviourMask : IEquatable<BehaviourMask>
{
    private readonly ulong[] _words;

    public BehaviourMask(ulong[] words)
    {
        // words 参数表示可扩展位集。
        // 每个 ulong 保存 64 个事件类型 bit，多个 ulong 组合后支持任意数量的 EventTypeId。
        _words = TrimTrailingZeroWords(words);
    }

    public ReadOnlySpan<ulong> Words => _words;

    public static BehaviourMask FromSortedEventIds(ReadOnlySpan<int> eventTypeIds)
    {
        // eventTypeIds 参数表示已经排序并去重的事件类型 ID 集合。
        // 该方法用于把 BehaviourSignature 转成可快速匹配的位集。
        if (eventTypeIds.Length == 0)
        {
            return new BehaviourMask(Array.Empty<ulong>());
        }

        int maxEventTypeId = eventTypeIds[eventTypeIds.Length - 1];

        if (maxEventTypeId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventTypeIds),
                "EventTypeId must be non-negative.");
        }

        int wordCount = maxEventTypeId / 64 + 1;
        ulong[] words = new ulong[wordCount];

        foreach (int eventTypeId in eventTypeIds)
        {
            // eventTypeId 参数来自 EventTypeId<TEvent>.Id。
            // 它可以大于 63；大于 63 时会自动落到后续 word 中。
            if (eventTypeId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventTypeIds),
                    "EventTypeId must be non-negative.");
            }

            int wordIndex = eventTypeId / 64;
            int bitIndex = eventTypeId % 64;

            // 1UL << bitIndex 表示设置当前 word 中的目标 bit。
            // 按位或 |= 表示把该事件类型加入当前行为集合。
            words[wordIndex] |= 1UL << bitIndex;
        }

        return new BehaviourMask(words);
    }

    public bool ContainsAll(BehaviourMask query)
    {
        // query 参数表示查询所需的行为集合。
        // 只要 query 中所有置 1 的 bit 都存在于当前 Mask 中，就说明 QuerySignature 是 BehaviourSignature 的子集。
        ReadOnlySpan<ulong> selfWords = _words;
        ReadOnlySpan<ulong> queryWords = query._words;

        for (int i = 0; i < queryWords.Length; i++)
        {
            ulong queryWord = queryWords[i];
            ulong selfWord = i < selfWords.Length ? selfWords[i] : 0UL;

            if ((selfWord & queryWord) != queryWord)
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(BehaviourMask other)
    {
        // other 参数表示另一个行为 Mask。
        // 两个 Mask 的 words 完全一致时，代表同一个事件集合。
        return _words.AsSpan().SequenceEqual(other._words);
    }

    public override bool Equals(object? obj)
    {
        // obj 参数是 object 形式的比较对象。
        // 只有它也是 BehaviourMask 且 words 一致时才返回 true。
        return obj is BehaviourMask other && Equals(other);
    }

    public override int GetHashCode()
    {
        // GetHashCode 只用于冷路径 Dictionary 或测试断言。
        // Post/Pump 热路径不依赖该 hash。
        var hash = new HashCode();

        foreach (ulong word in _words)
        {
            hash.Add(word);
        }

        return hash.ToHashCode();
    }

    private static ulong[] TrimTrailingZeroWords(ulong[] words)
    {
        // words 参数表示原始位集数组。
        // 去掉尾部 0 word 后，可以保证 Mask 相等判断不会受无意义容量影响。
        int length = words.Length;

        while (length > 0 && words[length - 1] == 0UL)
        {
            length--;
        }

        if (length == words.Length)
        {
            return words;
        }

        if (length == 0)
        {
            return Array.Empty<ulong>();
        }

        ulong[] trimmed = new ulong[length];
        Array.Copy(words, trimmed, length);
        return trimmed;
    }
}
```

```csharp
namespace LayerBase.Actor;

internal readonly struct BehaviourSignature : IEquatable<BehaviourSignature>
{
    public readonly BehaviourMask Mask;

    private readonly int[] _eventTypeIds;

    public BehaviourSignature(int[] eventTypeIds)
    {
        // eventTypeIds 参数表示 Actor 支持的事件类型 ID 集合。
        // 调用方必须传入已经排序并去重的数组。
        _eventTypeIds = eventTypeIds;

        // Mask 用于 Query 阶段的快速子集匹配。
        // Mask 使用 ulong[] words，不会把事件类型数量限制在 64 个以内。
        Mask = BehaviourMask.FromSortedEventIds(eventTypeIds);
    }

    public ReadOnlySpan<int> EventTypeIds => _eventTypeIds;

    public bool ContainsAll(BehaviourSignature query)
    {
        // query 参数表示查询签名。
        // 这里使用 Mask 判断 QuerySignature 是否为当前 BehaviourSignature 的子集。
        return Mask.ContainsAll(query.Mask);
    }

    public bool Equals(BehaviourSignature other)
    {
        // other 参数表示另一个行为签名。
        // Mask 相等意味着两者拥有完全相同的事件集合。
        return Mask.Equals(other.Mask);
    }

    public override bool Equals(object? obj)
    {
        // obj 参数是 object 形式的比较对象。
        // 只有它也是 BehaviourSignature 且 Mask 相等时才返回 true。
        return obj is BehaviourSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        // GetHashCode 只用于冷路径字典或测试断言。
        // Post/Pump 热路径不依赖它。
        return Mask.GetHashCode();
    }
}
```


### 7.6 ActorTypeMetaCache

```csharp
namespace LayerBase.Actor;

internal static class ActorTypeMetaCache<TActor>
    where TActor : class, IActor
{
    public static ActorTypeMeta<TActor>? Value;
}

internal static class ActorTypeMetaCache
{
    public static ActorTypeMeta<TActor> GetOrBuild<TActor>(
        IGeneratedActorMeta generated)
        where TActor : class, IActor
    {
        // generated 参数表示生成器补出的运行时能力接口。
        // ActorWorld 会用它调用 __BuildActorMeta。
        ActorTypeMeta<TActor>? cached = ActorTypeMetaCache<TActor>.Value;

        if (cached != null)
        {
            return cached;
        }

        var builder = new ActorTypeMetaBuilder();

        generated.__BuildActorMeta(builder);

        ActorTypeMeta<TActor> meta = builder.Build<TActor>();

        ActorTypeMetaCache<TActor>.Value = meta;

        return meta;
    }
}
```

---

## 8. 模块设计：Source Generator

### 8.1 文件

```text
LayerBase.Generator/LayerBase.Generator/ActorBehaviourGenerator.cs
LayerBase.Generator/LayerBase.Generator/ActorBehaviourDiagnostics.cs
```

### 8.2 Generator 职责

```text
1. 扫描 ClassDeclarationSyntax。
2. 找到带 [ActorBehaviour] 的方法。
3. 校验所在类是 partial。
4. 校验所在类实现 IActor。
5. 校验方法是实例方法。
6. 校验方法返回 void。
7. 校验方法只有一个参数。
8. 校验参数是 in TEvent。
9. 校验 TEvent 是 struct。
10. 校验同一个 Actor 类型内没有重复 TEvent。
11. 生成同名 partial class。
12. 让生成类实现 IGeneratedActorMeta。
13. 生成 __actorContext 字段。
14. 生成 GetId、ActorInit、Post、TryPost、__BuildActorMeta。
```

### 8.3 不做的事

```text
1. 不改用户方法体。
2. 不生成全局 Registry。
3. 不生成 ModuleInitializer。
4. 不生成 EnemyActor_ActorMetaGenerated。
5. 不要求用户手动注册 Actor 类型。
6. 不要求用户手动调用 ActorInit。
```

### 8.4 用户代码示例

```csharp
using LayerBase.Actor;

public sealed partial class EnemyActor : IActor
{
    private IActorView? _view;

    public void BindView(IActorView view)
    {
        // view 参数表示显示层适配对象。
        // 它可以包装 Unity GameObject、Godot Node 或测试中的 FakeView。
        _view = view;
    }

    [ActorBehaviour]
    private void OnDamage(in DamageEvent e)
    {
        // e 参数表示本次伤害事件。
        // 这里只写 Actor 视角下的响应逻辑，不关心 EventMail、slot、SOA 存储。
        _view?.Play("Hit");
    }

    [ActorBehaviour]
    private void OnDead(in DeadEvent e)
    {
        // e 参数表示本次死亡事件。
        // 这里可以播放死亡表现，也可以投递后续事件。
        _view?.Play("Dead");
    }
}
```

### 8.5 生成代码示例

```csharp
public sealed partial class EnemyActor : global::LayerBase.Actor.IGeneratedActorMeta
{
    private global::LayerBase.Actor.ActorContext __actorContext;

    global::LayerBase.Actor.ActorId global::LayerBase.Actor.IGeneratedActorMeta.GetId()
    {
        // __actorContext 是生成器生成的运行时上下文字段。
        // ActorWorld.CreateActor<TActor>() 会在分配 ActorId 后注入它。
        return __actorContext.ActorId;
    }

    void global::LayerBase.Actor.IGeneratedActorMeta.ActorInit(
        global::LayerBase.Actor.ActorContext context)
    {
        // context 参数由 ActorWorld 创建。
        // 它包含当前 Actor 所属 ActorWorld 和 ActorId。
        __actorContext = context;
    }

    global::LayerBase.Core.Event.PostResult global::LayerBase.Actor.IGeneratedActorMeta.Post<TEvent>(
        in TEvent value)
    {
        // value 参数是要投递给当前 Actor 的事件。
        // 这里只写入当前 Actor 的 EventMail，不直接调用 ActorBehaviour 方法。
        return __actorContext.Post(in value);
    }

    global::LayerBase.Core.Event.PostResult global::LayerBase.Actor.IGeneratedActorMeta.TryPost<TEvent>(
        in TEvent value)
    {
        // value 参数是要尝试投递给当前 Actor 的事件。
        // TryPost 需要把失败原因通过 PostResult 返回给调用方。
        return __actorContext.TryPost(in value);
    }

    void global::LayerBase.Actor.IGeneratedActorMeta.__BuildActorMeta(
        global::LayerBase.Actor.ActorTypeMetaBuilder builder)
    {
        // builder 参数用于收集当前 Actor 类型支持的行为方法。
        // 该方法只在 ActorTypeMeta 冷路径构建时调用。

        builder.AddBehaviour<EnemyActor, DamageEvent>(
            static (actor, in e) =>
            {
                // actor 参数是从 TypedActorStorage<EnemyActor>.Actors[slotIndex] 取出的实例。
                // e 参数是从 EventMail<DamageEvent> 出队得到的事件。
                actor.OnDamage(in e);
            });

        builder.AddBehaviour<EnemyActor, DeadEvent>(
            static (actor, in e) =>
            {
                // actor 参数是当前 EnemyActor 实例。
                // e 参数是从 EventMail<DeadEvent> 出队得到的事件。
                actor.OnDead(in e);
            });
    }
}
```

### 8.6 诊断规则

```text
LBACTOR001:
  ActorBehaviour 所在类型必须是 partial class。

LBACTOR002:
  ActorBehaviour 所在类型必须实现 IActor。

LBACTOR003:
  ActorBehaviour 方法不能是 static。

LBACTOR004:
  ActorBehaviour 方法必须返回 void。

LBACTOR005:
  ActorBehaviour 方法必须只有一个参数。

LBACTOR006:
  ActorBehaviour 参数必须是 in TEvent。

LBACTOR007:
  ActorBehaviour 的 TEvent 必须是 struct。

LBACTOR008:
  同一个 Actor 类型不能拥有两个相同 TEvent 的 ActorBehaviour。

LBACTOR009:
  用户不应手写 IGeneratedActorMeta。
```

---

## 9. 模块设计：Actor/Storage

### 9.1 ActorWorld

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

public sealed partial class ActorWorld
{
    private BehaviourArchetype[] _archetypes = Array.Empty<BehaviourArchetype>();

    private readonly Dictionary<BehaviourSignature, BehaviourArchetype> _archetypeMap = new();

    private IActorEventBucket[] _eventBucketsByEventId = Array.Empty<IActorEventBucket>();

    private int _bucketCursor;

    internal LayerRuntime Runtime { get; }

    public ActorWorld(LayerRuntime runtime)
    {
        // runtime 参数表示当前 ActorWorld 所属的 LayerRuntime。
        // 每个 LayerRuntime 持有一个 ActorWorld，避免多世界 ActorId 混用。
        Runtime = runtime;
    }
}
```

### 9.2 CreateActor

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public TActor CreateActor<TActor>()
        where TActor : class, IActor, new()
    {
        // 创建用户 Actor 逻辑对象。
        // 用户不需要手动 ActorInit。
        TActor actor = new TActor();

        // 获取生成器补出的运行时能力。
        // 如果用户没有 partial 或生成器没有生效，这里会立刻失败。
        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);

        // 获取或构建 Actor 类型元数据。
        // 构建只发生在冷路径，之后走泛型静态缓存。
        ActorTypeMeta<TActor> meta = ActorTypeMetaCache.GetOrBuild<TActor>(generated);

        // 根据 BehaviourSignature 找到或创建 BehaviourArchetype。
        // 冷路径允许查 Dictionary。
        BehaviourArchetype archetype = GetOrCreateArchetype(meta.Signature);

        // 在 BehaviourArchetype 内找到或创建当前 TActor 的强类型存储。
        // 同一签名下不同 Actor 类型拥有不同 TypedActorStorage<TActor>。
        TypedActorStorage<TActor> storage = archetype.GetOrCreateStorage<TActor>(
            meta: meta,
            world: this);

        // 分配 Actor slot。
        // slotIndex 会同时用于 Actors[] 和 EventMail[]。
        int slotIndex = storage.AllocateSlot(actor);

        // 创建 ActorId。
        // ActorId 是后续 Post 的直接定位句柄。
        ActorId actorId = new ActorId(
            archetypeId: archetype.ArchetypeId,
            typeStorageIndex: storage.TypeStorageIndex,
            slotIndex: slotIndex,
            generation: storage.GetGeneration(slotIndex));

        // 创建并注入 ActorContext。
        // ActorContext 保存 ActorWorld 和 ActorId。
        var context = new ActorContext(this, actorId);
        generated.ActorInit(context);

        return actor;
    }

    private BehaviourArchetype GetOrCreateArchetype(BehaviourSignature signature)
    {
        // signature 参数表示 Actor 支持的事件类型集合。
        // 该方法属于冷路径，允许使用 Dictionary 查找。
        if (_archetypeMap.TryGetValue(signature, out BehaviourArchetype? existing))
        {
            return existing;
        }

        int archetypeId = _archetypes.Length;

        var archetype = new BehaviourArchetype(
            archetypeId: archetypeId,
            signature: signature);

        Array.Resize(ref _archetypes, archetypeId + 1);
        _archetypes[archetypeId] = archetype;

        _archetypeMap.Add(signature, archetype);

        InvalidateQueryCache();

        return archetype;
    }

    private void InvalidateQueryCache()
    {
        // 当前阶段可以先留空。
        // QueryCache 在 Phase 7 引入后，这里负责使缓存失效。
    }
}
```

### 9.3 ActorWorld.Post

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

public sealed partial class ActorWorld
{
    public PostResult Post<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = default,
        ActorMailFullPolicy? fullPolicy = default)
        where TEvent : struct
    {
        // actorId 参数表示目标 Actor。
        // value 参数表示要投递的事件。
        // postPolicy 参数表示本次投递策略覆盖项。
        // fullPolicy 参数表示本次邮箱满策略覆盖项。

        PostResult result = TryPost(
            actorId: actorId,
            value: in value,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);

        return result;
    }

    public PostResult TryPost<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy = default,
        ActorMailFullPolicy? fullPolicy = default)
        where TEvent : struct
    {
        // actorId 参数表示目标 Actor。
        // 该方法不会查 ActorId -> Actor 字典，而是直接通过数组下标定位。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return PostResult.Failure("Invalid ActorId.ArchetypeId.");
        }

        BehaviourArchetype archetype = _archetypes[actorId.ArchetypeId];

        return archetype.Post(
            actorId: actorId,
            value: in value,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);
    }

    public void PostMany<TEvent>(
        ReadOnlySpan<ActorId> actorIds,
        in TEvent value,
        ActorPostPolicy? postPolicy = default,
        ActorMailFullPolicy? fullPolicy = default)
        where TEvent : struct
    {
        // actorIds 参数是业务系统已经找出的目标 ActorId 列表。
        // ActorWorld 不负责 AABB、空间索引、阵营过滤或技能目标选择。
        // value 参数是要投递给这些 Actor 的事件。
        foreach (ActorId actorId in actorIds)
        {
            _ = TryPost(
                actorId: actorId,
                value: in value,
                postPolicy: postPolicy,
                fullPolicy: fullPolicy);
        }
    }
}
```

### 9.4 BehaviourArchetype

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

internal sealed class BehaviourArchetype
{
    private TypedStorageRuntime[] _storages = Array.Empty<TypedStorageRuntime>();

    private readonly Dictionary<Type, ushort> _storageIndexByType = new();

    public int ArchetypeId { get; }

    public BehaviourSignature Signature { get; }

    public BehaviourArchetype(
        int archetypeId,
        BehaviourSignature signature)
    {
        // archetypeId 参数表示当前 BehaviourArchetype 在 ActorWorld._archetypes 中的下标。
        ArchetypeId = archetypeId;

        // signature 参数表示该 Archetype 支持的事件集合。
        Signature = signature;
    }

    public TypedActorStorage<TActor> GetOrCreateStorage<TActor>(
        ActorTypeMeta<TActor> meta,
        ActorWorld world)
        where TActor : class, IActor
    {
        // meta 参数表示当前 Actor 类型的行为元数据。
        // world 参数用于把新建 EventColumn 注册到全局 ActorEventBucket。
        Type actorType = typeof(TActor);

        if (_storageIndexByType.TryGetValue(actorType, out ushort existingIndex))
        {
            return (TypedActorStorage<TActor>)_storages[existingIndex];
        }

        ushort storageIndex = checked((ushort)_storages.Length);

        var storage = new TypedActorStorage<TActor>(
            typeStorageIndex: storageIndex,
            maxEventTypeId: EventTypeIdAllocator.MaxId,
            initialCapacity: 4);

        storage.BuildColumns(
            meta: meta,
            world: world);

        Array.Resize(ref _storages, storageIndex + 1);
        _storages[storageIndex] = storage;

        _storageIndexByType.Add(actorType, storageIndex);

        return storage;
    }

    public PostResult Post<TEvent>(
        ActorId actorId,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        // actorId 参数表示目标 Actor 的定位信息。
        // value 参数表示要投递的事件。
        ushort storageIndex = actorId.TypeStorageIndex;

        if ((uint)storageIndex >= (uint)_storages.Length)
        {
            return PostResult.Failure("Invalid ActorId.TypeStorageIndex.");
        }

        TypedStorageRuntime storage = _storages[storageIndex];

        if (!storage.IsAlive(actorId.SlotIndex, actorId.Generation))
        {
            return PostResult.Failure("ActorId is stale or actor slot is not alive.");
        }

        return storage.Post(
            slotIndex: actorId.SlotIndex,
            value: in value,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);
    }
}
```

### 9.5 TypedStorageRuntime

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

internal abstract class TypedStorageRuntime
{
    public abstract bool IsAlive(int slotIndex, int generation);

    public abstract PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct;
}
```

### 9.6 TypedActorStorage

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

internal sealed class TypedActorStorage<TActor> : TypedStorageRuntime
    where TActor : class, IActor
{
    private ActorEventColumnRuntime[] _columnsByEventId;

    private TActor?[] _actors;

    private int[] _generations;

    private ActorSlotFreeList _freeList;

    public ushort TypeStorageIndex { get; }

    public TActor?[] Actors => _actors;

    public TypedActorStorage(
        ushort typeStorageIndex,
        int maxEventTypeId,
        int initialCapacity)
    {
        // typeStorageIndex 参数表示当前 storage 在 BehaviourArchetype 内的下标。
        TypeStorageIndex = typeStorageIndex;

        // maxEventTypeId 参数表示当前已分配的最大事件类型 ID。
        // 该数组用于 Post 热路径通过 EventTypeId<TEvent>.Id 直接索引 EventColumn。
        _columnsByEventId = new ActorEventColumnRuntime[maxEventTypeId + 1];

        // initialCapacity 参数表示初始 Actor 数组容量。
        _actors = new TActor?[initialCapacity];
        _generations = new int[initialCapacity];
        _freeList = new ActorSlotFreeList(initialCapacity);
    }

    public int AllocateSlot(TActor actor)
    {
        // actor 参数是要写入 storage 的 Actor 实例。
        // slotIndex 会同时用于 Actors[] 和所有 EventColumn 的 EventMail[]。
        int slotIndex = _freeList.TryPop(out int freeSlot)
            ? freeSlot
            : AllocateNewSlot();

        _actors[slotIndex] = actor;

        return slotIndex;
    }

    public int GetGeneration(int slotIndex)
    {
        // slotIndex 参数表示 Actor 在 _actors 中的位置。
        // Generation 用于识别旧 ActorId。
        return _generations[slotIndex];
    }

    public override bool IsAlive(int slotIndex, int generation)
    {
        // slotIndex 参数来自 ActorId。
        // generation 参数来自 ActorId。
        return (uint)slotIndex < (uint)_actors.Length
               && _actors[slotIndex] != null
               && _generations[slotIndex] == generation;
    }

    public override PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct
    {
        // slotIndex 参数表示目标 Actor 在 _actors 中的位置。
        // value 参数表示要投递的事件。
        int eventId = EventTypeId<TEvent>.Id;

        if ((uint)eventId >= (uint)_columnsByEventId.Length)
        {
            return PostResult.Failure("Invalid event type id.");
        }

        ActorEventColumnRuntime runtime = _columnsByEventId[eventId];

        if (runtime == null)
        {
            return PostResult.Failure(
                $"Actor type {typeof(TActor).Name} does not support event {typeof(TEvent).Name}.");
        }

        var column = (EventColumn<TActor, TEvent>)runtime;

        return column.Post(
            slotIndex: slotIndex,
            value: in value,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);
    }

    public void BuildColumns(
        ActorTypeMeta<TActor> meta,
        ActorWorld world)
    {
        // meta 参数表示当前 TActor 的行为元数据。
        // world 参数用于注册 EventColumn 到 ActorEventBucket。
        foreach (ActorBehaviourEntry entry in meta.Behaviours)
        {
            BuildColumnFromEntry(entry, world);
        }
    }

    private void BuildColumnFromEntry(
        ActorBehaviourEntry entry,
        ActorWorld world)
    {
        // entry 参数表示一个 ActorBehaviour 元数据项。
        // world 参数用于注册新建的 EventColumn。
        // 这里需要通过泛型辅助方法完成强类型 EventColumn 创建。
        throw new NotImplementedException("Use generated or reflection-free typed bridge in implementation.");
    }

    private int AllocateNewSlot()
    {
        // 分配新 slot。
        // 如果当前数组容量不够，需要扩容 Actor 数组、Generation 数组和所有 EventColumn 的 Mail 数组。
        int slotIndex = _actors.Length;

        Array.Resize(ref _actors, slotIndex + 1);
        Array.Resize(ref _generations, slotIndex + 1);

        return slotIndex;
    }
}
```

实现注意：

```text
BuildColumnFromEntry 是设计占位点。
第一版可以在 ActorBehaviourEntry 中保存一个冷路径工厂委托，避免这里用反射。
不要把反射放进 Post/Pump 热路径。
```

### 9.7 ActorSlotFreeList

```csharp
namespace LayerBase.Actor;

internal struct ActorSlotFreeList
{
    private int[] _items;

    private int _count;

    public ActorSlotFreeList(int initialCapacity)
    {
        // initialCapacity 参数表示 FreeList 初始容量。
        // FreeList 只保存被释放后可复用的 slot 下标。
        _items = new int[Math.Max(initialCapacity, 4)];
        _count = 0;
    }

    public bool TryPop(out int slotIndex)
    {
        // slotIndex 返回可复用的 slot 下标。
        // 返回 false 表示没有空闲 slot，需要从数组末尾新分配。
        if (_count == 0)
        {
            slotIndex = default;
            return false;
        }

        _count--;
        slotIndex = _items[_count];
        return true;
    }

    public void Push(int slotIndex)
    {
        // slotIndex 参数表示释放后的 slot 下标。
        // 它会在下次 AllocateSlot 时被复用。
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[_count] = slotIndex;
        _count++;
    }
}
```

---

## 10. 模块设计：Actor/Mail

### 10.1 ActorPostPolicy

```csharp
namespace LayerBase.Actor;

public enum ActorPostPolicy
{
    Queued,
    Latest,
    Coalesced,
    Dirty
}
```

说明：

```text
Queued：
  普通排队，按 FIFO 顺序处理。

Latest：
  只保留最后一个事件。

Coalesced：
  合并多个同类事件。
  该策略需要 EventMetaData 提供合并器，建议 Phase 6 后实现。

Dirty：
  只标记需要处理一次。
  适合无 payload 或只关心“发生过”的事件。
```

### 10.2 ActorMailFullPolicy

```csharp
namespace LayerBase.Actor;

public enum ActorMailFullPolicy
{
    Grow,
    RejectNew,
    DropOldest,
    DropNewest
}
```

说明：

```text
Grow：
  尝试扩容邮箱 buffer。

RejectNew：
  拒绝新事件。

DropOldest：
  丢弃最旧事件，保留新事件。

DropNewest：
  丢弃新事件，保留旧事件。
```

### 10.3 ActorMailOptions

```csharp
namespace LayerBase.Actor;

public readonly struct ActorMailOptions
{
    public static ActorMailOptions Default => new ActorMailOptions(
        postPolicy: ActorPostPolicy.Queued,
        fullPolicy: ActorMailFullPolicy.Grow,
        growFailurePolicy: ActorMailFullPolicy.RejectNew,
        initialCapacity: 4,
        maxCapacity: 64,
        growFactor: 2,
        releaseWhenEmpty: true);

    public readonly ActorPostPolicy PostPolicy;
    public readonly ActorMailFullPolicy FullPolicy;
    public readonly ActorMailFullPolicy GrowFailurePolicy;
    public readonly int InitialCapacity;
    public readonly int MaxCapacity;
    public readonly int GrowFactor;
    public readonly bool ReleaseWhenEmpty;

    public ActorMailOptions(
        ActorPostPolicy postPolicy,
        ActorMailFullPolicy fullPolicy,
        ActorMailFullPolicy growFailurePolicy,
        int initialCapacity,
        int maxCapacity,
        int growFactor,
        bool releaseWhenEmpty)
    {
        // postPolicy 参数决定事件如何进入邮箱。
        PostPolicy = postPolicy;

        // fullPolicy 参数决定邮箱满时优先采取的策略。
        FullPolicy = fullPolicy;

        // growFailurePolicy 参数决定 Grow 达到 MaxCapacity 后的兜底策略。
        GrowFailurePolicy = growFailurePolicy;

        // initialCapacity 参数决定第一次租用 RingQueue 的容量。
        InitialCapacity = initialCapacity;

        // maxCapacity 参数决定单 Actor 单事件邮箱最大容量。
        MaxCapacity = maxCapacity;

        // growFactor 参数决定扩容倍率。
        GrowFactor = growFactor;

        // releaseWhenEmpty 参数决定邮箱空后是否归还 buffer。
        ReleaseWhenEmpty = releaseWhenEmpty;
    }
}
```

### 10.4 EventMail

```csharp
namespace LayerBase.Actor;

internal struct EventMail<TEvent>
    where TEvent : struct
{
    public int BufferId;
    public int Head;
    public int Count;
    public int Capacity;
}
```

字段说明：

```text
BufferId：
  当前邮箱租用的 RingQueue buffer 编号。
  0 表示没有真实 buffer。

Head：
  环形队列队首位置。

Count：
  当前邮箱中待处理事件数量。

Capacity：
  当前 buffer 容量。
```

### 10.5 DirtySlotList

```csharp
namespace LayerBase.Actor;

internal sealed class DirtySlotList
{
    private int[] _items = new int[4];

    private int _head;

    private int _count;

    private HashSet<int>? _containsSlowSet;

    public void AddIfNotExists(int slotIndex)
    {
        // slotIndex 参数表示邮箱变为非空的 Actor slot。
        // 同一个 slot 在邮箱清空前只应该进入 DirtySlotList 一次。
        if (Contains(slotIndex))
        {
            return;
        }

        EnsureCapacity(_count + 1);

        int tail = (_head + _count) % _items.Length;
        _items[tail] = slotIndex;
        _count++;

        _containsSlowSet?.Add(slotIndex);
    }

    public bool TryPeek(out int slotIndex)
    {
        // slotIndex 返回当前等待处理的 Actor slot。
        // 返回 false 表示当前没有脏邮箱。
        if (_count == 0)
        {
            slotIndex = default;
            return false;
        }

        slotIndex = _items[_head];
        return true;
    }

    public void Pop()
    {
        // 移除当前队首 slot。
        // 通常在该 slot 的邮箱清空后调用。
        if (_count == 0)
        {
            return;
        }

        int slotIndex = _items[_head];

        _head = (_head + 1) % _items.Length;
        _count--;

        _containsSlowSet?.Remove(slotIndex);
    }

    private bool Contains(int slotIndex)
    {
        // slotIndex 参数表示要检查的 Actor slot。
        // MVP 可先用 HashSet 保证正确性，后续再改成 slot flag 数组。
        _containsSlowSet ??= new HashSet<int>();
        return _containsSlowSet.Contains(slotIndex);
    }

    private void EnsureCapacity(int required)
    {
        // required 参数表示本次写入需要的最小容量。
        // 容量不足时扩容并把环形数据整理成线性数组。
        if (required <= _items.Length)
        {
            return;
        }

        int[] newItems = new int[_items.Length * 2];

        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }

        _items = newItems;
        _head = 0;
    }
}
```

说明：

```text
DirtySlotList 第一版允许 HashSet 参与去重。
这是 Pump 热路径的一部分，后续可用 slot flag 数组替代。
但为了 MVP 正确性，先保守实现。
```

如果追求更严格热路径规则，应改成：

```text
EventMail<TEvent>.IsDirty
```

或：

```text
bool[] _dirtyFlags
```

### 10.6 EventColumn

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

internal abstract class ActorEventColumnRuntime
{
    public abstract void EnsureSlotCapacity(int slotIndex);
}

internal interface IActorEventColumn<TEvent>
    where TEvent : struct
{
    bool PumpOne(ref RuntimeFrameBudget budget);
}

internal sealed class EventColumn<TActor, TEvent> :
    ActorEventColumnRuntime,
    IActorEventColumn<TEvent>
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly TypedActorStorage<TActor> _owner;

    private readonly ActorBehaviourInvoker<TActor, TEvent> _invoker;

    private readonly RingQueueBuffer<TEvent> _bufferPool;

    private readonly DirtySlotList _dirtySlots;

    private readonly ActorMailOptions _options;

    private EventMail<TEvent>[] _mails;

    public EventColumn(
        TypedActorStorage<TActor> owner,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        ActorMailOptions options,
        int initialSlotCapacity)
    {
        // owner 参数表示当前事件列所属的强类型 Actor 存储。
        _owner = owner;

        // invoker 参数表示 Source Generator 生成的强类型行为调用器。
        _invoker = invoker;

        // options 参数表示当前事件类型的 Actor 邮箱配置。
        _options = options;

        // initialSlotCapacity 参数表示初始 slot 容量。
        _mails = new EventMail<TEvent>[initialSlotCapacity];

        // _bufferPool 管理 TEvent 的真实环形队列 buffer。
        _bufferPool = new RingQueueBuffer<TEvent>(
            initialCapacity: options.InitialCapacity,
            maxCapacity: options.MaxCapacity);

        // _dirtySlots 记录哪些 slot 的邮箱非空。
        _dirtySlots = new DirtySlotList();
    }

    public PostResult Post(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
    {
        // slotIndex 参数表示目标 Actor 在 TActor[] 中的位置。
        // value 参数表示要写入邮箱的事件。
        EnsureSlotCapacity(slotIndex);

        return EventMailWriter.Enqueue(
            mail: ref _mails[slotIndex],
            value: in value,
            bufferPool: _bufferPool,
            dirtySlots: _dirtySlots,
            slotIndex: slotIndex,
            options: _options,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);
    }

    public bool PumpOne(ref RuntimeFrameBudget budget)
    {
        // budget 参数表示 LayerRuntime 本帧剩余预算。
        // 每处理一个 Actor 事件都必须 ConsumeEvent。
        if (!_dirtySlots.TryPeek(out int slotIndex))
        {
            return false;
        }

        ref EventMail<TEvent> mail = ref _mails[slotIndex];

        if (!EventMailReader.TryDequeue(
            mail: ref mail,
            bufferPool: _bufferPool,
            value: out TEvent value))
        {
            _dirtySlots.Pop();
            return false;
        }

        TActor? actor = _owner.Actors[slotIndex];

        if (actor == null)
        {
            _dirtySlots.Pop();
            return false;
        }

        // ActorBehaviour 异常不在这里吞掉。
        // 游戏实体逻辑错误应该立刻暴露。
        _invoker(actor, in value);

        budget.ConsumeEvent();

        if (mail.Count == 0)
        {
            _dirtySlots.Pop();

            EventMailReader.ReleaseIfEmpty(
                mail: ref mail,
                bufferPool: _bufferPool,
                options: _options);
        }

        return true;
    }

    public override void EnsureSlotCapacity(int slotIndex)
    {
        // slotIndex 参数表示要访问的 EventMail 下标。
        // Actor 数组扩容后，EventColumn 的邮箱数组也需要能覆盖该 slot。
        if ((uint)slotIndex < (uint)_mails.Length)
        {
            return;
        }

        int newSize = _mails.Length == 0 ? 4 : _mails.Length;

        while (newSize <= slotIndex)
        {
            newSize *= 2;
        }

        Array.Resize(ref _mails, newSize);
    }
}
```

### 10.7 ActorEventBucket

```csharp
namespace LayerBase.Actor;

internal interface IActorEventBucket
{
    bool PumpOne(ref RuntimeFrameBudget budget);
}

internal sealed class ActorEventBucket<TEvent> : IActorEventBucket
    where TEvent : struct
{
    private IActorEventColumn<TEvent>[] _columns = Array.Empty<IActorEventColumn<TEvent>>();

    private int _cursor;

    public void AddColumn(IActorEventColumn<TEvent> column)
    {
        // column 参数表示某个 TActor 对 TEvent 的事件列。
        // 该方法只在冷路径构建阶段调用。
        int oldLength = _columns.Length;
        Array.Resize(ref _columns, oldLength + 1);
        _columns[oldLength] = column;
    }

    public bool PumpOne(ref RuntimeFrameBudget budget)
    {
        // budget 参数表示 LayerRuntime 本帧剩余预算。
        // ActorEventBucket 在多个 Column 之间轮询，避免单个 Column 长期占用预算。
        IActorEventColumn<TEvent>[] columns = _columns;

        if (columns.Length == 0)
        {
            return false;
        }

        int checkedCount = 0;

        while (checkedCount < columns.Length)
        {
            int index = _cursor;

            _cursor = index + 1 == columns.Length ? 0 : index + 1;
            checkedCount++;

            IActorEventColumn<TEvent> column = columns[index];

            if (column.PumpOne(ref budget))
            {
                return true;
            }
        }

        return false;
    }
}
```

---

## 11. 模块设计：Actor/Pump

### 11.1 RuntimeFrameBudget

```csharp
namespace LayerBase.Actor;

public ref struct RuntimeFrameBudget
{
    public int MaxEvents;
    public int UsedEvents;
    public long DeadlineTicks;

    public RuntimeFrameBudget(
        int maxEvents,
        int usedEvents,
        long deadlineTicks)
    {
        // maxEvents 参数表示本帧最多允许处理多少个事件。
        // maxEvents <= 0 表示不限制事件数量。
        MaxEvents = maxEvents;

        // usedEvents 参数表示本帧已经处理的事件数量。
        // ActorWorld 会在 PostScheduler 之后使用剩余预算。
        UsedEvents = usedEvents;

        // deadlineTicks 参数表示本帧事件处理截止时间。
        // deadlineTicks <= 0 表示不限制时间。
        DeadlineTicks = deadlineTicks;
    }

    public bool HasRemainingEventBudget()
    {
        // 返回 true 表示仍有事件数量预算。
        // MaxEvents <= 0 时表示不限制数量。
        return MaxEvents <= 0 || UsedEvents < MaxEvents;
    }

    public bool HasRemainingTimeBudget(long nowTicks)
    {
        // nowTicks 参数表示当前 Stopwatch.GetTimestamp()。
        // DeadlineTicks <= 0 时表示不限制时间。
        return DeadlineTicks <= 0 || nowTicks < DeadlineTicks;
    }

    public void ConsumeEvent()
    {
        // 每处理一个 Actor 事件后调用一次。
        // 它用于和 LayerBase PostScheduler 共享事件预算。
        UsedEvents++;
    }
}
```

### 11.2 ActorWorld.Pump

```csharp
namespace LayerBase.Actor;

using System.Diagnostics;

public sealed partial class ActorWorld
{
    public void Pump(ref RuntimeFrameBudget budget)
    {
        // budget 参数表示 LayerRuntime 本帧剩余预算。
        // ActorWorld 不拥有独立帧预算。
        while (budget.HasRemainingEventBudget())
        {
            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                return;
            }

            if (!TryGetNextEventBucket(out IActorEventBucket? bucket))
            {
                return;
            }

            bool processed = bucket.PumpOne(ref budget);

            if (!processed)
            {
                AdvanceBucketCursor();
            }
        }
    }

    private bool TryGetNextEventBucket(out IActorEventBucket? bucket)
    {
        // bucket 参数返回当前轮询到的 ActorEventBucket。
        // 返回 false 表示当前没有可用事件桶。
        IActorEventBucket[] buckets = _eventBucketsByEventId;

        if (buckets.Length == 0)
        {
            bucket = null;
            return false;
        }

        int checkedCount = 0;

        while (checkedCount < buckets.Length)
        {
            int index = _bucketCursor;
            IActorEventBucket? current = buckets[index];

            if (current != null)
            {
                bucket = current;
                return true;
            }

            _bucketCursor = index + 1 == buckets.Length ? 0 : index + 1;
            checkedCount++;
        }

        bucket = null;
        return false;
    }

    private void AdvanceBucketCursor()
    {
        // 当前事件桶没有处理任何事件时，移动全局事件桶游标。
        if (_eventBucketsByEventId.Length == 0)
        {
            return;
        }

        _bucketCursor = _bucketCursor + 1 == _eventBucketsByEventId.Length
            ? 0
            : _bucketCursor + 1;
    }
}
```

---

## 12. LayerRuntime 接入设计

### 12.1 修改目标

修改文件：

```text
LayerBase/Application/LayerRuntime.cs
```

新增：

```csharp
public ActorWorld Actors { get; }
```

构造时创建：

```csharp
Actors = new ActorWorld(this);
```

Pump 时插入：

```text
Timer
Delay
Completion
PostScheduler
ActorWorld
LayerChain
```

### 12.2 接入代码示例

```csharp
public ActorWorld Actors { get; }

internal LayerRuntime(int id)
{
    // id 参数表示当前 LayerRuntime 的世界编号。
    // LayerHub 使用它区分多世界运行时。
    _id = id;

    // EventCenter 负责 LayerBase 原有的同步事件发送。
    EventCenter = new EventCenter();

    // Actors 是当前 Runtime 独占的 ActorWorld。
    // 每个 Runtime 一个 ActorWorld，避免不同世界的 ActorId 混用。
    Actors = new ActorWorld(this);

    LayerHub.Internal_Register(this);
}

public void Pump(float deltaTime)
{
    // deltaTime 参数表示本帧逻辑更新间隔。
    // 它会继续传给 LayerChain.Pump，让 IUpdate 服务照常执行。
    if (_disposed)
    {
        return;
    }

    if (_context != null)
    {
        using var scope = _context.EnterScope();

        _timer?.Tick(deltaTime, _timerSink!);

        if (_chain != null && _chain.HasAnyDelay)
        {
            DelayManager?.Tick(deltaTime);
        }

        var policy = IsDebugMode
            ? CompletionExceptionPolicy.Throw
            : CompletionExceptionPolicy.ReportAndContinue;

        _context.Update(
            maxCompletions: Scheduler.Options.MaxCompletionsPerPump,
            exceptionPolicy: policy,
            errorReporter: ReportCompletionError);

        PostPumpStats postStats = _scheduler?.Pump()
            ?? new PostPumpStats(0, 0, 0, 0);

        RuntimeFrameBudget actorBudget = CreateActorBudget(
            options: Scheduler.Options,
            postStats: postStats);

        Actors.Pump(ref actorBudget);

        _chain?.Pump(deltaTime);
    }
    else
    {
        _timer?.Tick(deltaTime, _timerSink!);

        if (_chain != null && _chain.HasAnyDelay)
        {
            DelayManager?.Tick(deltaTime);
        }

        PostPumpStats postStats = _scheduler?.Pump()
            ?? new PostPumpStats(0, 0, 0, 0);

        RuntimeFrameBudget actorBudget = CreateActorBudget(
            options: Scheduler.Options,
            postStats: postStats);

        Actors.Pump(ref actorBudget);

        _chain?.Pump(deltaTime);
    }
}

private static RuntimeFrameBudget CreateActorBudget(
    PostSchedulerOptions options,
    PostPumpStats postStats)
{
    // options 参数表示 PostScheduler 的帧预算配置。
    // ActorWorld 第一版复用该配置，不新增独立配置。
    // postStats 参数表示 PostScheduler 本帧已经处理的事件统计。
    // ActorWorld 只能消费剩余事件预算。

    long deadlineTicks = 0;

    if (options.MaxMillisecondsPerPump > 0)
    {
        long budgetTicks = (long)(
            Stopwatch.Frequency * options.MaxMillisecondsPerPump / 1000.0);

        deadlineTicks = Stopwatch.GetTimestamp() + budgetTicks;
    }

    return new RuntimeFrameBudget(
        maxEvents: options.MaxEventsPerPump,
        usedEvents: postStats.ProcessedCount,
        deadlineTicks: deadlineTicks);
}
```

### 12.3 后续更精确方案

第一版的 `CreateActorBudget` 是“PostScheduler 执行后重新计算 ActorWorld 时间预算”。  
更精确的方案是：

```text
LayerRuntime 创建 RuntimeFrameBudget。
PostScheduler.Pump(ref budget)。
ActorWorld.Pump(ref budget)。
```

但这需要改动 PostScheduler 签名。  
建议在 Phase 6 或 Phase 8 后再做，不要阻塞 MVP。

---

## 13. EventMetaData 集成

### 13.1 修改目标

修改文件：

```text
LayerBase/Event/EventMetaData/EventMetaData.cs
LayerBase/Event/PostScheduler/EventRuntimePolicyTable.cs
LayerBase/Application/LayerRuntime.cs
```

### 13.2 EventMetaData<TEvent> 新增方法

```csharp
public abstract class EventMetaData<TEvent> : IEventMetaData
    where TEvent : struct
{
    public virtual ActorMailOptions? GetActorMailOptions()
    {
        // 默认返回 null 表示该事件没有专门的 Actor 邮箱配置。
        // ActorWorld 创建 EventColumn 时会使用 ActorMailOptions.Default。
        return null;
    }
}
```

### 13.3 EventRuntimePolicyTable 新增缓存

```csharp
public sealed class EventRuntimePolicyTable
{
    private ActorMailOptions?[] _actorMailOptionsByEventId =
        Array.Empty<ActorMailOptions?>();

    public void SetActorMailOptions(
        int eventTypeId,
        ActorMailOptions options)
    {
        // eventTypeId 参数表示 EventTypeId<TEvent>.Id。
        // options 参数表示该事件在 Actor 邮箱中的默认配置。
        EnsureActorMailCapacity(eventTypeId);
        _actorMailOptionsByEventId[eventTypeId] = options;
    }

    public ActorMailOptions GetActorMailOptions(int eventTypeId)
    {
        // eventTypeId 参数表示 EventTypeId<TEvent>.Id。
        // 返回值用于 EventColumn 创建时缓存。
        if ((uint)eventTypeId >= (uint)_actorMailOptionsByEventId.Length)
        {
            return ActorMailOptions.Default;
        }

        return _actorMailOptionsByEventId[eventTypeId]
               ?? ActorMailOptions.Default;
    }

    private void EnsureActorMailCapacity(int eventTypeId)
    {
        // eventTypeId 参数表示需要写入的事件类型 ID。
        // 数组容量不足时扩容，保证后续可直接下标访问。
        if ((uint)eventTypeId < (uint)_actorMailOptionsByEventId.Length)
        {
            return;
        }

        int newSize = _actorMailOptionsByEventId.Length == 0
            ? 8
            : _actorMailOptionsByEventId.Length;

        while (newSize <= eventTypeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref _actorMailOptionsByEventId, newSize);
    }
}
```

### 13.4 规则

```text
1. EventMetaData 只在 Runtime 构建或策略重建时读取。
2. EventColumn 创建时读取 ActorMailOptions。
3. EventColumn 内部缓存 ActorMailOptions。
4. Post 热路径不访问 EventMetaData。
5. Pump 热路径不访问 EventMetaData。
```

---

## 14. Query 设计

### 14.1 第一版不做 Query

MVP 阶段只做：

```text
CreateActor
Post
PostMany
Pump
```

Query 放到 Phase 7。

### 14.2 Query 语义

```csharp
runtime.Actors.QueryActor<DamageEvent>();
runtime.Actors.QueryActor<DamageEvent, DeadEvent>();
```

语义：

```text
QuerySignature ⊆ BehaviourSignature
```

例如：

```text
QueryActor<DamageEvent>()
```

应该命中：

```text
ActorA: { DamageEvent }
ActorB: { DamageEvent, DeadEvent }
ActorC: { DamageEvent, StunEvent }
```

不命中：

```text
ActorD: { DeadEvent }
```

### 14.3 Query 必须基于 Mask

Query 的匹配不再通过遍历 `int[] EventTypeIds` 完成，而是通过 `BehaviourSignature.Mask` 完成。

核心判断：

```csharp
private static bool IsMatch(
    BehaviourSignature archetypeSignature,
    BehaviourSignature querySignature)
{
    // archetypeSignature 参数表示某个 BehaviourArchetype 支持的完整行为集合。
    // querySignature 参数表示本次 QueryActor<...>() 需要的行为集合。
    // 当 querySignature 的所有 bit 都存在于 archetypeSignature 中时，说明该 Archetype 命中查询。
    return archetypeSignature.Mask.ContainsAll(querySignature.Mask);
}
```

等价位运算语义：

```text
(archetypeMask & queryMask) == queryMask
```

但实现上使用 `ulong[] words`，不是固定 `ulong`，因此不会把 ActorBehaviour 事件类型限制在 64 个以内。

### 14.4 QueryCache 构建

QueryCache 构建时可以扫描 `_archetypes`，但匹配必须走 Mask：

```csharp
private ActorQueryCache BuildQueryCache(BehaviourSignature querySignature)
{
    // querySignature 参数表示查询所需的行为集合。
    // 例如 QueryActor<DamageEvent, DeadEvent>() 会生成包含两个事件 ID 的签名。
    var matched = new List<BehaviourArchetype>();

    foreach (BehaviourArchetype archetype in _archetypes)
    {
        // archetype.Signature.ContainsAll(querySignature) 使用 Mask 做子集判断。
        // 只要当前 Archetype 支持 querySignature 的所有事件，就加入缓存。
        if (archetype.Signature.ContainsAll(querySignature))
        {
            matched.Add(archetype);
        }
    }

    return new ActorQueryCache(
        querySignature: querySignature,
        archetypes: matched.ToArray());
}
```

说明：

```text
1. BuildQueryCache 属于 Query 冷路径，可以扫描 Archetype 数组。
2. 子集匹配必须基于 BehaviourMask。
3. QueryCache 构建完成后，Query 热路径不应重复扫描所有 Archetype。
```

### 14.5 Query 热路径规则

```text
1. QueryActor<TEvent>().PostAll() 不构造 IEnumerable<IActor>。
2. PostAll 直接遍历 QueryCache 中匹配的 EventColumn 或 Archetype runtime view。
3. Debug 路径可以提供 Actors 枚举。
4. QueryCache 在新增 BehaviourArchetype 后失效。
5. Query 匹配依据 BehaviourSignature.Mask，而不是 int[] 线性集合匹配。
```

---

## 15. 阶段拆分

## Phase 0：实现计划文档

新增：

```text
docs/actor/actor-runtime-design.md
```

不改生产代码。

DoD：

```text
1. 文档明确模块边界。
2. 文档明确阶段拆分。
3. 文档明确禁止项。
4. 文档明确测试计划。
```

---

## Phase 1：Actor Core + Meta

新增：

```text
LayerBase/Actor/Core/*
LayerBase/Actor/Meta/*
LayerBase.Test/ActorCoreTests.cs
```

不做：

```text
1. 不接 LayerRuntime。
2. 不做 Source Generator。
3. 不做 EventColumn。
4. 不做 Pump。
```

DoD：

```text
1. dotnet build 通过。
2. IActor 是空接口。
3. IGeneratedActorMeta 是 public。
4. ActorContext 构造器是 internal。
5. ActorGeneratedAccess 对未生成 Actor 抛异常。
6. ActorTypeMetaBuilder 能收集行为并生成 BehaviourSignature。
```

---

## Phase 2：ActorBehaviourGenerator MVP

新增：

```text
LayerBase.Generator/LayerBase.Generator/ActorBehaviourGenerator.cs
LayerBase.Generator/LayerBase.Generator/ActorBehaviourDiagnostics.cs
LayerBase.Test/ActorGeneratorTests.cs
```

DoD：

```text
1. partial Actor + [ActorBehaviour] 能生成 IGeneratedActorMeta。
2. 非 partial Actor 报错。
3. 未实现 IActor 报错。
4. static 方法报错。
5. 非 void 方法报错。
6. 参数不是 in TEvent 报错。
7. TEvent 不是 struct 报错。
8. 重复 TEvent 报错。
9. 生成代码不包含 Registry。
10. 生成代码不包含 ModuleInitializer。
```

---

## Phase 3：ActorWorld MVP

新增：

```text
LayerBase/Actor/Storage/*
LayerBase/Actor/Mail/*
LayerBase/Actor/Pump/RuntimeFrameBudget.cs
LayerBase.Test/ActorPostPumpTests.cs
```

实现：

```text
1. CreateActor<TActor>()。
2. Post<TEvent>(ActorId, in TEvent)。
3. TryPost<TEvent>(ActorId, in TEvent)。
4. PostMany<TEvent>()。
5. Pump(ref RuntimeFrameBudget)。
6. Queued 策略。
7. RejectNew 策略。
```

不做：

```text
1. 不接 LayerRuntime。
2. 不接 EventMetaData。
3. 不做 Query。
4. 不做 Latest。
5. 不做 Dirty。
6. 不做 Coalesced。
```

DoD：

```text
1. Post 后不会立刻调用 ActorBehaviour。
2. Pump 后调用 ActorBehaviour。
3. 同一 Actor 同一事件多次 Post 后 FIFO 执行。
4. 无效 ActorId 返回 Failure。
5. 不支持的事件返回 Failure。
6. ActorBehaviour 抛异常时 Pump 直接抛出。
```

---

## Phase 4：接入 LayerRuntime

修改：

```text
LayerBase/Application/LayerRuntime.cs
LayerBase.Test/ActorRuntimeIntegrationTests.cs
```

实现：

```text
1. LayerRuntime 增加 public ActorWorld Actors。
2. LayerRuntime 构造函数初始化 Actors。
3. LayerRuntime.Pump 中在 PostScheduler.Pump 后调用 Actors.Pump。
4. Actors.Pump 在 LayerChain.Pump 前执行。
```

DoD：

```text
1. runtime.Actors.CreateActor<T>() 可用。
2. runtime.Pump(deltaTime) 会处理 ActorWorld。
3. PostScheduler 事件预算用尽时 ActorWorld 不继续无限处理。
4. 原有 LayerRuntime 测试不失败。
```

---

## Phase 5：Actor 邮箱策略

实现：

```text
1. Latest。
2. Dirty。
3. Grow。
4. DropOldest。
5. DropNewest。
6. GrowFailurePolicy。
7. ReleaseWhenEmpty。
```

新增测试：

```text
LayerBase.Test/ActorMailPolicyTests.cs
```

DoD：

```text
1. Latest 多次 Post 只处理最后一次。
2. Dirty 多次 Post 只处理一次。
3. Grow 从 4 -> 8 -> 16。
4. 达到 MaxCapacity 后执行 GrowFailurePolicy。
5. RejectNew 返回 Failure。
6. DropOldest 丢旧保新。
7. DropNewest 丢新保旧。
8. 邮箱空后按配置归还 buffer。
```

---

## Phase 6：EventMetaData 集成

修改：

```text
LayerBase/Event/EventMetaData/EventMetaData.cs
LayerBase/Event/PostScheduler/EventRuntimePolicyTable.cs
LayerBase/Application/LayerRuntime.cs
LayerBase/Actor/Mail/EventColumn.cs
```

DoD：

```text
1. EventMetaData<TEvent>.GetActorMailOptions() 生效。
2. ActorWorld 创建 EventColumn 时读取配置。
3. EventColumn 缓存 ActorMailOptions。
4. Post 热路径不调用 EventMetaDataHandler。
5. Pump 热路径不调用 EventMetaDataHandler。
6. 原有 EventMetaDataGenerator 测试不失败。
```

---

## Phase 7：Query + PostAll

新增：

```text
LayerBase/Actor/Query/*
LayerBase.Test/ActorQueryTests.cs
```

实现：

```text
1. QueryActor<TEvent>()。
2. QueryActor<TEvent1, TEvent2>()。
3. QueryActor<...>().PostAll(in TEvent)。
4. DebugActors 枚举。
```

DoD：

```text
1. 查询 DamageEvent 能命中 DamageEvent + DeadEvent Actor。
2. PostAll 不构造 IActor 列表。
3. QueryCache 在新增 Archetype 后失效。
4. DebugActors 可用于调试，但不作为热路径。
```

---

## Phase 8：Benchmark

新增：

```text
LayerBase.BenchMark/ActorWorldBenchmarks.cs
```

对比：

```text
1. Direct method call。
2. LayerBase Send。
3. LayerBase PostScheduler。
4. ActorWorld Post + Pump。
5. Dictionary<ActorId, Actor> + interface call。
```

DoD：

```text
1. ActorWorld Post 热路径无 GC。
2. ActorWorld Pump 热路径无 GC。
3. ActorWorld 比 Dictionary Actor 调用路径更稳定。
4. ActorWorld 不明显拖慢现有 PostScheduler benchmark。
```

---

## Phase 9：Unity/Godot 适配

不进入核心第一版。

后续建议：

```text
LayerBase.Actor.Unity/
  UnityActorView.cs
  UnityActorViewBinder.cs
  ColliderActorIdMap.cs

LayerBase.Actor.Godot/
  GodotActorView.cs
  GodotActorViewBinder.cs
  NodeActorIdMap.cs
```

DoD：

```text
1. 核心 LayerBase 不引用 UnityEngine。
2. 核心 LayerBase 不引用 Godot。
3. Actor 不是 GameObject / Node 本体。
4. Collider / Node 只保存 ActorId。
```

---

## 16. PR 拆分建议

### PR-1：Actor Core API

```text
新增 LayerBase/Actor/Core/*
新增 LayerBase/Actor/Meta/*
新增 ActorCoreTests
不修改 LayerRuntime
不修改 PostScheduler
不修改 EventMetaData
```

### PR-2：ActorBehaviourGenerator

```text
新增 ActorBehaviourGenerator
新增 ActorBehaviourDiagnostics
新增 ActorGeneratorTests
复用 ManagerAutoSubscribeGenerator 的扫描和诊断结构
```

### PR-3：ActorWorld MVP

```text
新增 ActorWorld
新增 BehaviourArchetype
新增 TypedActorStorage
新增 EventColumn
新增 EventMail
新增 ActorEventBucket
新增 RuntimeFrameBudget
新增 ActorPostPumpTests
```

### PR-4：LayerRuntime 接入

```text
LayerRuntime 增加 public ActorWorld Actors
LayerRuntime 构造函数初始化 Actors
LayerRuntime.Pump 中接入 Actors.Pump
新增 ActorRuntimeIntegrationTests
```

### PR-5：邮箱策略

```text
实现 Latest
实现 Dirty
实现 Grow
实现 DropOldest
实现 DropNewest
新增 ActorMailPolicyTests
```

### PR-6：EventMetaData 集成

```text
EventMetaData<TEvent> 增加 GetActorMailOptions
EventRuntimePolicyTable 增加 ActorMailOptions 缓存
EventColumn 创建时读取配置
新增 ActorMetaDataIntegrationTests
```

### PR-7：Query + Benchmark

```text
新增 ActorQuery
新增 QueryCache
新增 PostAll
新增 ActorQueryTests
新增 ActorWorldBenchmarks
```

---

## 17. 测试计划

### 17.1 Generator 测试

```text
1. partial Actor + ActorBehaviour 生成 IGeneratedActorMeta。
2. 非 partial Actor 报错。
3. 未实现 IActor 报错。
4. static ActorBehaviour 报错。
5. 非 void ActorBehaviour 报错。
6. 多参数 ActorBehaviour 报错。
7. 参数不是 in TEvent 报错。
8. TEvent 不是 struct 报错。
9. 重复 TEvent 报错。
```

### 17.2 创建测试

```text
1. CreateActor<EnemyActor>() 返回实例。
2. 返回实例实现 IGeneratedActorMeta。
3. ActorId 注入成功。
4. ActorTypeMeta 只构建一次。
5. 相同 BehaviourSignature 的不同 Actor 进入同一 BehaviourArchetype。
6. 不同 TActor 在同一 BehaviourArchetype 内拥有不同 TypedActorStorage。
```

### 17.3 Post 测试

```text
1. Post 支持事件写入邮箱。
2. Post 不立即执行 ActorBehaviour。
3. 无效 ArchetypeId 返回 Failure。
4. 无效 TypeStorageIndex 返回 Failure。
5. stale Generation 返回 Failure。
6. Actor 不支持该事件返回 Failure。
```

### 17.4 Pump 测试

```text
1. Pump 执行邮箱中的事件。
2. 每处理一个事件消耗一个预算单位。
3. 预算不足时保留未处理邮箱。
4. ActorBehaviour 抛异常时不吞异常。
5. 多个 EventColumn 轮询执行，避免单列长期占用。
```

### 17.5 Mail 策略测试

```text
1. Queued FIFO。
2. Latest 覆盖旧值。
3. Dirty 只触发一次。
4. Grow 扩容。
5. Grow 到 MaxCapacity 后执行 GrowFailurePolicy。
6. RejectNew 拒绝新事件。
7. DropOldest 丢弃旧事件。
8. DropNewest 丢弃新事件。
9. ReleaseWhenEmpty 生效。
```

### 17.6 Runtime 集成测试

```text
1. runtime.Actors 可用。
2. runtime.Pump 会推进 ActorWorld。
3. ActorWorld 在 PostScheduler 后执行。
4. ActorWorld 在 LayerChain.Pump 前执行。
5. 原有 Timer、Delay、PostScheduler 测试不失败。
```

### 17.7 Query 测试

```text
1. QueryActor<DamageEvent>() 命中包含 DamageEvent 的 Actor。
2. QueryActor<DamageEvent, DeadEvent>() 只命中同时包含两者的 Actor。
3. PostAll 不构造 IActor 列表。
4. 新建 Archetype 后 QueryCache 失效。
```

---

## 18. 禁止项

任何阶段都禁止：

```text
1. 把 GetId / Post / ActorInit 放进 IActor。
2. 生成全局 ActorGeneratedRegistry。
3. 使用 ModuleInitializer 自动注册 Actor。
4. 生成 EnemyActor_ActorMetaGenerated.Register。
5. 让用户手动调用 ActorInit。
6. 引入 ActorHubFacade。
7. 让 GameObject / Node 成为 Actor 本体。
8. 把 AABB / 空间索引 / 阵营过滤写进 Actor 核心框架。
9. 为每个 EventBucket 重复存 TActor[]。
10. 为每个 Actor 存 handle<TEvent>[]。
11. 在 Post / Pump 热路径使用 Dictionary。
12. 在 Post / Pump 热路径使用反射。
13. 在 Post / Pump 热路径使用 Type 查找。
14. 给空 EventMail 预分配 RingQueue buffer。
15. 默认吞掉 ActorBehaviour 异常。
16. ActorWorld 使用独立于 LayerRuntime 的帧预算。
17. 核心 LayerBase 引用 UnityEngine。
18. 核心 LayerBase 引用 Godot。
```

---

## 19. Codex 执行约束

给 Codex / Gemini / opencode 的执行规则：

```text
1. 一次只做一个 PR 范围。
2. 不允许跨阶段提前实现 Query、Unity/Godot 适配、EventMetaData 深度集成。
3. 每个 PR 必须包含测试。
4. 每个新增 public API 必须说明归属模块。
5. 每个示例代码必须包含参数注释。
6. Post / Pump 相关改动必须说明是否进入热路径。
7. 热路径新增 Dictionary / reflection / Type lookup 必须拒绝。
8. 不允许为了通过测试破坏现有 LayerBase 行为。
9. 不允许修改现有 Subscribe / PostScheduler 语义。
10. 不允许删除现有测试。
```

---

## 20. 最终结论

Actor Behaviour Runtime 可以加入 LayerBase，但它应该作为新的实体行为运行时模块，而不是普通事件系统补丁。

最小主线是：

```text
partial Actor
  -> ActorBehaviourGenerator
  -> IGeneratedActorMeta
  -> ActorWorld.CreateActor<TActor>()
  -> ActorId
  -> ActorWorld.Post<TEvent>()
  -> EventMail<TEvent>
  -> ActorWorld.Pump()
  -> ActorBehaviourInvoker<TActor, TEvent>
```

第一阶段只要这条链路跑通，就已经证明功能成立。

Query、EventMetaData、邮箱高级策略、Benchmark、Unity/Godot 适配都应该后置。  
这样可以最大限度保持 LayerBase 当前主干稳定，同时把 Actor 行为系统自然接入已有的 Source Generator、SOA、EventTypeId、PostScheduler 和多世界 Runtime 体系。
