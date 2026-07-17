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
        ActorId    actorId,
        TLifecycle instance)
    {
        int index;

        if (_freeCount > 0)
        {
            _freeCount--;
            index = _free[_freeCount];
            if (index >= _count)
                _count = index + 1;
        }
        else
        {
            index = _count;
            _count++;
            EnsureCapacity(index + 1);
        }

        if (_count == 1)
            _cursor = 0;

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

        // 删除后尝试裁剪尾部空洞。
        // 如果删除的是尾部的条目，可以减少 _count，避免无效遍历。
        TrimTrailingHoles();

        return true;
    }

    /// <summary>
    /// 裁剪尾部空洞。
    ///
    /// 作用：
    /// 当尾部的条目被删除后，_count 不会自动下降。
    /// 这个方法会从尾部向前扫描，将 _count 降低到最后一个存活条目之后。
    ///
    /// 注意：
    /// 只裁剪尾部空洞，不移动中间存活条目。
    /// 这样可以避免破坏外部保存的 ActorLifecycleHandle。
    /// </summary>
    private void TrimTrailingHoles()
    {
        while (_count > 0)
        {
            int last = _count - 1;
            if (_occupied[last])
            {
                break;
            }

            _entries[last] = default;
            _count--;
        }

        if (_count == 0 || _cursor >= _count)
            _cursor = 0;
    }

    public void ForEach<TState>(
        ref TState                           state,
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
        LifecycleCall<TLifecycle> invoker,
        int                       timeCheckInterval = 1)
    {
        // state 参数表示生命周期遍历上下文。
        // budget 参数表示当前帧剩余预算。
        // invoker 参数表示具体调用哪个生命周期方法。
        // timeCheckInterval 参数表示每处理多少个生命周期条目后检查一次时间预算。
        //   默认值 1 表示每个条目都检查（旧行为）。
        //   值 64 表示每处理 64 个条目后检查一次时间预算。
        if (_count == 0)
        {
            return;
        }

        int checkedCount = 0;
        int maxCount = _count;
        int processedSinceTimeCheck = 0;

        while (checkedCount < maxCount)
        {
            if (!budget.HasRemainingWork())
            {
                return;
            }

            // 时间预算检查分摊：
            // 只有在处理了 timeCheckInterval 个条目后才检查时间预算。
            // 这样可以减少 Stopwatch.GetTimestamp() 的调用频率。
            if (processedSinceTimeCheck <= 0)
            {
                if (!budget.HasRemainingTime(Stopwatch.GetTimestamp()))
                {
                    return;
                }

                processedSinceTimeCheck = timeCheckInterval;
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

            budget.Consume(1);
            processedSinceTimeCheck--;
        }
    }

    public void ForEachRemoveIf<TState>(
        ref TState                                   state,
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
    in  ActorLifecycleEntry<TLifecycle> entry,
    ref TState                          state)
    where TLifecycle : class;

internal delegate bool LifecycleRemovePredicate<TLifecycle, TState>(
    in  ActorLifecycleEntry<TLifecycle> entry,
    ref TState                          state)
    where TLifecycle : class;

internal delegate void LifecycleCall<TLifecycle>(
    TLifecycle instance,
    float      deltaTime)
    where TLifecycle : class;
