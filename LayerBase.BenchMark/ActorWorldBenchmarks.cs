using BenchmarkDotNet.Attributes;
using LayerBase;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace Benchmarks;

[MemoryDiagnoser]
public class ActorWorldBenchmarks : EventBenchmarkBase
{
    private const int BatchSize = 1000;

    private DirectActorDispatchBaseline _direct = null!;
    private LayerRuntime _runtime = null!;
    private ActorWorld _actorWorld = null!;
    private BenchmarkActor _actor = null!;
    private ActorWorld _postOnlyWorld = null!;
    private BenchmarkActor _postOnlyActor = null!;
    private ActorWorld _pumpOnlyWorld = null!;
    private BenchmarkActor _pumpOnlyActor = null!;
    private ActorWorld _queryWorld = null!;
    private ActorQueryResult _query = default;
    private ActorId _dictionaryActorId;
    private Dictionary<ActorId, IActorDispatchReceiver> _dictionaryDispatch = null!;
    private DictionaryActorDispatchReceiver _dictionaryReceiver = null!;

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        BenchmarkSink.IntValue = 0;

        _direct = new DirectActorDispatchBaseline();
        _dictionaryReceiver = new DictionaryActorDispatchReceiver();
        _dictionaryActorId = new ActorId(0, 0, 1);
        _dictionaryDispatch = new Dictionary<ActorId, IActorDispatchReceiver>
        {
            [_dictionaryActorId] = _dictionaryReceiver
        };

        var layer = new ActorBenchLayer();
        layer.RegisterService(new ActorBenchManager());

        var postOptions = new PostSchedulerOptions(
            readyCapacity: OneMillion + BatchSize,
            nextCapacity: OneMillion + BatchSize,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);

        _runtime = LayerHub.CreateLayers()
            .Push(layer)
            .SetPostOptions(postOptions)
            .Build();

        _actorWorld = CreateBenchmarkWorld(BatchSize);
        _actor = _actorWorld.CreateActor<BenchmarkActor>();
        WarmupActorWorld(_actorWorld, _actor, BatchSize);

        _postOnlyWorld = CreateBenchmarkWorld(OneMillion);
        _postOnlyActor = _postOnlyWorld.CreateActor<BenchmarkActor>();
        WarmupActorWorld(_postOnlyWorld, _postOnlyActor, OneMillion);

        _pumpOnlyWorld = CreateBenchmarkWorld(OneMillion);
        _pumpOnlyActor = _pumpOnlyWorld.CreateActor<BenchmarkActor>();
        WarmupActorWorld(_pumpOnlyWorld, _pumpOnlyActor, OneMillion);

        _queryWorld = CreateBenchmarkWorld(2048);
        for (int i = 0; i < 1000; i++)
        {
            _queryWorld.CreateActor<BenchmarkActor>();
        }

        _query = _queryWorld.QueryActor<ActorBenchEvent>();
        _query.PostAll(ActorBenchEvent.Instance);
        var warmupBudget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        _queryWorld.Pump(0f, 0f, false, ref warmupBudget);
    }

    [Benchmark(Baseline = true, Description = "Direct method call - 20万次")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.Direct")]
    public void DirectMethodCall()
    {
        for (var i = 0; i < OneMillion; i++)
        {
            _direct.Handle(in ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "LayerBase Send - 20万次")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.Send")]
    public void LayerBaseSend()
    {
        for (var i = 0; i < OneMillion; i++)
        {
            _runtime.Send(ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "LayerBase PostScheduler - 20万次")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.PostScheduler")]
    public void LayerBasePostScheduler()
    {
        int batches = OneMillion / BatchSize;
        for (int i = 0; i < batches; i++)
        {
            for (int j = 0; j < BatchSize; j++)
            {
                _runtime.Post(ActorBenchEvent.Instance);
            }

            _runtime.Scheduler.Pump();
        }
    }

    [Benchmark(Description = "ActorWorld Post + Pump - 20万次")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.ActorWorld")]
    public void ActorWorldPostAndPump()
    {
        int batches = OneMillion / BatchSize;
        for (int i = 0; i < batches; i++)
        {
            for (int j = 0; j < BatchSize; j++)
            {
                _actor.PostInside(ActorBenchEvent.Instance);
            }

            var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
            _actorWorld.Pump(0f, 0f, false, ref budget);
        }
    }

    [IterationCleanup(Target = nameof(ActorWorldPostOnly))]
    public void CleanupPostOnly()
    {
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        _postOnlyWorld.Pump(0f, 0f, false, ref budget);
    }

    [Benchmark(Description = "ActorWorld Post only - 200k")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.ActorWorld.Split")]
    public void ActorWorldPostOnly()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _postOnlyActor.PostInside(ActorBenchEvent.Instance);
        }
    }

    [IterationSetup(Target = nameof(ActorWorldPumpOnlyPreposted))]
    public void SetupPumpOnly()
    {
        for (int i = 0; i < OneMillion; i++)
        {
            _pumpOnlyActor.PostInside(ActorBenchEvent.Instance);
        }
    }

    [Benchmark(Description = "ActorWorld Pump only - 200k")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.ActorWorld.Split")]
    public void ActorWorldPumpOnlyPreposted()
    {
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        _pumpOnlyWorld.Pump(0f, 0f, false, ref budget);
    }

    [Benchmark(Description = "ActorWorld Query.PostAll + Pump - 1000 Actors")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.QueryPostAll")]
    public void ActorWorldQueryPostAll()
    {
        _query.PostAll(ActorBenchEvent.Instance);
        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        _queryWorld.Pump(0f, 0f, false, ref budget);
    }

    [Benchmark(Description = "Dictionary<ActorId, Actor> + interface call - 20万次")]
    [BenchmarkCategory("08.Actor", "ActorRuntime", "Compare.Dictionary")]
    public void DictionaryActorDispatch()
    {
        for (var i = 0; i < OneMillion; i++)
        {
            if (_dictionaryDispatch.TryGetValue(_dictionaryActorId, out IActorDispatchReceiver? receiver))
            {
                receiver.Handle(in ActorBenchEvent.Instance);
            }
        }
    }

    private static ActorWorld CreateBenchmarkWorld(int maxCapacity)
    {
        return new ActorWorld(new ActorMailOptions(
            postPolicy: ActorPostPolicy.Queued,
            fullPolicy: ActorMailFullPolicy.Grow,
            growFailurePolicy: ActorMailFullPolicy.RejectNew,
            initialCapacity: maxCapacity,
            maxCapacity: maxCapacity,
            growFactor: 2,
            releaseWhenEmpty: false));
    }

    private static void WarmupActorWorld(ActorWorld world, BenchmarkActor actor, int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            actor.PostInside(ActorBenchEvent.Instance);
        }

        var budget = new RuntimeFrameBudget(maxEvents: 0, usedEvents: 0, deadlineTicks: 0);
        world.Pump(0f, 0f, false, ref budget);
    }
}

public readonly struct ActorBenchEvent
{
    public static readonly ActorBenchEvent Instance = default;
}

public sealed class DirectActorDispatchBaseline
{
    public void Handle(in ActorBenchEvent value)
    {
        BenchmarkSink.IntValue++;
    }
}

public interface IActorDispatchReceiver
{
    void Handle(in ActorBenchEvent value);
}

public sealed class DictionaryActorDispatchReceiver : IActorDispatchReceiver
{
    public void Handle(in ActorBenchEvent value)
    {
        BenchmarkSink.IntValue++;
    }
}

public sealed partial class BenchmarkActor : IActor
{
    [ActorBehaviour]
    private void OnActorEvent(in ActorBenchEvent value)
    {
        BenchmarkSink.IntValue++;
    }
}

public sealed class ActorBenchLayer : Layer
{
}

public partial class ActorBenchManager : IService
{
    public void ConfigureServices(IServiceCollection s)
    {
        s.AddSingleton(this);
    }

    [Subscribe]
    public void Handle(in ActorBenchEvent value)
    {
        BenchmarkSink.IntValue++;
    }
}


