# LayerBase Actor Lifecycle Runtime 设计方案

建议文件路径：

```text
docs/actor/actor-lifecycle-runtime-design.md
```

本文档只针对当前 `LayerBase` 中已经存在的 Actor 模块补充生命周期系统。

当前 Actor 模块已经具备：

```text
LayerBase/Actor/Core
LayerBase/Actor/Mail
LayerBase/Actor/Meta
LayerBase/Actor/Pump
LayerBase/Actor/Query
LayerBase/Actor/Storage
```

当前 Actor Runtime 已经具备：

```text
ActorWorld
BehaviourArchetype
TypedActorStorage<TActor>
EventColumn<TActor, TEvent>
EventMail<TEvent>
ActorEventBucket<TEvent>
ActorWorld.CreateActor<TActor>()
ActorWorld.Post<TEvent>()
ActorWorld.Pump(ref RuntimeFrameBudget)
Actor Query / PostAll
```

本文档只设计新增生命周期能力：

```text
IStart
IUpdate
ILateUpdate
IFixedUpdate
IDestroy
Enable
DestroyActor
PendingDestroy
生命周期 FreeList
ActorWorld 生命周期阶段 Pump
```

---

## 1. 设计目标

### 1.1 目标

新增 Actor 级别生命周期系统，使 Actor 不只是事件邮箱接收者，也可以参与 Runtime 帧循环。

目标能力：

```text
1. Actor 创建时自动检测生命周期接口。
2. IStart / IUpdate / ILateUpdate / IFixedUpdate 随用随取。
3. 实现生命周期接口的 Actor 会被预注册到统一 FreeList。
4. Actor slot 保存生命周期 FreeList handle。
5. DestroyActor 时能通过 handle 从生命周期 FreeList 中移除对应条目。
6. Enable=false 时，IUpdate / ILateUpdate / IFixedUpdate 不执行。
7. Enable=false 不影响 IStart。
8. Enable=false 不影响 IDestroy。
9. Enable=false 不影响 ActorBehaviour Post。
10. ActorBehaviour 事件处理先于生命周期接口。
11. 生命周期接口不进入 BehaviourSignature。
12. 生命周期接口不进入 Query。
13. 生命周期接口不进入 EventMail。
```

### 1.2 非目标

本设计不处理：

```text
1. DelayPost。
2. DispatchNow。
3. Diagnostics / Trace / HUD。
4. ActorBehaviourGenerator 改造。
5. Query 系统改造。
6. EventMetaData 改造。
7. Unity / Godot 适配。
8. 多线程生命周期执行。
```

---

## 2. 核心语义

### 2.1 ActorBehaviour 与生命周期顺序

ActorWorld 每帧顺序：

```text
1. 处理 ActorBehaviour 邮箱事件。
2. 清理 ActorBehaviour 阶段请求销毁的 Actor。
3. 调用 IStart。
4. 调用 IFixedUpdate。
5. 调用 IUpdate。
6. 调用 ILateUpdate。
7. 清理生命周期阶段请求销毁的 Actor。
```

设计原因：

```text
1. Post / EventMail / ActorBehaviour 是 Actor Runtime 的行为事件主线。
2. 生命周期接口是帧循环补充能力。
3. 行为事件应先于生命周期处理，避免上一阶段积压事件晚于 Update 生效。
4. ActorBehaviour 中 DestroyActor 后，本帧不应再进入 Update 类生命周期。
5. 生命周期中 DestroyActor 后，本帧末尾统一清理。
```

### 2.2 Enable 语义

```text
Enable=true:
  IUpdate / ILateUpdate / IFixedUpdate 正常执行。

Enable=false:
  IUpdate / ILateUpdate / IFixedUpdate 跳过。
```

Enable 不影响：

```text
IStart
IDestroy
Post
TryPost
ActorBehaviour
DestroyActor
IsAlive
```

设计原因：

```text
Enable 是每帧生命周期接口开关，不是 Actor 存活状态。
```

### 2.3 DestroyActor 语义

```text
DestroyActor:
  只标记 PendingDestroy。
  不立即破坏正在遍历的 EventColumn / DirtySlotList / LifecycleFreeList。

SweepPendingDestroy:
  在 ActorWorld 安全点真正释放 Actor slot。
```

销毁流程：

```text
1. Actor 状态变为 PendingDestroy。
2. 新 Post 失败。
3. Update 类生命周期跳过。
4. 到达 SweepPendingDestroy。
5. 调用 IDestroy。
6. 从生命周期 FreeList 移除。
7. 清理该 slot 的所有 EventMail。
8. 清空 Actor slot。
9. Generation++。
10. slot 回收到 ActorSlotFreeList。
```

---

## 3. 新增目录与文件

新增目录：

```text
LayerBase/Actor/Lifecycle/
```

新增文件：

```text
LayerBase/Actor/Lifecycle/IStart.cs
LayerBase/Actor/Lifecycle/IUpdate.cs
LayerBase/Actor/Lifecycle/ILateUpdate.cs
LayerBase/Actor/Lifecycle/IFixedUpdate.cs
LayerBase/Actor/Lifecycle/IDestroy.cs
LayerBase/Actor/Lifecycle/ActorLifecycleEntry.cs
LayerBase/Actor/Lifecycle/ActorLifecycleHandle.cs
LayerBase/Actor/Lifecycle/ActorLifecycleHandles.cs
LayerBase/Actor/Lifecycle/ActorLifecycleFreeList.cs
LayerBase/Actor/Lifecycle/ActorLifecycleScheduler.cs
LayerBase/Actor/Lifecycle/LifecycleFrameState.cs
```

新增 Storage 文件：

```text
LayerBase/Actor/Storage/ActorSlotState.cs
LayerBase/Actor/Storage/ActorWorld.Lifecycle.cs
LayerBase/Actor/Storage/ActorWorld.Destroy.cs
```

修改现有文件：

```text
LayerBase/Actor/Storage/ActorWorld.cs
LayerBase/Actor/Storage/ActorWorld.Create.cs
LayerBase/Actor/Storage/ActorWorld.Pump.cs
LayerBase/Actor/Storage/BehaviourArchetype.cs
LayerBase/Actor/Storage/TypedStorageRuntime.cs
LayerBase/Actor/Storage/TypedActorStorage.cs
LayerBase/Actor/Mail/ActorEventColumnRuntime.cs
LayerBase/Actor/Mail/EventColumn.cs
LayerBase/Actor/Mail/EventMailReader.cs
LayerBase/Application/LayerRuntime.cs
```

---

## 4. 生命周期接口

### 4.1 IStart

```csharp
namespace LayerBase.Actor;

public interface IStart
{
    void Start();
}
```

语义：

```text
1. Actor 创建后，如果实现 IStart，则注册到 Start FreeList。
2. IStart 在 ActorBehaviour 事件处理之后调用。
3. IStart 只调用一次。
4. IStart 调用后从 Start FreeList 移除。
5. IStart 不受 Enable=false 影响。
```

### 4.2 IUpdate

```csharp
namespace LayerBase.Actor;

public interface IUpdate
{
    void Update(float deltaTime);
}
```

语义：

```text
1. Actor 创建后，如果实现 IUpdate，则注册到 Update FreeList。
2. 每帧 ActorBehaviour 事件处理之后调用。
3. Enable=false 时跳过。
4. PendingDestroy 时跳过。
```

参数说明：

```text
deltaTime:
  当前帧间隔时间，单位通常是秒。
```

### 4.3 ILateUpdate

```csharp
namespace LayerBase.Actor;

public interface ILateUpdate
{
    void LateUpdate(float deltaTime);
}
```

语义：

```text
1. Actor 创建后，如果实现 ILateUpdate，则注册到 LateUpdate FreeList。
2. 每帧 IUpdate 之后调用。
3. Enable=false 时跳过。
4. PendingDestroy 时跳过。
```

参数说明：

```text
deltaTime:
  当前帧间隔时间，单位通常是秒。
```

### 4.4 IFixedUpdate

```csharp
namespace LayerBase.Actor;

public interface IFixedUpdate
{
    void FixedUpdate(float fixedDeltaTime);
}
```

语义：

```text
1. Actor 创建后，如果实现 IFixedUpdate，则注册到 FixedUpdate FreeList。
2. 在 ActorWorld 生命周期阶段调用。
3. Enable=false 时跳过。
4. PendingDestroy 时跳过。
```

参数说明：

```text
fixedDeltaTime:
  固定逻辑步长，单位通常是秒。
```

### 4.5 IDestroy

```csharp
namespace LayerBase.Actor;

public interface IDestroy
{
    void Destroy();
}
```

语义：

```text
1. IDestroy 不注册到 FreeList。
2. Actor 真正释放 slot 前，如果实现 IDestroy，则直接调用 Destroy。
3. Enable=false 不影响 IDestroy。
4. IDestroy 异常不吞掉。
```

---

## 5. ActorSlotState

新增文件：

```text
LayerBase/Actor/Storage/ActorSlotState.cs
```

```csharp
namespace LayerBase.Actor;

internal enum ActorSlotState : byte
{
    Empty = 0,
    Alive = 1,
    PendingDestroy = 2
}
```

状态说明：

```text
Empty:
  当前 slot 没有 Actor，可被 ActorSlotFreeList 复用。

Alive:
  当前 slot 持有有效 Actor。
  Post、Query、ActorBehaviour、生命周期接口可以正常访问。

PendingDestroy:
  Actor 已请求销毁，但还没有到安全点释放。
  新 Post 失败。
  IUpdate / ILateUpdate / IFixedUpdate 跳过。
```

---

## 6. Enable 存储与 API

### 6.1 存储位置

`Enable` 放在 `TypedActorStorage<TActor>` 的 slot 数组中：

```csharp
private bool[] _enabled;
```

默认规则：

```text
1. AllocateSlot 时设置为 true。
2. DestroyActor 标记 PendingDestroy 时设置为 false。
3. DestroyNow 释放 slot 时设置为 false。
4. slot 复用时重新设置为 true。
```

### 6.2 ActorWorld API

新增文件：

```text
LayerBase/Actor/Storage/ActorWorld.Lifecycle.cs
```

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool IsEnable(ActorId actorId)
    {
        // actorId 参数表示目标 Actor 的运行时句柄。
        // 返回 true 表示该 Actor 的 IUpdate / ILateUpdate / IFixedUpdate 可以执行。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].IsEnable(actorId);
    }

    public bool SetEnable(ActorId actorId, bool enable)
    {
        // actorId 参数表示目标 Actor 的运行时句柄。
        // enable 参数表示是否允许 IUpdate / ILateUpdate / IFixedUpdate 执行。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].SetEnable(
            actorId: actorId,
            enable: enable);
    }
}
```

### 6.3 ActorContext API

在 `ActorContext` 中增加：

```csharp
namespace LayerBase.Actor;

public readonly struct ActorContext
{
    public bool IsEnable()
    {
        // 返回当前 Actor 的 Enable 状态。
        return World.IsEnable(ActorId);
    }

    public bool SetEnable(bool enable)
    {
        // enable 参数表示是否允许当前 Actor 的 Update 类生命周期接口执行。
        return World.SetEnable(
            actorId: ActorId,
            enable: enable);
    }
}

同时改 IGeneratedActorMeta、ActorExtensions 和 ActorBehaviourGenerator。

IGeneratedActorMeta:
  新增 GetEnable / SetEnable。
  因为 IActor 是空接口，ActorExtensions 只能通过 IGeneratedActorMeta 访问运行时能力。

ActorExtensions:
  新增 GetEnable(this IActor) / SetEnable(this IActor, bool)。
  内部通过 ActorGeneratedAccess.RequireGenerated(actor) 转发。

ActorBehaviourGenerator:
  生成 IGeneratedActorMeta.GetEnable / SetEnable 的显式接口实现。
  实现内部转发到 __actorContext.GetEnable / SetEnable。

Post 策略说明:
  ActorContext.Post、IGeneratedActorMeta.Post、ActorExtensions.Post 当前不暴露策略覆盖参数。
  它们使用 EventColumn 创建时缓存的 ActorMailOptions。
  ActorMailOptions 来自 EventMetaData<TEvent>.ActorMailOptions。
  如果事件没有元数据或元数据没有配置 ActorMailOptions，则回退到 ActorMailOptions.Default。
  当前 ActorMailOptions.Default 的 FullPolicy 是 Grow，因此无元数据事件默认邮箱满时扩容。

```

说明：

```text
ActorContext 已经持有 ActorWorld 和 ActorId。
因此 Enable API 放到 ActorContext 中最自然，不需要改 IActor。
```

---

## 7. 生命周期 FreeList 数据结构

### 7.1 ActorLifecycleEntry

新增文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleEntry.cs
```

```csharp
namespace LayerBase.Actor;

internal readonly struct ActorLifecycleEntry<TLifecycle>
    where TLifecycle : class
{
    public readonly ActorId ActorId;
    public readonly TLifecycle Instance;

    public ActorLifecycleEntry(
        ActorId actorId,
        TLifecycle instance)
    {
        // actorId 参数表示该生命周期接口所属 Actor。
        // 遍历时通过 ActorId 检查 Actor 是否仍然 Alive，以及 Enable 是否开启。
        ActorId = actorId;

        // instance 参数表示具体生命周期接口实例。
        // 例如 IStart、IUpdate、ILateUpdate 或 IFixedUpdate。
        Instance = instance;
    }
}
```

### 7.2 ActorLifecycleHandle

新增文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleHandle.cs
```

```csharp
namespace LayerBase.Actor;

internal readonly struct ActorLifecycleHandle
{
    public static ActorLifecycleHandle Invalid => new ActorLifecycleHandle(-1, 0);

    public readonly int Index;
    public readonly int Version;

    public ActorLifecycleHandle(
        int index,
        int version)
    {
        // index 参数表示该条目在生命周期 FreeList 中的位置。
        Index = index;

        // version 参数用于避免 FreeList 位置复用后旧 handle 错删新条目。
        Version = version;
    }

    public bool IsValid => Index >= 0;
}
```

### 7.3 ActorLifecycleHandles

新增文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleHandles.cs
```

```csharp
namespace LayerBase.Actor;

internal struct ActorLifecycleHandles
{
    public ActorLifecycleHandle Start;
    public ActorLifecycleHandle Update;
    public ActorLifecycleHandle LateUpdate;
    public ActorLifecycleHandle FixedUpdate;

    public static ActorLifecycleHandles Empty => new ActorLifecycleHandles
    {
        Start = ActorLifecycleHandle.Invalid,
        Update = ActorLifecycleHandle.Invalid,
        LateUpdate = ActorLifecycleHandle.Invalid,
        FixedUpdate = ActorLifecycleHandle.Invalid
    };
}
```

说明：

```text
IDestroy 不需要 handle。
因为 IDestroy 不参与每帧遍历，只在 DestroyNow 前通过 Actor 实例直接调用。
```

### 7.4 LifecycleFrameState

新增文件：

```text
LayerBase/Actor/Lifecycle/LifecycleFrameState.cs
```

```csharp
namespace LayerBase.Actor;

internal readonly struct LifecycleFrameState
{
    public readonly ActorWorld World;
    public readonly float DeltaTime;

    public LifecycleFrameState(
        ActorWorld world,
        float deltaTime)
    {
        // world 参数表示所属 ActorWorld。
        // 生命周期遍历时通过它检查 Actor 是否 Alive、是否 Enable。
        World = world;

        // deltaTime 参数表示当前生命周期阶段使用的时间步长。
        DeltaTime = deltaTime;
    }
}
```

---

## 8. ActorLifecycleFreeList

新增文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleFreeList.cs
```

```csharp
namespace LayerBase.Actor;

internal sealed class ActorLifecycleFreeList<TLifecycle>
    where TLifecycle : class
{
    private ActorLifecycleEntry<TLifecycle>[] _entries =
        new ActorLifecycleEntry<TLifecycle>[4];

    private int[] _versions = new int[4];

    private bool[] _occupied = new bool[4];

    private int[] _free = new int[4];

    private int _freeCount;

    private int _count;

    public ActorLifecycleHandle Add(
        ActorId actorId,
        TLifecycle instance)
    {
        // actorId 参数表示生命周期接口所属 Actor。
        // instance 参数表示具体生命周期接口实例。
        int index;

        if (_freeCount > 0)
        {
            _freeCount--;
            index = _free[_freeCount];
        }
        else
        {
            index = _count;
            _count++;
            EnsureCapacity(index + 1);
        }

        _entries[index] = new ActorLifecycleEntry<TLifecycle>(
            actorId: actorId,
            instance: instance);

        _occupied[index] = true;

        return new ActorLifecycleHandle(
            index: index,
            version: _versions[index]);
    }

    public bool Remove(ActorLifecycleHandle handle)
    {
        // handle 参数表示 Add 时返回的生命周期条目位置。
        // Version 不匹配时，说明该位置已经被释放并复用，不能删除。
        if (!handle.IsValid)
        {
            return false;
        }

        if ((uint)handle.Index >= (uint)_entries.Length)
        {
            return false;
        }

        if (!_occupied[handle.Index])
        {
            return false;
        }

        if (_versions[handle.Index] != handle.Version)
        {
            return false;
        }

        _entries[handle.Index] = default;
        _occupied[handle.Index] = false;

        unchecked
        {
            _versions[handle.Index]++;
        }

        if (_freeCount == _free.Length)
        {
            Array.Resize(ref _free, _free.Length * 2);
        }

        _free[_freeCount] = handle.Index;
        _freeCount++;

        return true;
    }

    public void ForEach<TState>(
        ref TState state,
        LifecycleInvoker<TLifecycle, TState> invoker)
    {
        // state 参数表示遍历上下文。
        // invoker 参数表示具体生命周期调用逻辑。
        for (int i = 0; i < _count; i++)
        {
            if (!_occupied[i])
            {
                continue;
            }

            invoker(
                entry: in _entries[i],
                state: ref state);
        }
    }

    public void ForEachRemoveIf<TState>(
        ref TState state,
        LifecycleRemovePredicate<TLifecycle, TState> predicate)
    {
        // state 参数表示遍历上下文。
        // predicate 参数返回 true 时移除当前条目。
        for (int i = 0; i < _count; i++)
        {
            if (!_occupied[i])
            {
                continue;
            }

            bool remove = predicate(
                entry: in _entries[i],
                state: ref state);

            if (!remove)
            {
                continue;
            }

            Remove(new ActorLifecycleHandle(
                index: i,
                version: _versions[i]));
        }
    }

    private void EnsureCapacity(int required)
    {
        // required 参数表示需要的最小容量。
        if (required <= _entries.Length)
        {
            return;
        }

        int newSize = _entries.Length == 0 ? 4 : _entries.Length;

        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _entries, newSize);
        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _occupied, newSize);
    }
}

internal delegate void LifecycleInvoker<TLifecycle, TState>(
    in ActorLifecycleEntry<TLifecycle> entry,
    ref TState state)
    where TLifecycle : class;

internal delegate bool LifecycleRemovePredicate<TLifecycle, TState>(
    in ActorLifecycleEntry<TLifecycle> entry,
    ref TState state)
    where TLifecycle : class;
```

---

## 9. ActorLifecycleScheduler

新增文件：

```text
LayerBase/Actor/Lifecycle/ActorLifecycleScheduler.cs
```

```csharp
namespace LayerBase.Actor;

internal sealed class ActorLifecycleScheduler
{
    private readonly ActorWorld _world;

    private readonly ActorLifecycleFreeList<IStart> _starts = new();

    private readonly ActorLifecycleFreeList<IUpdate> _updates = new();

    private readonly ActorLifecycleFreeList<ILateUpdate> _lateUpdates = new();

    private readonly ActorLifecycleFreeList<IFixedUpdate> _fixedUpdates = new();

    public ActorLifecycleScheduler(ActorWorld world)
    {
        // world 参数表示所属 ActorWorld。
        // 遍历生命周期时需要用它检查 ActorId 是否仍然 Alive，以及 Enable 是否开启。
        _world = world;
    }

    public ActorLifecycleHandle AddStart(
        ActorId actorId,
        IStart start)
    {
        // actorId 参数表示实现 IStart 的 Actor。
        // start 参数表示 IStart 接口实例。
        return _starts.Add(
            actorId: actorId,
            instance: start);
    }

    public ActorLifecycleHandle AddUpdate(
        ActorId actorId,
        IUpdate update)
    {
        // actorId 参数表示实现 IUpdate 的 Actor。
        // update 参数表示 IUpdate 接口实例。
        return _updates.Add(
            actorId: actorId,
            instance: update);
    }

    public ActorLifecycleHandle AddLateUpdate(
        ActorId actorId,
        ILateUpdate lateUpdate)
    {
        // actorId 参数表示实现 ILateUpdate 的 Actor。
        // lateUpdate 参数表示 ILateUpdate 接口实例。
        return _lateUpdates.Add(
            actorId: actorId,
            instance: lateUpdate);
    }

    public ActorLifecycleHandle AddFixedUpdate(
        ActorId actorId,
        IFixedUpdate fixedUpdate)
    {
        // actorId 参数表示实现 IFixedUpdate 的 Actor。
        // fixedUpdate 参数表示 IFixedUpdate 接口实例。
        return _fixedUpdates.Add(
            actorId: actorId,
            instance: fixedUpdate);
    }

    public void RemoveStart(ActorLifecycleHandle handle)
    {
        // handle 参数表示 IStart FreeList 中的条目位置。
        _starts.Remove(handle);
    }

    public void RemoveUpdate(ActorLifecycleHandle handle)
    {
        // handle 参数表示 IUpdate FreeList 中的条目位置。
        _updates.Remove(handle);
    }

    public void RemoveLateUpdate(ActorLifecycleHandle handle)
    {
        // handle 参数表示 ILateUpdate FreeList 中的条目位置。
        _lateUpdates.Remove(handle);
    }

    public void RemoveFixedUpdate(ActorLifecycleHandle handle)
    {
        // handle 参数表示 IFixedUpdate FreeList 中的条目位置。
        _fixedUpdates.Remove(handle);
    }

    public void PumpStart()
    {
        // IStart 是一次性生命周期。
        // 调用后从 Start FreeList 移除。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: 0f);

        _starts.ForEachRemoveIf(
            state: ref state,
            predicate: static (
                in ActorLifecycleEntry<IStart> entry,
                ref LifecycleFrameState state) =>
            {
                // entry 参数表示当前 IStart 条目。
                // state 参数提供 ActorWorld。
                if (!state.World.IsAlive(entry.ActorId))
                {
                    return true;
                }

                entry.Instance.Start();

                return true;
            });
    }

    public void PumpFixedUpdate(float fixedDeltaTime)
    {
        // fixedDeltaTime 参数表示固定逻辑步长。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: fixedDeltaTime);

        _fixedUpdates.ForEach(
            state: ref state,
            invoker: static (
                in ActorLifecycleEntry<IFixedUpdate> entry,
                ref LifecycleFrameState state) =>
            {
                // entry 参数表示当前 IFixedUpdate 条目。
                // state 参数提供 ActorWorld 和 fixedDeltaTime。
                if (!state.World.IsAlive(entry.ActorId))
                {
                    return;
                }

                if (!state.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.FixedUpdate(state.DeltaTime);
            });
    }

    public void PumpUpdate(float deltaTime)
    {
        // deltaTime 参数表示当前帧间隔。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: deltaTime);

        _updates.ForEach(
            state: ref state,
            invoker: static (
                in ActorLifecycleEntry<IUpdate> entry,
                ref LifecycleFrameState state) =>
            {
                // entry 参数表示当前 IUpdate 条目。
                // state 参数提供 ActorWorld 和 deltaTime。
                if (!state.World.IsAlive(entry.ActorId))
                {
                    return;
                }

                if (!state.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.Update(state.DeltaTime);
            });
    }

    public void PumpLateUpdate(float deltaTime)
    {
        // deltaTime 参数表示当前帧间隔。
        var state = new LifecycleFrameState(
            world: _world,
            deltaTime: deltaTime);

        _lateUpdates.ForEach(
            state: ref state,
            invoker: static (
                in ActorLifecycleEntry<ILateUpdate> entry,
                ref LifecycleFrameState state) =>
            {
                // entry 参数表示当前 ILateUpdate 条目。
                // state 参数提供 ActorWorld 和 deltaTime。
                if (!state.World.IsAlive(entry.ActorId))
                {
                    return;
                }

                if (!state.World.IsEnable(entry.ActorId))
                {
                    return;
                }

                entry.Instance.LateUpdate(state.DeltaTime);
            });
    }
}
```

---

## 10. ActorWorld 接入 Lifecycle

修改 `ActorWorld.cs`。

新增字段：

```csharp
internal ActorLifecycleScheduler Lifecycle { get; }

private bool _hasPendingDestroy;
```

所有构造函数都初始化 `Lifecycle`：

```csharp
internal ActorWorld()
{
    DefaultMailOptions = ActorMailOptions.Default;
    Lifecycle = new ActorLifecycleScheduler(this);
}

internal ActorWorld(ActorMailOptions defaultMailOptions)
{
    DefaultMailOptions = defaultMailOptions;
    Lifecycle = new ActorLifecycleScheduler(this);
}

internal ActorWorld(LayerRuntime runtime)
{
    // runtime 参数表示所属 LayerRuntime。
    Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    DefaultMailOptions = ActorMailOptions.Default;
    Lifecycle = new ActorLifecycleScheduler(this);
}
```

---

## 11. ActorWorld.CreateActor 接入生命周期注册

修改 `ActorWorld.Create.cs`。

目标流程：

```text
1. new TActor。
2. 获取 IGeneratedActorMeta。
3. 构建 ActorTypeMeta。
4. 获取 BehaviourArchetype。
5. 获取 TypedActorStorage<TActor>。
6. AllocateSlot。
7. 创建 ActorId。
8. ActorInit。
9. RegisterLifecycleInterfaces。
10. return actor。
```

示例代码：

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public TActor CreateActor<TActor>()
        where TActor : class, IActor, new()
    {
        // 创建 Actor 实例。
        TActor actor = new TActor();

        // 获取生成器补出的运行时能力。
        IGeneratedActorMeta generated = ActorGeneratedAccess.RequireGenerated(actor);

        // 获取或构建当前 Actor 类型的行为元数据。
        ActorTypeMeta<TActor> meta = ActorTypeMetaCache.GetOrBuild<TActor>(generated);

        // 根据 BehaviourSignature 找到或创建 BehaviourArchetype。
        BehaviourArchetype archetype = GetOrCreateArchetype(meta.Signature);

        // 在 BehaviourArchetype 内找到或创建当前 TActor 的强类型 storage。
        TypedActorStorage<TActor> storage = archetype.GetOrCreateStorage<TActor>(
            meta: meta,
            world: this);

        // 分配 Actor slot。
        int slotIndex = storage.AllocateSlot(actor);

        // 创建 ActorId。
        ActorId actorId = new ActorId(
            archetypeId: archetype.ArchetypeId,
            typeStorageIndex: storage.TypeStorageIndex,
            slotIndex: slotIndex,
            generation: storage.GetGeneration(slotIndex));

        // 注入 ActorContext。
        generated.ActorInit(new ActorContext(this, actorId));

        // 注册生命周期接口。
        // 该步骤只在创建冷路径执行，不会进入每帧热路径。
        storage.RegisterLifecycleInterfaces(
            actor: actor,
            actorId: actorId,
            slotIndex: slotIndex,
            world: this);

        return actor;
    }
}
```

---

## 12. TypedActorStorage 接入生命周期

### 12.1 字段

修改 `TypedActorStorage<TActor>`：

```csharp
private ActorSlotState[] _states;

private bool[] _enabled;

private ActorLifecycleHandles[] _lifecycleHandles;
```

### 12.2 构造函数

```csharp
public TypedActorStorage(
    ushort typeStorageIndex,
    int maxEventTypeId,
    int initialCapacity)
{
    // typeStorageIndex 参数表示当前 storage 在 BehaviourArchetype 内的下标。
    TypeStorageIndex = typeStorageIndex;

    // maxEventTypeId 参数用于创建 EventColumn 直接索引数组。
    _columnsByEventId = new ActorEventColumnRuntime[Math.Max(maxEventTypeId + 1, 1)];

    int capacity = Math.Max(initialCapacity, 1);

    _actors = new TActor?[capacity];
    _generations = new int[capacity];
    _states = new ActorSlotState[capacity];
    _enabled = new bool[capacity];
    _lifecycleHandles = new ActorLifecycleHandles[capacity];

    for (int i = 0; i < _lifecycleHandles.Length; i++)
    {
        _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
    }

    _freeList = new ActorSlotFreeList(capacity);
    _nextSlotIndex = 0;
}
```

### 12.3 EnsureActorCapacity

```csharp
private void EnsureActorCapacity(int required)
{
    // required 参数表示需要容纳的最小 slot 数量。
    if (required <= _actors.Length)
    {
        return;
    }

    int oldSize = _actors.Length;
    int newSize = _actors.Length == 0 ? 4 : _actors.Length;

    while (newSize < required)
    {
        newSize *= 2;
    }

    Array.Resize(ref _actors, newSize);
    Array.Resize(ref _generations, newSize);
    Array.Resize(ref _states, newSize);
    Array.Resize(ref _enabled, newSize);
    Array.Resize(ref _lifecycleHandles, newSize);

    for (int i = oldSize; i < newSize; i++)
    {
        _lifecycleHandles[i] = ActorLifecycleHandles.Empty;
    }
}
```

### 12.4 AllocateSlot

```csharp
public int AllocateSlot(TActor actor)
{
    // actor 参数是要写入 storage 的 Actor 实例。
    // slotIndex 会同时用于 Actor 数组、Generation 数组、生命周期数组和 EventMail 数组。
    int slotIndex = _freeList.TryPop(out int freeSlot)
        ? freeSlot
        : AllocateNewSlot();

    _actors[slotIndex] = actor;
    _states[slotIndex] = ActorSlotState.Alive;
    _enabled[slotIndex] = true;
    _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

    EnsureColumnCapacity(slotIndex);

    return slotIndex;
}
```

### 12.5 IsAlive

```csharp
public override bool IsAlive(int slotIndex, int generation)
{
    // slotIndex 参数来自 ActorId。
    // generation 参数用于防止旧 ActorId 命中新 Actor。
    return (uint)slotIndex < (uint)_actors.Length
           && _states[slotIndex] == ActorSlotState.Alive
           && _actors[slotIndex] != null
           && _generations[slotIndex] == generation;
}
```

### 12.6 IsAliveSlot

```csharp
internal bool IsAliveSlot(int slotIndex)
{
    // slotIndex 参数表示 storage 内部 slot。
    // EventColumn.PumpOne 使用该方法避免调用已销毁 Actor。
    return (uint)slotIndex < (uint)_actors.Length
           && _states[slotIndex] == ActorSlotState.Alive
           && _actors[slotIndex] != null;
}
```

### 12.7 IsEnable / SetEnable

```csharp
public override bool IsEnable(int slotIndex, int generation)
{
    // slotIndex 参数来自 ActorId。
    // generation 参数用于防止旧 ActorId 查询新 Actor。
    return IsAlive(slotIndex, generation)
           && _enabled[slotIndex];
}

public override bool SetEnable(int slotIndex, int generation, bool enable)
{
    // slotIndex 参数来自 ActorId。
    // generation 参数用于防止旧 ActorId 修改新 Actor。
    // enable 参数表示是否允许 Update 类生命周期接口执行。
    if (!IsAlive(slotIndex, generation))
    {
        return false;
    }

    _enabled[slotIndex] = enable;
    return true;
}
```

### 12.8 RegisterLifecycleInterfaces

```csharp
internal void RegisterLifecycleInterfaces(
    TActor actor,
    ActorId actorId,
    int slotIndex,
    ActorWorld world)
{
    // actor 参数表示刚创建的 Actor 实例。
    // actorId 参数表示该 Actor 的运行时句柄。
    // slotIndex 参数表示 Actor 在当前 TypedActorStorage 中的 slot。
    // world 参数提供统一 ActorLifecycleScheduler。
    ActorLifecycleHandles handles = ActorLifecycleHandles.Empty;

    if (actor is IStart start)
    {
        handles.Start = world.Lifecycle.AddStart(
            actorId: actorId,
            start: start);
    }

    if (actor is IUpdate update)
    {
        handles.Update = world.Lifecycle.AddUpdate(
            actorId: actorId,
            update: update);
    }

    if (actor is ILateUpdate lateUpdate)
    {
        handles.LateUpdate = world.Lifecycle.AddLateUpdate(
            actorId: actorId,
            lateUpdate: lateUpdate);
    }

    if (actor is IFixedUpdate fixedUpdate)
    {
        handles.FixedUpdate = world.Lifecycle.AddFixedUpdate(
            actorId: actorId,
            fixedUpdate: fixedUpdate);
    }

    _lifecycleHandles[slotIndex] = handles;
}
```

---

## 13. TypedStorageRuntime 扩展

修改 `TypedStorageRuntime.cs`：

```csharp
namespace LayerBase.Actor;

using LayerBase.Core.Event;

internal abstract class TypedStorageRuntime
{
    public abstract bool IsAlive(int slotIndex, int generation);

    public abstract bool IsEnable(int slotIndex, int generation);

    public abstract bool SetEnable(int slotIndex, int generation, bool enable);

    public abstract bool MarkPendingDestroy(int slotIndex, int generation);

    public abstract void SweepPendingDestroy(ActorWorld world);

    public abstract PostResult Post<TEvent>(
        int slotIndex,
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct;

    public abstract void PostToAliveActors<TEvent>(
        in TEvent value,
        ActorPostPolicy? postPolicy,
        ActorMailFullPolicy? fullPolicy)
        where TEvent : struct;

    public abstract IEnumerable<IActor> EnumerateActors();
}
```

---

## 14. BehaviourArchetype 扩展

在 `BehaviourArchetype` 中新增：

```csharp
internal bool IsAlive(ActorId actorId)
{
    // actorId 参数表示要检查的 Actor。
    ushort storageIndex = actorId.TypeStorageIndex;

    if ((uint)storageIndex >= (uint)_storages.Length)
    {
        return false;
    }

    return _storages[storageIndex].IsAlive(
        slotIndex: actorId.SlotIndex,
        generation: actorId.Generation);
}

internal bool IsEnable(ActorId actorId)
{
    // actorId 参数表示要检查 Enable 的 Actor。
    ushort storageIndex = actorId.TypeStorageIndex;

    if ((uint)storageIndex >= (uint)_storages.Length)
    {
        return false;
    }

    return _storages[storageIndex].IsEnable(
        slotIndex: actorId.SlotIndex,
        generation: actorId.Generation);
}

internal bool SetEnable(ActorId actorId, bool enable)
{
    // actorId 参数表示目标 Actor。
    // enable 参数表示是否允许 Update 类生命周期接口执行。
    ushort storageIndex = actorId.TypeStorageIndex;

    if ((uint)storageIndex >= (uint)_storages.Length)
    {
        return false;
    }

    return _storages[storageIndex].SetEnable(
        slotIndex: actorId.SlotIndex,
        generation: actorId.Generation,
        enable: enable);
}

internal bool MarkPendingDestroy(ActorId actorId)
{
    // actorId 参数表示要标记销毁的 Actor。
    ushort storageIndex = actorId.TypeStorageIndex;

    if ((uint)storageIndex >= (uint)_storages.Length)
    {
        return false;
    }

    return _storages[storageIndex].MarkPendingDestroy(
        slotIndex: actorId.SlotIndex,
        generation: actorId.Generation);
}

internal void SweepPendingDestroy(ActorWorld world)
{
    // world 参数提供生命周期调度器，用于移除 FreeList 条目。
    foreach (TypedStorageRuntime storage in _storages)
    {
        storage.SweepPendingDestroy(world);
    }
}
```

---

## 15. DestroyActor 实现

新增文件：

```text
LayerBase/Actor/Storage/ActorWorld.Destroy.cs
```

```csharp
namespace LayerBase.Actor;

public sealed partial class ActorWorld
{
    public bool DestroyActor(ActorId actorId)
    {
        // actorId 参数表示要销毁的 Actor。
        // DestroyActor 不直接破坏正在遍历的邮箱或生命周期列表。
        // 它只标记 PendingDestroy，真正释放发生在 SweepPendingDestroy。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        bool marked = _archetypes[actorId.ArchetypeId].MarkPendingDestroy(actorId);

        if (marked)
        {
            _hasPendingDestroy = true;
        }

        return marked;
    }

    public bool IsAlive(ActorId actorId)
    {
        // actorId 参数表示要检查的 Actor 运行时句柄。
        // PendingDestroy 不算 Alive。
        if ((uint)actorId.ArchetypeId >= (uint)_archetypes.Length)
        {
            return false;
        }

        return _archetypes[actorId.ArchetypeId].IsAlive(actorId);
    }

    private void SweepPendingDestroy()
    {
        // 清理所有 PendingDestroy Actor。
        // 这是 ActorWorld 的销毁安全点。
        if (!_hasPendingDestroy)
        {
            return;
        }

        foreach (BehaviourArchetype archetype in _archetypes)
        {
            archetype.SweepPendingDestroy(this);
        }

        _hasPendingDestroy = false;
    }
}
```

---

## 16. TypedActorStorage 销毁实现

### 16.1 MarkPendingDestroy

```csharp
public override bool MarkPendingDestroy(int slotIndex, int generation)
{
    // slotIndex 参数来自 ActorId。
    // generation 参数用于避免旧 ActorId 标记新 Actor。
    if (!IsAlive(slotIndex, generation))
    {
        return false;
    }

    _states[slotIndex] = ActorSlotState.PendingDestroy;
    _enabled[slotIndex] = false;

    return true;
}
```

### 16.2 SweepPendingDestroy

```csharp
public override void SweepPendingDestroy(ActorWorld world)
{
    // world 参数提供 ActorLifecycleScheduler。
    // 遍历所有 slot，释放 PendingDestroy Actor。
    int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (_states[slotIndex] != ActorSlotState.PendingDestroy)
        {
            continue;
        }

        DestroyNow(
            slotIndex: slotIndex,
            generation: _generations[slotIndex],
            world: world);
    }
}
```

### 16.3 DestroyNow

```csharp
private bool DestroyNow(
    int slotIndex,
    int generation,
    ActorWorld world)
{
    // slotIndex 参数表示要释放的 Actor slot。
    // generation 参数用于确认 ActorId 仍然匹配当前 slot。
    // world 参数用于移除生命周期 FreeList 条目。
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

    UnregisterLifecycleInterfaces(
        slotIndex: slotIndex,
        world: world);

    ClearAllMails(slotIndex);

    _actors[slotIndex] = null;
    _enabled[slotIndex] = false;
    _states[slotIndex] = ActorSlotState.Empty;
    _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;

    unchecked
    {
        _generations[slotIndex]++;
    }

    _freeList.Push(slotIndex);

    return true;
}
```

### 16.4 UnregisterLifecycleInterfaces

```csharp
private void UnregisterLifecycleInterfaces(
    int slotIndex,
    ActorWorld world)
{
    // slotIndex 参数表示即将销毁的 Actor slot。
    // world 参数提供统一 ActorLifecycleScheduler。
    ActorLifecycleHandles handles = _lifecycleHandles[slotIndex];

    world.Lifecycle.RemoveStart(handles.Start);
    world.Lifecycle.RemoveUpdate(handles.Update);
    world.Lifecycle.RemoveLateUpdate(handles.LateUpdate);
    world.Lifecycle.RemoveFixedUpdate(handles.FixedUpdate);

    _lifecycleHandles[slotIndex] = ActorLifecycleHandles.Empty;
}
```

---

## 17. EventMail 清理

### 17.1 ActorEventColumnRuntime

修改：

```csharp
namespace LayerBase.Actor;

internal abstract class ActorEventColumnRuntime
{
    public abstract void EnsureSlotCapacity(int slotIndex);

    public abstract void ClearMail(int slotIndex);
}
```

### 17.2 EventColumn.ClearMail

```csharp
public override void ClearMail(int slotIndex)
{
    // slotIndex 参数表示要释放邮箱的 Actor slot。
    // DestroyActor 时调用，确保该 Actor 不再残留待处理事件。
    if ((uint)slotIndex >= (uint)_mails.Length)
    {
        return;
    }

    ref EventMail<TEvent> mail = ref _mails[slotIndex];

    EventMailReader.ForceRelease(
        mail: ref mail,
        bufferPool: _bufferPool);
}
```

### 17.3 EventMailReader.ForceRelease

```csharp
internal static class EventMailReader
{
    public static void ForceRelease<TEvent>(
        ref EventMail<TEvent> mail,
        RingQueueBuffer<TEvent> bufferPool)
        where TEvent : struct
    {
        // mail 参数表示要强制释放的邮箱。
        // bufferPool 参数表示邮箱租用的 buffer 池。
        if (mail.BufferId != 0)
        {
            bufferPool.Release(mail.BufferId);
        }

        mail = default;
    }
}
```

如果当前 `RingQueueBuffer<TEvent>` 的释放方法不是 `Release`，以现有实现为准改名。

### 17.4 TypedActorStorage.ClearAllMails

```csharp
private void ClearAllMails(int slotIndex)
{
    // slotIndex 参数表示被销毁 Actor 的 slot。
    // 所有 EventColumn 都需要清理该 slot 上的 EventMail。
    foreach (ActorEventColumnRuntime? column in _columnsByEventId)
    {
        column?.ClearMail(slotIndex);
    }
}
```

说明：

```text
DirtySlotList 中可能还残留该 slot。
ClearMail 不要求随机删除 DirtySlot。
后续 PumpOne 遇到非 Alive slot 或空邮箱时自然跳过。
```

---

## 18. PostToAliveActors 与 EnumerateActors 调整

### 18.1 PostToAliveActors

```csharp
public override void PostToAliveActors<TEvent>(
    in TEvent value,
    ActorPostPolicy? postPolicy,
    ActorMailFullPolicy? fullPolicy)
{
    // value 参数表示要批量投递给所有 Alive Actor 的事件。
    int eventId = EventTypeId<TEvent>.Id;

    if ((uint)eventId >= (uint)_columnsByEventId.Length)
    {
        return;
    }

    if (_columnsByEventId[eventId] is not EventColumn<TActor, TEvent> column)
    {
        return;
    }

    int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (_states[slotIndex] != ActorSlotState.Alive)
        {
            continue;
        }

        if (_actors[slotIndex] == null)
        {
            continue;
        }

        _ = column.Post(
            slotIndex: slotIndex,
            value: in value,
            postPolicy: postPolicy,
            fullPolicy: fullPolicy);
    }
}
```

### 18.2 EnumerateActors

```csharp
public override IEnumerable<IActor> EnumerateActors()
{
    int maxSlot = Math.Min(_nextSlotIndex, _actors.Length);

    for (int slotIndex = 0; slotIndex < maxSlot; slotIndex++)
    {
        if (_states[slotIndex] != ActorSlotState.Alive)
        {
            continue;
        }

        if (_actors[slotIndex] is IActor actor)
        {
            yield return actor;
        }
    }
}
```

---

## 19. EventColumn.PumpOne 调整

目的：

```text
如果 Actor 已 DestroyActor 并变成 PendingDestroy 或 Empty，EventColumn 不应再调用它的 ActorBehaviour。
```

在 `TypedActorStorage<TActor>` 增加：

```csharp
internal bool IsAliveSlot(int slotIndex)
{
    // slotIndex 参数表示 storage 内部 slot。
    // EventColumn.PumpOne 使用该方法避免调用已销毁 Actor。
    return (uint)slotIndex < (uint)_actors.Length
           && _states[slotIndex] == ActorSlotState.Alive
           && _actors[slotIndex] != null;
}
```

修改 `EventColumn.PumpOne`：

```csharp
public bool PumpOne(ref RuntimeFrameBudget budget)
{
    // budget 参数表示 ActorWorld 本帧剩余行为事件预算。
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

        EventMailReader.ReleaseIfEmpty(
            mail: ref mail,
            bufferPool: _bufferPool,
            options: _options);

        return false;
    }

    if (!_owner.IsAliveSlot(slotIndex))
    {
        _dirtySlots.Pop();

        EventMailReader.ReleaseIfEmpty(
            mail: ref mail,
            bufferPool: _bufferPool,
            options: _options);

        return false;
    }

    TActor? actor = _owner.Actors[slotIndex];

    if (actor == null)
    {
        _dirtySlots.Pop();

        EventMailReader.ReleaseIfEmpty(
            mail: ref mail,
            bufferPool: _bufferPool,
            options: _options);

        return false;
    }

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
```

---

## 20. ActorWorld.Pump 调整

当前签名：

```csharp
public void Pump(ref RuntimeFrameBudget budget)
```

改为：

```csharp
public void Pump(
    float deltaTime,
    float fixedDeltaTime,
    bool pumpFixedUpdate,
    ref RuntimeFrameBudget budget)
```

实现：

```csharp
public void Pump(
    float deltaTime,
    float fixedDeltaTime,
    bool pumpFixedUpdate,
    ref RuntimeFrameBudget budget)
{
    // deltaTime 参数表示当前帧间隔。
    // fixedDeltaTime 参数表示固定逻辑步长。
    // pumpFixedUpdate 参数表示本次是否执行 Actor IFixedUpdate。
    // budget 参数表示 PostScheduler 之后剩余的 ActorBehaviour 事件预算。

    PumpActorBehaviours(ref budget);

    // ActorBehaviour 中 DestroyActor 的对象，本帧不再进入生命周期。
    SweepPendingDestroy();

    Lifecycle.PumpStart();

    if (pumpFixedUpdate)
    {
        Lifecycle.PumpFixedUpdate(fixedDeltaTime);
    }

    Lifecycle.PumpUpdate(deltaTime);

    Lifecycle.PumpLateUpdate(deltaTime);

    // 生命周期中 DestroyActor 的对象，本帧末尾清理。
    SweepPendingDestroy();
}

private void PumpActorBehaviours(ref RuntimeFrameBudget budget)
{
    // budget 参数表示 ActorBehaviour 事件预算。
    while (budget.HasRemainingEventBudget())
    {
        if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
        {
            return;
        }

        if (!TryPumpOne(ref budget))
        {
            return;
        }
    }
}
```

---

## 21. LayerRuntime 接入调整

当前 `LayerRuntime.Pump` 中调用：

```csharp
RuntimeFrameBudget actorBudget = CreateActorBudget(_scheduler.Options, postStats);
Actors.Pump(ref actorBudget);
```

改为：

```csharp
RuntimeFrameBudget actorBudget = CreateActorBudget(
    options: _scheduler.Options,
    postStats: postStats);

bool pumpActorFixedUpdate = _fixedUpdateOptions.Enabled;

float actorFixedDeltaTime = _fixedUpdateOptions.Enabled
    ? _fixedUpdateOptions.FixedDeltaTime
    : 0f;

Actors.Pump(
    deltaTime: deltaTime,
    fixedDeltaTime: actorFixedDeltaTime,
    pumpFixedUpdate: pumpActorFixedUpdate,
    budget: ref actorBudget);
```

说明：

```text
1. ActorWorld 继续在 PostScheduler.Pump 后执行。
2. ActorWorld 继续在 LayerChain.Pump 前执行。
3. Actor IFixedUpdate 是否执行由 LayerRuntime 的 FixedUpdateOptions 控制。
4. 如果 FixedUpdateOptions 未启用，则 Actor IFixedUpdate 不调用。
```

---

## 22. 最终 Pump 顺序

`LayerRuntime.Pump(deltaTime)`：

```text
1. Timer.Tick。
2. Delay.Tick。
3. Completion drain。
4. Layer FixedUpdate accumulator。
5. PostIngress drain。
6. PostScheduler.Pump。
7. ActorWorld.PumpActorBehaviours。
8. ActorWorld.SweepPendingDestroy。
9. ActorWorld.PumpStart。
10. ActorWorld.PumpFixedUpdate。
11. ActorWorld.PumpUpdate。
12. ActorWorld.PumpLateUpdate。
13. ActorWorld.SweepPendingDestroy。
14. LayerChain.Pump。
```

ActorWorld 内部：

```text
ActorBehaviour events
  -> Sweep destroy after behaviours
  -> IStart
  -> IFixedUpdate
  -> IUpdate
  -> ILateUpdate
  -> Sweep destroy after lifecycle
```

---

## 23. Query 与生命周期的边界

生命周期系统不使用 Query。

禁止：

```text
QueryActor<IUpdate>()
QueryActor<ILateUpdate>()
QueryActor<IFixedUpdate>()
把生命周期接口加入 BehaviourSignature
```

原因：

```text
1. Query 是 ActorBehaviour 事件能力查询。
2. 生命周期是 Runtime 帧循环能力。
3. 生命周期接口不是事件类型。
4. 每帧生命周期应直接遍历 FreeList，而不是走 QueryCache。
```

---

## 24. Source Generator 边界

本设计不要求修改 `ActorBehaviourGenerator`。

生命周期接口检测发生在 `CreateActor` 冷路径：

```text
actor is IStart
actor is IUpdate
actor is ILateUpdate
actor is IFixedUpdate
```

`IDestroy` 检测发生在 `DestroyNow` 冷路径：

```text
actor is IDestroy
```

不要把生命周期接口生成进 `IGeneratedActorMeta`。  
不要把 Enable 放进生成器字段。  
Enable 应该是 storage slot 状态。

---

## 25. 测试计划

### 25.1 生命周期注册测试

```text
Actor 实现 IStart：
  CreateActor 后进入 Start FreeList。
  第一次 Pump 后调用 Start。
  第二次 Pump 不再调用 Start。

Actor 实现 IUpdate：
  CreateActor 后进入 Update FreeList。
  Pump 后调用 Update。

Actor 实现 ILateUpdate：
  CreateActor 后进入 LateUpdate FreeList。
  Pump 后调用 LateUpdate。

Actor 实现 IFixedUpdate：
  CreateActor 后进入 FixedUpdate FreeList。
  pumpFixedUpdate=true 时调用。
  pumpFixedUpdate=false 时不调用。
```

### 25.2 Enable 测试

```text
默认 Enable=true。
SetEnable(false) 后 IUpdate 不调用。
SetEnable(false) 后 ILateUpdate 不调用。
SetEnable(false) 后 IFixedUpdate 不调用。
SetEnable(false) 后 IStart 仍按规则调用。
SetEnable(false) 后 Post / ActorBehaviour 仍然工作。
SetEnable(false) 后 DestroyActor 仍调用 IDestroy。
SetEnable(true) 后 Update 类接口恢复调用。
```

### 25.3 DestroyActor 测试

```text
DestroyActor 后 IsAlive=false。
DestroyActor 后 Post 返回 Failure。
DestroyActor 后 Query / EnumerateActors 不返回该 Actor。
Destroy 前调用 IDestroy。
Destroy 后生命周期 FreeList 移除对应条目。
Destroy 后 EventMail 清理。
Destroy 后 Generation 递增。
Destroy 后 slot 可复用。
旧 ActorId 不能命中新 Actor。
```

### 25.4 顺序测试

```text
ActorBehaviour 先于 IStart。
ActorBehaviour 先于 IUpdate。
IStart 先于 IUpdate。
IFixedUpdate 先于 IUpdate。
IUpdate 先于 ILateUpdate。
ActorBehaviour 中 DestroyActor 后，本帧不调用 IUpdate。
IUpdate 中 DestroyActor 后，本帧不调用 ILateUpdate。
```

### 25.5 DirtySlot 残留测试

```text
Actor 邮箱中已有事件。
DestroyActor 后清理邮箱。
DirtySlotList 中即使残留 slot，PumpOne 也能安全跳过。
不会调用已销毁 Actor 的 ActorBehaviour。
```

---

## 26. 分阶段实现

### Phase A：生命周期接口与 FreeList

新增：

```text
LayerBase/Actor/Lifecycle/*
```

完成：

```text
IStart / IUpdate / ILateUpdate / IFixedUpdate / IDestroy
ActorLifecycleEntry
ActorLifecycleHandle
ActorLifecycleHandles
ActorLifecycleFreeList
ActorLifecycleScheduler
LifecycleFrameState
```

验收：

```text
FreeList Add / Remove / Version 校验通过。
ForEach 可遍历 occupied entry。
ForEachRemoveIf 可安全移除 IStart。
```

### Phase B：Storage 接入生命周期

修改：

```text
TypedActorStorage.cs
TypedStorageRuntime.cs
BehaviourArchetype.cs
ActorWorld.Create.cs
ActorWorld.cs
```

完成：

```text
ActorSlotState
_enabled
_lifecycleHandles
RegisterLifecycleInterfaces
IsEnable / SetEnable
IsAlive 按 state 判断
PostToAliveActors 过滤 Alive
EnumerateActors 过滤 Alive
```

验收：

```text
CreateActor 自动注册生命周期接口。
Enable 默认 true。
Enable=false 跳过 Update 类生命周期。
```

### Phase C：DestroyActor

新增/修改：

```text
ActorWorld.Destroy.cs
TypedActorStorage DestroyNow / MarkPendingDestroy / SweepPendingDestroy
ActorEventColumnRuntime.ClearMail
EventColumn.ClearMail
EventMailReader.ForceRelease
```

完成：

```text
DestroyActor 标记 PendingDestroy。
SweepPendingDestroy 释放 slot。
IDestroy 正常调用。
生命周期 handle 正常移除。
EventMail 正常清理。
Generation 递增。
```

### Phase D：Pump 顺序

修改：

```text
ActorWorld.Pump.cs
LayerRuntime.cs
```

完成：

```text
ActorBehaviour 先执行。
生命周期后执行。
Destroy 安全点前后各一次。
LayerRuntime 传入 deltaTime / fixedDeltaTime / pumpFixedUpdate。
```

验收：

```text
ActorBehaviour 事件先于生命周期。
生命周期先于 LayerChain.Pump。
FixedUpdateOptions 控制 Actor IFixedUpdate 是否调用。
```

---

## 27. 禁止项

```text
不要把生命周期接口加入 BehaviourSignature。
不要通过 Query 执行生命周期。
不要每帧对所有 Actor 做 actor is IUpdate 判断。
不要把 Enable 放进 IActor。
不要让 Enable=false 阻止 IDestroy。
不要让 Enable=false 阻止 Post。
不要让 IDestroy 进入 FreeList。
不要让 DestroyActor 立即破坏正在遍历的 FreeList。
不要让 PendingDestroy Actor 继续接收 Post。
不要让 PendingDestroy Actor 继续执行 Update 类生命周期。
不要让旧 ActorId 命中新 Actor。
不要让 EventMail 残留导致已销毁 ActorBehaviour 被调用。
不要修改 ActorBehaviourGenerator 来支持生命周期。
```

---

## 28. 最终模型

```text
CreateActor:
  创建 Actor
  注入 ActorContext
  检测 IStart / IUpdate / ILateUpdate / IFixedUpdate
  注册到四个统一 FreeList
  slot 保存 lifecycle handles
  Enable=true

ActorWorld.Pump:
  先处理 ActorBehaviour 邮箱事件
  清理事件中请求销毁的 Actor
  遍历 IStart
  遍历 IFixedUpdate
  遍历 IUpdate
  遍历 ILateUpdate
  清理生命周期中请求销毁的 Actor

DestroyActor:
  标记 PendingDestroy
  安全点调用 IDestroy
  移除生命周期 handles
  清理 EventMail
  清空 Actor slot
  Generation++
  回收 slot

Enable:
  只控制 IUpdate / ILateUpdate / IFixedUpdate
  不控制 IStart
  不控制 IDestroy
  不控制 Post / ActorBehaviour
```

这套设计只补充当前 Actor 模块缺失的生命周期运行时，不改变现有 ActorBehaviour、EventMail、Query、Generator 的职责边界。
