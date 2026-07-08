using Arch.Core;
using LayerBase.ECS.Runtime;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public World EcsWorld { get; private set; } = null!;

    public IEcsScheduler EcsScheduler { get; private set; } = null!;

    public EcsRuntimeOptions EcsOptions { get; private set; }

    internal void InitializeEcsWorld(EcsRuntimeOptions options = default)
    {
        EcsOptions = options.Equals(default)
            ? EcsRuntimeOptions.Default
            : options;

        EcsWorld = World.Create();
        EcsWorld.BindRuntime(this);

        EcsScheduler = EcsOptions.ExecutionMode switch
        {
            EcsExecutionMode.Sync => new SyncEcsScheduler(this, EcsWorld),
            EcsExecutionMode.Async => new AsyncEcsScheduler(this, EcsWorld, EcsOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
    }

    internal void ConfigureEcs(EcsRuntimeOptions options)
    {
        if (_scheduler != null)
        {
            throw new InvalidOperationException("ECS mode must be configured before Build.");
        }

        EcsScheduler?.Dispose();
        EcsWorld?.Dispose();
        InitializeEcsWorld(options);
    }

    internal IEcsWorkScheduler EcsWorkScheduler => (IEcsWorkScheduler)EcsScheduler;

    public void WaitEcsIdleForTest(TimeSpan timeout)
    {
        EcsWorkScheduler.WaitIdleForTest(timeout);
    }
}
