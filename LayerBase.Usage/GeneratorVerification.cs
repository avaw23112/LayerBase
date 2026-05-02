using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Usage;

public partial class VerifyManager : ILayerContext                 
{
    [Subscribe]
    public void OnVerify(in ChangeSceneRequest value)
    {
        
    }
}   
[OwnerLayer(typeof(VerifyLayer))]
public partial class VerifyService1 : IService
{
    public bool IsInitialized = false;
    public void ConfigureServices(IServiceCollection services) 
    {
        IsInitialized = true;
    }
}
[OwnerLayer(typeof(VerifyLayer))]
public partial class VerifyService2 : IService
{
    public bool IsInitialized = false;
    public void ConfigureServices(IServiceCollection services) 
    {
        IsInitialized = true;
    }
}
[OwnerLayer(typeof(VerifyLayer))]
public partial class VerifyService3 : IService
{
    public bool IsInitialized = false;
    public void ConfigureServices(IServiceCollection services) 
    {
        IsInitialized = true;
    }
    
    [Subscribe]
    public void OnVerify(in ChangeSceneRequest @event)
    {
        
    }
}


[OwnerLayer(typeof(VerifyLayer))]
public partial class VerifyCallHandler : ILayerCallHandler<ChangeSceneRequest, ChangeSceneResponse>
{
    public LBTask<ChangeSceneResponse> HandleAsync(ChangeSceneRequest request, CancellationToken cancellationToken = default)
    {
        return LBTask<ChangeSceneResponse>.FromResult(new ChangeSceneResponse(request.SceneName));
    }
}

public partial class VerifyLayer : Layer
{
    // 证明 1：如果生成器工作，这个字段会被 new VerifyService() 赋值
    [Mount] public VerifyService2 MountedService2;
    [Mount] public VerifyService3 MountedService3;
    [Mount] public VerifyService1 MountedService1;
}

public static class GeneratorVerification
{
    public static void Run()
    {
        Console.WriteLine("--- LayerServiceGenerator Deep Verification ---");

        // 初始化环境
        LayerHub.Reset();
        var layer = new VerifyLayer();

        // 执行挂载逻辑（模拟正常的 Layer 生命周期）
        // LayerHub 在 Build 时会触发所有 [SourceGeneratedServiceInit] 标记的方法
        LayerHub.CreateLayers().Push(layer).Build();
    }
}
