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
    private async LBTask<ChangeSceneResponse> HandleChangeSceneAsync(ChangeSceneRequest request)
    {
        await LBTask.CompletedTask;
        GetService<SceneStateService>().ChangeScene(request.SceneName);
        return new ChangeSceneResponse(request.SceneName);
    }
}

public partial class SceneEchoService : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<SceneStateService, SceneStateService>();
    }

    [Call]
    public async LBTask<EchoSceneResponse> EchoSceneAsync(
        EchoSceneRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LBTask.CompletedTask;
        var sceneState = this.GetService<SceneStateService>();
        sceneState.ChangeScene(request.SceneName);
        return new EchoSceneResponse(request.SceneName);
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
        var runtime = LayerHub.CreateLayers().Push(layer).Build();

        var layerResponse = runtime.CallAsync<ChangeSceneRequest, ChangeSceneResponse>(
                                       new ChangeSceneRequest("Battle"))
                                   .GetAwaiter()
                                   .GetResult();
        Console.WriteLine($"[Layer Call] Changed scene to: {layerResponse.SceneName}");

        var serviceResponse = runtime.CallAsync<EchoSceneRequest, EchoSceneResponse>(
                                     new EchoSceneRequest("Settlement"))
                                 .GetAwaiter()
                                 .GetResult();
        Console.WriteLine($"[Service Call] Echoed scene to: {serviceResponse.SceneName}");
    }
}
