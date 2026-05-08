using System.Diagnostics;

namespace LayerBase.Actor;

internal sealed class ActorLifecycleFreeList<TLifecycle>
    where TLifecycle : class
{
    private ActorLifecycleEntry<TLifecycle>[] _entries = new ActorLifecycleEntry<TLifecycle>[4];
    private int[] _versions = new int[4];
    private bool[] _occupied = new bool[4];
    private int[] _free = new int[4];
    private int _freeCount;
    private int _count;
    private int _cursor;

    public ActorLifecycleHandle Add(
        ActorId actorId,
        TLifecycle instance)
    {
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

        _entries[index] = new ActorLifecycleEntry<TLifecycle>(actorId, instance);
        _occupied[index] = true;

        return new ActorLifecycleHandle(index, _versions[index]);
    }

    public bool Remove(ActorLifecycleHandle handle)
    {  
        // handle 参数表示 Add 时返回的生命周期条目位置。
        // Version 不匹配时说明该位置已经被释放并复用，不能删除。
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
        for (int i = 0; i < _count; i++)
        {
            if (!_occupied[i])
            {
                continue;
            }

            invoker(in _entries[i], ref state);
        }
    }

    public void PumpBudgeted(
        ref LifecycleFrameState   state,
        ref RuntimeFrameBudget    budget,
        LifecycleCall<TLifecycle> invoker)
    {
        // state 参数表示生命周期遍历上下文。
        // budget 参数表示当前帧剩余预算。
        // invoker 参数表示具体调用哪个生命周期方法。
        if (_count == 0)
        {
            return;
        }

        int checkedCount = 0;
        int maxCount = _count;

        while (checkedCount < maxCount)
        {
            if (!budget.HasRemainingEventBudget())
            {
                return;
            }

            if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
            {
                return;
            }

            int index = _cursor;

            _cursor = index + 1 == _count
                ? 0
                : index + 1;

            checkedCount++;

            if (!_occupied[index])
            {
                continue;
            }

            ActorLifecycleEntry<TLifecycle> entry = _entries[index];

            if (!state.World.IsLifecycleRunnable(entry.ActorId))
            {
                continue;
            }

            invoker(
                instance: entry.Instance,
                deltaTime: state.DeltaTime);

            // 每调用一个生命周期方法，消耗一个预算单元。
            // 当前 RuntimeFrameBudget 叫 Event，但这里实际作为 WorkUnit 使用。
            budget.ConsumeEvent();
        }
    }
    public void ForEachRemoveIf<TState>(
        ref TState state,
        LifecycleRemovePredicate<TLifecycle, TState> predicate)
    {
        for (int i = 0; i < _count; i++)
        {
            if (!_occupied[i])
            {
                continue;
            }

            if (!predicate(in _entries[i], ref state))
            {
                continue;
            }

            Remove(new ActorLifecycleHandle(i, _versions[i]));
        }
    }

    private void EnsureCapacity(int required)
    {
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

internal delegate void LifecycleCall<TLifecycle>(
    TLifecycle instance,
    float      deltaTime)
    where TLifecycle : class;