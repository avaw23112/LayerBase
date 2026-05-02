using LayerBase.DI;
using LayerBase.Layers;

namespace LayerBase.Usage;

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
}

public partial class VerifyLayer : Layer
{
    // 证明 1：如果生成器工作，这个字段会被 new VerifyService() 赋值
    [Mount] public VerifyService2 MountedService2;
    [Mount] public VerifyService1 MountedService1;
    [Mount] public VerifyService3 MountedService3;
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

        // 验证 1：字段初始化证明
        if (layer.MountedService1 != null)
        {
            Console.WriteLine("[PASS] Mount Proof 1: Field 'MountedService' was auto-instantiated by Generator.");
        }
        else
        {
            Console.WriteLine("[FAIL] Mount Proof 1: Field 'MountedService' is still null.");
        }

        // 验证 2：自动注册证明
        // 如果生成器生成了 typedLayer.RegisterService(...)，那么我们应该能从 Layer 拿到它
        var resolvedService = layer.GetService<VerifyService1>();
        if (resolvedService != null && ReferenceEquals(resolvedService, layer.MountedService1))
        {
            Console.WriteLine("[PASS] Mount Proof 2: Service was auto-registered to Layer DI container.");
        }
        else
        {
            Console.WriteLine("[FAIL] Mount Proof 2: Service was NOT found in DI container.");
        }

        // 验证 3：生命周期触发证明
        if (layer.MountedService1?.IsInitialized == true)
        {
            Console.WriteLine("[PASS] Mount Proof 3: Service lifecycle (ConfigureServices) was triggered.");
        }
        else
        {
            Console.WriteLine("[FAIL] Mount Proof 3: Service lifecycle was NOT triggered.");
        }
    }
}
