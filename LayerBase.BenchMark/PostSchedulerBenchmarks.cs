using BenchmarkDotNet.Attributes;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;

namespace Benchmarks;

[MemoryDiagnoser]
public class PostSchedulerBenchmarks : EventBenchmarkBase
{
    private LayerRuntime _runtime = null!;

    public partial struct CoalescedBenchEvent
    {
        public int Value;
    }

    public class CoalescedBenchEventMetaData : EventMetaData<CoalescedBenchEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0);
    }

    public partial struct LatestBenchEvent
    {
        public int Value;
    }

    public class LatestBenchEventMetaData : EventMetaData<LatestBenchEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0);
    }

    [GlobalSetup]
    public void Setup()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
        EventMetaDataRegistry.RegisterMetaData<CoalescedBenchEvent>(new CoalescedBenchEventMetaData());
        EventMetaDataRegistry.RegisterMetaData<LatestBenchEvent>(new LatestBenchEventMetaData());

        var layer = new BenchLayer();
        layer.RegisterService(new BenchManager());

        var postOptions = new PostSchedulerOptions(
            readyCapacity: OneMillion + 1024,
            nextCapacity: OneMillion + 1024,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);

        var timerOptions = TimeSchedulerOptions.Default;

        _runtime = LayerHub.CreateLayers()
                           .Push(layer)
                           .SetPostOptions(postOptions)
                           .SetTimerOptions(timerOptions)
                           .Build();
    }

    [Benchmark(Description = "Send (同步分发) - 20万次")]
    public void Send()
    {
        for (var i = 0; i < OneMillion; i++)
            _runtime.Send(BenchEvent.Instance);
    }

    [Benchmark(Description = "Post Normal (20万次)")]
    public void PostNormal()
    {
        const int batchSize = 1000;
        int batches = OneMillion / batchSize;
        for (var i = 0; i < batches; i++)
        {
            for (int j = 0; j < batchSize; j++)
                _runtime.Post(BenchEvent.Instance);
            _runtime.Scheduler.Pump();
        }
    }

    [Benchmark(Description = "SchedulePost (1000次)")]
    public void SchedulePost()
    {
        // Scheduling is more expensive, so we do fewer in benchmark
        for (var i = 0; i < 1000; i++)
            _runtime.SchedulePost(BenchEvent.Instance, 0.1f);

        // Cleanup
        _runtime.Pump(0.2f);
    }
}