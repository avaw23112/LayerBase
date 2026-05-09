using System.Threading;
using LayerBase.Actor;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace LayerBase.Test;

public struct ActorBridgeRequest
{
    public int Value;

    public ActorBridgeRequest(int value)
    {
        Value = value;
    }
}

public struct ActorBridgeResponse
{
    public int Value;
}

internal sealed partial class ActorCallIntegrationActor : IActor
{
    [ActorCallBehaviour]
    private LBTask<ActorBridgeResponse> OnAsk(
        in ActorBridgeRequest request,
        CancellationToken cancellationToken)
    {
        return LBTask<ActorBridgeResponse>.FromResult(new ActorBridgeResponse
        {
            Value = request.Value + 1
        });
    }

   
}

public sealed partial class ActorCallIntegrationLayer : Layer
{
    [Mount] private ActorCallIntegrationService _service = null!;

    public ActorCallIntegrationService Service => _service;

}

public sealed partial class ActorCallIntegrationService : IService, ILayerContext
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public LBTask<ActorBridgeResponse> AskActorBridge(
        ActorId actorId,
        int value,
        CancellationToken cancellationToken = default)
    {
        return ServiceActorExtensions.AskActor<ActorBridgeRequest, ActorBridgeResponse>(
            this,
            actorId,
            new ActorBridgeRequest(value),
            cancellationToken);
    }
}

[TestFixture]
public class ActorCallIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Layer_runtime_layer_service_and_context_can_ask_actor()
    {
        var runtime = new LayerRuntime(1);
        var layer = new ActorCallIntegrationLayer();
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(layer);
        builder.Build();

        ActorCallIntegrationActor actor = runtime.CreateActor<ActorCallIntegrationActor>();
        ActorId actorId = actor.GetActorId();

        Assert.That(AskAndPump(runtime, runtime.AskActor<ActorBridgeRequest, ActorBridgeResponse>(actorId, new ActorBridgeRequest(2))).Value, Is.EqualTo(3));
        Assert.That(AskAndPump(runtime, layer.AskActor<ActorBridgeRequest, ActorBridgeResponse>(actorId, new ActorBridgeRequest(3))).Value, Is.EqualTo(4));
        Assert.That(AskAndPump(runtime, layer.Service.AskActorBridge(actorId, 4)).Value, Is.EqualTo(5));
        Assert.That(
            AskAndPump(
                runtime,
                LayerContextActorExtensions.AskActor<ActorBridgeRequest, ActorBridgeResponse>(
                    (ILayerContext)layer.Service,
                    actorId,
                    new ActorBridgeRequest(5))).Value,
            Is.EqualTo(6));
    }

    private static TResponse AskAndPump<TResponse>(LayerRuntime runtime, LBTask<TResponse> task)
        where TResponse : struct
    {
        Assert.That(task.GetAwaiter().IsCompleted, Is.False);
        var budget = new RuntimeFrameBudget(16, 0, 0);
        runtime.Actors.Pump(0f, 0f, false, ref budget);
        return task.GetAwaiter().GetResult();
    }
}
