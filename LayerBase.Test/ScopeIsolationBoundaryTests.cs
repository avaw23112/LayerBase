using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeIsolationBoundaryTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        StaticScopeSubscriber.Received.Clear();
    }

    [Test]
    public void Same_service_type_is_resolved_from_callers_owner_scope()
    {
        var layer = new ScopeBoundaryLayer();
        layer.RegisterService(
            typeof(IScopedBoundaryService),
            new ScopedBoundaryService("main"),
            typeof(MainScope));
        layer.RegisterService(
            typeof(IScopedBoundaryService),
            new ScopedBoundaryService("secondary"),
            typeof(SecondaryBoundaryScope));
        layer.RegisterService(
            typeof(SecondaryBoundaryConsumer),
            new SecondaryBoundaryConsumer(),
            typeof(SecondaryBoundaryScope));

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        var main = layer.GetService<IScopedBoundaryService>();
        var secondary = layer.GetService<SecondaryBoundaryConsumer>().ResolveScopedService();

        Assert.That(main.Name, Is.EqualTo("main"));
        Assert.That(secondary.Name, Is.EqualTo("secondary"));
    }

    [Test]
    public void Service_subscribe_uses_callers_owner_scope_even_for_static_handlers()
    {
        var layer = new ScopeBoundaryLayer();
        layer.RegisterService(
            typeof(StaticScopeSubscriber),
            new StaticScopeSubscriber(),
            typeof(SecondaryBoundaryScope));
        using var runtime = LayerHub.CreateLayers()
                                    .Push(layer)
                                    .Build();

        var subscriber = layer.GetService<StaticScopeSubscriber>();
        subscriber.SubscribeStaticHandler();
        Assert.That(ServiceLayerBinder.GetBinding(subscriber)?.OwnerScope.ScopeId, Is.EqualTo(SecondaryBoundaryScope.ScopeId));
        subscriber.SendLocalEvent("secondary");

        Assert.That(StaticScopeSubscriber.Received, Is.EqualTo(new[] { "secondary" }));
    }

    [Test]
    public async Task Layer_routes_calls_through_scope_owned_registry()
    {
        var layer = new RouteBoundaryLayer();
        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        var response = await LayerHub.CallAsync<RouteBoundaryRequest, RouteBoundaryResponse>(
            new RouteBoundaryRequest("ok"));

        Assert.That(response.Value, Is.EqualTo("handled:ok"));
    }

    private sealed class ScopeBoundaryLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        public Type[] __GetScopeDefinitionTypes()
        {
            return new[] { typeof(SecondaryBoundaryScope) };
        }
    }

    private readonly struct SecondaryBoundaryScope : IScopeDefinition
    {
        public const int ScopeId = 73;
    }

    private interface IScopedBoundaryService
    {
        string Name { get; }
    }

    private sealed class ScopedBoundaryService : IService, IScopedBoundaryService
    {
        public ScopedBoundaryService(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class SecondaryBoundaryConsumer : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public IScopedBoundaryService ResolveScopedService()
        {
            return this.GetService<IScopedBoundaryService>();
        }
    }

    private readonly struct StaticScopeEvent
    {
        public StaticScopeEvent(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private sealed class StaticScopeSubscriber : IService
    {
        public static readonly List<string> Received = new();

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void SubscribeStaticHandler()
        {
            this.Subscribe<StaticScopeEvent>(OnStaticEvent);
        }

        public void SendLocalEvent(string value)
        {
            this.Send(new StaticScopeEvent(value));
        }

        private static void OnStaticEvent(in StaticScopeEvent value)
        {
            Received.Add(value.Value);
        }
    }

    private sealed class RouteBoundaryLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterCallHandler<RouteBoundaryRequest, RouteBoundaryResponse>(
                new RouteBoundaryHandler());
        }
    }

    public readonly struct RouteBoundaryRequest
    {
        public RouteBoundaryRequest(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public readonly struct RouteBoundaryResponse
    {
        public RouteBoundaryResponse(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public sealed class RouteBoundaryHandler
        : IScopeLocalCallHandler<RouteBoundaryRequest, RouteBoundaryResponse>
    {
        public async LBTask<RouteBoundaryResponse> HandleAsync(
            RouteBoundaryRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LBTask.CompletedTask;
            return new RouteBoundaryResponse("handled:" + request.Value);
        }
    }
}
