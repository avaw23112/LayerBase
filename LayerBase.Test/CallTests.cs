using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public partial class CallTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        CancellationEchoCallHandler.LastToken = default;
    }

    [Test]
    public async Task CallAsync_routes_by_request_response_and_returns_result()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());

        LayerHub.CreateLayers().Push(coreLayer).Build();

        var response = await LayerHub.CallAsync<SwitchSceneRequest, SwitchSceneResponse>(
            new SwitchSceneRequest("BattleScene"));

        Assert.That(response.SceneName, Is.EqualTo("BattleScene"));
        Assert.That(response.Success, Is.True);
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("BattleScene"));
    }

    [Test]
    public void CallAsync_fails_when_local_route_is_missing()
    {
        LayerHub.CreateLayers().Push(new UiLayer()).Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<SwitchSceneRequest, SwitchSceneResponse>(new SwitchSceneRequest("Missing")),
            Throws.TypeOf<ScopeLocalCallRouteNotFoundException>());
    }

    [Test]
    public void CallAsync_fails_when_route_is_missing()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<UnknownRequest, UnknownResponse>(new UnknownRequest("NoRoute")),
            Throws.TypeOf<ScopeLocalCallRouteNotFoundException>());
    }

    [Test]
    public async Task CallHandler_can_access_current_layer_service_via_Get()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        var response = await LayerHub.CallAsync<ServiceLookupRequest, ServiceLookupResponse>(
            new ServiceLookupRequest("LookupValue"));

        Assert.That(response.StoredValue, Is.EqualTo("LookupValue"));
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("LookupValue"));
    }

    [Test]
    public async Task CallHandler_can_compose_other_layer_capability_via_local_call()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        var audioLayer = new AudioLayer();
        audioLayer.RegisterService(new AudioLayerServicesModule());

        LayerHub.CreateLayers().Push(coreLayer).Push(audioLayer).Build();

        var response = await LayerHub.CallAsync<CrossLayerAccessRequest, CrossLayerAccessResponse>(
            new CrossLayerAccessRequest());

        Assert.That(response.Value, Is.EqualTo("MainMixer"));
    }

    [Test]
    public void CallHandler_cannot_directly_get_other_layer_scoped_service()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        var audioLayer = new AudioLayer();
        audioLayer.RegisterService(new AudioLayerServicesModule());

        LayerHub.CreateLayers().Push(coreLayer).Push(audioLayer).Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<DirectCrossLayerAccessRequest, DirectCrossLayerAccessResponse>(
                    new DirectCrossLayerAccessRequest()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void OwnerLayer_is_required_for_call_handler_auto_registration()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<NoOwnerLayerRequest, NoOwnerLayerResponse>(new NoOwnerLayerRequest()),
            Throws.TypeOf<ScopeLocalCallRouteNotFoundException>());
    }

    [Test]
    public async Task CancellationToken_is_passed_to_handler()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        using var cts = new CancellationTokenSource();
        var response = await LayerHub.CallAsync<CancellationEchoRequest, CancellationEchoResponse>(
            new CancellationEchoRequest(), cts.Token);

        Assert.That(response.CanBeCanceled, Is.True);
        Assert.That(CancellationEchoCallHandler.LastToken, Is.EqualTo(cts.Token));
    }

    [Test]
    public void Call_handler_exception_is_propagated_to_caller()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<ThrowingRequest, ThrowingResponse>(new ThrowingRequest()),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Call exploded"));
    }

    [Test]
    public async Task CallAsync_supports_the_expected_business_call_shape()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(coreLayer).Build();

        var response = await LayerHub.CallAsync<SwitchSceneRequest, SwitchSceneResponse>(
            new SwitchSceneRequest("TitleScene"));

        Assert.That(response.SceneName, Is.EqualTo("TitleScene"));
    }

    [Test]
    public async Task Layer_method_marked_with_Call_is_auto_registered()
    {
        var layer = new LayerMethodLayer();
        layer.RegisterService(new CoreLayerServicesModule());
        LayerHub.CreateLayers().Push(layer).Build();

        var response = await LayerHub.CallAsync<LayerMethodRequest, LayerMethodResponse>(
            new LayerMethodRequest("LayerScene"));

        Assert.That(response.SceneName, Is.EqualTo("LayerScene"));
        Assert.That(layer.GetService<SceneService>().LastScene, Is.EqualTo("LayerScene"));
    }

    [Test]
    public async Task Local_call_routes_by_request_response_without_target_layer()
    {
        var coreLayer = new CoreLayer();
        coreLayer.RegisterService(new CoreLayerServicesModule());

        LayerHub.CreateLayers().Push(coreLayer).Build();

        var response = await LayerHub.CallAsync<SwitchSceneRequest, SwitchSceneResponse>(
            new SwitchSceneRequest("ScopeLocalScene"));

        Assert.That(response.SceneName, Is.EqualTo("ScopeLocalScene"));
        Assert.That(response.Success, Is.True);
        Assert.That(coreLayer.GetService<SceneService>().LastScene, Is.EqualTo("ScopeLocalScene"));
    }

}

public struct SwitchSceneRequest
{
    public SwitchSceneRequest(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; set; }
}

public struct SwitchSceneResponse
{
    public SwitchSceneResponse(bool success, string sceneName)
    {
        Success = success;
        SceneName = sceneName;
    }

    public bool Success { get; set; }
    public string SceneName { get; set; }
}

public struct UnknownRequest
{
    public UnknownRequest(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

public struct UnknownResponse
{
    public UnknownResponse(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

public struct ServiceLookupRequest
{
    public ServiceLookupRequest(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public struct ServiceLookupResponse
{
    public ServiceLookupResponse(string storedValue)
    {
        StoredValue = storedValue;
    }

    public string StoredValue { get; set; }
}

public struct CrossLayerAccessRequest
{
}

public struct CrossLayerAccessResponse
{
    public CrossLayerAccessResponse(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public struct DirectCrossLayerAccessRequest
{
}

public struct DirectCrossLayerAccessResponse
{
    public DirectCrossLayerAccessResponse(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public struct AudioMixerQueryRequest
{
}

public struct AudioMixerQueryResponse
{
    public AudioMixerQueryResponse(string mixerName)
    {
        MixerName = mixerName;
    }

    public string MixerName { get; set; }
}

public struct NoOwnerLayerRequest
{
}

public struct NoOwnerLayerResponse
{
    public NoOwnerLayerResponse(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public struct CancellationEchoRequest
{
}

public struct CancellationEchoResponse
{
    public CancellationEchoResponse(bool canBeCanceled)
    {
        CanBeCanceled = canBeCanceled;
    }

    public bool CanBeCanceled { get; set; }
}

public struct ThrowingRequest
{
}

public struct ThrowingResponse
{
}

public struct DuplicateRequest
{
}

public struct DuplicateResponse
{
    public DuplicateResponse(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
}

public readonly struct LayerMethodRequest
{
    public LayerMethodRequest(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public readonly struct LayerMethodResponse
{
    public LayerMethodResponse(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public sealed class SceneService
{
    public string LastScene { get; private set; } = string.Empty;

    public void SwitchTo(string sceneName)
    {
        LastScene = sceneName;
    }
}

public sealed class AudioScopedService
{
    public string MixerName => "MainMixer";
}

public sealed partial class CoreLayerServicesModule : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SceneService, SceneService>();
    }
}

public sealed partial class AudioLayerServicesModule : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<AudioScopedService, AudioScopedService>();
    }
}

public partial class CoreLayer : Layer
{
}

public class UiLayer : Layer
{
}

public partial class AudioLayer : Layer
{
}

public partial class DuplicateLayer : Layer
{
}

public partial class LayerMethodLayer : Layer
{
    [Call]
    public LBTask<LayerMethodResponse> HandleLayerMethodAsync(LayerMethodRequest request)
    {
        GetService<SceneService>().SwitchTo(request.SceneName);
        return LBTask<LayerMethodResponse>.FromResult(new LayerMethodResponse(request.SceneName));
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class SwitchSceneCallHandler : IScopeLocalCallHandler<SwitchSceneRequest, SwitchSceneResponse>
{
    public LBTask<SwitchSceneResponse> HandleAsync(SwitchSceneRequest request,
                                                   CancellationToken  cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sceneService = this.Get<SceneService>();
        sceneService.SwitchTo(request.SceneName);
        return LBTask<SwitchSceneResponse>.FromResult(new SwitchSceneResponse(true, request.SceneName));
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class ServiceLookupCallHandler : IScopeLocalCallHandler<ServiceLookupRequest, ServiceLookupResponse>
{
    public async LBTask<ServiceLookupResponse> HandleAsync(ServiceLookupRequest request,
                                                           CancellationToken    cancellationToken = default)
    {
        await LBTask.CompletedTask;
        var sceneService = this.Get<SceneService>();
        sceneService.SwitchTo(request.Value);
        return new ServiceLookupResponse(sceneService.LastScene);
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class CrossLayerAccessCallHandler : IScopeLocalCallHandler<CrossLayerAccessRequest, CrossLayerAccessResponse>
{
    public async LBTask<CrossLayerAccessResponse> HandleAsync(CrossLayerAccessRequest request,
                                                              CancellationToken       cancellationToken = default)
    {
        var audioResponse = await this.Call<AudioMixerQueryRequest, AudioMixerQueryResponse>(
            new AudioMixerQueryRequest(),
            cancellationToken);

        return new CrossLayerAccessResponse(audioResponse.MixerName);
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class DirectCrossLayerAccessCallHandler
    : IScopeLocalCallHandler<DirectCrossLayerAccessRequest, DirectCrossLayerAccessResponse>
{
    public LBTask<DirectCrossLayerAccessResponse> HandleAsync(DirectCrossLayerAccessRequest request,
                                                              CancellationToken             cancellationToken = default)
    {
        var audioService = this.Get<AudioScopedService>();
        return LBTask<DirectCrossLayerAccessResponse>.FromResult(
            new DirectCrossLayerAccessResponse(audioService.MixerName));
    }
}

[OwnerLayer(typeof(AudioLayer))]
public sealed class AudioMixerQueryCallHandler : IScopeLocalCallHandler<AudioMixerQueryRequest, AudioMixerQueryResponse>
{
    public LBTask<AudioMixerQueryResponse> HandleAsync(AudioMixerQueryRequest request,
                                                       CancellationToken      cancellationToken = default)
    {
        var audioService = this.Get<AudioScopedService>();
        return LBTask<AudioMixerQueryResponse>.FromResult(new AudioMixerQueryResponse(audioService.MixerName));
    }
}

public sealed class NoOwnerLayerCallHandler : IScopeLocalCallHandler<NoOwnerLayerRequest, NoOwnerLayerResponse>
{
    public LBTask<NoOwnerLayerResponse> HandleAsync(NoOwnerLayerRequest request,
                                                    CancellationToken   cancellationToken = default)
    {
        return LBTask<NoOwnerLayerResponse>.FromResult(new NoOwnerLayerResponse("ShouldNotRegister"));
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class CancellationEchoCallHandler : IScopeLocalCallHandler<CancellationEchoRequest, CancellationEchoResponse>
{
    public static CancellationToken LastToken;

    public LBTask<CancellationEchoResponse> HandleAsync(CancellationEchoRequest request,
                                                        CancellationToken       cancellationToken = default)
    {
        LastToken = cancellationToken;
        return LBTask<CancellationEchoResponse>.FromResult(
            new CancellationEchoResponse(cancellationToken.CanBeCanceled));
    }
}

[OwnerLayer(typeof(CoreLayer))]
public sealed class ThrowingCallHandler : IScopeLocalCallHandler<ThrowingRequest, ThrowingResponse>
{
    public LBTask<ThrowingResponse> HandleAsync(ThrowingRequest   request,
                                                CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Call exploded.");
    }
}

[OwnerLayer(typeof(DuplicateLayer))]
public sealed class DuplicateCallHandlerA : IScopeLocalCallHandler<DuplicateRequest, DuplicateResponse>
{
    public LBTask<DuplicateResponse> HandleAsync(DuplicateRequest  request,
                                                 CancellationToken cancellationToken = default)
    {
        return LBTask<DuplicateResponse>.FromResult(new DuplicateResponse("A"));
    }
}
