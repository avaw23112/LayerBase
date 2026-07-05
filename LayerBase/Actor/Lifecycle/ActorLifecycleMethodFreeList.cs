using System.Diagnostics;

namespace LayerBase.Actor;

internal sealed class ActorLifecycleMethodFreeList
{
    private ActorLifecycleMethodEntry[] _entries = new ActorLifecycleMethodEntry[4];
    private int[] _versions = new int[4];
    private bool[] _occupied = new bool[4];
    private int[] _free = new int[4];
    private int _freeCount;
    private int _count;
    private int _cursor;

    public ActorLifecycleHandle Add(
        ActorId                     actorId,
        IActor                      actor,
        ActorLifecycleMethodInvoker invoker)
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

        _entries[index] = new ActorLifecycleMethodEntry(actorId, actor, invoker);
        _occupied[index] = true;
        return new ActorLifecycleHandle(index, _versions[index]);
    }

    public bool Remove(ActorLifecycleHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        if ((uint)handle.Index >= (uint)_entries.Length)
        {
            return false;
        }

        if (!_occupied[handle.Index] || _versions[handle.Index] != handle.Version)
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
        TrimTrailingHoles();
        return true;
    }

    public void PumpBudgeted(
        ref LifecycleFrameState state,
        ref RuntimeFrameBudget  budget,
        int                     timeCheckInterval = 1)
    {
        if (_count == 0)
        {
            return;
        }

        int checkedCount = 0;
        int maxCount = _count;
        int processedSinceTimeCheck = 0;

        while (checkedCount < maxCount)
        {
            if (!budget.HasRemainingEventBudget())
            {
                return;
            }

            if (processedSinceTimeCheck <= 0)
            {
                if (!budget.HasRemainingTimeBudget(Stopwatch.GetTimestamp()))
                {
                    return;
                }

                processedSinceTimeCheck = timeCheckInterval;
            }

            int index = _cursor;
            _cursor = index + 1 == _count ? 0 : index + 1;
            checkedCount++;

            if (!_occupied[index])
            {
                continue;
            }

            ActorLifecycleMethodEntry entry = _entries[index];
            if (!state.World.IsLifecycleRunnable(entry.ActorId))
            {
                continue;
            }

            entry.Invoker(entry.Actor, state.DeltaTime);
            budget.ConsumeEvent();
            processedSinceTimeCheck--;
        }
    }

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
