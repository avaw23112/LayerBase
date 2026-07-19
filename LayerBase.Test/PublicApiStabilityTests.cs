using System.Reflection;
using LayerBase;
using LayerBase.Scope;
using LayerBase.Worker;

namespace EventsTest;

[TestFixture]
public sealed class PublicApiStabilityTests
{
    [Test]
    public void Worker_job_public_api_remains_a_simple_accessor()
    {
        MethodInfo[] publicMethods = typeof(WorkerJobAccessor)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);

        MethodInfo? run = publicMethods.SingleOrDefault(
            static method => method.Name == nameof(WorkerJobAccessor.Run));

        Assert.That(run, Is.Not.Null);
        Assert.That(run!.IsGenericMethodDefinition, Is.True);
        Assert.That(run.GetGenericArguments(), Has.Length.EqualTo(3));

        string[] forbiddenParameterTypes =
        {
            "ScopeRuntime",
            "ScopeEndpoint",
            "ScopeTransport",
            "ScopeEventEnvelope",
            "ScopeCallEnvelope",
            "PayloadHandle"
        };

        foreach (ParameterInfo parameter in run.GetParameters())
        {
            Assert.That(
                forbiddenParameterTypes,
                Does.Not.Contain(parameter.ParameterType.Name),
                $"Public WorkerJobs.Run leaked internal type {parameter.ParameterType}.");
        }
    }

    [Test]
    public void Internal_concurrency_protocol_does_not_become_public()
    {
        Assembly assembly = typeof(LayerRuntime).Assembly;

        string[] internalTypeNames =
        {
            "WorkerJobCoordinator",
            "WorkerExecutionCompletedScopeEvent",
            "WorkerCancelRequestedScopeEvent",
            "ShutdownDeadline"
        };

        foreach (string typeName in internalTypeNames)
        {
            Type? type = assembly
                .GetTypes()
                .SingleOrDefault(candidate => candidate.Name == typeName);

            if (type != null)
            {
                Assert.That(
                    type.IsPublic || type.IsNestedPublic,
                    Is.False,
                    $"{typeName} must stay internal.");
            }
        }
    }

    [Test]
    public void Scope_ref_does_not_expose_transport_protocol()
    {
        Type scopeRefType = typeof(ScopeRef<MainScope>);

        string[] forbiddenNames =
        {
            "Transport",
            "Endpoint",
            "EnqueueControlCall",
            "EnqueueEventEnvelope",
            "EnqueueCallEnvelope"
        };

        string[] publicNames = scopeRefType
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(static member => member.Name)
            .ToArray();

        foreach (string forbidden in forbiddenNames)
            Assert.That(publicNames, Does.Not.Contain(forbidden));
    }

    [Test]
    public void FullSnap_public_api_is_runtime_capture_builder_restore_only()
    {
        Type runtimeType = typeof(LayerRuntime);

        Assert.That(runtimeType.GetProperty("FullSnap", BindingFlags.Public | BindingFlags.Instance), Is.Null);
        string[] methodNames = runtimeType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(methodNames, Does.Contain("SerializeFullSnapAsync"));
        Assert.That(methodNames, Does.Contain("SerializeFullSnapJsonAsync"));
        Assert.That(methodNames, Does.Not.Contain("DeserializeFullSnapAsync"));
        Assert.That(methodNames, Does.Not.Contain("DeserializeFullSnapJsonAsync"));

        Type builderType = typeof(LayerRuntime).GetNestedType("LayersBuilder", BindingFlags.Public | BindingFlags.NonPublic)!;
        string[] builderMethodNames = builderType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(builderMethodNames, Does.Contain("RestoreFrom"));
    }

    [Test]
    public void FullSnap_runtime_contract_does_not_become_public()
    {
        Assembly assembly = typeof(LayerRuntime).Assembly;
        Type? contract = assembly.GetTypes().SingleOrDefault(static type => type.Name == "IFullSnapRuntime");

        Assert.That(contract == null || !(contract.IsPublic || contract.IsNestedPublic), Is.True);
    }
}
