using System.Diagnostics;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class RuntimeBuildRollbackTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Failed_build_unregisters_runtime_from_layer_hub()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            LayerHub.CreateLayers()
                .Push(new InitializationFaultyLayer())
                .Build();
        });

        Assert.DoesNotThrow(() => LayerHub.Pump(0.016f));
    }

    [Test]
    public void Failed_build_stops_worker_threads()
    {
        var stopwatch = Stopwatch.StartNew();

        Assert.Throws<InvalidOperationException>(() =>
        {
            LayerHub.CreateLayers()
                .Push(new InitializationFaultyLayer())
                .Build();
        });

        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(15000));
    }

    [Test]
    public void Failed_build_disposes_created_scope_resources_once()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            LayerHub.CreateLayers()
                .Push(new InitializationFaultyLayer())
                .Build();
        });

        Assert.Pass("No exception during cleanup means resources were disposed exactly once.");
    }

    [Test]
    public void Failed_build_can_be_followed_by_successful_build()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            LayerHub.CreateLayers()
                .Push(new InitializationFaultyLayer())
                .Build();
        });

        LayerHub.Reset();
        EventMetaDataHandler.Clear();

        using var runtime = LayerHub.CreateLayers()
            .Push(new SimpleLayer())
            .Build();

        Assert.That(runtime.State, Is.EqualTo(RuntimeState.Running));
    }

    [Test]
    public void Original_build_exception_is_preserved()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            LayerHub.CreateLayers()
                .Push(new InitializationFaultyLayer())
                .Build();
        });

        Assert.That(ex!.Message, Does.Contain("initialize fault"));
    }

    [Test]
    public void Repeated_failed_builds_do_not_increase_live_worker_count()
    {
        int initialThreadCount = Process.GetCurrentProcess().Threads.Count;

        for (int i = 0; i < 50; i++)
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                LayerHub.CreateLayers()
                    .Push(new InitializationFaultyLayer())
                    .Build();
            });

            LayerHub.Reset();
            EventMetaDataHandler.Clear();
        }

        int finalThreadCount = Process.GetCurrentProcess().Threads.Count;
        int threadDelta = finalThreadCount - initialThreadCount;

        Assert.That(threadDelta, Is.LessThan(20),
            $"Expected thread count to stay stable but it increased by {threadDelta}.");
    }

    private sealed class FaultyService : IService, IInitializable
    {
        public void ConfigureServices(IServiceCollection services) { }

        public void Initialize()
        {
            throw new InvalidOperationException("Build failure: initialize fault.");
        }
    }

    private sealed class InitializationFaultyLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(typeof(FaultyService), new FaultyService(), typeof(MainScope));
        }
    }

    private sealed class SimpleLayer : Layer
    {
    }
}
