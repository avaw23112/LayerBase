# Actor Post Bare Array Hot Path Upgrade Design

## 1. 目标

本设计用于继续压缩 LayerBase Actor Post 热路径，将 Hot / PrewarmHot 缓存路径从“缓存命中”升级为“裸数组直写”。

目标是：

1. 清理 `ActorEventFastCache<TEvent>` 获取过程中的 Dictionary / object / Type 查找。
2. 清理 `EventMailPool<TEvent>` 获取过程中的 Dictionary / object / Type 查找。
3. 清理 `DirtySlotList` / `DirtyBucketList` 的线性去重、HashSet 去重或 Dictionary 去重。
4. 清理 public `PostTo<TEvent>` 的 `PostResult` 兼容成本对内部热路径的影响。
5. 让 PrewarmHot / Hot cached 路径尽量变成：
   ```text
   actorId.FastIndex
     -> ActorFastState[]
     -> ActorEventFastCache<TEvent>
     -> EventMail<TEvent>[]
     -> EventMailPool<TEvent>
     -> DirtySlotList.Mark
     -> DirtyBucketList.Mark
   ```
6. 保持 `IActor`、`IService`、`ILayerContext` 的对外接口不变。
7. 保留 public `ActorWorld.PostTo<TEvent>(ActorId, in TEvent, ...)` 作为兼容入口。
8. 新增 internal / framework-level `bool PostFast<TEvent>` 作为真正极致热路径入口。

---

## 2. 总体原则

### 2.1 热路径禁止项

PrewarmHot / Hot cached 的极致路径中不允许出现：

```text
Dictionary
HashSet
Type
object cast
MethodInfo
Reflection
MakeGenericMethod
Delegate 路由层
virtual / abstract 方法路由
BehaviourArchetype.Post
TypedStorageRuntime.Post<TEvent>
GetOrCreateFastCache<TEvent>
GetOrCreateEventMailPool<TEvent>
PostResult.Failure 字符串构造
DirtySlotList 线性查重
DirtyBucketList 线性查重
IsAlive
GetSlotState
GetGeneration
disabled / pending / destroying 检查
```

### 2.2 热路径允许项

PrewarmHot / Hot cached 的极致路径只允许：

```text
泛型静态数组
普通数组
uint 越界判断
int version/generation 校验
ref EventMail<TEvent>
EventMailPool<TEvent>.Write
DirtySlotList.Mark
DirtyBucketList.Mark
bool 返回
```

### 2.3 SafePath 与 FastPath 分离

public API 保留：

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct
```

内部新增：

```csharp
internal bool PostFast<TEvent>(
    ActorId actorId,
    in TEvent value)
    where TEvent : struct
```

规则：

1. public `PostTo<TEvent>` 保留兼容语义。
2. internal `PostFast<TEvent>` 不构造 `PostResult`。
3. internal `PostFast<TEvent>` 不支持自定义 policy。
4. internal `PostFast<TEvent>` 不进入 SafePath。
5. public `PostTo<TEvent>` 可以优先尝试 `PostFast<TEvent>`，失败后再回退 SafePath。

---

## 3. 最终热路径结构

```text
ActorWorld.PostFast<TEvent>
  -> actorId.FastIndex
  -> _fastStates[fastIndex]
  -> ActorEventRuntime<TEvent>.GetFastCache(world)
  -> ActorEventFastCache<TEvent>.TryGet(...)
  -> PostQueuedGrowFastNoResult(...)
  -> EventMail<TEvent>[] mails
  -> EventMailPool<TEvent> pool
  -> DirtySlotList.Mark(slotIndex)
  -> DirtyBucketList.Mark(bucketIndex)
```

核心目标：

```text
不再根据 TEvent 查 Dictionary。
不再根据 TEvent 查 object[] 后 cast。
不再每次 Post 找 pool。
不再返回 PostResult。
不再做 DirtyList 线性去重。
```

---

## 4. ActorWorld RuntimeIndex

为每个 `ActorWorld` 分配一个运行时编号，用于泛型静态数组定位当前 World 的 cache / pool。

```csharp
public sealed partial class ActorWorld
{
    internal readonly int RuntimeIndex;

    /// <summary>
    /// 创建 ActorWorld。
    /// </summary>
    /// <param name="options">
    /// Actor 邮箱配置。
    ///
    /// 作用：
    /// 保存默认 PostPolicy、FullPolicy、InitialCapacity、MaxCapacity 等配置。
    /// Hot / PrewarmHot 极致路径只使用构建期解析后的固定 QueuedGrow 配置，
    /// 不在 Post 热路径中重新解析 options。
    /// </param>
    public ActorWorld(ActorMailOptions options)
    {
        Options = options;

        // RuntimeIndex 的作用：
        // 给当前 ActorWorld 分配一个进程内连续编号。
        // ActorEventRuntime<TEvent> 会用这个编号在泛型静态数组中直取当前 World 的 cache/pool。
        RuntimeIndex = ActorWorldRuntimeIndexAllocator.Rent();
    }

    /// <summary>
    /// 释放 ActorWorld 时调用。
    /// </summary>
    public void Dispose()
    {
        ActorWorldRuntimeIndexAllocator.Return(RuntimeIndex);
    }
}
```

```csharp
internal static class ActorWorldRuntimeIndexAllocator
{
    private static int s_nextIndex;
    private static readonly Stack<int> s_free = new();

    /// <summary>
    /// 分配 ActorWorld 运行时编号。
    /// </summary>
    /// <returns>
    /// 可用于泛型静态数组索引的 World 编号。
    /// </returns>
    public static int Rent()
    {
        return s_free.Count > 0
            ? s_free.Pop()
            : s_nextIndex++;
    }

    /// <summary>
    /// 回收 ActorWorld 运行时编号。
    /// </summary>
    /// <param name="index">
    /// 要回收的 ActorWorld.RuntimeIndex。
    ///
    /// 作用：
    /// 允许 ActorWorld 销毁后复用编号，避免 ActorEventRuntime<TEvent> 内部数组无限增长。
    /// </param>
    public static void Return(int index)
    {
        s_free.Push(index);
    }
}
```

---

## 5. ActorEventRuntime<TEvent>

`ActorEventRuntime<TEvent>` 是每个事件类型的泛型静态运行时槽。

它负责用 `world.RuntimeIndex` 直接定位：

1. `ActorEventFastCache<TEvent>`
2. `EventMailPool<TEvent>`

```csharp
internal static class ActorEventRuntime<TEvent>
    where TEvent : struct
{
    private static ActorEventFastCache<TEvent>?[] s_fastCaches = new ActorEventFastCache<TEvent>?[4];
    private static EventMailPool<TEvent>?[] s_mailPools = new EventMailPool<TEvent>?[4];

    /// <summary>
    /// 绑定当前 World 对应的 TEvent cache/pool。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    ///
    /// 作用：
    /// 使用 world.RuntimeIndex 作为泛型静态数组下标。
    /// </param>
    /// <param name="fastCache">
    /// 当前 World 内 TEvent 的快速缓存。
    ///
    /// 作用：
    /// 保存 Actor + Event 的裸数组写入信息。
    /// </param>
    /// <param name="mailPool">
    /// 当前 World 内 TEvent 的邮箱池。
    ///
    /// 作用：
    /// 保存 TEvent 队列 buffer 的租用、写入、增长、释放逻辑。
    /// </param>
    public static void BindWorld(
        ActorWorld world,
        ActorEventFastCache<TEvent> fastCache,
        EventMailPool<TEvent> mailPool)
    {
        int index = world.RuntimeIndex;

        EnsureCapacity(index);

        s_fastCaches[index] = fastCache;
        s_mailPools[index] = mailPool;
    }

    /// <summary>
    /// 获取当前 World 的 TEvent 快速缓存。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    ///
    /// 作用：
    /// 只读取 RuntimeIndex，不做 Dictionary 查找。
    /// </param>
    public static ActorEventFastCache<TEvent> GetFastCache(ActorWorld world)
    {
        return s_fastCaches[world.RuntimeIndex]!;
    }

    /// <summary>
    /// 获取当前 World 的 TEvent 邮箱池。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    ///
    /// 作用：
    /// 只读取 RuntimeIndex，不做 Dictionary 查找。
    /// </param>
    public static EventMailPool<TEvent> GetMailPool(ActorWorld world)
    {
        return s_mailPools[world.RuntimeIndex]!;
    }

    private static void EnsureCapacity(int worldIndex)
    {
        if ((uint)worldIndex < (uint)s_fastCaches.Length)
        {
            return;
        }

        int newSize = s_fastCaches.Length;

        while (newSize <= worldIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref s_fastCaches, newSize);
        Array.Resize(ref s_mailPools, newSize);
    }
}
```

热路径使用：

```csharp
ActorEventFastCache<TEvent> cache =
    ActorEventRuntime<TEvent>.GetFastCache(this);
```

不再使用：

```csharp
GetOrCreateFastCache<TEvent>();
GetOrCreateEventMailPool<TEvent>();
```

---

## 6. ActorEventFastCache<TEvent> 持有 Pool

为了彻底移除 Post 时的 pool 查找，`ActorEventFastCache<TEvent>` 直接持有 `EventMailPool<TEvent>`。

```csharp
internal sealed class ActorEventFastCache<TEvent>
    where TEvent : struct
{
    private readonly EventMailPool<TEvent> _pool;

    private int[] _versions = Array.Empty<int>();
    private int[] _slotIndices = Array.Empty<int>();
    private int[] _generations = Array.Empty<int>();
    private EventMail<TEvent>[][] _mailArrays = Array.Empty<EventMail<TEvent>[]>();
    private DirtySlotList?[] _dirtySlotLists = Array.Empty<DirtySlotList?>();
    private int[] _bucketIndices = Array.Empty<int>();
    private byte[] _states = Array.Empty<byte>();

    /// <summary>
    /// 创建 TEvent 快速缓存。
    /// </summary>
    /// <param name="pool">
    /// 当前 World 内 TEvent 的邮箱池。
    ///
    /// 作用：
    /// Post 热路径通过 cache.Pool 直接获取 pool，
    /// 避免每次 Post 再做 GetOrCreateEventMailPool<TEvent>()。
    /// </param>
    public ActorEventFastCache(EventMailPool<TEvent> pool)
    {
        _pool = pool;
    }

    public EventMailPool<TEvent> Pool => _pool;

    /// <summary>
    /// 绑定 Actor + Event 的快速缓存项。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 内 ActorFastState[] 的下标。
    ///
    /// 作用：
    /// Post 时用 actorId.FastIndex 直接定位当前 Actor 对当前 TEvent 的缓存。
    /// </param>
    /// <param name="version">
    /// ActorFastState.Version 的快照。
    ///
    /// 作用：
    /// Actor 死亡或 fastIndex 复用后，旧缓存会因为 version 不匹配而失效。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 中的槽位。
    ///
    /// 作用：
    /// Post 热路径通过 mailArray[slotIndex] 直接定位邮箱。
    /// </param>
    /// <param name="generation">
    /// Actor slot 生命周期代际。
    ///
    /// 作用：
    /// 防止旧 ActorId 命中复用后的新 Actor。
    /// </param>
    /// <param name="mailArray">
    /// EventColumn<TActor,TEvent> 持有的邮箱数组。
    ///
    /// 作用：
    /// Post 热路径直接写入这个数组。
    /// </param>
    /// <param name="dirtySlots">
    /// EventColumn<TActor,TEvent> 持有的 DirtySlotList。
    ///
    /// 作用：
    /// 邮箱从空变为非空时，标记 slotIndex 待 Pump。
    /// </param>
    /// <param name="bucketIndex">
    /// EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    ///
    /// 作用：
    /// 邮箱从空变为非空时，标记该列待 Pump。
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
    /// 获取裸数组写入信息。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 内 ActorFastState[] 的下标。
    /// </param>
    /// <param name="version">
    /// 当前 ActorFastState.Version。
    /// </param>
    /// <param name="generation">
    /// 当前 ActorId.Generation。
    /// </param>
    /// <param name="slotIndex">
    /// 输出 Actor 在 mailArray 中的槽位。
    /// </param>
    /// <param name="mailArray">
    /// 输出 EventColumn<TActor,TEvent> 的邮箱数组。
    /// </param>
    /// <param name="dirtySlots">
    /// 输出 EventColumn<TActor,TEvent> 的 DirtySlotList。
    /// </param>
    /// <param name="bucketIndex">
    /// 输出 EventColumn<TActor,TEvent> 的 DirtyBucket 下标。
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
    /// 让某个 fastIndex 的缓存失效。
    /// </summary>
    /// <param name="fastIndex">
    /// ActorWorld 内 ActorFastState[] 的下标。
    ///
    /// 作用：
    /// 手动清空该 Actor 对当前 TEvent 的缓存。
    /// 常规销毁流程不需要扫描所有 cache，通常只通过 ActorFastState.Version 惰性失效。
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

    private void EnsureCapacity(int fastIndex)
    {
        if ((uint)fastIndex < (uint)_states.Length)
        {
            return;
        }

        int newSize = _states.Length == 0 ? 4 : _states.Length;

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
}
```

---

## 7. DirtySlotList O(1) mark

`DirtySlotList` 必须使用 mark 数组去重。

```csharp
internal sealed class DirtySlotList
{
    private int[] _items;
    private int[] _marks;
    private int _count;
    private int _stamp = 1;

    public int Count => _count;

    /// <summary>
    /// 创建 DirtySlotList。
    /// </summary>
    /// <param name="capacity">
    /// 初始 slot 容量。
    ///
    /// 作用：
    /// 同时初始化 dirty slot 数组和 mark 数组。
    /// mark 数组使用 slotIndex 作为下标，实现 O(1) 去重。
    /// </param>
    public DirtySlotList(int capacity)
    {
        int size = Math.Max(capacity, 4);
        _items = new int[size];
        _marks = new int[size];
    }

    /// <summary>
    /// O(1) 标记 slot 为 dirty。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<TActor> 中的槽位。
    ///
    /// 作用：
    /// 如果该 slot 当前轮次尚未进入 dirty 列表，则加入。
    /// 如果已经加入，则直接返回。
    /// </param>
    public void Mark(int slotIndex)
    {
        EnsureSlotCapacity(slotIndex);

        if (_marks[slotIndex] == _stamp)
        {
            return;
        }

        _marks[slotIndex] = _stamp;
        EnsureItemCapacity(_count);
        _items[_count++] = slotIndex;
    }

    /// <summary>
    /// 弹出一个 dirty slot。
    /// </summary>
    /// <param name="slotIndex">
    /// 输出待 Pump 的 slotIndex。
    /// </param>
    /// <returns>
    /// 如果存在 dirty slot，返回 true。
    /// </returns>
    public bool TryPop(out int slotIndex)
    {
        if (_count == 0)
        {
            slotIndex = -1;
            return false;
        }

        _count--;
        slotIndex = _items[_count];
        return true;
    }

    /// <summary>
    /// 清空 dirty slot 列表。
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _stamp++;

        if (_stamp == int.MaxValue)
        {
            Array.Clear(_marks, 0, _marks.Length);
            _stamp = 1;
        }
    }

    private void EnsureSlotCapacity(int slotIndex)
    {
        if ((uint)slotIndex < (uint)_marks.Length)
        {
            return;
        }

        int newSize = _marks.Length;

        while (newSize <= slotIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _marks, newSize);
    }

    private void EnsureItemCapacity(int index)
    {
        if ((uint)index < (uint)_items.Length)
        {
            return;
        }

        Array.Resize(ref _items, _items.Length << 1);
    }
}
```

---

## 8. DirtyBucketList O(1) mark

```csharp
internal sealed class DirtyBucketList
{
    private int[] _items;
    private int[] _marks;
    private int _count;
    private int _stamp = 1;

    public int Count => _count;

    /// <summary>
    /// 创建 DirtyBucketList。
    /// </summary>
    /// <param name="capacity">
    /// 初始 bucket 容量。
    ///
    /// 作用：
    /// 同时初始化 dirty bucket 数组和 mark 数组。
    /// mark 数组使用 bucketIndex 作为下标，实现 O(1) 去重。
    /// </param>
    public DirtyBucketList(int capacity)
    {
        int size = Math.Max(capacity, 4);
        _items = new int[size];
        _marks = new int[size];
    }

    /// <summary>
    /// O(1) 标记 bucket 为 dirty。
    /// </summary>
    /// <param name="bucketIndex">
    /// EventColumn 在 DirtyBucketList 中的下标。
    ///
    /// 作用：
    /// 如果该 bucket 当前轮次尚未进入 dirty 列表，则加入。
    /// 如果已经加入，则直接返回。
    /// </param>
    public void Mark(int bucketIndex)
    {
        EnsureBucketCapacity(bucketIndex);

        if (_marks[bucketIndex] == _stamp)
        {
            return;
        }

        _marks[bucketIndex] = _stamp;
        EnsureItemCapacity(_count);
        _items[_count++] = bucketIndex;
    }

    /// <summary>
    /// 弹出一个 dirty bucket。
    /// </summary>
    /// <param name="bucketIndex">
    /// 输出待 Pump 的 bucketIndex。
    /// </param>
    /// <returns>
    /// 如果存在 dirty bucket，返回 true。
    /// </returns>
    public bool TryPop(out int bucketIndex)
    {
        if (_count == 0)
        {
            bucketIndex = -1;
            return false;
        }

        _count--;
        bucketIndex = _items[_count];
        return true;
    }

    /// <summary>
    /// 清空 dirty bucket 列表。
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _stamp++;

        if (_stamp == int.MaxValue)
        {
            Array.Clear(_marks, 0, _marks.Length);
            _stamp = 1;
        }
    }

    private void EnsureBucketCapacity(int bucketIndex)
    {
        if ((uint)bucketIndex < (uint)_marks.Length)
        {
            return;
        }

        int newSize = _marks.Length;

        while (newSize <= bucketIndex)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _marks, newSize);
    }

    private void EnsureItemCapacity(int index)
    {
        if ((uint)index < (uint)_items.Length)
        {
            return;
        }

        Array.Resize(ref _items, _items.Length << 1);
    }
}
```

---

## 9. PostFast<TEvent>

`PostFast<TEvent>` 是真正的极致热路径。

```csharp
public sealed partial class ActorWorld
{
    /// <summary>
    /// 内部极致 Post 路径。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    ///
    /// 作用：
    /// JIT 会为每个闭合 TEvent 生成具体泛型路径。
    /// </typeparam>
    /// <param name="actorId">
    /// 目标 ActorId。
    ///
    /// 作用：
    /// 使用 actorId.FastIndex 进入 ActorFastState 和 ActorEventFastCache。
    /// </param>
    /// <param name="value">
    /// 要投递的事件值。
    ///
    /// 作用：
    /// 直接写入 EventMail<TEvent>[]。
    /// </param>
    /// <returns>
    /// true 表示写入成功。
    /// false 表示 fast cache miss、版本失效或邮箱增长失败。
    /// </returns>
    internal bool PostFast<TEvent>(
        ActorId actorId,
        in TEvent value)
        where TEvent : struct
    {
        int fastIndex = actorId.FastIndex;

        if ((uint)fastIndex >= (uint)_fastStates.Length)
        {
            return false;
        }

        ref ActorFastState state = ref _fastStates[fastIndex];

        ActorEventFastCache<TEvent> cache =
            ActorEventRuntime<TEvent>.GetFastCache(this);

        if (!cache.TryGet(
                fastIndex,
                state.Version,
                actorId.Generation,
                out int slotIndex,
                out EventMail<TEvent>[] mails,
                out DirtySlotList dirtySlots,
                out int bucketIndex))
        {
            return false;
        }

        return PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            mails,
            dirtySlots,
            bucketIndex,
            cache.Pool);
    }
}
```

---

## 10. PostQueuedGrowFastNoResult

```csharp
private bool PostQueuedGrowFastNoResult<TEvent>(
    int slotIndex,
    in TEvent value,
    EventMail<TEvent>[] mails,
    DirtySlotList dirtySlots,
    int bucketIndex,
    EventMailPool<TEvent> pool)
    where TEvent : struct
{
    /// slotIndex 参数作用：
    /// Actor 在 EventMail<TEvent>[] 中的槽位。
    ///
    /// value 参数作用：
    /// 要写入邮箱的事件数据。
    ///
    /// mails 参数作用：
    /// EventColumn<TActor,TEvent> 的邮箱数组。
    ///
    /// dirtySlots 参数作用：
    /// 当前 EventColumn<TActor,TEvent> 的 DirtySlotList。
    ///
    /// bucketIndex 参数作用：
    /// 当前 EventColumn<TActor,TEvent> 在 DirtyBucketList 中的下标。
    ///
    /// pool 参数作用：
    /// 当前 TEvent 的 World 级邮箱池。
    /// 它已经由 ActorEventFastCache<TEvent> 持有，不需要 Post 时再次查找。

    ref EventMail<TEvent> mail = ref mails[slotIndex];

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
            return false;
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
        dirtySlots.Mark(slotIndex);
        _dirtyBuckets.Mark(bucketIndex);
    }

    return true;
}
```

---

## 11. Public PostTo<TEvent> 兼容层

```csharp
public PostResult PostTo<TEvent>(
    ActorId actorId,
    in TEvent value,
    ActorPostPolicy? postPolicy = null,
    ActorMailFullPolicy? fullPolicy = null)
    where TEvent : struct
{
    /// postPolicy / fullPolicy 参数作用：
    /// 兼容旧 API。
    /// 只要用户显式传入策略，就不进入极致 FastPath。
    if (postPolicy != null || fullPolicy != null)
    {
        return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
    }

    if (PostFast(actorId, in value))
    {
        return PostResult.Success;
    }

    return TryPostToSafe(actorId, in value, postPolicy, fullPolicy);
}
```

说明：

1. public API 保留 `PostResult`。
2. framework 内部和 Query 可直接使用 `PostFast<TEvent>`。
3. Hot / PrewarmHot cached benchmark 应单独测试 `PostFast<TEvent>`，用于观察极致路径。
4. public `PostTo<TEvent>` benchmark 用于观察兼容层成本。

---

## 12. Actor.PostInside 优化

如果 `PostInside` 是内部高频入口，应新增 fast 版本。

```csharp
public static class ActorPostInsideExtensions
{
    /// <summary>
    /// Actor 内部快速投递。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    ///
    /// 作用：
    /// 为每个事件类型生成闭合泛型热路径。
    /// </typeparam>
    /// <param name="actor">
    /// 发起 Post 的 Actor。
    ///
    /// 作用：
    /// 从 Actor 注入的上下文中取得 ActorWorld 和 ActorId。
    /// </param>
    /// <param name="value">
    /// 要投递的事件值。
    /// </param>
    /// <returns>
    /// true 表示 FastPath 写入成功。
    /// </returns>
    public static bool PostFastInside<TEvent>(
        this IActor actor,
        in TEvent value)
        where TEvent : struct
    {
        ActorContext context = actor.GetActorContext();
        return context.World.PostFast(context.ActorId, in value);
    }
}
```

进一步优化：

```text
如果 actor.GetActorContext() 仍然有字典、接口或上下文容器查找，
则应改成 Actor 实例内部直接注入 ActorWorld + ActorId。
```

---

## 13. PrewarmHot 创建期绑定

PrewarmHot 必须在 Actor 创建时绑定 fast cache。

推荐由 Source Generator 生成直接绑定代码。

```csharp
internal static partial class EnemyActor_ActorMetaGenerated
{
    /// <summary>
    /// 绑定 EnemyActor 的 PrewarmHot 快速缓存。
    /// </summary>
    /// <param name="world">
    /// 当前 ActorWorld。
    ///
    /// 作用：
    /// 访问 ActorFastState、ActorEventRuntime<TEvent> 和对应 World 的 fast cache。
    /// </param>
    /// <param name="storage">
    /// EnemyActor 的强类型 Storage。
    ///
    /// 作用：
    /// 直接取得 EventColumn<EnemyActor,TEvent>。
    /// </param>
    /// <param name="fastIndex">
    /// ActorWorld 内 ActorFastState[] 的下标。
    /// </param>
    /// <param name="slotIndex">
    /// Actor 在 TypedActorStorage<EnemyActor> 中的槽位。
    /// </param>
    /// <param name="generation">
    /// Actor slot 当前生命周期代际。
    /// </param>
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

        ActorEventRuntime<DamageEvent>
            .GetFastCache(world)
            .Bind(
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

不得在 PrewarmHot 绑定路径中使用：

```text
MethodInfo
MakeGenericMethod
Reflection
Dictionary<Type, ...>
ActorEventColumnRuntime 虚方法遍历
```

---

## 14. Hot 首次绑定

Hot 的首次绑定属于冷路径，可以接受少量查找，但仍不允许反射。

规则：

1. Hot 首次触发时，如果 `ActorEventFastCache<TEvent>` miss，则尝试绑定。
2. 绑定成功后，立即走 `PostFast<TEvent>`。
3. 绑定失败则回退 SafePath。
4. Hot 首次绑定可用元数据数组或构建期表。
5. Hot 首次绑定不应分配闭包、delegate、record 对象。

建议：

```text
Hot 首次绑定也复用 PrewarmHot 的绑定逻辑。
区别只在绑定时机不同。
```

---

## 15. EventMailPool<TEvent> 冷路径创建

```csharp
internal ActorEventFastCache<TEvent> GetOrCreateFastCacheCold<TEvent>()
    where TEvent : struct
{
    /// 该方法只能在构建期 / 首次绑定冷路径调用。
    /// 禁止在 PostFast<TEvent> 热路径调用。

    EventMailPool<TEvent> pool = GetOrCreateEventMailPoolCold<TEvent>();
    var cache = new ActorEventFastCache<TEvent>(pool);

    ActorEventRuntime<TEvent>.BindWorld(
        this,
        cache,
        pool);

    return cache;
}
```

命名建议使用 `Cold` 后缀，明确不能进入热路径。

---

## 16. Query / PostAll 极致路径

Query 内部应保存：

1. `TypedActorStorage<TActor>`
2. `EventMail<TEvent>[]`
3. `DirtySlotList`
4. `bucketIndex`
5. `EventMailPool<TEvent>`

单事件：

```csharp
internal void PostAllFast<TEvent>(
    in TEvent value)
    where TEvent : struct
{
    EventColumn<TActor, TEvent> column = GetRequiredColumn<TEvent>();

    EventMail<TEvent>[] mails = column.Mails;
    DirtySlotList dirtySlots = column.DirtySlots;
    int bucketIndex = column.BucketIndex;
    EventMailPool<TEvent> pool =
        ActorEventRuntime<TEvent>.GetFastCache(World).Pool;

    ReadOnlySpan<int> slots = QuerySelectedSlots;

    for (int i = 0; i < slots.Length; i++)
    {
        int slotIndex = slots[i];

        World.PostQueuedGrowFastNoResult(
            slotIndex,
            in value,
            mails,
            dirtySlots,
            bucketIndex,
            pool);
    }
}
```

多事件必须单次扫描 slot：

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

    EventMailPool<TEvent1> pool1 =
        ActorEventRuntime<TEvent1>.GetFastCache(World).Pool;

    EventMailPool<TEvent2> pool2 =
        ActorEventRuntime<TEvent2>.GetFastCache(World).Pool;

    ReadOnlySpan<int> slots = QuerySelectedSlots;

    for (int i = 0; i < slots.Length; i++)
    {
        int slotIndex = slots[i];

        World.PostQueuedGrowFastNoResult(
            slotIndex,
            in value1,
            mails1,
            dirty1,
            bucket1,
            pool1);

        World.PostQueuedGrowFastNoResult(
            slotIndex,
            in value2,
            mails2,
            dirty2,
            bucket2,
            pool2);
    }
}
```

最终目标：

```text
PostAll<T1...T12> 只扫描一次 slots。
每个 slot 连续写 12 个事件。
不允许每个事件列单独扫描一遍 storage。
```

---

## 17. Benchmark 增补

新增以下 benchmark：

```text
ActorPost_PrewarmHot_PostFast_OneActor_OneEvent
ActorPost_Hot_PostFast_OneActor_OneEvent
ActorPost_PrewarmHot_PostTo_OneActor_OneEvent
ActorPost_PrewarmHot_PostFast_1000Actors_OneEvent
ActorPost_Query_PostAllFast_1000Actors_12Events
```

目的：

```text
PostFast:
  测极致裸数组路径。

PostTo:
  测 public 兼容成本。

PostInside:
  测 Actor 内部扩展方法成本。

Query.PostAllFast:
  测 ECS 批量路径成本。
```

示例：

```csharp
[Benchmark(
    Description = "ActorPost_PrewarmHot_PostFast_OneActor_OneEvent",
    OperationsPerInvoke = SingleActorPostOps)]
[BenchmarkCategory("08.Actor", "ActorRuntime", "HotPath.Final")]
[InvocationCount(1)]
public void ActorPost_PrewarmHot_PostFast_OneActor_OneEvent()
{
    ActorId actorId = _prewarmActor.GetActorId();

    for (int i = 0; i < SingleActorPostOps; i++)
    {
        _prewarmWorld.PostFast(
            actorId,
            PrewarmPostEvent.Instance);
    }
}
```

---

## 18. 实施顺序

### Phase 1：泛型静态 WorldSlot

1. 增加 `ActorWorld.RuntimeIndex`。
2. 增加 `ActorWorldRuntimeIndexAllocator`。
3. 增加 `ActorEventRuntime<TEvent>`。
4. 在构建期绑定 `ActorEventRuntime<TEvent>.BindWorld(...)`。
5. 禁止 `PostFast<TEvent>` 调用 `GetOrCreateFastCache<TEvent>()`。

### Phase 2：FastCache 持有 Pool

1. `ActorEventFastCache<TEvent>` 构造函数接收 `EventMailPool<TEvent>`。
2. `PostFast<TEvent>` 通过 `cache.Pool` 取 pool。
3. 删除 Post 热路径中的 `GetOrCreateEventMailPool<TEvent>()`。

### Phase 3：Dirty mark 数组

1. `DirtySlotList` 改为 `int[] items + int[] marks + stamp`。
2. `DirtyBucketList` 改为 `int[] items + int[] marks + stamp`。
3. Post 热路径改调用 `Mark(...)`。
4. 删除 `AddIfNotExists` 中的线性查重或 HashSet。

### Phase 4：PostFast

1. 新增 `internal bool PostFast<TEvent>`。
2. 新增 `PostQueuedGrowFastNoResult<TEvent>`。
3. public `PostTo<TEvent>` 先尝试 `PostFast`。
4. 内部 Actor / Query 优先走 `PostFast`。

### Phase 5：PrewarmHot 生成式绑定

1. Source Generator 生成 `BindPrewarmHotFastCaches(...)`。
2. Actor 创建时直接调用生成方法。
3. 不通过 `ActorEventColumnRuntime` 虚方法遍历绑定。
4. 不使用反射。

### Phase 6：Query.PostAllFast

1. Query 缓存 typed storage。
2. Query 缓存 selected slots。
3. Query.PostAllFast 单次扫描 slots。
4. 多事件连续写入。
5. 不走 public `PostTo`。

---

## 19. 验收标准

### 19.1 PostFast 热路径

`PostFast<TEvent>` 命中路径不得出现：

```text
Dictionary
object cast
Type
MethodInfo
Reflection
Delegate 路由
virtual 调用
abstract 调用
PostResult
IsAlive
GetSlotState
GetGeneration
GetOrCreateFastCache
GetOrCreateEventMailPool
```

### 19.2 DirtyList

`DirtySlotList.Mark` 和 `DirtyBucketList.Mark` 必须是：

```text
array index
mark compare
append item
```

不得出现：

```text
List.Contains
HashSet
Dictionary
foreach 查重
线性扫描查重
```

### 19.3 缓存路径

PrewarmHot 创建后第一次 Post 必须直接命中 fast cache。

Hot 第一次 Post 可以绑定 cache。

Cold 不创建 cache。

### 19.4 Benchmark

预期趋势：

```text
PostFast < PostTo
PostFast < PostInside
Query.PostAllFast <= PrewarmHot.PostFast per event
Hot cached ~= PrewarmHot cached
Hot first bind 明显慢于 Hot cached
Cold SafePath 明显慢于 Hot/PrewarmHot cached
```

---

## 20. 最终结论

本升级将 Hot / PrewarmHot 从“缓存路由”进一步升级为“裸数组直写”。

最终结构是：

```text
TEvent 泛型静态 Runtime:
  ActorEventRuntime<TEvent>

World 级事件缓存:
  ActorEventFastCache<TEvent>

World 级事件邮箱池:
  EventMailPool<TEvent>

Actor 级快速索引:
  ActorId.FastIndex
  ActorFastState[]

Column 裸数组:
  EventMail<TEvent>[]
  DirtySlotList
  BucketIndex

热路径入口:
  internal bool PostFast<TEvent>
```

最终目标是：

```text
PostFast<TEvent>
  不查字典
  不取 Type
  不走 object cast
  不构造 PostResult
  不走虚方法
  不检查生命周期状态
  不做 DirtyList 线性去重
  只做裸数组访问和 QueuedGrow 写入
```
