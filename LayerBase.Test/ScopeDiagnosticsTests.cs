using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeDiagnosticsTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Master_layer_event_info_callback_remains_valid()
    {
        LayerEventInfo? captured = null;
        using var runtime = LayerHub.CreateLayers()
                                    .Push(new DiagnosticsLayer())
                                    .Build();
        runtime.OnLayerEventInfo += info => captured = info;

        runtime.ReportWarning(-1, "Diagnostics", "Probe", "still valid");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.Source, Is.EqualTo("Diagnostics"));
        Assert.That(captured.Value.EventName, Is.EqualTo("Probe"));
        Assert.That(captured.Value.ScopeId, Is.EqualTo(-1));
    }

    [Test]
    public void Capture_diagnostics_reports_runtime_and_scope_snapshots()
    {
        using var runtime = BuildRuntimeWithSecondaryScope();

        var snapshot = runtime.CaptureDiagnostics();

        Assert.That(snapshot.RuntimeId, Is.EqualTo(runtime.Id));
        Assert.That(snapshot.RuntimeGeneration, Is.EqualTo(runtime.Generation));
        Assert.That(snapshot.State, Is.EqualTo(RuntimeState.Running));
        Assert.That(snapshot.Scopes.Select(static scope => scope.ScopeId),
            Is.EqualTo(new[] { ScopeDefinitionIds.Main, DiagnosticsScope.ScopeId }));
        Assert.That(snapshot.Scopes.All(static scope => scope.ScopeName.Length > 0), Is.True);
        Assert.That(snapshot.Scopes.All(static scope => scope.EventInboxCapacity > 0), Is.True);
        Assert.That(snapshot.Scopes.All(static scope => scope.CallInboxCapacity > 0), Is.True);
        Assert.That(snapshot.Scopes.Single(static scope => scope.ScopeId == ScopeDefinitionIds.Main).Tools.RegisteredCount,
            Is.EqualTo(1));
    }

    [Test]
    public async Task Capture_diagnostics_async_matches_sync_shape()
    {
        using var runtime = BuildRuntimeWithSecondaryScope();

        var snapshot = await runtime.CaptureDiagnosticsAsync();

        Assert.That(snapshot.Scopes.Select(static scope => scope.ScopeId),
            Is.EqualTo(new[] { ScopeDefinitionIds.Main, DiagnosticsScope.ScopeId }));
    }

    [Test]
    public void Capture_diagnostics_rejects_sync_when_worker_scope_exists()
    {
        using var runtime = BuildRuntimeWithWorkerScope();

        Assert.That(
            () => runtime.CaptureDiagnostics(),
            Throws.InvalidOperationException.With.Message.Contains("WorkerScope"));
    }

    [Test]
    public async Task Worker_snapshot_runs_on_worker_owner_thread()
    {
        using var runtime = BuildRuntimeWithWorkerScope();
        int mainThreadId = Environment.CurrentManagedThreadId;

        var snapshot = await runtime.CaptureDiagnosticsAsync()
                                    .AsTask()
                                    .WaitAsync(TimeSpan.FromSeconds(2));
        var worker = snapshot.Scopes.Single(static scope => scope.ScopeId == WorkerDiagnosticsScope.ScopeId);

        Assert.That(worker.OwnerThreadId, Is.Not.EqualTo(0));
        Assert.That(worker.OwnerThreadId, Is.Not.EqualTo(mainThreadId));
        Assert.That(worker.CallInboxAccepted, Is.GreaterThanOrEqualTo(1));
        Assert.That(worker.CallInboxHighWatermark, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Main_scope_does_not_read_worker_runtime_directly()
    {
        using var runtime = BuildRuntimeWithWorkerScope();
        var worker = runtime.ScopeHost.Scopes.Single(static scope => scope.ScopeId == WorkerDiagnosticsScope.ScopeId);

        Assert.That(
            () => worker.CaptureDiagnostics(),
            Throws.InvalidOperationException.With.Message.Contains("control call"));
    }

    [Test]
    public void Capture_cancel_does_not_change_runtime_state()
    {
        using var runtime = BuildRuntimeWithWorkerScope();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var state = runtime.State;

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await runtime.CaptureDiagnosticsAsync(cts.Token));
        Assert.That(runtime.State, Is.EqualTo(state));
    }

    [Test]
    public void Payload_outstanding_returns_to_zero_after_pump()
    {
        using var runtime = BuildRuntimeWithSecondaryScope();

        Assert.That(runtime.Main.Post(new DiagnosticsPayloadEvent(1)).IsAccepted, Is.True);
        Assert.That(runtime.CaptureDiagnostics().Payloads.Outstanding, Is.EqualTo(1));

        runtime.Pump(0.016f);

        Assert.That(runtime.CaptureDiagnostics().Payloads.Outstanding, Is.EqualTo(0));
        Assert.That(runtime.CaptureDiagnostics().Payloads.PeakOutstanding, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void No_diagnostics_queue_exists()
    {
        var queueTypes = typeof(LayerRuntime).Assembly
            .GetTypes()
            .Select(static type => type.Name)
            .Where(static name => name.Contains("Diagnostics", StringComparison.Ordinal) &&
                                  name.Contains("Queue", StringComparison.Ordinal))
            .ToArray();

        Assert.That(queueTypes, Is.Empty);
    }

    [Test]
    public void Topology_and_policy_markdown_remain_available()
    {
        using var runtime = BuildRuntimeWithSecondaryScope();

        Assert.That(runtime.GetTopologyMarkdown(), Does.Contain("## 2. Scopes"));
        Assert.That(runtime.GetTopologyMarkdown(), Does.Contain(nameof(DiagnosticsScope)));
        Assert.That(runtime.GetPolicyMarkdown(), Does.Contain("# LayerBase Runtime Policy Dump"));
    }

    [Test]
    public void Diagnostics_snapshot_does_not_expose_runtime_objects()
    {
        var forbidden = new[]
        {
            typeof(ScopeRuntime).Name,
            typeof(EventCenter).Name,
            "ActorWorld",
            "World",
            "LayerToolRegistry"
        };

        var exposedTypeNames = typeof(RuntimeDiagnosticsSnapshot).Assembly
            .GetTypes()
            .Where(static type => type.Name.EndsWith("DiagnosticsSnapshot", StringComparison.Ordinal))
            .SelectMany(static type => type.GetProperties().Select(static property => property.PropertyType.Name)
                .Concat(type.GetFields().Select(static field => field.FieldType.Name)))
            .ToArray();

        foreach (var name in forbidden)
            Assert.That(exposedTypeNames, Does.Not.Contain(name));
    }

    private static LayerRuntime BuildRuntimeWithSecondaryScope()
    {
        return LayerHub.CreateLayers()
                       .Push(new DiagnosticsLayer())
                       .AddAssemblyModule(new TestAssemblyModule(
                           "diagnostics",
                           services: new[]
                           {
                               ServiceContribution.ForTypes(
                                   typeof(IDiagnosticsScopedService),
                                   typeof(DiagnosticsScopedService),
                                   typeof(DiagnosticsLayer),
                                   typeof(DiagnosticsScope),
                                   ServiceLifetime.Singleton)
                           },
                           tools: new[]
                           {
                               LayerToolContribution.ForTypes(
                                   typeof(DiagnosticsTool),
                                   typeof(DiagnosticsTool),
                                   "default",
                                   typeof(DiagnosticsLayer))
                           }))
                       .Build();
    }

    private static LayerRuntime BuildRuntimeWithWorkerScope()
    {
        var runtime = new LayerRuntime(2701);
        runtime.InstallScopeHost(new[]
        {
            ScopeExecutionPlan.CreateMain(),
            new ScopeExecutionPlan(
                new ScopeDescriptor(
                    WorkerDiagnosticsScope.ScopeId,
                    nameof(WorkerDiagnosticsScope),
                    typeof(WorkerDiagnosticsScope)),
                ScopeOptions.Worker(tickRateHz: 100))
        });
        runtime.ScopeHost.MainScope.InstallSynchronizationContext();
        runtime.ScopeHost.StartWorkers();
        return runtime;
    }

    private sealed class DiagnosticsLayer : Layer
    {
    }

    private interface IDiagnosticsScopedService
    {
    }

    private sealed class DiagnosticsScopedService : IDiagnosticsScopedService
    {
    }

    private sealed class DiagnosticsTool
    {
    }

    private readonly struct DiagnosticsPayloadEvent
    {
        public DiagnosticsPayloadEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }

    private sealed class DiagnosticsScope : IScopeDefinition
    {
        public const int ScopeId = 27;
        public ScopeOptions Options => ScopeOptions.Inline;
        
    }

    private sealed class WorkerDiagnosticsScope : IScopeDefinition
    {
        public const int ScopeId = 2701;
        public ScopeOptions Options => ScopeOptions.Inline;
        
    }

    private sealed class TestAssemblyModule : IAssemblyModule
    {
        public TestAssemblyModule(
            string id,
            ServiceContribution[]? services = null,
            LayerToolContribution[]? tools = null)
        {
            Id = new AssemblyModuleId(id);
            Manifest = new AssemblyModuleManifest(
                Id,
                services ?? Array.Empty<ServiceContribution>(),
                Array.Empty<ContextContribution>(),
                Array.Empty<LocalCallContribution>(),
                tools ?? Array.Empty<LayerToolContribution>());
        }

        public AssemblyModuleId Id { get; }

        public AssemblyModuleManifest Manifest { get; }
    }
}
