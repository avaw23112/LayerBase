using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ServiceProviderAliasDisposalTests
{
    [Test]
    public void Aliased_service_is_disposed_once()
    {
        var runtime = new LayerRuntime(40001);
        var layer = new TestLayer();
        layer.AttachToContext(runtime);

        var instance = new AliasedDisposable();

        var provider = new ServiceProvider(
            runtime,
            new[]
            {
                new ServiceDescriptor(
                    typeof(IAliasReader),
                    null,
                    ServiceLifetime.Instance,
                    null,
                    instance),
                new ServiceDescriptor(
                    typeof(IAliasWriter),
                    null,
                    ServiceLifetime.Instance,
                    null,
                    instance),
            },
            layer);

        // Resolve services to trigger instance creation
        provider.GetService(typeof(IAliasReader));
        provider.GetService(typeof(IAliasWriter));

        provider.Dispose();

        Assert.That(instance.DisposeCount, Is.EqualTo(1));
    }

    private interface IAliasReader
    {
    }

    private interface IAliasWriter
    {
    }

    private sealed class AliasedDisposable :
        IAliasReader,
        IAliasWriter,
        IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class TestLayer : Layer
    {
    }
}
