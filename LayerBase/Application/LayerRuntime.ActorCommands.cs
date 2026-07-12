using System.Collections.Concurrent;
using LayerBase.Actor;

namespace LayerBase;

internal interface IRuntimeActorCommand
{
    void Execute(ActorWorld world);
}

internal readonly struct RuntimeActorPostCommand<TEvent> : IRuntimeActorCommand
    where TEvent : struct
{
    private readonly ActorId _actorId;
    private readonly TEvent _value;

    public RuntimeActorPostCommand(ActorId actorId, in TEvent value)
    {
        _actorId = actorId;
        _value = value;
    }

    public void Execute(ActorWorld world)
    {
        world.PostTo(_actorId, in _value);
    }
}

internal readonly struct RuntimeActorPostManyCommand<TEvent> : IRuntimeActorCommand
    where TEvent : struct
{
    private readonly ActorId[] _actorIds;
    private readonly TEvent _value;

    public RuntimeActorPostManyCommand(ReadOnlySpan<ActorId> actorIds, in TEvent value)
    {
        _actorIds = actorIds.ToArray();
        _value = value;
    }

    public void Execute(ActorWorld world)
    {
        world.PostToMany(_actorIds, in _value);
    }
}

public sealed partial class LayerRuntime
{
    private readonly ConcurrentQueue<IRuntimeActorCommand> _scopeActorCommands = new();

    internal void EnqueueScopeActorCommand(IRuntimeActorCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (_disposed)
        {
            return;
        }

        _scopeActorCommands.Enqueue(command);
    }

    internal int DrainScopeActorCommands(int maxCount = 0)
    {
        int drained = 0;
        while (_scopeActorCommands.TryDequeue(out IRuntimeActorCommand? command))
        {
            command.Execute(Actors);
            drained++;
            if (maxCount > 0 && drained >= maxCount)
            {
                break;
            }
        }

        return drained;
    }

    private void ClearScopeActorCommands()
    {
        while (_scopeActorCommands.TryDequeue(out _))
        {
        }
    }
}
