using BenchmarkDotNet.Attributes;
using LayerBase.Core.Event;
using LayerBase.Event.Delay;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace LayerBase.BenchMark;

[MemoryDiagnoser]
public class Phase5Benchmarks
{
    private LayerRuntime _runtime = null!;
    private IDelayPublisher<BenchEvent> _publisher = null!;
    private PostScheduler _scheduler = null!;

    public partial struct BenchEvent { public int Value; }
    public class BenchEventMeta : EventMetaData<BenchEvent>
    {
        public override EventPostPolicy? PostPolicy => new EventPostPolicy(PostDeliveryMode.Normal, BackpressurePolicy.RejectNew, 0);
        public override EventBufferPolicy? BufferPolicy => new EventBufferPolicy(BufferMode.Latest, 0.5f, 1, BufferOverflowPolicy.ReplaceLatest, false);
    }

    [GlobalSetup]
    public void Setup()
    {
        EventMetaDataHandler.Clear();
        EventMetaDataHandler.RegisterMetaData<BenchEvent>(new BenchEventMeta());

        var layer = new TestLayer();
        _runtime = new LayerRuntime.LayersBuilder(new LayerRuntime(601))
            .Push(layer)
            .Build();
        
        _publisher = layer.SubscribeDelay<BenchEvent>();
        _scheduler = _runtime.Scheduler;
    }

    [Benchmark(OperationsPerInvoke = 100000)]
    public void Post_WithMetaData()
    {
        for (int i = 0; i < 100000; i++)
        {
            _scheduler.TryPost(new BenchEvent { Value = i });
        }
        _scheduler.Pump();
    }

    [Benchmark(OperationsPerInvoke = 100000)]
    public void DelayPublish_WithMetaData()
    {
        for (int i = 0; i < 100000; i++)
        {
            _publisher.Publish(new BenchEvent { Value = i }, 0);
        }
    }

    private class TestLayer : Layer
    {
    }
}
