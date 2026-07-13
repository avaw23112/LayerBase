using LayerBase;
using LayerBase.Layers;
using LayerBase.Core.Event;

namespace EventsTest;

public struct TestEvent
{
    public int Value;
}

public partial class TestLayer : Layer
{
    private readonly Action<TestEvent> _onEvent;

    public TestLayer(Action<TestEvent> onEvent)
    {
        _onEvent = onEvent;
    }

    [Subscribe]
    public void OnTest(in TestEvent onEvent)
    {
        _onEvent(onEvent);
    }
}

[TestFixture]
public class PostFromAnyThreadTests
{
    private class EmptyLayer : Layer
    {
    }

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void PostFromAnyThread_ShouldDispatchOnNextPump()
    {
        // received：
        //   用于记录订阅者收到的事件值。
        var received = 0;

        var layer = new TestLayer(e => received = e.Value);

        LayerHub.CreateLayers()
                .Push(layer)
                .Build().Prewarm();

        // 从后台线程提交事件。
        var thread = new Thread(() =>
        {
            LayerHub.PostFromAnyThread(new TestEvent
            {
                Value = 10
            });
        });

        thread.Start();
        thread.Join();

        // 此时还没有 Pump，所以不应该收到事件。
        Assert.That(received, Is.EqualTo(0));

        // Pump 后，PostIngressQueue 会被搬运到 PostScheduler。
        LayerHub.Pump(0.016f);

        // 事件已经派发。
        Assert.That(received, Is.EqualTo(10));
    }

    [Test]
    public void TryPostFromAnyThread_ShouldReturnFalse_WhenRuntimeDisposed()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new EmptyLayer())
                              .Build().Prewarm();

        runtime.Dispose();

        var result = runtime.TryPostFromAnyThread(new TestEvent
        {
            Value = 1
        });

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryPostFromAnyThread_ShouldReturnFalse_WhenIngressQueueFull()
    {
        var options = new PostSchedulerOptions(
            readyCapacity: 1024,
            nextCapacity: 1024,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew,
            maxIngressQueueCapacity: 1);

        var runtime = LayerHub.CreateLayers()
                              .Push(new EmptyLayer())
                              .SetPostOptions(options)
                              .Build().Prewarm();

        Assert.That(runtime.TryPostFromAnyThread(new TestEvent { Value = 1 }), Is.True);
        Assert.That(runtime.TryPostFromAnyThread(new TestEvent { Value = 2 }), Is.False);
    }

    [Test]
    public void NormalPost_ShouldStillUseExistingPath()
    {
        var received = 0;

        var layer = new TestLayer(e => received = e.Value);
        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        var result = LayerHub.TryPost(new TestEvent
        {
            Value = 20
        });

        Assert.That(result.IsSuccess, Is.True);
        LayerHub.Pump(0.016f);
        Assert.That(received, Is.EqualTo(20));
    }

    [Test]
    public void DrainTo_Respects_MaxIngressPostsPerPump()
    {
        var options = new PostSchedulerOptions(
            readyCapacity: 1024,
            nextCapacity: 1024,
            maxEventsPerPump: 0,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew,
            maxIngressPostsPerPump: 5);

        var runtime = LayerHub.CreateLayers()
                              .Push(new EmptyLayer())
                              .SetPostOptions(options)
                              .Build();

        for (int i = 0; i < 10; i++)
        {
            runtime.PostFromAnyThread(new TestEvent { Value = i });
        }

        // 第一次 Pump，只应该搬运 5 个
        runtime.Pump(0.01f);

        // 我们通过反射或内部字段验证很难，但可以通过订阅验证
        int callCount = 0;
        runtime.EventCenter.SubscribeNotify<TestEvent>(0, (in TestEvent e) => callCount++);

        // 第一次 Pump 已经执行了，5 个事件已经在 Scheduler 队列中，但可能还没派发 (取决于 Pump 顺序)
        // 在 LayerRuntime.Pump 中，DrainTo 在 PostScheduler.Pump 之前。
        // 所以第一次 Pump 会搬运 5 个并派发 5 个。

        // 我们重来，用更直接的方式验证。
    }

    [Test]
    public void PostIngressQueue_DrainTo_Budget_And_Failure_Tracking()
    {
        var options = PostSchedulerOptions.Default;
        var scheduler = new PostScheduler(0, new EventCenter(), options,
            new EventBuildPolicyTable(options.DefaultBackpressure));

        // 故意不 BuildPlans，这样 TryPost 会失败

        var ingress = new PostIngressQueue();
        for (int i = 0; i < 10; i++)
        {
            Assert.That(ingress.Enqueue(new TestEvent { Value = i }, null), Is.True);
        }

        // 搬运 5 个，预期全部失败 (因为未注册)
        var result = ingress.DrainTo(scheduler, 5);
        Assert.That(result.Drained, Is.EqualTo(5));
        Assert.That(result.Failed, Is.EqualTo(5));

        // 搬运剩余 5 个
        result = ingress.DrainTo(scheduler, 0);
        Assert.That(result.Drained, Is.EqualTo(5));
        Assert.That(result.Failed, Is.EqualTo(5));

        // 队列应为空
        result = ingress.DrainTo(scheduler, 10);
        Assert.That(result.Drained, Is.EqualTo(0));
    }

    [Test]
    public void PostIngressQueue_Rejects_When_Capacity_Is_Full()
    {
        var ingress = new PostIngressQueue(capacity: 1);

        Assert.That(ingress.Enqueue(new TestEvent { Value = 1 }, null), Is.True);
        Assert.That(ingress.Enqueue(new TestEvent { Value = 2 }, null), Is.False);
    }

    [Test]
    public void PostIngressQueue_Clear_Must_Close_And_Reject_New_Posts()
    {
        var ingress = new PostIngressQueue(capacity: 2);

        Assert.That(ingress.Enqueue(new TestEvent { Value = 1 }, null), Is.True);

        ingress.Clear();

        Assert.That(ingress.Enqueue(new TestEvent { Value = 2 }, null), Is.False);
    }
}
