using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.ECS.Projection;
using LayerBase.Layers;
using LayerBase.Scope;

namespace Benchmarks.Integration;

[MemoryDiagnoser]
[Config(typeof(ScopeEcsPipelineBenchConfig))]
[BenchmarkCategory("09.Scope.ECS.Actor")]
public class ScopeEcsPipelineBenchmarks
{
    private LayerRuntime _runtime = null!;
    private ScopeEcsPipelineService _service = null!;

    [Params(1, 64, 1024)]
    public int EntityCount;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        ScopeEcsPipelineActor.Reset();
        var layer = new ScopeEcsPipelineLayer(EntityCount);
        _runtime = LayerHub.CreateLayers().Push(layer).Build();
        _service = layer.Service;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        LayerHub.Reset();
    }

    [Benchmark(Description = "Scope Pump + ECS Query + Actor projection")]
    [BenchmarkCategory("09.Scope.ECS.Actor")]
    public int ScopeEcsActorPump()
    {
        _service.RequestProjection();
        _runtime.ScopeHost!.Pump(0.016f);
        return ScopeEcsPipelineActor.Received;
    }
}

[ScopeOptions]
public sealed partial class ScopeEcsPipelineScope
{
}

public sealed class ScopeEcsPipelineLayer : Layer
{
    public ScopeEcsPipelineLayer(int entityCount)
    {
        Service = new ScopeEcsPipelineService(entityCount);
        RegisterService(typeof(ScopeEcsPipelineService), Service);
    }

    public ScopeEcsPipelineService Service { get; }
}

[Scope<ScopeEcsPipelineScope>]
public sealed partial class ScopeEcsPipelineService : IService, IInitializable, LayerBase.DI.Options.IUpdate
{
    private readonly int _entityCount;
    private bool _projectRequested;

    public ScopeEcsPipelineService(int entityCount)
    {
        _entityCount = entityCount;
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Initialize()
    {
        World world = this.ECSWorld();
        for (int i = 0; i < _entityCount; i++)
        {
            Entity entity = world.Create(
                new ScopeEcsPipelinePosition { Value = i },
                new ScopeEcsPipelineVelocity { Value = 1 },
                new ProjectedActorRef());
            world.WithProjectedActor<ScopeEcsPipelineActor>(entity, keepAliveSeconds: 1f);
        }
    }

    public void RequestProjection()
    {
        _projectRequested = true;
    }

    public void Update()
    {
        if (!_projectRequested)
        {
            return;
        }

        _projectRequested = false;
        this.Query<ScopeEcsPipelinePosition, ScopeEcsPipelineVelocity>()
            .Bring<ScopeEcsPipelineMoveEvent>()
            .ForEach(static (
                in Entity _,
                ref ScopeEcsPipelinePosition position,
                ref ScopeEcsPipelineVelocity velocity,
                ref ScopeEcsPipelineMoveEvent output) =>
            {
                position.Value += velocity.Value;
                output = new ScopeEcsPipelineMoveEvent(position.Value);
            })
            .Post();
    }
}

public struct ScopeEcsPipelinePosition : IComponent
{
    public int Value;
}

public struct ScopeEcsPipelineVelocity : IComponent
{
    public int Value;
}

public readonly struct ScopeEcsPipelineMoveEvent : IActorEvent
{
    public ScopeEcsPipelineMoveEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public sealed partial class ScopeEcsPipelineActor : IPooledActor
{
    public static int Received;

    public static void Reset()
    {
        Received = 0;
    }

    [ActorBehaviour]
    private void OnMove(in ScopeEcsPipelineMoveEvent value)
    {
        Received += value.Value;
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}

public sealed class ScopeEcsPipelineBenchConfig : ManualConfig
{
    public ScopeEcsPipelineBenchConfig()
    {
        AddJob(Job.ShortRun);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max);
    }
}
