using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public sealed class BuildFailureCleanupTests
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
    public void ConfigureTools_failure_must_abort_runtime_registration()
    {
        AssertBuildFailureCleans(
            LayerHub.CreateLayers()
                    .Push(new EmptyBuildLayer())
                    .ConfigureTools(_ => throw new InvalidOperationException("configure tools failure")),
            "configure tools failure");
    }

    [Test]
    public void Layer_prebuild_failure_must_abort_runtime_registration()
    {
        AssertBuildFailureCleans(
            LayerHub.CreateLayers().Push(new ThrowingConfigureLayer()),
            "layer configure failure");
    }

    [Test]
    public void Service_factory_failure_must_abort_runtime_registration()
    {
        AssertBuildFailureCleans(
            LayerHub.CreateLayers().Push(new ThrowingServiceFactoryLayer()),
            "service factory failure");
    }

    [Test]
    public void Layer_postbuild_failure_must_abort_runtime_registration()
    {
        AssertBuildFailureCleans(
            LayerHub.CreateLayers().Push(new ThrowingPostBuildLayer()),
            "post build failure");
    }

    [Test]
    public void RuntimeStart_failure_must_abort_without_runtime_stop_callback()
    {
        var layer = new ThrowingRuntimeStartLayer();

        AssertBuildFailureCleans(
            LayerHub.CreateLayers().Push(layer),
            "runtime start failure");

        Assert.That(layer.RuntimeStopCalled, Is.False);
    }

    [Test]
    public void Build_failure_must_not_keep_runtime_or_layer_alive()
    {
        (WeakReference runtime, WeakReference layer) = CreateFailedRuntimeWeakReferences();

        for (int i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.That(runtime.IsAlive, Is.False);
        Assert.That(layer.IsAlive, Is.False);
    }

    private static void AssertBuildFailureCleans(
        LayerRuntime.LayersBuilder builder,
        string expectedMessage)
    {
        var error = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(error!.Message, Does.Contain(expectedMessage));

        Assert.That(LayerHub.ActiveRuntimeCountForTest, Is.EqualTo(0));
        Assert.That(LayerHub.HasPrimaryRuntimeForTest, Is.False);

        using var next = LayerHub.CreateLayers().Push(new EmptyBuildLayer()).Build();
        Assert.That(next.Id, Is.EqualTo(0));
    }

    private static (WeakReference Runtime, WeakReference Layer) CreateFailedRuntimeWeakReferences()
    {
        var layer = new ThrowingPostBuildLayer();
        var builder = LayerHub.CreateLayers().Push(layer);
        var runtime = new WeakReference(builder.RuntimeForTest);
        var layerReference = new WeakReference(layer);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
        return (runtime, layerReference);
    }

    private sealed class EmptyBuildLayer : Layer
    {
    }

    private sealed class ThrowingConfigureLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            throw new InvalidOperationException("layer configure failure");
        }
    }

    private sealed class ThrowingServiceFactoryLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ThrowingFactoryService>(_ =>
                throw new InvalidOperationException("service factory failure"));
        }
    }

    private sealed class ThrowingFactoryService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class ThrowingPostBuildLayer : Layer, IPostBuild
    {
        public void PostBuild()
        {
            throw new InvalidOperationException("post build failure");
        }
    }

    private sealed class ThrowingRuntimeStartLayer : Layer, IRuntimeStart, IRuntimeStop
    {
        public bool RuntimeStopCalled { get; private set; }

        public void RuntimeStart()
        {
            throw new InvalidOperationException("runtime start failure");
        }

        public void RuntimeStop()
        {
            RuntimeStopCalled = true;
        }
    }
}
