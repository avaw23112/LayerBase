using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;

namespace EventsTest;

public partial class AutoLayerMountTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        AutoMountLayer.AutoMountedService.Created = 0;
    }

    [Test]
    public void Mount_on_layer_generates_auto_layer_mount_and_registers_service()
    {
        var layer = new AutoMountLayer();

        Assert.That(layer, Is.InstanceOf<IAutoLayerMount>());

        LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<AutoMountLayer.AutoMountedService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(layer.Service, Is.SameAs(service), "Field _service should be injected");
        Assert.That(AutoMountLayer.AutoMountedService.Created, Is.EqualTo(1));
        Assert.That(service.ResolveSelfThroughBinding(), Is.SameAs(service));
    }

    [Test]
    public void OwnerLayer_service_uses_auto_layer_mount_workflow()
    {
        var layer = new AutoMountLayer();

        Assert.That(layer, Is.InstanceOf<IAutoLayerMount>());

        LayerHub.CreateLayers().Push(layer).Build();

        var service = layer.GetService<AutoMountLayer.AutoMountedService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(AutoMountLayer.AutoMountedService.Created, Is.EqualTo(1));
    }
}

public partial class AutoMountLayer : Layer
{
    [Mount] private AutoMountedService _service = default!;
    public AutoMountedService Service => _service;

    public partial class OwnerAutoMountLayer : Layer
    {
    }
    
    [OwnerLayer(typeof(AutoMountLayer))]
    public partial class AutoMountedService : IService
    {
        public static int Created;

        public AutoMountedService()
        {
            Created++;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this);
        }

        public AutoMountedService ResolveSelfThroughBinding()
        {
            return this.GetService<AutoMountedService>();
        }
    }

 
}
