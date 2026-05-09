# Actor Post Hot Path Refactor Final Design

## 1. 目标

本设计用于重构 LayerBase Actor 系统的 Post 热路径，目标是：

1. 移除 Actor 列构建阶段的反射调用。
2. 保持 `IActor`、`IService`、`ILayerContext` 的对外接口基本不变。
3. 保留现有 `ActorWorld.PostTo<TEvent>(ActorId, in TEvent, ...)` 使用方式。
4. 将高频 Actor 行为从传统安全路径中拆出，走裸数组缓存路径。
5. 默认热路径只支持 `QueuedGrow` 邮箱写入模式。
6. `disabled`、`destroying`、`pending destroy` 不在 Post 热路径检查，改由脏位和 Pump 前置处理完成。
7. `RingQueueBuffer<TEvent>` 从 `EventColumn<TActor, TEvent>` 私有池上移为 `ActorWorld` 级 `EventMailPool<TEvent>`。
8. Pump 仍然保持 ECS / Column / DirtyBucket / DirtySlot 风格运行。

---

## 2. 术语

### 2.1 热路径

热路径指一帧内可能被大量执行的代码路径。  
本设计中的热路径主要指：

```text
ActorWorld.PostTo<TEvent>(ActorId, in TEvent)
```

在 Hot / PrewarmHot 命中缓存后的执行路径。

### 2.2 裸数组

裸数组指直接使用 `T[]`、`int[]`、`byte[]` 等连续数组结构，不通过 `Dictionary`、接口、虚方法、反射或对象包装进行路由。

### 2.3 JIT

JIT 是 Just-In-Time 编译器。  
在 .NET 中，JIT 会把 IL 转换成本机机器码。  
JIT 友好的代码通常具备以下特点：

1. 泛型类型闭合。
2. 调用目标稳定。
3. 少虚方法。
4. 少接口调用。
5. 少分支。
6. 少对象间接访问。
7. 热路径逻辑集中且简单。

### 2.4 脏位

脏位是用于表示“有待处理变化”的标记。  
例如：

```csharp
private bool _hasPendingDestroy;
private bool _hasDisabledChanges;
```

如果没有脏位，系统不进入对应清理流程。

---

## 3. 总体原则

### 3.1 Post 热路径只做最少工作

Hot / PrewarmHot 的默认 Post 路径只允许做：

1. `actorId.FastIndex` 数组索引。
2. `ActorFastState.Version` 校验。
3. `ActorEventFastCache<TEvent>` 裸数组读取。
4. `EventMail<TEvent>[]` 裸数组写入。
5. `EventMailPool<TEvent>` 写入。
6. `DirtySlotList` 标记。
7. `DirtyBucketList` 标记。

### 3.2 Post 热路径禁止项

Hot / PrewarmHot 命中缓存后的路径不允许出现：

1. `MethodInfo`
2. `MethodInfo.Invoke`
3. `MakeGenericMethod`
4. `Dictionary` 查找
5. `Type` 查找
6. `typeof(T).Name` 错误字符串构造
7. `BehaviourArchetype.Post`
8. `TypedStorageRuntime.Post<TEvent>`
9. `IsAlive`
10. `GetSlotState`
11. `GetGeneration`
12. disabled 检查
13. pending destroy 检查
14. destroying 检查
15. 每次 Post 的策略分支
16. 接口调用
17. 虚方法调用
18. 委托路由调用

### 3.3 生命周期状态由脏位处理

Actor 生命周期状态由 Runtime 主动追踪。

Post 不负责判断：

1. Actor 是否 disabled。
2. Actor 是否 pending destroy。
3. Actor 是否 destroying。
4. Actor 是否 alive。

这些状态由以下机制处理：

1. Actor 被禁用时，写入禁用脏位。
2. Actor 请求销毁时，写入销毁脏位。
3. Actor 进入销毁流程前，先让 `ActorFastState.Version` 递增，使旧缓存失效。
4. Pump 某个 Actor 前，处理该 Actor 的禁用 / 销毁 / 清理状态。
5. 如果没有脏位，Pump 不进入对应清理逻辑。

### 3.4 默认热路径只支持 QueuedGrow

Hot / PrewarmHot 默认走 `QueuedGrow`。

`QueuedGrow` 含义：

1. 事件写入队列。
2. 邮箱满时尝试增长。
3. 增长失败时拒绝新消息。
4. 这是最通用、最适合高频消息的默认模式。

如果用户显式传入 `ActorPostPolicy?` 或 `ActorMailFullPolicy?`，则回退 SafePath。

### 3.5 构建期能确定的，不放到 Post 期计算

以下内容必须在构建期或 Actor 创建期确定：

1. `EventColumn<TActor, TEvent>`。
2. `EventMail<TEvent>[]` 邮箱数组引用。
3. `DirtySlotList` 引用。
4. `DirtyBucketList` 的 `bucketIndex`。
5. `EventMailPool<TEvent>`。
6. `ActorEventFastCache<TEvent>` 容量。
7. `PrewarmHot` 行为的缓存绑定。

---

## 4. BehaviourType

新增三种行为模式：

```csharp
public enum BehaviourType
{
    /// <summary>
    /// 默认模式。
    ///
    /// 不创建 ActorEventFastCache。
    /// Post 始终走传统 SafePath。
    /// 适合低频行为、调试行为、一次性业务行为。
    /// </summary>
    Cold = 0,

    /// <summary>
    /// 运行时热路径模式。
    ///
    /// 第一次触发时构建 ActorEventFastCache。
    /// 后续同一个 Actor + Event 组合走裸数组缓存路径。
    /// 适合可能高频，但不确定每个 Actor 都会触发的行为。
    /// </summary>
    Hot = 1,

    /// <summary>
    /// 创建期预热热路径模式。
    ///
    /// Actor 创建时直接绑定 ActorEventFastCache。
    /// 第一次 Post 就走裸数组缓存路径。
    /// 适合确定高频、确定大多数 Actor 都会触发的行为。
    /// </summary>
    PrewarmHot = 2
}
```

Attribute 示例：

```csharp
[ActorBehaviours(BehaviourType.PrewarmHot)]
private void OnDamage(in DamageEvent value)
{
}
```

```csharp
[ActorBehaviours(BehaviourType.Hot)]
private void OnMove(in MoveEvent value)
{
}
```

未显式标记时默认为 `Cold`。

---

## 5. 三种行为路径

### 5.1 Cold

Cold 不创建 fast cache。

路径：

```text
ActorWorld.PostTo<TEvent>
  -> SafePath
  -> BehaviourArchetype.Post<TEvent>
  -> TypedStorageRuntime.Post<TEvent>
  -> TypedActorStorage<TActor>.Post<TEvent>
  -> EventColumn<TActor,TEvent>.PostGeneral
```

Cold 保留完整语义：

1. 详细 `PostResult`。
2. 详细失败原因。
3. 兼容自定义 `ActorPostPolicy`。
4. 兼容自定义 `ActorMailFullPolicy`。
5. 兼容调试信息。

### 5.2 Hot

Hot 第一次触发时构建裸数组缓存。

第一次：

```text
ActorWorld.PostTo<TEvent>
  -> ActorEventFastCache<TEvent> miss
  -> Safe metadata lookup
  -> Bind fast cache
  -> Direct QueuedGrow write
```

第二次及以后：

```text
ActorWorld.PostTo<TEvent>
  -> actorId.FastIndex
  -> ActorFastState[]
  -> ActorEventFastCache<TEvent>
  -> EventMail<TEvent>[]
  -> EventMailPool<TEvent>
  -> DirtySlotList
  -> DirtyBucketList
```

Hot 适合：

1. 某些 Actor 可能触发。
2. 一旦触发后可能高频。
3. 不希望所有 Actor 创建时都付出预热成本。

### 5.3 PrewarmHot

PrewarmHot 在 Actor 创建时直接绑定缓存。

Actor 创建时：

```text
CreateActor<TActor>
  -> Allocate actor slot
  -> Allocate fastIndex
  -> Bind ActorFastState
  -> Bind all PrewarmHot ActorEventFastCache entries
```

Post 时：

```text
ActorWorld.PostTo<TEvent>
  -> actorId.FastIndex
  -> ActorFastState[]
  -> ActorEventFastCache<TEvent>
  -> EventMail<TEvent>[]
  -> EventMailPool<TEvent>
  -> DirtySlotList
  -> DirtyBucketList
```

PrewarmHot 适合：

1. 高频战斗事件。
2. 高频移动事件。
3. 高频同步事件。
4. 高频 AI 状态事件。
5. 创建后大概率马上触发的行为。

---

## 6. ActorId 增加 FastIndex

`ActorId` 增加 `FastIndex` 字段。

```csharp
public readonly struct ActorId
{
    public readonly int ArchetypeId;
    public readonly ushort TypeStorageIndex;
    public readonly int SlotIndex;
    public readonly int Generation;
    public readonly int FastIndex;

    /// <param name="archetypeId">
    /// 旧路径使用的行为签名分组编号。
    /// 保留它是为了兼容 SafePath。
    /// </param>
    /// <param name="typeStorageIndex">
    /// 旧路径中 BehaviourArchetype 内部的 Storage 下标。
    /// 保留它是为了兼容 SafePath。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 内部的槽位。
    /// FastPath 用它定位 EventMail<TEvent>[]。
    /// </param>
    /// <param name="generation">
    /// Actor slot 的生命周期代际。
    /// 用于防止旧 ActorId 命中新 Actor。
    /// </param>
    /// <param name="fastIndex">
    /// ActorWorld 内部 ActorFastState[] 的连续数组下标。
    /// FastPath 用它直接进入缓存表。
    /// </param>
    public ActorId(
        int archetypeId,
        ushort typeStorageIndex,
        int slotIndex,
        int generation,
        int fastIndex)
    {
        ArchetypeId = archetypeId;
        TypeStorageIndex = typeStorageIndex;
        SlotIndex = slotIndex;
        Generation = generation;
        FastIndex = fastIndex;
    }
}
```

---

## 7. ActorFastState

`ActorFastState` 是 `ActorWorld` 级连续数组中的运行时状态。

```csharp
internal struct ActorFastState
{
    public int Version;
    public int SlotIndex;
    public int Generation;
    public int StorageRouteId;

    /// <summary>
    /// 绑定一个新 Actor 生命周期。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 内部的槽位。
    /// </param>
    /// <param name="generation">
    /// Actor slot 当前生命周期代际。
    /// </param>
    /// <param name="storageRouteId">
    /// 当前 Actor 类型对应的内部 Storage 路由编号。
    /// Hot 首次触发时可用它找到对应列。
    /// </param>
    public void Bind(
        int slotIndex,
        int generation,
        int storageRouteId)
    {
        Version++;
        SlotIndex = slotIndex;
        Generation = generation;
        StorageRouteId = storageRouteId;
    }

    /// <summary>
    /// 标记当前 fastIndex 不再指向有效 Actor。
    /// </summary>
    public void MarkDead()
    {
        Version++;
        SlotIndex = -1;
        Generation = 0;
        StorageRouteId = -1;
    }
}
```

`Version` 用于让所有旧 fast cache 自动失效。

Actor 销毁时不扫描所有 `ActorEventFastCache<TEvent>`，只递增 `Version`。

---

## 8. ActorEventFastCache<TEvent>

`ActorEventFastCache<TEvent>` 使用 SoA 裸数组。

```csharp
internal sealed class ActorEventFastCache<TEvent>
    where TEvent : struct
{
    private int[] _versions = Array.Empty<int>();
    private int[] _slotIndices = Array.Empty<int>();
    private int[] _generations = Array.Empty<int>();

    private EventMail<TEvent>[][] _mailArrays = Array.Empty<EventMail<TEvent>[]>();
    private DirtySlotList?[] _dirtySlotLists = Array.Empty<DirtySlotList?>();
    private int[] _bucketIndices = Array.Empty<int>();

    private byte[] _states = Array.Empty<byte>();

    /// <summary>
    /// 确保缓存数组可以容纳指定 fastIndex。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 为 Actor 分配的快速索引。
    /// 所有缓存数组都使用它作为统一下标。
    /// </param>
    public void EnsureCapacity(int fastIndex)
    {
        if ((uint)fastIndex < (uint)_versions.Length)
        {
            return;
        }

        int newSize = _versions.Length == 0 ? 4 : _versions.Length;

        while (newSize <= fastIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _slotIndices, newSize);
        Array.Resize(ref _generations, newSize);
        Array.Resize(ref _mailArrays, newSize);
        Array.Resize(ref _dirtySlotLists, newSize);
        Array.Resize(ref _bucketIndices, newSize);
        Array.Resize(ref _states, newSize);
    }

    /// <summary>
    /// 绑定 Actor + Event 的裸数组写入缓存。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 级快速索引。
    /// </param>
    /// <param name="version">
    /// ActorFastState.Version 的当前值。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 内部的槽位。
    /// </param>
    /// <param name="generation">
    /// Actor slot 当前生命周期代际。
    /// </param>
    /// <param name="mailArray">
    /// EventColumn<TActor,TEvent> 内部的 EventMail<TEvent>[]。
    /// Post 热路径直接写 mailArray[slotIndex]。
    /// </param>
    /// <param name="dirtySlots">
    /// EventColumn<TActor,TEvent> 内部的 DirtySlotList。
    /// 邮箱从空变非空时写入 slotIndex。
    /// </param>
    /// <param name="bucketIndex">
    /// EventColumn 在 DirtyBucketList 中的下标。
    /// 邮箱从空变非空时标记该 bucket。
    /// </param>
    public void Bind(
        int fastIndex,
        int version,
        int slotIndex,
        int generation,
        EventMail<TEvent>[] mailArray,
        DirtySlotList dirtySlots,
        int bucketIndex)
    {
        EnsureCapacity(fastIndex);

        _versions[fastIndex] = version;
        _slotIndices[fastIndex] = slotIndex;
        _generations[fastIndex] = generation;
        _mailArrays[fastIndex] = mailArray;
        _dirtySlotLists[fastIndex] = dirtySlots;
        _bucketIndices[fastIndex] = bucketIndex;
        _states[fastIndex] = 1;
    }

    /// <summary>
    /// 获取裸数组写入所需数据。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 级快速索引。
    /// </param>
    /// <param name="version">
    /// 当前 ActorFastState.Version。
    /// </param>
    /// <param name="generation">
    /// ActorId.Generation。
    /// </param>
    /// <param name="slotIndex">
    /// 输出 Actor 在邮箱数组中的槽位。
    /// </param>
    /// <param name="mailArray">
    /// 输出邮箱数组。
    /// </param>
    /// <param name="dirtySlots">
    /// 输出脏 slot 列表。
    /// </param>
    /// <param name="bucketIndex">
    /// 输出脏 bucket 下标。
    /// </param>
    public bool TryGet(
        int fastIndex,
        int version,
        int generation,
        out int slotIndex,
        out EventMail<TEvent>[] mailArray,
        out DirtySlotList dirtySlots,
        out int bucketIndex)
    {
        if ((uint)fastIndex >= (uint)_states.Length ||
            _states[fastIndex] == 0 ||
            _versions[fastIndex] != version ||
            _generations[fastIndex] != generation)
        {
            slotIndex = -1;
            mailArray = null!;
            dirtySlots = null!;
            bucketIndex = -1;
            return false;
        }

        slotIndex = _slotIndices[fastIndex];
        mailArray = _mailArrays[fastIndex];
        dirtySlots = _dirtySlotLists[fastIndex]!;
        bucketIndex = _bucketIndices[fastIndex];
        return true;
    }

    /// <summary>
    /// 惰性失效缓存。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 级快速索引。
    /// </param>
    public void Invalidate(int fastIndex)
    {
        if ((uint)fastIndex >= (uint)_states.Length)
        {
            return;
        }

        _states[fastIndex] = 0;
        _mailArrays[fastIndex] = null!;
        _dirtySlotLists[fastIndex] = null;
        _bucketIndices[fastIndex] = -1;
    }
}
```

Hot / PrewarmHot 的区别不在 `ActorEventFastCache<TEvent>`，而在绑定时机。

---

## 9. EventMailPool<TEvent>

`RingQueueBuffer<TEvent>` 不再由每个 `EventColumn<TActor,TEvent>` 私有持有。

新的结构：

```text
ActorWorld
  -> EventMailPool<TEvent>
    -> RingQueueBuffer<TEvent>
```

`EventColumn<TActor,TEvent>` 只保存：

1. `EventMail<TEvent>[] Mails`
2. `DirtySlotList DirtySlots`
3. `int BucketIndex`
4. `ActorBehaviourInvoker<TActor,TEvent> Invoker`

`RingQueueBuffer<TEvent>` / `EventMailPool<TEvent>` 按 `TEvent` 在 World 内共享。

```csharp
internal sealed class EventMailPool<TEvent>
    where TEvent : struct
{
    private readonly RingQueueBuffer<TEvent> _buffer = new();

    public int RentInitial()
    {
        return _buffer.Rent(ActorMailOptions.Default.InitialCapacity);
    }

    public int GetCapacity(int bufferId)
    {
        return _buffer.GetCapacity(bufferId);
    }

    public void Write(int bufferId, int index, in TEvent value)
    {
        _buffer.Write(bufferId, index, in value);
    }

    public bool TryGrow(ref EventMail<TEvent> mail)
    {
        int nextCapacity = mail.Capacity * 2;

        if (nextCapacity <= mail.Capacity)
        {
            return false;
        }

        if (nextCapacity > ActorMailOptions.Default.MaxCapacity)
        {
            return false;
        }

        _buffer.Resize(
            mail.BufferId,
            mail.Head,
            mail.Count,
            nextCapacity);

        mail.Head = 0;
        mail.Tail = mail.Count;
        mail.Capacity = nextCapacity;
        return true;
    }

    public void Release(int bufferId)
    {
        _buffer.Release(bufferId);
    }
}
```

实际实现中，`ActorMailOptions.Default` 应替换为 Column 构建期解析后的 fixed options。

Hot / PrewarmHot 不应在 Post 热路径重新读取复杂 options。

---

## 10. EventColumn<TActor,TEvent> 调整

`EventColumn<TActor,TEvent>` 不再在构造函数中创建 `RingQueueBuffer<TEvent>`。

它保留列式 Pump 所需数据。

```csharp
internal sealed class EventColumn<TActor, TEvent> : ActorEventColumnRuntime
    where TActor : class, IActor
    where TEvent : struct
{
    private readonly TypedActorStorage<TActor> _owner;
    private readonly ActorBehaviourInvoker<TActor, TEvent> _invoker;
    private readonly BehaviourType _behaviourType;

    private EventMail<TEvent>[] _mails;
    private readonly DirtySlotList _dirtySlots;
    private readonly int _bucketIndex;

    public EventMail<TEvent>[] Mails => _mails;
    public DirtySlotList DirtySlots => _dirtySlots;
    public int BucketIndex => _bucketIndex;
    public BehaviourType BehaviourType => _behaviourType;

    /// <param name="owner">
    /// 当前列所属的强类型 Actor Storage。
    /// Pump 时用它根据 slotIndex 取回 TActor 实例。
    /// </param>
    /// <param name="invoker">
    /// 当前 Actor 收到 TEvent 时执行的强类型处理函数。
    /// </param>
    /// <param name="behaviourType">
    /// 当前行为缓存模式。
    /// Cold / Hot / PrewarmHot。
    /// </param>
    /// <param name="bucketIndex">
    /// 当前列在 DirtyBucketList 中的下标。
    /// </param>
    /// <param name="initialSlotCapacity">
    /// 初始 slot 容量。
    /// </param>
    public EventColumn(
        TypedActorStorage<TActor> owner,
        ActorBehaviourInvoker<TActor, TEvent> invoker,
        BehaviourType behaviourType,
        int bucketIndex,
        int initialSlotCapacity)
    {
        _owner = owner;
        _invoker = invoker;
        _behaviourType = behaviourType;
        _bucketIndex = bucketIndex;
        _mails = new EventMail<TEvent>[Math.Max(initialSlotCapacity, 1)];
        _dirtySlots = new DirtySlotList(initialSlotCapacity);
    }

    public override ActorColumnPumpResult PumpOne(
        ref RuntimeFrameBudget budget,
        in ActorMailPumpOptions options,
        ActorMailPumpStatsBuilder stats)
    {
        // Pump 路径保留原 ECS / DirtySlot 运行方式。
        // Pump 才负责处理 disabled / pending destroy / destroying 造成的跳过或清理。
        throw new NotImplementedException();
    }
}
```

---

## 11. ActorWorld PostTo<TEvent> FastPath

`ActorWorld.PostTo<TEvent>` 对外签名不变。

默认 `postPolicy == null && fullPolicy == null` 时尝试 FastPath。

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct
{
    /// postPolicy / fullPolicy 参数作用：
    /// 保留旧 API 兼容能力。
    /// 如果用户显式传入策略，则直接回退 SafePath。
    if (postPolicy != null || fullPolicy != null)
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    int fastIndex = actorId.FastIndex;

    if ((uint)fastIndex >= (uint)_fastStates.Length)
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    ref ActorFastState state = ref _fastStates[fastIndex];

    ActorEventFastCache<TEvent> cache = GetOrCreateFastCache<TEvent>();

    if (!cache.TryGet(
            fastIndex,
            state.Version,
            actorId.Generation,
            out int slotIndex,
            out EventMail<TEvent>[] mails,
            out DirtySlotList dirtySlots,
            out int bucketIndex))
    {
        return TryBindHotOrFallbackSafe(actorId, in value, postPolicy, fullPolicy);
    }

    return PostQueuedGrowDirect(
        slotIndex,
        in value,
        mails,
        dirtySlots,
        bucketIndex);
}
```

---

## 12. 裸数组 QueuedGrow 写入

```csharp
private PostResult PostQueuedGrowDirect<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex)
    where TEvent : struct
{
    /// slotIndex 参数作用：
    /// Actor 在 EventMail<TEvent>[] 中的槽位。
    ///
    /// value 参数作用：
    /// 要写入邮箱的事件。
    ///
    /// mails 参数作用：
    /// EventColumn<TActor,TEvent> 持有的邮箱数组。
    ///
    /// dirtySlots 参数作用：
    /// 当前列的脏 slot 列表。
    ///
    /// bucketIndex 参数作用：
    /// 当前列在 DirtyBucketList 中的下标。

    ref EventMail<TEvent> mail = ref mails[slotIndex];

    EventMailPool<TEvent> pool = GetOrCreateEventMailPool<TEvent>();

    if (mail.BufferId == 0)
    {
        mail.BufferId = pool.RentInitial();
        mail.Head = 0;
        mail.Tail = 0;
        mail.Count = 0;
        mail.Capacity = pool.GetCapacity(mail.BufferId);
    }

    if (mail.Count >= mail.Capacity)
    {
        if (!pool.TryGrow(ref mail))
        {
            return PostResult.Failure(
                ActorPostStatus.MailFullRejected,
                "Actor mail reached max capacity.",
                PostFailureKind.MailboxFull);
        }
    }

    pool.Write(mail.BufferId, mail.Tail, in value);

    mail.Tail++;

    if (mail.Tail == mail.Capacity)
    {
        mail.Tail = 0;
    }

    mail.Count++;

    if (mail.Count == 1)
    {
        dirtySlots.AddIfNotExists(slotIndex);
        _dirtyBuckets.AddIfNotExists(bucketIndex);
    }

    return PostResult.Success;
}
```

后续可增加内部专用版本：

```csharp
internal bool PostFast<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
```

内部 `PostFast` 返回 `bool`，不构造 `PostResult.Failure`。

---

## 13. Hot 首次触发绑定

当 `ActorEventFastCache<TEvent>` miss 时：

1. 根据 `ActorFastState.StorageRouteId` 找到目标 Storage 元数据。
2. 判断该 `TActor + TEvent` 行为是否为 `Hot`。
3. 如果是 `Hot`，绑定缓存。
4. 然后执行裸数组写入。
5. 如果不是 `Hot`，回退 SafePath。

```csharp
private PostResult TryBindHotOrFallbackSafe<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
    where TEvent : struct
{
    int fastIndex = actorId.FastIndex;

    if ((uint)fastIndex >= (uint)_fastStates.Length)
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    ref ActorFastState state = ref _fastStates[fastIndex];

    if (!TryBindHotFastCache<TEvent>(
            fastIndex,
            in state,
            actorId.SlotIndex,
            actorId.Generation))
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    ActorEventFastCache<TEvent> cache = GetOrCreateFastCache<TEvent>();

    if (!cache.TryGet(
            fastIndex,
            state.Version,
            actorId.Generation,
            out int slotIndex,
            out EventMail<TEvent>[] mails,
            out DirtySlotList dirtySlots,
            out int bucketIndex))
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    return PostQueuedGrowDirect(
        slotIndex,
        in value,
        mails,
        dirtySlots,
        bucketIndex);
}
```

`TryBindHotFastCache<TEvent>` 属于冷路径，可以接受一次元数据查找，但不能使用反射。

---

## 14. PrewarmHot 创建期绑定

Actor 创建时直接绑定 PrewarmHot 行为。

```text
CreateActor<TActor>
  -> AllocateSlot
  -> AllocateFastIndex
  -> ActorFastState.Bind
  -> BindPrewarmHotFastCaches<TActor>
```

推荐生成器生成：

```csharp
internal static partial class EnemyActor_ActorMetaGenerated
{
    internal static void BindPrewarmHotFastCaches(
        ActorWorld world,
        TypedActorStorage<EnemyActor> storage,
        int fastIndex,
        int slotIndex,
        int generation)
    {
        ref ActorFastState state = ref world.GetFastStateRef(fastIndex);

        EventColumn<EnemyActor, DamageEvent> damageColumn =
            storage.GetRequiredColumn<DamageEvent>();

        world.GetOrCreateFastCache<DamageEvent>().Bind(
            fastIndex,
            state.Version,
            slotIndex,
            generation,
            damageColumn.Mails,
            damageColumn.DirtySlots,
            damageColumn.BucketIndex);
    }
}
```

如果暂时不用生成器，可以在构建期生成直接绑定表，但最终目标应是生成直接代码，避免创建期虚方法和反射。

---

## 15. 去反射列构建

列构建不允许再使用 `MethodInfo`。

`ActorBehaviourEntry` 应携带强类型列构建工厂。

```csharp
internal delegate ActorEventColumnRuntime ActorEventColumnFactory(
    TypedStorageRuntime storage,
    object invoker,
    ActorWorld world,
    BehaviourType behaviourType);

internal readonly struct ActorBehaviourEntry
{
    public readonly int EventTypeId;
    public readonly Type EventType;
    public readonly object Invoker;
    public readonly BehaviourType BehaviourType;
    public readonly ActorEventColumnFactory Factory;

    /// <param name="eventTypeId">
    /// TEvent 的稳定整数 ID。
    /// </param>
    /// <param name="eventType">
    /// 事件类型，仅用于冷路径调试和元数据。
    /// </param>
    /// <param name="invoker">
    /// 强类型 ActorBehaviourInvoker<TActor,TEvent>，以 object 保存。
    /// </param>
    /// <param name="behaviourType">
    /// Cold / Hot / PrewarmHot。
    /// </param>
    /// <param name="factory">
    /// 无反射列构建工厂。
    /// </param>
    public ActorBehaviourEntry(
        int eventTypeId,
        Type eventType,
        object invoker,
        BehaviourType behaviourType,
        ActorEventColumnFactory factory)
    {
        EventTypeId = eventTypeId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        BehaviourType = behaviourType;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }
}
```

`ActorTypeMetaBuilder.AddBehaviour`：

```csharp
public void AddBehaviour<TActor, TEvent>(
    ActorBehaviourInvoker<TActor, TEvent> invoker,
    BehaviourType behaviourType = BehaviourType.Cold)
    where TActor : class, IActor
    where TEvent : struct
{
    if (invoker == null)
    {
        throw new ArgumentNullException(nameof(invoker));
    }

    int eventTypeId = EventTypeId<TEvent>.Id;

    if (!_eventIds.Add(eventTypeId))
    {
        throw new InvalidOperationException(
            $"Actor type {typeof(TActor).Name} already has behaviour for event {typeof(TEvent).Name}.");
    }

    _entries.Add(new ActorBehaviourEntry(
        eventTypeId,
        typeof(TEvent),
        invoker,
        behaviourType,
        static (storage, rawInvoker, world, mode) =>
        {
            var typedStorage = (TypedActorStorage<TActor>)storage;
            var typedInvoker = (ActorBehaviourInvoker<TActor, TEvent>)rawInvoker;

            return typedStorage.BuildColumnDirect(
                world,
                typedInvoker,
                mode);
        }));
}
```

`TypedActorStorage<TActor>`：

```csharp
internal ActorEventColumnRuntime BuildColumnDirect<TEvent>(
    ActorWorld world,
    ActorBehaviourInvoker<TActor, TEvent> invoker,
    BehaviourType behaviourType)
    where TEvent : struct
{
    int eventTypeId = EventTypeId<TEvent>.Id;

    EnsureEventColumnArrayCapacity(eventTypeId);

    int bucketIndex = world.AllocateDirtyBucketIndex();

    var column = new EventColumn<TActor, TEvent>(
        this,
        invoker,
        behaviourType,
        bucketIndex,
        _actors.Length);

    _columnsByEventId[eventTypeId] = column;

    RegisterHotColumnIfNeeded(column);

    return column;
}
```

这样不需要：

1. `MethodInfo`
2. `BindingFlags`
3. `MakeGenericMethod`
4. `Invoke`

---

## 16. 生命周期脏位处理

### 16.1 字段

```csharp
private bool _hasPendingDestroy;
private bool _hasDisabledChanges;

private DeadActorRecord[] _deadActors = Array.Empty<DeadActorRecord>();
private int _deadActorCount;
```

### 16.2 DeadActorRecord

```csharp
internal readonly struct DeadActorRecord
{
    public readonly int FastIndex;
    public readonly int SlotIndex;
    public readonly int Generation;
    public readonly int StorageRouteId;

    /// <param name="fastIndex">
    /// ActorWorld 级快速索引。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 中的槽位。
    /// </param>
    /// <param name="generation">
    /// Actor slot 生命周期代际。
    /// </param>
    /// <param name="storageRouteId">
    /// Actor 类型对应的 Storage 路由编号。
    /// </param>
    public DeadActorRecord(
        int fastIndex,
        int slotIndex,
        int generation,
        int storageRouteId)
    {
        FastIndex = fastIndex;
        SlotIndex = slotIndex;
        Generation = generation;
        StorageRouteId = storageRouteId;
    }
}
```

### 16.3 销毁流程

```text
DestroyActor(actorId)
  -> 读取 ActorFastState
  -> 写 DeadActorRecord
  -> ActorFastState.MarkDead
  -> _hasPendingDestroy = true
  -> 不扫描所有 cache
  -> 不立即回收所有邮箱
```

### 16.4 Pump 前处理

```text
ActorWorld.Pump
  -> if _hasPendingDestroy:
       SweepDeadActors
  -> if _hasDisabledChanges:
       ApplyDisabledChanges
  -> Pump mail columns
  -> if _hasPendingDestroy:
       SweepDeadActors
```

### 16.5 SweepDeadActors

`SweepDeadActors` 属于冷路径。

它负责：

1. 调用 `OnDestroy`。
2. 清理生命周期句柄。
3. 清理对应 slot 的所有邮箱。
4. 回收 Actor slot。
5. 回收 fastIndex。
6. 重置相关脏位。

---

## 17. Pump 侧处理 disabled / destroying / pending

Post 不处理 disabled / destroying / pending。

Pump 某个 Actor 前处理：

1. 如果 slot 处于 pending destroy，跳过并清理该 slot 邮箱。
2. 如果 slot 处于 destroying，跳过并清理该 slot 邮箱。
3. 如果 slot disabled 且该行为不允许 disabled actor 处理，跳过或保留，具体由 Pump 策略决定。
4. 如果没有任何相关脏位，可走快速 Pump。

Pump 可以保留安全判断，因为 Pump 是统一调度点，不是单次 Post 热入口。

---

## 18. Query / PostAll 优化

Query 内部应直接持有 `TypedActorStorage<TActor>`。

```text
ActorQuery<TActor>.PostAll<TEvent>
  -> TypedActorStorage<TActor>.PostAllFast<TEvent>
  -> EventColumn<TActor,TEvent>
  -> EventMail<TEvent>[]
```

多事件 PostAll 必须单次扫描 slot。

```csharp
internal void PostAllFast<TEvent1, TEvent2>(
    in TEvent1 value1,
    in TEvent2 value2)
    where TEvent1 : struct
    where TEvent2 : struct
{
    EventColumn<TActor, TEvent1> column1 = GetRequiredColumn<TEvent1>();
    EventColumn<TActor, TEvent2> column2 = GetRequiredColumn<TEvent2>();

    EventMail<TEvent1>[] mails1 = column1.Mails;
    EventMail<TEvent2>[] mails2 = column2.Mails;

    DirtySlotList dirty1 = column1.DirtySlots;
    DirtySlotList dirty2 = column2.DirtySlots;

    int bucket1 = column1.BucketIndex;
    int bucket2 = column2.BucketIndex;

    ReadOnlySpan<int> selectedSlots = CurrentQuerySelectedSlots;

    for (int i = 0; i < selectedSlots.Length; i++)
    {
        int slotIndex = selectedSlots[i];

        PostQueuedGrowDirect(slotIndex, in value1, mails1, dirty1, bucket1);
        PostQueuedGrowDirect(slotIndex, in value2, mails2, dirty2, bucket2);
    }
}
```

如果 Query 结果已经是连续 slot 列表，则直接遍历 slot list，不扫描完整 storage。

---

## 19. 兼容性策略

### 19.1 对外接口保持不变

保持不变：

1. `IActor`
2. `IService`
3. `ILayerContext`
4. `ActorWorld.PostTo<TEvent>(ActorId, in TEvent, ...)`
5. `ActorWorld.TryPostTo<TEvent>(ActorId, in TEvent, ...)`

### 19.2 SafePath 保留

以下情况回退 SafePath：

1. 行为是 `Cold`。
2. `Hot` 首次绑定失败。
3. 显式传入 `postPolicy`。
4. 显式传入 `fullPolicy`。
5. `ActorId.FastIndex` 无效。
6. `ActorFastState.Version` 不匹配。
7. `ActorEventFastCache<TEvent>` 不存在。
8. 调试模式下需要完整错误信息。

---

## 20. 实施顺序

### Phase 1：去反射

1. 删除 `TypedActorStorage<TActor>` 中的 `MethodInfo` 字段。
2. 删除 `BuildColumnFromEntry` 的反射调用。
3. `ActorBehaviourEntry` 增加 `ActorEventColumnFactory`。
4. `ActorCallEntry` 同步改为无反射工厂。
5. `ActorTypeMetaBuilder.AddBehaviour` 写入强类型 factory。

### Phase 2：BehaviourType 元数据

1. 增加 `BehaviourType`。
2. `[ActorBehaviours]` 支持 `BehaviourType` 参数。
3. `ActorBehaviourEntry` 记录 `BehaviourType`。
4. `EventColumn<TActor,TEvent>` 保存 `BehaviourType`。

### Phase 3：EventMailPool 上移

1. 新增 `ActorWorld.GetOrCreateEventMailPool<TEvent>()`。
2. 删除 `EventColumn<TActor,TEvent>` 私有 `RingQueueBuffer<TEvent>`。
3. `EventMailReader` / `EventMailWriter` 改为接收 World 级 pool。
4. 验证同一 `TEvent` 在不同 `TActor` 之间共享 buffer pool。

### Phase 4：ActorFastState + FastIndex

1. `ActorId` 增加 `FastIndex`。
2. `ActorWorld` 增加 `ActorFastState[]`。
3. Actor 创建时分配 fastIndex。
4. Actor 销毁时 `ActorFastState.MarkDead()`。
5. 增加 fastIndex free list。

### Phase 5：ActorEventFastCache<TEvent>

1. 新增 SoA 版 `ActorEventFastCache<TEvent>`。
2. 新增 `ActorWorld.GetOrCreateFastCache<TEvent>()`。
3. `PrewarmHot` 创建期绑定 cache。
4. `Hot` 首次触发绑定 cache。
5. `Cold` 不绑定 cache。

### Phase 6：PostTo FastPath

1. `PostTo<TEvent>` 默认先尝试 fast cache。
2. 命中后直接 `PostQueuedGrowDirect`。
3. miss 时：
   - Hot 尝试绑定后写入。
   - Cold 回退 SafePath。
4. 显式 policy 直接回退 SafePath。

### Phase 7：生命周期脏位化

1. Post 热路径删除 disabled / pending / destroying 检查。
2. Destroy 请求只写脏位和 dead record。
3. Pump 前后按脏位批量回收。
4. Pump 某 Actor 前处理 disabled / pending / destroying。

### Phase 8：Query / PostAll 单次扫描

1. Query 内部缓存 `TypedActorStorage<TActor>`。
2. `PostAll<T1...T12>` 改成单次扫描。
3. 每个 slot 连续写多个事件。
4. 不再每个 column 单独扫一遍 storage。

---

## 21. 验收标准

### 21.1 反射

Actor 运行时列构建不允许出现：

1. `MethodInfo.Invoke`
2. `MakeGenericMethod`
3. `BindingFlags.Instance | BindingFlags.NonPublic` 查找热路径方法

### 21.2 Post 热路径

PrewarmHot 命中路径不允许出现：

1. `BehaviourArchetype.Post`
2. `TypedStorageRuntime.Post<TEvent>`
3. `IsAlive`
4. `GetSlotState`
5. `GetGeneration`
6. `Dictionary`
7. `Type`
8. `MethodInfo`
9. 委托路由层
10. `virtual` / `abstract` 方法
11. disabled / pending / destroying 检查

### 21.3 内存

1. `RingQueueBuffer<TEvent>` 不再按 `TActor + TEvent` 创建。
2. 同一 `ActorWorld` 内，同一 `TEvent` 使用同一个 `EventMailPool<TEvent>`。
3. `PrewarmHot` 只为支持的行为绑定 cache。
4. `Cold` 不占用 fast cache。

### 21.4 生命周期

1. Actor 死亡时递增 `ActorFastState.Version`。
2. 旧缓存通过 version 自动失效。
3. 销毁回收只在 `_hasPendingDestroy == true` 时执行。
4. Post 热路径不检查 disabled / pending / destroying。

### 21.5 Benchmark

必须增加：

```text
ActorPost_Cold_SafePath_OneActor_OneEvent
ActorPost_Hot_FirstBind_OneActor_OneEvent
ActorPost_Hot_Cached_OneActor_OneEvent
ActorPost_PrewarmHot_Cached_OneActor_OneEvent
ActorPost_PrewarmHot_1000Actors_OneEvent
ActorPost_PrewarmHot_1000Actors_4Events
ActorPost_Query_PostAll_1000Actors_OneEvent
ActorPost_Query_PostAll_1000Actors_12Events
ActorPost_DictionaryBaseline_OneActor
ActorPost_DictionaryBaseline_1000Actors
```

目标：

1. `PrewarmHot_Cached` 无 GC。
2. `Hot_Cached` 无 GC。
3. `Query_PostAll_12Events` 单次扫描 slot。
4. `PrewarmHot` 不因 Actor 状态检查进入传统路径。
5. `Cold` 行为保持兼容。

---

## 22. 最终结构

```text
ActorWorld
  ├─ ActorFastState[] _fastStates
  ├─ ActorEventFastCache<TEvent>
  ├─ EventMailPool<TEvent>
  ├─ DirtyBucketList
  ├─ DeadActorRecord[]
  ├─ _hasPendingDestroy
  └─ SafePath fallback

ActorId
  ├─ ArchetypeId
  ├─ TypeStorageIndex
  ├─ SlotIndex
  ├─ Generation
  └─ FastIndex

TypedActorStorage<TActor>
  ├─ TActor?[] _actors
  ├─ ActorSlotFlags[] _slotFlags
  ├─ ActorEventColumnRuntime[] _columnsByEventId
  ├─ PrewarmHot bind metadata
  └─ Query fast path

EventColumn<TActor,TEvent>
  ├─ EventMail<TEvent>[] Mails
  ├─ DirtySlotList DirtySlots
  ├─ int BucketIndex
  ├─ ActorBehaviourInvoker<TActor,TEvent> Invoker
  └─ BehaviourType

ActorEventFastCache<TEvent>
  ├─ int[] versions
  ├─ int[] slotIndices
  ├─ int[] generations
  ├─ EventMail<TEvent>[][] mailArrays
  ├─ DirtySlotList[] dirtySlotLists
  ├─ int[] bucketIndices
  └─ byte[] states

EventMailPool<TEvent>
  └─ RingQueueBuffer<TEvent>
```

---

## 23. 最终结论

最终方案是：

1. Cold 默认保守，走传统 SafePath。
2. Hot 首次触发构建裸数组缓存。
3. PrewarmHot 创建期绑定裸数组缓存。
4. Post 热路径只走 `FastIndex -> FastState -> FastCache -> EventMail[]`。
5. Actor 生命周期状态不在 Post 检查。
6. 禁用、销毁、pending destroy 都通过脏位和 Pump 前处理。
7. `RingQueueBuffer<TEvent>` 上移到 `ActorWorld` 级。
8. 列构建去掉反射。
9. Query / PostAll 走强类型单次扫描。
10. 外部 `IActor`、`IService`、`ILayerContext` 保持不变。
