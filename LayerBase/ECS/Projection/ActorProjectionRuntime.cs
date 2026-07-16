using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.ECS.Projection.Flow;
using LayerBase.Scope;

namespace LayerBase.ECS.Projection;

internal readonly record struct ActorProjectionBudgetOptions(
    int MaxBatchSliceItems,
    int DeadlineCheckStride)
{
    public static ActorProjectionBudgetOptions Default => new(128, 32);
}

internal struct ProjectionBatchLease<TEvent> : IDisposable
    where TEvent : struct
{
    public ProjectionBatchLease(
        ActorId[] actorIds,
        TEvent[] events,
        int count)
    {
        ActorIds = actorIds ?? throw new ArgumentNullException(nameof(actorIds));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Count = count;
        Cursor = 0;
    }

    public ActorId[] ActorIds;
    public TEvent[] Events;
    public int Count;
    public int Cursor;

    public bool IsEmpty => Count <= 0;

    public void Dispose()
    {
        if (ActorIds.Length > 0)
            ArrayPool<ActorId>.Shared.Return(ActorIds, clearArray: false);
        if (Events.Length > 0)
            ArrayPool<TEvent>.Shared.Return(Events, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>());

        ActorIds = Array.Empty<ActorId>();
        Events = Array.Empty<TEvent>();
        Count = 0;
        Cursor = 0;
    }
}

internal sealed class ActorProjectionRuntime : IDisposable
{
    private readonly ActorWorld _actorWorld;
    private readonly ActorProjectionBudgetOptions _options;
    private readonly ActorProjectionCommandLane _commandLane;
    private readonly Dictionary<int, IActorProjectionLane> _lanesByRoute = new();
    private readonly List<IActorProjectionLane> _lanes = new();
    private int _laneCursor;

    public ActorProjectionRuntime(
        ActorWorld actorWorld,
        ActorProjectionBudgetOptions? options = null)
    {
        _actorWorld = actorWorld ?? throw new ArgumentNullException(nameof(actorWorld));
        _options = options ?? ActorProjectionBudgetOptions.Default;
        _commandLane = new ActorProjectionCommandLane();
        CommandSink = new ActorProjectionCommandSink(this, _actorWorld);
    }

    public IProjectedActorCommandSink CommandSink { get; }

    internal void BindMainWorld(World world)
    {
        ((ActorProjectionCommandSink)CommandSink).BindWorld(world);
    }

    internal void EnqueueCommand(ProjectedActorScopeCommand command, World resultWorld)
    {
        _commandLane.Enqueue(command, resultWorld);
    }

    internal void EnqueueCommand(ProjectedActorScopeCommand command, ScopeEndpoint resultEndpoint)
    {
        _commandLane.Enqueue(command, resultEndpoint);
    }

    internal void EnqueuePost<TEvent>(ActorId actorId, in TEvent value)
        where TEvent : struct
    {
        ActorId[] actorIds = ArrayPool<ActorId>.Shared.Rent(1);
        TEvent[] events = ArrayPool<TEvent>.Shared.Rent(1);
        actorIds[0] = actorId;
        events[0] = value;
        EnqueuePostBatch(new ProjectionBatchLease<TEvent>(actorIds, events, 1));
    }

    internal void EnqueuePostBatch<TEvent>(ProjectionBatchLease<TEvent> lease)
        where TEvent : struct
    {
        if (lease.IsEmpty)
        {
            lease.Dispose();
            return;
        }

        GetLane<TEvent>().Enqueue(lease);
    }

    public void Pump(ref RuntimeFrameBudget budget)
    {
        _commandLane.Pump(_actorWorld, ref budget);

        if (_lanes.Count == 0)
            return;

        int emptyScans = 0;
        while (budget.CanContinue(Stopwatch.GetTimestamp()) && emptyScans < _lanes.Count)
        {
            if (_laneCursor >= _lanes.Count)
                _laneCursor = 0;

            IActorProjectionLane lane = _lanes[_laneCursor];
            _laneCursor = (_laneCursor + 1) % _lanes.Count;

            if (!lane.HasPending)
            {
                emptyScans++;
                continue;
            }

            emptyScans = 0;
            if (!lane.Pump(_actorWorld, ref budget, _options))
                break;
        }
    }

    public void Dispose()
    {
        _commandLane.Dispose();
        foreach (IActorProjectionLane lane in _lanes)
            lane.Dispose();
        _lanes.Clear();
        _lanesByRoute.Clear();
    }

    private ActorProjectionLane<TEvent> GetLane<TEvent>()
        where TEvent : struct
    {
        int routeId = EventTypeId<ActorPostBatchScopeEvent<TEvent>>.Id;
        if (_lanesByRoute.TryGetValue(routeId, out IActorProjectionLane? existing))
            return (ActorProjectionLane<TEvent>)existing;

        var lane = new ActorProjectionLane<TEvent>();
        _lanesByRoute.Add(routeId, lane);
        _lanes.Add(lane);
        return lane;
    }

    private sealed class ActorProjectionCommandSink : IProjectedActorCommandSink
    {
        private readonly ActorProjectionRuntime _runtime;
        private readonly ActorWorld _actorWorld;
        private World? _world;

        public ActorProjectionCommandSink(ActorProjectionRuntime runtime, ActorWorld actorWorld)
        {
            _runtime = runtime;
            _actorWorld = actorWorld;
        }

        public void BindWorld(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public ProjectedActorEnsureResult Ensure(Entity entity, int actorTypeId, long nowTicks)
        {
            World world = RequireWorld();
            var command = ProjectedActorScopeCommand.Ensure(
                ScopeDefinitionIds.Main,
                entity,
                actorTypeId,
                nowTicks);
            _runtime.EnqueueCommand(command, world);
            return ProjectedActorEnsureResult.Pending(accepted: true);
        }

        public bool Exists(ActorId actorId)
        {
            return _actorWorld.TryGetPooledActor(actorId, out _);
        }

        public bool IsDisabled(ActorId actorId)
        {
            return _actorWorld.IsProjectedActorDisabled(actorId);
        }

        public bool EnableIfDisabled(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
        {
            World world = RequireWorld();
            var command = ProjectedActorScopeCommand.Enable(ScopeDefinitionIds.Main, entity, actorTypeId, actorId, nowTicks);
            _runtime.EnqueueCommand(command, world);
            return true;
        }

        public bool Disable(Entity entity, int actorTypeId, ActorId actorId, long nowTicks)
        {
            World world = RequireWorld();
            var command = ProjectedActorScopeCommand.Disable(ScopeDefinitionIds.Main, entity, actorTypeId, actorId, nowTicks);
            _runtime.EnqueueCommand(command, world);
            return true;
        }

        public bool Release(
            Entity entity,
            int actorTypeId,
            ActorId actorId,
            ProjectedActorReleasePolicy releasePolicy,
            long nowTicks)
        {
            World world = RequireWorld();
            var command = ProjectedActorScopeCommand.Release(
                ScopeDefinitionIds.Main,
                entity,
                actorTypeId,
                actorId,
                releasePolicy,
                nowTicks);
            _runtime.EnqueueCommand(command, world);
            return true;
        }

        public void PostTo<TEvent>(ActorId actorId, in TEvent value)
            where TEvent : struct
        {
            _runtime.EnqueuePost(actorId, in value);
        }

        public void PostBatch<TEvent>(ref ProjectionBatchBuffer<TEvent> batch)
            where TEvent : struct
        {
            if (batch.Count == 0)
                return;

            _runtime.EnqueuePostBatch(batch.Detach());
        }

        private World RequireWorld()
        {
            return _world ?? throw new InvalidOperationException("Main projection world is not bound.");
        }
    }
}

internal sealed class ActorProjectionCommandLane : IDisposable
{
    private readonly Queue<PendingProjectionCommand> _commands = new();

    public void Enqueue(ProjectedActorScopeCommand command, World resultWorld)
    {
        _commands.Enqueue(PendingProjectionCommand.ForWorld(command, resultWorld));
    }

    public void Enqueue(ProjectedActorScopeCommand command, ScopeEndpoint resultEndpoint)
    {
        _commands.Enqueue(PendingProjectionCommand.ForEndpoint(command, resultEndpoint));
    }

    public void Pump(ActorWorld actorWorld, ref RuntimeFrameBudget budget)
    {
        while (_commands.Count > 0 && budget.CanContinue(Stopwatch.GetTimestamp()))
        {
            PendingProjectionCommand pending = _commands.Dequeue();
            ProjectedActorScopeResult result =
                ActorProjectionScopeEventDispatcher.Execute(actorWorld, pending.Command);
            budget.Consume(1);
            pending.Apply(in result);
        }
    }

    public void Dispose()
    {
        _commands.Clear();
    }

    private readonly struct PendingProjectionCommand
    {
        private readonly World? _resultWorld;
        private readonly ScopeEndpoint _resultEndpoint;
        private readonly bool _hasEndpoint;

        private PendingProjectionCommand(
            ProjectedActorScopeCommand command,
            World? resultWorld,
            ScopeEndpoint resultEndpoint,
            bool hasEndpoint)
        {
            Command = command;
            _resultWorld = resultWorld;
            _resultEndpoint = resultEndpoint;
            _hasEndpoint = hasEndpoint;
        }

        public ProjectedActorScopeCommand Command { get; }

        public static PendingProjectionCommand ForWorld(ProjectedActorScopeCommand command, World resultWorld)
        {
            return new PendingProjectionCommand(command, resultWorld, default, hasEndpoint: false);
        }

        public static PendingProjectionCommand ForEndpoint(ProjectedActorScopeCommand command, ScopeEndpoint resultEndpoint)
        {
            return new PendingProjectionCommand(command, null, resultEndpoint, hasEndpoint: true);
        }

        public void Apply(in ProjectedActorScopeResult result)
        {
            if (_resultWorld != null)
            {
                _resultWorld.ApplyProjectedActorResult(in result);
                return;
            }

            if (_hasEndpoint)
            {
                var batch = new ActorProjectionResultBatchScopeEvent(result);
                _resultEndpoint.Transport.EnqueueEvent(
                    EventTypeId<ActorProjectionResultBatchScopeEvent>.Id,
                    ScopeEventClass.Internal,
                    in batch);
            }
        }
    }
}

internal interface IActorProjectionLane : IDisposable
{
    bool HasPending { get; }

    bool Pump(
        ActorWorld actorWorld,
        ref RuntimeFrameBudget budget,
        ActorProjectionBudgetOptions options);
}

internal sealed class ActorProjectionLane<TEvent> : IActorProjectionLane
    where TEvent : struct
{
    private readonly Queue<ProjectionBatchLease<TEvent>> _batches = new();

    public bool HasPending => _batches.Count > 0;

    public void Enqueue(ProjectionBatchLease<TEvent> batch)
    {
        _batches.Enqueue(batch);
    }

    public bool Pump(
        ActorWorld actorWorld,
        ref RuntimeFrameBudget budget,
        ActorProjectionBudgetOptions options)
    {
        if (_batches.Count == 0)
            return false;

        ProjectionBatchLease<TEvent> batch = _batches.Dequeue();
        int processed = PumpBatch(actorWorld, ref batch, ref budget, options);
        if (batch.Cursor < batch.Count)
        {
            _batches.Enqueue(batch);
            return processed > 0;
        }

        batch.Dispose();
        return processed > 0;
    }

    public void Dispose()
    {
        while (_batches.Count > 0)
        {
            ProjectionBatchLease<TEvent> batch = _batches.Dequeue();
            batch.Dispose();
        }
    }

    private static int PumpBatch(
        ActorWorld actorWorld,
        ref ProjectionBatchLease<TEvent> batch,
        ref RuntimeFrameBudget budget,
        ActorProjectionBudgetOptions options)
    {
        int remainingInBatch = batch.Count - batch.Cursor;
        int allowed = Math.Min(remainingInBatch, budget.RemainingWorkItems);
        allowed = Math.Min(allowed, Math.Max(1, options.MaxBatchSliceItems));
        if (allowed <= 0)
            return 0;

        int stride = Math.Max(1, options.DeadlineCheckStride);
        int processed = 0;

        while (processed < allowed)
        {
            int blockCount = Math.Min(stride, allowed - processed);
            int start = batch.Cursor + processed;
            for (int i = 0; i < blockCount; i++)
            {
                int index = start + i;
                actorWorld.PostTo(batch.ActorIds[index], in batch.Events[index]);
            }

            processed += blockCount;
            budget.Consume(blockCount);

            if (!budget.HasRemainingTime(Stopwatch.GetTimestamp()))
                break;
        }

        batch.Cursor += processed;
        return processed;
    }
}
