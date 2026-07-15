using System.Reflection;
using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Layers;
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
    }

    private sealed class ScopeLocalCallerLayer : Layer
    {
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
    public LBTask<ScopeLocalCallMigrationResponse> HandleAsync(
        ScopeLocalCallMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask<ScopeLocalCallMigrationResponse>.FromResult(
            new ScopeLocalCallMigrationResponse("handled:" + request.Value));
    }
}

[OwnerLayer(typeof(DuplicateScopeLocalCallMigrationHandlerLayer))]
public sealed class DuplicateScopeLocalCallMigrationHandler
    : IScopeLocalCallHandler<ScopeLocalCallMigrationRequest, ScopeLocalCallMigrationResponse>
{
    public LBTask<ScopeLocalCallMigrationResponse> HandleAsync(
        ScopeLocalCallMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        return LBTask<ScopeLocalCallMigrationResponse>.FromResult(
            new ScopeLocalCallMigrationResponse("duplicate:" + request.Value));
    }
}
