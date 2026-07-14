using Arch.Core;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;
using LayerBase.ECS.Runtime.Query;
using LayerBase.Scope;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    private World? _ecsWorld;

    private EcsQueryRegistry? _ecsQueryRegistry;

    private IEcsScheduler? _ecsScheduler;

    private EcsRuntimeOptions _ecsOptions;

    internal World EcsWorld
    {
        get
        {
            if (ScopeHost?.MainScope is { } mainScope)
            {
                return mainScope.EcsWorld;
            }

            return _ecsWorld ?? throw new InvalidOperationException("Runtime ECS world is not initialized.");
        }
        private set => _ecsWorld = value;
    }

    internal EcsQueryRegistry EcsQueryRegistry
    {
        get
        {
            if (ScopeHost?.MainScope is { } mainScope)
            {
                return mainScope.EcsQueryRegistry;
            }

            return _ecsQueryRegistry ?? throw new InvalidOperationException("Runtime ECS query registry is not initialized.");
        }
        private set => _ecsQueryRegistry = value;
    }

    internal IEcsScheduler EcsScheduler
    {
        get
        {
            if (ScopeHost?.MainScope.EcsScheduler is { } scheduler)
            {
                return scheduler;
            }

            return _ecsScheduler ?? throw new InvalidOperationException("Runtime ECS scheduler is not initialized.");
        }
        private set => _ecsScheduler = value;
    }

    public EcsRuntimeOptions EcsOptions => ScopeHost?.MainScope.EcsOptions ?? _ecsOptions;

    internal void InitializeEcsWorld(EcsRuntimeOptions options = default)
    {
        _ecsOptions = options.Equals(default)
            ? EcsRuntimeOptions.Default
            : options;

        EcsWorld = World.Create();
        EcsWorld.BindRuntime(this);
        EcsQueryRegistry = new EcsQueryRegistry(EcsWorld);

        EcsScheduler = _ecsOptions.ExecutionMode switch
        {
            EcsExecutionMode.Sync => new SyncEcsScheduler(this, EcsWorld),
            EcsExecutionMode.Async => new AsyncEcsScheduler(this, EcsWorld, _ecsOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }

    internal void ConfigureEcs(EcsRuntimeOptions options)
    {
        if (_scheduler != null)
        {
            throw new InvalidOperationException("ECS mode must be configured before Build.");
        }

        _ecsScheduler?.Dispose();
        _ecsWorld?.Dispose();
        InitializeEcsWorld(options);
    }

    private void AdoptMainScopeEcsResources(ScopeRuntime mainScope)
    {
        if (!ReferenceEquals(_ecsScheduler, mainScope.EcsScheduler))
        {
            _ecsScheduler?.Dispose();
        }

        if (!ReferenceEquals(_ecsWorld, mainScope.EcsWorld))
        {
            _ecsWorld?.Dispose();
        }

        _ecsWorld = mainScope.EcsWorld;
        _ecsQueryRegistry = mainScope.EcsQueryRegistry;
        _ecsScheduler = mainScope.EcsScheduler;
        _ecsOptions = mainScope.EcsOptions;
    }

    internal IEcsWorkScheduler EcsWorkScheduler => (IEcsWorkScheduler)EcsScheduler;

    public void WaitEcsIdleForTest(TimeSpan timeout)
    {
        EcsWorkScheduler.WaitIdleForTest(timeout);
    }

    public long FlushEcsSubmissionsForTest()
    {
        return EcsWorkScheduler.FlushSubmissionsForTest();
    }

    public void WaitEcsFenceForTest(long fence, TimeSpan timeout)
    {
        EcsWorkScheduler.WaitFenceForTest(fence, timeout);
    }
}
