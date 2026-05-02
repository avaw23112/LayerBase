using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public partial class DelayPublisherTests
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

    private partial class DelayTestService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 🚀 关键：Service 必须注册自己或被注册，才能在 Build 后被获取。
            // 使用 Scoped 确保服务绑定到 Layer，从而可以使用 Layer-only API (Delay)。
            services.AddScoped<DelayTestService>(_ => this);
        }

        public void RequestDelay(float ttl, int value, int contractId = 0)
        {
            this.Delay(new DelayTestEvent { Value = value }, ttl, contractId);
        }

        public void RequestGlobalDelay(float ttl, int value)
        {
            this.Delay(new DelayTestEvent { Value = value }, ttl);
        }

        public void RequestBigDelay(float ttl, int value)
        {
            this.Delay(new BigDelayEvent(value), ttl);
        }
    }

    [Test]
    public void Delay_is_stored_and_can_be_retrieved_via_service()
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
        LayerHub.Pump(0.1f);
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

    [Test]
    public void Concurrent_publish_and_read_observes_whole_value_snapshots()
    {
        var layer = new DelayTestLayer();
        var manager = new DelayTestService();
        layer.AddManager(manager);
        LayerHub.CreateLayers().Push(layer).Build();

        var retrievedManager = layer.GetService<DelayTestService>();
        var publisher = layer.SubscribeDelay<BigDelayEvent>();
        Exception? readerError = null;
        var run = true;

        var reader = Task.Run(() =>
        {
            try
            {
                while (Volatile.Read(ref run))
                {
                    if (!publisher.TryGet(out var value)) continue;
                    Assert.That(value.B, Is.EqualTo(value.A));
                    Assert.That(value.C, Is.EqualTo(value.A));
                    Assert.That(value.D, Is.EqualTo(value.A));
                }
            }
            catch (Exception ex)
            {
                readerError = ex;
            }
        });

        for (var i = 1; i <= 2048; i++) retrievedManager.RequestBigDelay(10.0f, i);

        Volatile.Write(ref run, false);
        reader.Wait();
        Assert.That(readerError, Is.Null);
    }

    public struct DelayTestEvent
    {
        public int Value;
    }

    public struct BigDelayEvent
    {
        public int A;
        public int B;
        public int C;
        public int D;

        public BigDelayEvent(int value)
        {
            A = value;
            B = value;
            C = value;
            D = value;
        }
    }
}
