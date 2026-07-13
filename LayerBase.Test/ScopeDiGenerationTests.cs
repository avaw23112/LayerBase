using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopeDiGenerationTests
{
    [Test]
    public void Scope_planner_keeps_main_and_scoped_services_in_separate_runtime_boundaries()
    {
        var main = new DiBoundaryMainService();
        var scoped = new DiBoundaryScopedService();

        IReadOnlyList<ScopeRuntimePlan> plans = ScopeRuntimePlanner.Build(new IService[] { main, scoped });
        using var host = ScopeRuntimeHost.Create(plans);

        Assert.That(host.Scopes, Has.Count.EqualTo(2));
        Assert.That(host.Scopes[0].Services, Is.EqualTo(new IService[] { main }));
        Assert.That(host.Scopes[1].Services, Is.EqualTo(new IService[] { scoped }));
        Assert.That(scoped.__ScopeObjectBinding, Is.Not.Null);
        Assert.That(scoped.__ScopeObjectBinding!.Scope, Is.SameAs(host.Scopes[1]));
        Assert.That(scoped.__ScopeObjectBinding.ServiceSlot, Is.EqualTo(0));

        host.Stop();

        Assert.That(scoped.__ScopeObjectBinding, Is.Null);
    }

    [Test]
    public void Scope_mount_members_are_bound_by_generated_mount_contract()
    {
        var service = new ScopeDiGeneratedMountService();
        var context = new ScopeDiGeneratedMountContext();
        using var runtime = new ScopeRuntime(ScopeDescriptors.Main, new IService[] { service });

        runtime.SetContexts(new ILayerContext[] { context });

        Assert.That(service.Context, Is.SameAs(context));
        Assert.That(context.Service, Is.SameAs(service));
    }

    [Test]
    public void Scope_service_provider_does_not_use_reflection_member_injection()
    {
        string root = FindRepositoryRoot();
        string providerSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeServiceProvider.cs"));
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeRuntime.cs"));

        Assert.That(providerSource, Does.Not.Contain("InjectMembers"));
        Assert.That(providerSource, Does.Not.Contain("GetFields"));
        Assert.That(providerSource, Does.Not.Contain("GetProperties"));
        Assert.That(providerSource, Does.Not.Contain("GetCustomAttribute"));
        Assert.That(providerSource, Does.Not.Contain("SetValue"));
        Assert.That(runtimeSource, Does.Not.Contain(".InjectMembers("));
        Assert.That(runtimeSource, Does.Contain("IGeneratedScopeMount"));
    }

    [Test]
    public void Scope_runtime_does_not_reflectively_bind_interface_event_handlers()
    {
        string root = FindRepositoryRoot();
        string runtimeSource = File.ReadAllText(Path.Combine(root, "LayerBase", "Scope", "ScopeRuntime.cs"));

        Assert.That(runtimeSource, Does.Not.Contain("BindInterfaceEventHandlers"));
        Assert.That(runtimeSource, Does.Not.Contain("GetInterfaces()"));
        Assert.That(runtimeSource, Does.Not.Contain("GetGenericTypeDefinition"));
        Assert.That(runtimeSource, Does.Contain("BindGeneratedSubscriptions"));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LayerBase.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    [ScopeOptions(
        threading: ScopeThreadingMode.Inline,
        clock: ScopeClockMode.EngineDriven,
        tickRateHz: 0,
        stopPolicy: ScopeStopPolicy.Drain)]
    private sealed class DiBoundaryScope
    {
    }

    private sealed class DiBoundaryMainService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    [Scope<DiBoundaryScope>]
    private sealed class DiBoundaryScopedService : IService, IScopeObjectBindingAccessor
    {
        public ScopeObjectBinding? __ScopeObjectBinding { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

}

internal sealed partial class ScopeDiGeneratedMountService : IService
{
    [Mount] private ScopeDiGeneratedMountContext? _context;

    public ScopeDiGeneratedMountContext? Context => _context;

    public void ConfigureServices(IServiceCollection services)
    {
    }
}

internal sealed partial class ScopeDiGeneratedMountContext : ILayerContext
{
    [Mount] private ScopeDiGeneratedMountService? _service;

    public ScopeDiGeneratedMountService? Service => _service;
}
