using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

public class ServiceRegistrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Singleton_service_from_root_is_shared_across_layers()
    {
        var rootInstance = new DemoService();
        var layer1 = new DemoLayer();
        var layer2 = new DemoLayer();
        
        layer1.RegisterService(new InstanceServiceModule(rootInstance));
        layer2.RegisterService(new InstanceServiceModule(rootInstance));

        using var runtime = LayerHub.CreateLayers().Push(layer1).Push(layer2).Build();

        Assert.That(layer1.GetService<IDemoService>(), Is.SameAs(layer2.GetService<IDemoService>()));
    }

    [Test]
    public void MultiInstance_Isolation_And_Broadcast_Tests()
    {
        // 1. 创建两个隔离的运行时
        var layer1 = new DemoLayer();
        var layer2 = new DemoLayer();

        using var rt1 = LayerHub.CreateLayers().Push(layer1).Build();
        using var rt2 = LayerHub.CreateLayers().Push(layer2).Build();

        var count1 = 0;
        var count2 = 0;

        // 🚀 通过 Layer 实例进行订阅（Public API）
        layer1.Subscribe((in ServiceTestEvent _) => { count1++; return EventHandledState.Continue; });
        layer2.Subscribe((in ServiceTestEvent _) => { count2++; return EventHandledState.Continue; });

        // 2. 验证每个运行时的隔离发送能力
        rt1.Send(new ServiceTestEvent());
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(0));

        rt2.Send(new ServiceTestEvent());
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    private class InstanceServiceModule : IService
    {
        private readonly object _instance;
        public InstanceServiceModule(object instance) => _instance = instance;
        public void ConfigureServices(IServiceCollection services)
        {
            if (_instance is IDemoService ds) services.AddSingleton<IDemoService>(ds);
        }
    }
}

public class DemoLayer : Layer { }
public class DemoService : IDemoService { }
public interface IDemoService { }
public struct ServiceTestEvent { }
