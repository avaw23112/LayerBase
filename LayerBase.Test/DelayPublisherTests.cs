using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class DelayPublisherTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    private class DelayTestLayer : Layer
    {
        public void AddManager(DelayTestService service)
        {
            // 手动注册到配置中
            RegisterService(service);
        }
    }

    private class DelayTestService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 🚀 关键：Service 必须注册自己或被注册，才能在 Build 后被获取
            services.AddSingleton<DelayTestService>(this);
        }

        public void RequestDelay(float ttl, int value, int contractId = 0)
        {
            this.DelayLocal(new DelayTestEvent { Value = value }, ttl, contractId);
        }

        public void RequestGlobalDelay(float ttl, int value)
        {
            this.DelayGlobal(new DelayTestEvent { Value = value }, ttl);
        }
    }

    [Test]
    public void DelayLocal_is_stored_and_can_be_retrieved_via_service()
    {
        var layer = new DelayTestLayer();
        var manager = new DelayTestService();
        layer.AddManager(manager);

        LayerHub.CreateLayers().Push(layer).Build();

        var retrievedManager = layer.GetService<DelayTestService>();
        retrievedManager.RequestDelay(1.0f, 42);

        var publisher = layer.SubscribeDelay<DelayTestEvent>();
        Assert.That(publisher.HasValue, Is.True);
        Assert.That(publisher.TryGet(out var retrieved), Is.True);
        Assert.That(retrieved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Delay_expires_after_ttl_and_is_dropped()
    {
        var layer = new DelayTestLayer();
        var manager = new DelayTestService();
        layer.AddManager(manager);
        LayerHub.CreateLayers().Push(layer).Build();

        var retrievedManager = layer.GetService<DelayTestService>();
        retrievedManager.RequestDelay(0.05f, 10);
        var publisher = layer.SubscribeDelay<DelayTestEvent>();

        Assert.That(publisher.HasValue, Is.True);
        layer.Pump(0.1f);
        Assert.That(publisher.HasValue, Is.False);
    }

    [Test]
    public void TryTake_consumes_the_value()
    {
        var layer = new DelayTestLayer();
        var manager = new DelayTestService();
        layer.AddManager(manager);
        LayerHub.CreateLayers().Push(layer).Build();

        var retrievedManager = layer.GetService<DelayTestService>();
        retrievedManager.RequestGlobalDelay(1.0f, 100);
        var publisher = layer.SubscribeDelay<DelayTestEvent>();

        Assert.That(publisher.TryTake(out var val), Is.True);
        Assert.That(val.Value, Is.EqualTo(100));
        Assert.That(publisher.HasValue, Is.False);
    }

    [Test]
    public void ContractId_is_preserved()
    {
        var layer = new DelayTestLayer();
        var manager = new DelayTestService();
        layer.AddManager(manager);
        LayerHub.CreateLayers().Push(layer).Build();

        var retrievedManager = layer.GetService<DelayTestService>();
        retrievedManager.RequestDelay(1.0f, 1, 888);
        var publisher = layer.SubscribeDelay<DelayTestEvent>();

        Assert.That(publisher.ContractId, Is.EqualTo(888));
    }

    public struct DelayTestEvent
    {
        public int Value;
    }
}