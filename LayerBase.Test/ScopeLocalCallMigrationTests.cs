using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeLocalCallMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public async Task Local_call_invokes_handler_in_current_scope_without_call_inbox()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeLocalCallerLayer())
                              .Push(new ScopeLocalCallMigrationHandlerLayer())
                              .Build();

        var before = runtime.Main.Address;
        var response = await LayerHub.CallAsync<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
            new ScopeLocalCallMigrationRequest("local"));

        Assert.That(response.Value, Is.EqualTo("handled:local"));
        Assert.That(runtime.Main.Address, Is.EqualTo(before));
        Assert.That(runtime.ScopeHost.MainScope.Transport.CallInbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Duplicate_handler_in_two_layers_same_scope_fails_build()
    {
        var builder = LayerHub.CreateLayers()
                              .Push(new ScopeLocalCallMigrationHandlerLayer())
                              .Push(new DuplicateScopeLocalCallMigrationHandlerLayer());

        Assert.That(() => builder.Build(),
            Throws.TypeOf<ScopeLocalCallRouteConflictException>()
                  .With.Message.Contains(nameof(ScopeLocalCallMigrationRequest))
                  .And.Message.Contains(nameof(ScopeLocalCallMigrationHandlerLayer))
                  .And.Message.Contains(nameof(DuplicateScopeLocalCallMigrationHandlerLayer)));
    }

    [Test]
    public void Missing_local_handler_does_not_fallback_remote()
    {
        LayerHub.CreateLayers()
                .Push(new ScopeLocalCallerLayer())
                .Build();

        Assert.That(async () =>
                await LayerHub.CallAsync<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                    new ScopeLocalCallMigrationRequest("missing")),
            Throws.TypeOf<ScopeLocalCallRouteNotFoundException>());
    }

    [Test]
    public void Local_call_never_searches_other_scope_registry()
    {
        using var runtime = new LayerRuntime(9301);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(SecondaryScope.ScopeId, nameof(SecondaryScope), typeof(SecondaryScope)),
                    ScopeOptions.Inline)
            },
            runtimeId: 9301,
            generation: 1);

        host.Scopes.Single(scope => scope.ScopeId == SecondaryScope.ScopeId)
            .LocalCalls.Register(new ScopeLocalCallRouteEntry(
                SecondaryScope.ScopeId,
                ScopeLocalCallRouteId<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>.Id,
                typeof(ScopeLocalCallMigrationRequest),
                typeof(ScopeLocalCallMigrationResponse),
                typeof(ScopeLocalCallMigrationHandler),
                typeof(ScopeLocalCallMigrationHandlerLayer),
                new ScopeLocalCallInvoker<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                    static (request, _) => LBTask<ScopeLocalCallMigrationResponse>.FromResult(
                        new ScopeLocalCallMigrationResponse("secondary:" + request.Value))),
                new ScopeLocalCallDispatcher<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                    static (request, _) => LBTask<ScopeLocalCallMigrationResponse>.FromResult(
                        new ScopeLocalCallMigrationResponse("secondary:" + request.Value)))));

        Assert.That(async () =>
                await host.MainScope.CallLocalAsync<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                    new ScopeLocalCallMigrationRequest("main")),
            Throws.TypeOf<ScopeLocalCallRouteNotFoundException>());
    }

    [Test]
    public void Same_request_response_in_different_scopes_is_allowed()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeLocalCallMigrationHandlerLayer())
                              .AddAssemblyModule(new SameRequestDifferentScopeModule())
                              .Build();

        var matchingCalls = runtime.CompositionPlan.LocalCalls
            .Where(static call =>
                call.RequestType == typeof(ScopeLocalCallMigrationRequest) &&
                call.ResponseType == typeof(ScopeLocalCallMigrationResponse))
            .Select(static call => call.OwnerScopeId)
            .OrderBy(static scopeId => scopeId)
            .ToArray();

        Assert.That(matchingCalls, Is.EqualTo(new[] { MainScope.ScopeId, SecondaryScope.ScopeId }));
    }

    [Test]
    public void Wrong_thread_local_call_fails()
    {
        LayerHub.CreateLayers()
                .Push(new ScopeLocalCallerLayer())
                .Push(new ScopeLocalCallMigrationHandlerLayer())
                .Build();

        Assert.That(async () =>
                await Task.Run(async () =>
                    await LayerHub.CallAsync<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                        new ScopeLocalCallMigrationRequest("wrong-thread"))),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task Scope_stop_rejects_new_local_call()
    {
        var runtime = LayerHub.CreateLayers()
                              .Push(new ScopeLocalCallerLayer())
                              .Push(new ScopeLocalCallMigrationHandlerLayer())
                              .Build();

        var stopTask = runtime.ScopeHost.MainScope.RequestStopAsync();
        runtime.ScopeHost.MainScope.PumpIngress();
        await stopTask;

        Assert.That(async () =>
                await runtime.CallAsync<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>(
                    new ScopeLocalCallMigrationRequest("stopped")),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Old_layer_call_public_contracts_do_not_exist()
    {
        var assembly = typeof(LayerRuntime).Assembly;

        Assert.That(assembly.GetType("LayerBase.Call.ILayerCallHandler"), Is.Null);
        Assert.That(assembly.GetType("LayerBase.Call.LayerCallRouteId`2"), Is.Null);
        Assert.That(assembly.GetType("LayerBase.Call.LayerCallRouteRegistry"), Is.Null);

        var legacyLayerCallCacheMethods = typeof(LayerHub)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(static method => method.Name.Contains("LayerCall", StringComparison.Ordinal))
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(legacyLayerCallCacheMethods, Is.Empty);
        Assert.That(typeof(Layer).GetMethod("GetCallInvoker", BindingFlags.NonPublic | BindingFlags.Instance), Is.Null);
        Assert.That(typeof(Layer).GetMethod("CallAsync", BindingFlags.NonPublic | BindingFlags.Instance), Is.Null);
    }

    private sealed class ScopeLocalCallerLayer : Layer
    {
    }

    public readonly struct SecondaryScope : IScopeDefinition
    {
        public const int ScopeId = 20;
    }

    private sealed class SameRequestDifferentScopeModule : IAssemblyModule
    {
        private static readonly AssemblyModuleId s_id = new("scope-local-call-test");

        public SameRequestDifferentScopeModule()
        {
            Manifest = new AssemblyModuleManifest(
                s_id,
                Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                new[]
                {
                    LocalCallContribution.ForTypes(
                        typeof(ScopeLocalCallMigrationRequest),
                        typeof(ScopeLocalCallMigrationResponse),
                        typeof(ScopeLocalCallMigrationHandler),
                        typeof(ScopeLocalCallMigrationHandlerLayer),
                        typeof(MainScope)),
                    LocalCallContribution.ForTypes(
                        typeof(ScopeLocalCallMigrationRequest),
                        typeof(ScopeLocalCallMigrationResponse),
                        typeof(SecondaryScopeLocalCallMigrationHandler),
                        typeof(ScopeLocalCallMigrationHandlerLayer),
                        typeof(SecondaryScope))
                },
                Array.Empty<EventHandlerContribution>(),
                Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id => s_id;

        public AssemblyModuleManifest Manifest { get; }
    }
}

public readonly struct ScopeLocalCallMigrationRequest
{
    public ScopeLocalCallMigrationRequest(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

public readonly struct ScopeLocalCallMigrationResponse
{
    public ScopeLocalCallMigrationResponse(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

public partial class ScopeLocalCallMigrationHandlerLayer : Layer
{
}

public partial class DuplicateScopeLocalCallMigrationHandlerLayer : Layer
{
}

[OwnerLayer(typeof(ScopeLocalCallMigrationHandlerLayer))]
public sealed class ScopeLocalCallMigrationHandler
    : IScopeLocalCallHandler<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>
{
    public async LBTask<ScopeLocalCallMigrationResponse> HandleAsync(
        ScopeLocalCallMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await LBTask.CompletedTask;
        return new ScopeLocalCallMigrationResponse("handled:" + request.Value);
    }
}

[OwnerLayer(typeof(DuplicateScopeLocalCallMigrationHandlerLayer))]
public sealed class DuplicateScopeLocalCallMigrationHandler
    : IScopeLocalCallHandler<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>
{
    public async LBTask<ScopeLocalCallMigrationResponse> HandleAsync(
        ScopeLocalCallMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await LBTask.CompletedTask;
        return new ScopeLocalCallMigrationResponse("duplicate:" + request.Value);
    }
}

public sealed class SecondaryScopeLocalCallMigrationHandler
    : IScopeLocalCallHandler<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>
{
    public async LBTask<ScopeLocalCallMigrationResponse> HandleAsync(
        ScopeLocalCallMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await LBTask.CompletedTask;
        return new ScopeLocalCallMigrationResponse("secondary:" + request.Value);
    }
}
