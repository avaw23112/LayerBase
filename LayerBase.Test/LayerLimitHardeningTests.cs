using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class LayerLimitHardeningTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Building_65_layers_fails_clearly()
    {
        var builder = LayerHub.CreateLayers();

        for (int i = 0; i < 64; i++)
        {
            builder.Push(new LimitLayer());
        }

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.Push(new LimitLayer()));

        Assert.That(ex!.Message, Does.Contain("maximum of 64 layers"));
        Assert.That(ex.Message, Does.Contain("bitmap routing constraints"));
    }

    private sealed class LimitLayer : Layer
    {
    }
}
