using LayerBase;
using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public partial class DiMultiWorldTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    public interface ICounter
    {
        int Value { get; set; }
    }

    public sealed class Counter : ICounter
    {
        public int Value { get; set; }
    }

    public partial class CounterModule : IService
    {
        private readonly ServiceLifetime _lifetime;

        public CounterModule(ServiceLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            switch (_lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<ICounter, Counter>();
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<ICounter, Counter>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<ICounter, Counter>();
                    break;
            }
        }
    }

    public class TestLayer : Layer
    {
    }

    [Test]
    public void Singleton_IsNotShared_Across_Runtimes()
    {
        // World A
        var layerA = new TestLayer();
        layerA.RegisterService(new CounterModule(ServiceLifetime.Singleton));
        var worldA = LayerHub.CreateLayers()
            .Push(layerA)
            .Build();

        // World B
        var layerB = new TestLayer();
        layerB.RegisterService(new CounterModule(ServiceLifetime.Singleton));
        var worldB = LayerHub.CreateLayers()
            .Push(layerB)
            .Build();

        var counterA = layerA.GetService<ICounter>();
        var counterB = layerB.GetService<ICounter>();

        counterA.Value = 100;

        Assert.That(counterB.Value, Is.EqualTo(0));
        Assert.That(ReferenceEquals(counterA, counterB), Is.False);
    }

    public class LayerA : Layer { }
    public class LayerB : Layer { }

    [Test]
    public void Singleton_IsShared_Between_Layers_In_Same_Runtime()
    {
        var layerA = new LayerA();
        var layerB = new LayerB();
        var module = new CounterModule(ServiceLifetime.Singleton);
        layerA.RegisterService(module);
        layerB.RegisterService(module);

        var runtime = LayerHub.CreateLayers()
            .Push(layerA)
            .Push(layerB)
            .Build();

        var counterA = layerA.GetService<ICounter>();
        var counterB = layerB.GetService<ICounter>();

        Assert.That(ReferenceEquals(counterA, counterB), Is.True);
    }

    [Test]
    public void Scoped_IsNotShared_Between_Layers_In_Same_Runtime()
    {
        var layerA = new LayerA();
        var layerB = new LayerB();
        var module = new CounterModule(ServiceLifetime.Scoped);
        layerA.RegisterService(module);
        layerB.RegisterService(module);

        var runtime = LayerHub.CreateLayers()
            .Push(layerA)
            .Push(layerB)
            .Build();

        var counterA = layerA.GetService<ICounter>();
        var counterB = layerB.GetService<ICounter>();

        Assert.That(ReferenceEquals(counterA, counterB), Is.False);
    }

    [Test]
    public void Transient_Always_Creates_New_Instance()
    {
        var layer = new LayerA();
        layer.RegisterService(new CounterModule(ServiceLifetime.Transient));
        var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var counter1 = layer.GetService<ICounter>();
        var counter2 = layer.GetService<ICounter>();

        Assert.That(ReferenceEquals(counter1, counter2), Is.False);
    }

    public partial class ManualInstanceModule : IService
    {
        private readonly ICounter _instance;
        public ManualInstanceModule(ICounter instance) => _instance = instance;
        public void ConfigureServices(IServiceCollection services) => services.AddSingleton(_instance);
    }

    [Test]
    public void Manual_Instance_IsShared_If_User_Explicitly_Registers_Same_Instance()
    {
        var sharedCounter = new Counter();

        var layerA = new TestLayer();
        layerA.RegisterService(new ManualInstanceModule(sharedCounter));
        var worldA = LayerHub.CreateLayers()
            .Push(layerA)
            .Build();

        var layerB = new TestLayer();
        layerB.RegisterService(new ManualInstanceModule(sharedCounter));
        var builderB = LayerHub.CreateLayers().Push(layerB);

        // 🚀 关键：跨 Runtime 复用同一个 Singleton / Instance 实例时必须抛异常
        Assert.Throws<InvalidOperationException>(() => builderB.Build());
    }

    [Test]
    public void RegisterService_After_Runtime_Build_IsRejected()
    {
        var layer = new TestLayer();
        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => layer.RegisterService(new CounterModule(ServiceLifetime.Singleton)));

        Assert.That(ex!.Message, Does.Contain("before the layer is built"));
    }
}
