using System.Collections.Generic;
using System.Linq;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public partial class ServiceMountContextTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Service_Mount_ILayerContext_Should_Register_And_Inject_Manager()
    {
        var layer = new ServiceMountTestLayer();

        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        Assert.That(layer, Is.InstanceOf<IAutoLayerMount>(), "Layer should implement IAutoLayerMount");
        Assert.That(layer.Service, Is.Not.Null, "layer.Service should not be null after Build");
        Assert.That(layer.Service!.MountedManager, Is.Not.Null);
        Assert.That(layer.Service.MountedManager!.LayerIndex, Is.EqualTo(layer.RouteIndex));
    }

    [Test]
    public void AutoMounted_ILayerContext_Should_Run_Lifecycle()
    {
        var trace = new List<string>();
        LifecycleManager.SetTrace(trace);

        var layer = new LifecycleLayer(trace);

        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        Assert.That(trace, Does.Contain("Init_Manager"));

        layer.Pump(0.016f);

        Assert.That(trace, Does.Contain("Update_Manager"));
        
        LifecycleManager.SetTrace(null!);
    }

    [Test]
    public void Duplicate_Mounted_Manager_Type_Should_Register_Only_Once()
    {
        var trace = new List<string>();
        DuplicateManager.SetTrace(trace);

        var layer = new DuplicateMountLayer(trace);

        LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        var initCount = trace.Count(x => x == "Init_DuplicateManager");

        Assert.That(initCount, Is.EqualTo(1));
        Assert.That(layer.Service!.A, Is.Not.Null);
        Assert.That(layer.Service!.B, Is.Not.Null);
        Assert.That(layer.Service!.A, Is.SameAs(layer.Service!.B));
        
        DuplicateManager.SetTrace(null!);
    }
}

public partial class ServiceMountTestLayer : Layer
{
    [Mount] private ServiceMountTestService _service = null!;

    public ServiceMountTestService? Service => _service;
}

public partial class ServiceMountTestService : IService
{
    [Mount] private ServiceMountTestManager _manager = null!;

    public ServiceMountTestManager? MountedManager => _manager;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

public partial class ServiceMountTestManager : ILayerContext, IInternalLayerContext
{
    public int LayerIndex { get; set; }
}

public partial class LifecycleLayer : Layer
{
    [Mount] private LifecycleService _service = null!;

    public LifecycleLayer(List<string> trace)
    {
        Trace = trace;
    }

    public List<string> Trace { get; }
}

public partial class LifecycleService : IService
{
    [Mount] private LifecycleManager _manager = null!;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

public partial class LifecycleManager : ILayerContext, IInitializable, IUpdate
{
    private static List<string>? s_currentTrace;

    public static void SetTrace(List<string> trace) => s_currentTrace = trace;

    public void Initialize()
    {
        s_currentTrace?.Add("Init_Manager");
    }

    public void Update()
    {
        s_currentTrace?.Add("Update_Manager");
    }
}

public partial class DuplicateMountLayer : Layer
{
    [Mount] private DuplicateMountService _service = null!;
    public DuplicateMountService? Service => _service;

    public DuplicateMountLayer(List<string> trace)
    {
        Trace = trace;
    }

    public List<string> Trace { get; }
}

public partial class DuplicateMountService : IService
{
    [Mount] private DuplicateManager _a = null!;
    [Mount] private DuplicateManager _b = null!;

    public DuplicateManager A => _a;
    public DuplicateManager B => _b;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }
}

public partial class DuplicateManager : ILayerContext, IInitializable
{
    private static List<string>? s_currentTrace;
    public static void SetTrace(List<string> trace) => s_currentTrace = trace;

    public void Initialize()
    {
        s_currentTrace?.Add("Init_DuplicateManager");
    }
}
