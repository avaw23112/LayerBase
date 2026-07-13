using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace LayerBase.Test;

[TestFixture]
public class OwnerServiceTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        OwnerServiceLifecycleContext.Trace = null;
        OwnerServiceDuplicateContext.Trace = null;
        OwnerServiceEventHandler.TotalDamage = 0;
    }

    [Test]
    public void OwnerService_registers_ILayerContext_into_service_scope_and_runs_lifecycle()
    {
        var trace = new List<string>();
        OwnerServiceLifecycleContext.Trace = trace;

        var layer = new OwnerServiceLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        var context = layer.GetService<OwnerServiceLifecycleContext>();

        Assert.That(context, Is.Not.Null);
        Assert.That(trace, Does.Contain("OwnerServiceLifecycleContext.Initialize"));
    }

    [Test]
    public void OwnerService_event_handler_is_auto_subscribed()
    {
        var layer = new OwnerServiceEventLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        layer.Send(new OwnerServiceDamageEvent(3));
        layer.Send(new OwnerServiceDamageEvent(4));

        Assert.That(OwnerServiceEventHandler.TotalDamage, Is.EqualTo(7));
    }

    [Test]
    public void OwnerService_deduplicates_with_matching_explicit_mount()
    {
        var trace = new List<string>();
        OwnerServiceDuplicateContext.Trace = trace;

        var layer = new OwnerServiceDuplicateLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        var resolved = layer.GetService<OwnerServiceDuplicateContext>();

        Assert.That(layer.Service, Is.Not.Null);
        Assert.That(layer.Service!.MountedContext, Is.SameAs(resolved));
        Assert.That(trace.Count(static item => item == "OwnerServiceDuplicateContext.Initialize"), Is.EqualTo(1));
    }
}

public partial class OwnerServiceLayer : Layer
{
    [Mount] private OwnerServiceModule _service = null!;
}

[OwnerLayer(typeof(OwnerServiceLayer))]
public sealed partial class OwnerServiceModule : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

[OwnerService(typeof(OwnerServiceModule))]
public sealed partial class OwnerServiceLifecycleContext : ILayerContext, IInitializable
{
    public static List<string>? Trace { get; set; }

    public void Initialize()
    {
        Trace?.Add("OwnerServiceLifecycleContext.Initialize");
    }
}

public partial class OwnerServiceEventLayer : Layer
{
    [Mount] private OwnerServiceEventModule _service = null!;
}

[OwnerLayer(typeof(OwnerServiceEventLayer))]
public sealed partial class OwnerServiceEventModule : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public readonly struct OwnerServiceDamageEvent
{
    public OwnerServiceDamageEvent(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

[OwnerService(typeof(OwnerServiceEventModule))]
public sealed partial class OwnerServiceEventHandler : ILayerContext
{
    public static int TotalDamage { get; set; }

    [Subscribe]
    public void Deal(in OwnerServiceDamageEvent @event)
    {
        TotalDamage += @event.Value;
    }
}

public partial class OwnerServiceDuplicateLayer : Layer
{
    [Mount] private OwnerServiceDuplicateModule _service = null!;

    public OwnerServiceDuplicateModule? Service => _service;
}

[OwnerLayer(typeof(OwnerServiceDuplicateLayer))]
public sealed partial class OwnerServiceDuplicateModule : IService
{
    [Mount] private OwnerServiceDuplicateContext _context = null!;

    public OwnerServiceDuplicateContext MountedContext => _context;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

[OwnerService(typeof(OwnerServiceDuplicateModule))]
public sealed partial class OwnerServiceDuplicateContext : ILayerContext, IInitializable
{
    public static List<string>? Trace { get; set; }

    public void Initialize()
    {
        Trace?.Add("OwnerServiceDuplicateContext.Initialize");
    }
}
