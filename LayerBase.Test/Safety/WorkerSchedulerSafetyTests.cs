using System.Reflection;
using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Worker;

namespace EventsTest.Safety;

[TestFixture]
public sealed class WorkerSchedulerSafetyTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        LayerHub.Reset();
    }

    [Test]
    public void WorkerScheduler_CanExecuteMoreThanStateCapacityJobs()
    {
        int stateCapacity = 32;
        int targetCount = stateCapacity * 4;

        var layer = new ProbeLayer();
        var service = new ProbeService();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var freeCountField = typeof(WorkerJobScheduler)
            .GetField("_freeCount", BindingFlags.NonPublic | BindingFlags.Instance)!;

        int initialFree = (int)freeCountField.GetValue(runtime.WorkerJobs)!;

        for (int batch = 0; batch < targetCount / stateCapacity; batch++)
        {
            var handles = new List<WorkerHandle>();
            for (int i = 0; i < stateCapacity; i++)
            {
                handles.Add(service.Run(batch * stateCapacity + i));
            }

            bool allDone = SpinUntil(() =>
            {
                foreach (var h in handles)
                {
                    var state = runtime.WorkerJobs.GetState(h);
                    if (state != WorkerState.Completed &&
                        state != WorkerState.Failed &&
                        state != WorkerState.Cancelled)
                        return false;
                }
                return true;
            }, TimeSpan.FromSeconds(15));

            Assert.That(allDone, Is.True,
                $"Batch {batch}: not all jobs completed within timeout.");
        }

        int finalFree = (int)freeCountField.GetValue(runtime.WorkerJobs)!;
        Assert.That(finalFree, Is.EqualTo(initialFree),
            "After all jobs complete, free count should return to initial capacity.");
    }

    [Test]
    public void WorkerScheduler_OldHandleCannotControlReusedSlot()
    {
        var layer = new ProbeLayer();
        var service = new ProbeService();
        layer.RegisterService(service);

        using var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        WorkerHandle firstHandle = service.Run(1);
        Assert.That(
            SpinUntil(() => runtime.WorkerJobs.GetState(firstHandle) == WorkerState.Completed, TimeSpan.FromSeconds(10)),
            Is.True);

        var oldHandle = firstHandle;
        WorkerHandle secondHandle = service.Run(2);
        Assert.That(
            SpinUntil(() => runtime.WorkerJobs.GetState(secondHandle) == WorkerState.Completed, TimeSpan.FromSeconds(10)),
            Is.True);

        var oldState = runtime.WorkerJobs.GetState(oldHandle);
        var newState = runtime.WorkerJobs.GetState(secondHandle);
        Assert.That(oldState, Is.Not.EqualTo(newState),
            "Old handle state must not equal new handle state.");
    }

    private static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    private sealed class ProbeLayer : Layer
    {
    }

    private sealed class ProbeService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public WorkerHandle Run(int value, WorkerEventJobOptions options = default)
        {
            return this.WorkerJobs().Run<ProbeJob, ProbeInput, ProbeResult>(
                new ProbeJob(),
                new ProbeInput(value),
                options);
        }
    }

    private readonly struct ProbeInput
    {
        public ProbeInput(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct ProbeResult
    {
        public ProbeResult(int value) { Value = value; }
        public int Value { get; }
    }

    private readonly struct ProbeJob : IWorkerEventJob<ProbeInput, ProbeResult>
    {
        public ProbeResult Execute(in ProbeInput input, in WorkerJobContext context)
        {
            return new ProbeResult(input.Value * 2);
        }
    }
}
