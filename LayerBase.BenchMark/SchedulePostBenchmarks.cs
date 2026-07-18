using BenchmarkDotNet.Attributes;
using LayerBase.Core.Event;

namespace Benchmarks;

[MemoryDiagnoser]
public class SchedulePostBenchmarks
{
    private struct BenchEvent
    {
        public int X;
        public int Y;
    }

    private PostTimerScheduler _timer = null!;
    private EventPayloadStorage _payloadStorage = null!;
    private PostScheduler _postScheduler = null!;

    [GlobalSetup]
    public void Setup()
    {
        var eventCenter = new EventCenter();
        var policyTable = new EventBuildPolicyTable();
        _payloadStorage = new EventPayloadStorage(PayloadDiagnosticsMode.Disabled);
        _postScheduler = new PostScheduler(0, eventCenter, PostSchedulerOptions.Default, policyTable);
        _timer = new PostTimerScheduler(0, TimeSchedulerOptions.Default, _payloadStorage, _postScheduler);

        _timer.PrewarmEvent<BenchEvent>();
        _postScheduler.PrewarmEvent<BenchEvent>();
        _timer.CompilePlans(policyTable, EventTypeIdAllocator.MaxId);

        for (int i = 0; i < 1000; i++)
        {
            var h = _timer.Schedule(new BenchEvent { X = i, Y = i }, 10.0f);
            _timer.Cancel(h);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _timer.Dispose();
        _postScheduler.Dispose();
        _payloadStorage.Dispose();
    }

    [Benchmark(Description = "Schedule + Cancel (OneShot, Warmed)")]
    public void ScheduleAndCancel_OneShot_Warmed()
    {
        var h = _timer.Schedule(new BenchEvent { X = 1, Y = 2 }, 10.0f);
        _timer.Cancel(h);
    }

    [Benchmark(Description = "Schedule + Expire (OneShot, Warmed)")]
    public void ScheduleAndExpire_OneShot_Warmed()
    {
        _ = _timer.Schedule(new BenchEvent { X = 1, Y = 2 }, 0.001f);
        _timer.Tick(0.02f);
    }

    [Benchmark(Description = "Schedule + Cancel (Long Timer, Warmed)")]
    public void ScheduleAndCancel_LongTimer_Warmed()
    {
        var h = _timer.Schedule(new BenchEvent { X = 1, Y = 2 }, 100.0f);
        _timer.Cancel(h);
    }
}
