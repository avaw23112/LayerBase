using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace LayerBase.Test;

public struct RuntimeSchedulerEvent
{
    public int Value;

    public RuntimeSchedulerEvent(int value)
    {
        Value = value;
    }
}

public struct RuntimeActorEvent
{
    public int Value;

    public RuntimeActorEvent(int value)
    {
        Value = value;
    }
}

internal static class ActorRuntimeIntegrationTrace
{
    public static List<string> Entries { get; } = new();
}

internal sealed class UpdateOrderingLayer : Layer
{
}

internal sealed class BudgetLayer : Layer
{
}

internal sealed partial class UpdateOrderingService : IService, IUpdate
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnScheduler(in RuntimeSchedulerEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"scheduler:{value.Value}");
    }

    public void Update()
    {
        ActorRuntimeIntegrationTrace.Entries.Add("update");
    }
}

internal sealed partial class BudgetService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    [Subscribe]
    public void OnScheduler(in RuntimeSchedulerEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"scheduler:{value.Value}");
    }
}

internal sealed partial class IntegrationActor : IActor
{
    [ActorBehaviour]
    private void OnActor(in RuntimeActorEvent value)
    {
        ActorRuntimeIntegrationTrace.Entries.Add($"actor:{value.Value}");
    }
}

[TestFixture]
public class ActorRuntimeIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        ActorRuntimeIntegrationTrace.Entries.Clear();
    }

    [Test]
    public void Runtime_exposes_actor_world_and_pump_advances_actor_processing()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(), PostSchedulerOptions.Default);

        Assert.That(runtime.Actors, Is.Not.Null);

        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();
        actor.Post(new RuntimeActorEvent(7));

        runtime.Pump(0.016f);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Does.Contain("actor:7"));
    }

    [Test]
    public void Actor_world_runs_after_post_scheduler_and_before_layer_update()
    {
        LayerRuntime runtime = BuildRuntime(new UpdateOrderingLayer(), new UpdateOrderingService(), PostSchedulerOptions.Default);
        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();

        runtime.Post(new RuntimeSchedulerEvent(1));
        actor.Post(new RuntimeActorEvent(2));

        runtime.Pump(0.016f);

        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:1", "actor:2", "update" }));
    }

    [Test]
    public void Post_scheduler_budget_exhaustion_prevents_actor_pump_until_next_frame()
    {
        var options = new PostSchedulerOptions(
            readyCapacity: 16,
            nextCapacity: 16,
            maxEventsPerPump: 1,
            maxMillisecondsPerPump: 0,
            maxWavesPerPump: 1,
            timeCheckInterval: 64,
            defaultBackpressure: BackpressurePolicy.RejectNew);

        LayerRuntime runtime = BuildRuntime(new BudgetLayer(), new BudgetService(), options);
        IntegrationActor actor = runtime.Actors.CreateActor<IntegrationActor>();

        runtime.Post(new RuntimeSchedulerEvent(9));
        actor.Post(new RuntimeActorEvent(3));

        runtime.Pump(0.016f);
        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:9" }));

        runtime.Pump(0.016f);
        Assert.That(ActorRuntimeIntegrationTrace.Entries, Is.EqualTo(new[] { "scheduler:9", "actor:3" }));
    }

    private static LayerRuntime BuildRuntime(Layer layer, IService service, PostSchedulerOptions options)
    {
        var runtime = new LayerRuntime(1);
        layer.RegisterService(service);

        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(layer);
        builder.SetPostOptions(options);
        return builder.Build();
    }
}
