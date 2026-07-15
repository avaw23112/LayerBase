using Arch.Buffer;
using Arch.Core;

namespace LayerBase.ECS;

internal enum ScopeEcsSchedulerState
{
    Created = 0,
    Running = 1,
    Stopped = 2,
    Disposed = 3
}

internal sealed class ScopeEcsScheduler : IEcsScheduler
{
    private readonly int _runtimeGeneration;
    private readonly int _scopeId;
    private readonly World _world;
    private readonly CommandBuffer _commandBuffer;
    private int _ownerThreadId;
    private ScopeEcsSchedulerState _state;
    private long _structuralPlaybackCount;

    public ScopeEcsScheduler(
        int runtimeGeneration,
        int scopeId,
        World world,
        EcsRuntimeOptions options)
    {
        if (scopeId < 0)
            throw new ArgumentOutOfRangeException(nameof(scopeId));

        _runtimeGeneration = runtimeGeneration;
        _scopeId = scopeId;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        BatchOptions = options.QueryBatch;
        _commandBuffer = new CommandBuffer();
        _state = ScopeEcsSchedulerState.Created;
    }

    public World World => _world;

    public EcsQueryBatchOptions BatchOptions { get; }

    public CommandBuffer CommandBuffer => _commandBuffer;

    public ScopeEcsSchedulerState State => _state;

    public void RequireOwnerThread()
    {
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);
        if (ownerThreadId == 0)
            return;

        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException(
                $"Scope ECS scheduler owner thread violation. RuntimeGeneration={_runtimeGeneration}, ScopeId={_scopeId}.");
        }
    }

    public void BeginTick()
    {
        ThrowIfDisposed();
        BindOwnerThreadIfNeeded();
        _state = ScopeEcsSchedulerState.Running;
    }

    public void FlushStructuralChanges()
    {
        ThrowIfDisposed();
        RequireOwnerThread();
        if (_commandBuffer.Size > 0)
        {
            _commandBuffer.Playback(_world);
            _structuralPlaybackCount++;
        }
    }

    internal EcsDiagnosticsSnapshot CaptureDiagnostics()
    {
        return new EcsDiagnosticsSnapshot(
            _world.Size,
            BatchOptions.EnableImplicitBatching,
            lastQueryBatchCount: 0,
            lastQueryEntityCount: 0,
            _commandBuffer.Size,
            Volatile.Read(ref _structuralPlaybackCount));
    }

    public void EndTick()
    {
        ThrowIfDisposed();
        FlushStructuralChanges();
    }

    public void Stop()
    {
        if (_state == ScopeEcsSchedulerState.Disposed)
            return;

        _state = ScopeEcsSchedulerState.Stopped;
    }

    public void Dispose()
    {
        if (_state == ScopeEcsSchedulerState.Disposed)
            return;

        _state = ScopeEcsSchedulerState.Disposed;
        _commandBuffer.Dispose();
    }

    private void BindOwnerThreadIfNeeded()
    {
        int currentThreadId = Environment.CurrentManagedThreadId;
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);
        if (ownerThreadId == currentThreadId)
            return;

        if (ownerThreadId == 0 &&
            Interlocked.CompareExchange(ref _ownerThreadId, currentThreadId, 0) == 0)
        {
            return;
        }

        RequireOwnerThread();
    }

    private void ThrowIfDisposed()
    {
        if (_state == ScopeEcsSchedulerState.Disposed)
            throw new ObjectDisposedException(nameof(ScopeEcsScheduler));
    }
}
