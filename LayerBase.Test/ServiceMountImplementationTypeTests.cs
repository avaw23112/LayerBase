using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using NUnit.Framework;

namespace LayerBase.Test;

[TestFixture]
public partial class ServiceMountImplementationTypeTests
{
    private static List<string>? s_lifecycleTrace;
    private static List<string>? s_duplicateTrace;

    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        s_lifecycleTrace = null;
        s_duplicateTrace = null;
    }

    [Test]
    public void Mount_With_ImplementationType_Should_Register_And_Inject_Interface_Field()
    {
        var layer = new TestLayerImplType();

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        Assert.That(layer.Service, Is.Not.Null);
        Assert.That(layer.Service!.DamageManager, Is.Not.Null);
        Assert.That(layer.Service.DamageManager, Is.TypeOf<DamageManagerImplType>());
    }

    [Test]
    public void Mount_With_ImplementationType_Should_Register_And_Inject_Abstract_Field()
    {
        var layer = new AbstractMountLayerImplType();

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        Assert.That(layer.Service, Is.Not.Null);
        Assert.That(layer.Service!.Manager, Is.Not.Null);
        Assert.That(layer.Service.Manager, Is.TypeOf<AbstractDamageManagerImplType>());
    }

    [Test]
    public void Mount_With_ImplementationType_Should_Run_Lifecycle()
    {
        s_lifecycleTrace = new List<string>();
        var layer = new LifecycleMountLayerImplType();

        LayerHub.CreateLayers()
                .Push(layer)
                .Build();

        Assert.That(s_lifecycleTrace, Does.Contain("Init_DamageManager"));

        LayerHub.Pump(0.016f);

        Assert.That(s_lifecycleTrace, Does.Contain("Update_DamageManager"));
    }

    [Test]
    public void MountAttribute_Should_Be_Visible_Via_Reflection()
    {
        var type = typeof(ConcreteMountLayerImplType);
        var field = type.GetField("_service",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.That(field, Is.Not.Null);
        var attr = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<MountAttribute>(field!);
        Assert.That(attr, Is.Not.Null, "MountAttribute should be present on the field");
    }
}

public partial class ConcreteMountLayerImplType : Layer
{
    [Mount] public ConcreteMountServiceImplType _service = null!;

    public ConcreteMountServiceImplType? Service => _service;
}

public partial class ConcreteMountServiceImplType : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public partial class TestLayerImplType : Layer
{
    [Mount] public CombatServiceImplType _service = null!;

    public CombatServiceImplType? Service => _service;
}

public partial class CombatServiceImplType : IService
{
    [Mount(typeof(DamageManagerImplType))] public IDamageManagerImplType _damageManager = null!;

    public IDamageManagerImplType? DamageManager => _damageManager;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public interface IDamageManagerImplType
{
    void ApplyDamage(int targetId, int amount);
}

public sealed partial class DamageManagerImplType : IDamageManagerImplType, ILayerContext
{
    public int LayerIndex { get; set; }

    public void ApplyDamage(int targetId, int amount)
    {
    }
}

public partial class AbstractMountLayerImplType : Layer
{
    [Mount] private AbstractMountServiceImplType _service = null!;

    public AbstractMountServiceImplType? Service => _service;
}

public partial class AbstractMountServiceImplType : IService
{
    [Mount(typeof(AbstractDamageManagerImplType))]
    private DamageManagerBaseImplType _manager = null!;

    public DamageManagerBaseImplType? Manager => _manager;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public abstract partial class DamageManagerBaseImplType : ILayerContext
{
    public int LayerIndex { get; set; }

    public abstract void ApplyDamage(int targetId, int amount);
}

public sealed partial class AbstractDamageManagerImplType : DamageManagerBaseImplType
{
    public override void ApplyDamage(int targetId, int amount)
    {
    }
}

public partial class LifecycleMountLayerImplType : Layer
{
    [Mount] private LifecycleCombatServiceImplType _service = null!;
}

public partial class LifecycleCombatServiceImplType : IService
{
    [Mount(typeof(LifecycleDamageManagerImplType))]
    private ILifecycleDamageManagerImplType _manager = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public interface ILifecycleDamageManagerImplType
{
}

public sealed partial class LifecycleDamageManagerImplType :
    ILifecycleDamageManagerImplType,
    ILayerContext,
    IInitializable,
    IUpdate
{
    public int LayerIndex { get; set; }

    private static List<string>? Trace => ServiceMountImplementationTypeTests.GetLifecycleTrace();

    public void Initialize()
    {
        Trace?.Add("Init_DamageManager");
    }

    public void Update()
    {
        Trace?.Add("Update_DamageManager");
    }
}

public partial class DuplicateMountLayerImplType : Layer
{
    [Mount] private DuplicateMountServiceImplType _service = null!;
}

public partial class DuplicateMountServiceImplType : IService
{
    [Mount(typeof(DuplicateDamageManagerImplType))]
    private IDuplicateDamageManagerImplType _a = null!;

    [Mount(typeof(DuplicateDamageManagerImplType))]
    private IDuplicateDamageManagerImplType _b = null!;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

public interface IDuplicateDamageManagerImplType
{
}

public sealed partial class DuplicateDamageManagerImplType :
    IDuplicateDamageManagerImplType,
    ILayerContext,
    IInitializable
{
    public int LayerIndex { get; set; }

    private static List<string>? Trace => ServiceMountImplementationTypeTests.GetDuplicateTrace();

    public void Initialize()
    {
        Trace?.Add("Init_DuplicateDamageManager");
    }
}

// Helper to access private static fields for tests
public partial class ServiceMountImplementationTypeTests
{
    internal static List<string>? GetLifecycleTrace() => s_lifecycleTrace;
    internal static List<string>? GetDuplicateTrace() => s_duplicateTrace;
}