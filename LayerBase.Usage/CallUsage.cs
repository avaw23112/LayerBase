using LayerBase.Async;
using LayerBase.Call;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Usage;

public readonly struct ChangeSceneRequest
{
    public ChangeSceneRequest(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public readonly struct ChangeSceneResponse
{
    public ChangeSceneResponse(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public readonly struct EchoSceneRequest
{
    public EchoSceneRequest(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public readonly struct EchoSceneResponse
{
    public EchoSceneResponse(string sceneName)
    {
        SceneName = sceneName;
    }

    public string SceneName { get; }
}

public sealed class SceneStateService
{
    public string CurrentScene { get; private set; } = "Boot";

    public void ChangeScene(string sceneName)
    {
        CurrentScene = sceneName;
    }
}

public partial class SceneLayer : Layer
{
    [Call]
    private LBTask<ChangeSceneResponse> HandleChangeSceneAsync(ChangeSceneRequest request)
    {
        GetService<SceneStateService>().ChangeScene(request.SceneName);
        return LBTask<ChangeSceneResponse>.FromResult(new ChangeSceneResponse(request.SceneName));
    }
}

public partial class SceneEchoService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SceneStateService, SceneStateService>();
    }
}

[OwnerLayer(typeof(SceneLayer))]
public sealed class EchoSceneCallHandler : ILayerCallHandler<EchoSceneRequest, EchoSceneResponse>
{
    public LBTask<EchoSceneResponse> HandleAsync(EchoSceneRequest request,
                                                 CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sceneState = this.Get<SceneStateService>();
        sceneState.ChangeScene(request.SceneName);
        return LBTask<EchoSceneResponse>.FromResult(new EchoSceneResponse(request.SceneName));
    }
}

public static class CallUsage
{
    public static void Run()
    {
        Console.WriteLine("--- Call Usage ---");
        LayerHub.Reset();

        var layer = new SceneLayer();
        layer.RegisterService(new SceneEchoService());
        LayerHub.CreateLayers().Push(layer).Build();

        var layerResponse = LayerHub.CallAsync<SceneLayer, ChangeSceneRequest, ChangeSceneResponse>(
                                        new ChangeSceneRequest("Battle"))
                                    .GetAwaiter()
                                    .GetResult();
        Console.WriteLine($"[Layer Call] Changed scene to: {layerResponse.SceneName}");

        var serviceResponse = LayerHub.For<SceneLayer>()
                                      .CallAsync<EchoSceneRequest, EchoSceneResponse>(
                                          new EchoSceneRequest("Settlement"))
                                      .GetAwaiter()
                                      .GetResult();
        Console.WriteLine($"[Service Call] Echoed scene to: {serviceResponse.SceneName}");
    }
}
