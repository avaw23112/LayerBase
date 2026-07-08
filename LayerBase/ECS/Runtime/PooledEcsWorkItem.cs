using System.Collections.Concurrent;
using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal interface IPooledEcsWorkItem
{
    void ReturnToPool();
}

internal sealed class PooledEcsWorkItem<TState> : IEcsWorkItem, IPooledEcsWorkItem
{
    private static readonly ConcurrentBag<PooledEcsWorkItem<TState>> s_pool = new();

    private Action<World, TState>? _execute;
    private TState _state = default!;

    private PooledEcsWorkItem()
    {
        DebugName = string.Empty;
    }

    public string DebugName { get; private set; }

    public static PooledEcsWorkItem<TState> Rent(
        string debugName,
        in TState state,
        Action<World, TState> execute)
    {
        PooledEcsWorkItem<TState> item = s_pool.TryTake(out PooledEcsWorkItem<TState>? pooled)
            ? pooled
            : new PooledEcsWorkItem<TState>();

        item.DebugName = debugName;
        item._state = state;
        item._execute = execute;
        return item;
    }

    public void Execute(World world, EcsResultQueue results)
    {
        Action<World, TState> execute =
            _execute ?? throw new InvalidOperationException("Pooled ECS work item was returned before execution.");

        execute(world, _state);
    }

    public void ReturnToPool()
    {
        _execute = null;
        _state = default!;
        DebugName = string.Empty;
        s_pool.Add(this);
    }
}
