using System.Reflection;
using LayerBase.Actor;
using LayerBase.DI;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
public sealed class ScopePublicApiRoutingTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Custom_scope_service_actor_api_uses_owner_scope_remote_accessor()
    {
        using var runtime = new LayerRuntime(10101);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            CreateMainAndCustomPlans(),
            runtime.Id,
            generation: 1);
        var service = new PublicApiRoutingService();

        AttachScopeRuntime(service, runtime, host.Scopes[1]);

        ActorAccessor accessor = service.Actors();

        Assert.That(accessor.IsLocal, Is.False);
        Assert.DoesNotThrow(() => _ = accessor.Remote);
        Assert.Throws<InvalidOperationException>(() => accessor.Local.ToString());
    }

    [Test]
    public void Custom_scope_context_actor_api_uses_owner_scope_remote_accessor()
    {
        using var runtime = new LayerRuntime(10102);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            CreateMainAndCustomPlans(),
            runtime.Id,
            generation: 1);
        var context = new PublicApiRoutingContext();

        AttachScopeRuntime(context, runtime, host.Scopes[1]);

        ActorAccessor accessor = context.Actors();

        Assert.That(accessor.IsLocal, Is.False);
        Assert.DoesNotThrow(() => _ = accessor.Remote);
        Assert.Throws<InvalidOperationException>(() => accessor.Local.ToString());
    }

    [Test]
    public void Scope_ref_public_api_does_not_expose_runtime_or_provider()
    {
        Type scopeRefType = typeof(ScopeRef<PublicApiRoutingScope>);
        Type[] exposedTypes = scopeRefType
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(member => member switch
            {
                PropertyInfo property => property.PropertyType,
                MethodInfo method => method.ReturnType,
                FieldInfo field => field.FieldType,
                _ => null
            })
            .Where(static type => type != null)
            .Cast<Type>()
            .ToArray();

        Assert.That(exposedTypes.Any(static type => type.Name.Contains("ScopeRuntime", StringComparison.Ordinal)), Is.False);
        Assert.That(exposedTypes.Any(static type => type.Name.Contains("ServiceProvider", StringComparison.Ordinal)), Is.False);
        Assert.That(exposedTypes.Any(static type => type.Name.Contains("LocalAccess", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void Layer_runtime_public_api_does_not_expose_local_runtime_resources()
    {
        string[] forbiddenNames =
        {
            "EventCenter",
            "PostScheduler",
            "Scheduler",
            "Timer",
            "EcsWorld",
            "ActorWorld",
            "Actors",
            "ScopeHost",
            "ServiceProvider"
        };

        string[] publicMemberNames = typeof(LayerRuntime)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(static member => member.Name)
            .Distinct()
            .ToArray();

        foreach (string name in forbiddenNames)
            Assert.That(publicMemberNames, Has.No.EqualTo(name));
    }

    [Test]
    public void Post_from_any_thread_api_does_not_exist()
    {
        string[] publicMethodNames = typeof(LayerRuntime)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(publicMethodNames, Has.No.EqualTo("PostFromAnyThread"));
        Assert.That(publicMethodNames, Has.No.EqualTo("TryPostFromAnyThread"));
    }

    private static ScopeExecutionPlan[] CreateMainAndCustomPlans()
    {
        return new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(2, nameof(PublicApiRoutingScope), typeof(PublicApiRoutingScope)),
                ScopeOptions.Inline)
        };
    }

    private static void AttachScopeRuntime(object target, LayerRuntime runtime, ScopeRuntime scope)
    {
        MethodInfo? method = typeof(ServiceLayerBinder).GetMethod(
            "AttachScopeRuntime",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(object), typeof(LayerRuntime), typeof(ScopeRuntime) },
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object[] { target, runtime, scope });
    }

    private readonly struct PublicApiRoutingScope : IScopeDefinition
    {
    }

    private sealed class PublicApiRoutingService : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class PublicApiRoutingContext : IInternalLayerContext
    {
        public int LayerIndex { get; set; } = -1;
    }
}
