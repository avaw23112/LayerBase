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

        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);

        LayerHub.Pump(0.016f);

        Assert.That(received, Is.EqualTo(20));
    }
}
