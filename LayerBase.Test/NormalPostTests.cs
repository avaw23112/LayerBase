using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;

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
public class NormalPostTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void NormalPost_ShouldStillUseExistingPath()
    {
        var received = 0;

        var layer = new TestLayer(e => received = e.Value);
        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        var result = runtime.TryPost(new TestEvent
        {
            Value = 20
        });

        Assert.That(result.IsSuccess, Is.True);
        LayerHub.Pump(0.016f);
        Assert.That(received, Is.EqualTo(20));
    }
}
