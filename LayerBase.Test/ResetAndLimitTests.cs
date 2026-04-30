using LayerBase;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class ResetAndLimitTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Reset_Clears_All_State()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers().Push(layer).Build();
        
        LayerHub.Pump(0.1f);
        
        LayerHub.Reset();
        
        var topology = LayerHub.GetTopologyMarkdown();
        Assert.That(topology, Does.Contain("No layers built."), "Topology should be cleared after Reset");
    }

    [Test]
    public void Layer_Limit_64_Is_Enforced()
    {
        var builder = LayerHub.CreateLayers();
        var layers = new List<TestLayer>();
        for (int i = 0; i < 64; i++)
        {
            var layer = new TestLayer();
            layers.Add(layer);
            builder.Push(layer);
        }
        
        Assert.DoesNotThrow(() => builder.Build());
        Assert.That(layers.Select(layer => layer.RouteIndex), Is.EqualTo(Enumerable.Range(0, 64)),
            "The 64-layer bitmap route space must be assigned exactly once during Build.");
        
        LayerHub.Reset();
        var builder2 = LayerHub.CreateLayers();
        for (int i = 0; i < 64; i++)
        {
            builder2.Push(new TestLayer());
        }
        
        Assert.Throws<InvalidOperationException>(() => builder2.Push(new TestLayer()), 
            "Should throw exception when exceeding 64 layers");
    }

    private class TestLayer : Layer
    {
    }
}
