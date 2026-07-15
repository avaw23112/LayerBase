using LayerBase;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;
using LayerBase.Snap;

namespace LayerBase.Test;

[TestFixture]
public class ScopeSnapTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        ScopeSnapScopedService.ResetForTest();
    }

    [Test]
    public void Snap_nodes_are_grouped_by_owner_scope()
    {
        using var runtime = new LayerRuntime(2400);
        using ScopeRuntimeHost host = CreateInlineHost(runtime, ScopeSnapCustomScope.ScopeId);
        var fullSnap = new FullSnapRuntime(runtime, host);

        fullSnap.Register(
            ScopeDefinitionIds.Main,
            new ScopeSnapNodePlan(0, 0, new ThreadRecordingSnapNode("main-node")));
        fullSnap.Register(
            ScopeSnapCustomScope.ScopeId,
            new ScopeSnapNodePlan(0, 1, new ThreadRecordingSnapNode("custom-node")));
        fullSnap.FreezePlans();

        Assert.That(fullSnap.ScopeNodeCounts[ScopeDefinitionIds.Main], Is.EqualTo(1));
        Assert.That(fullSnap.ScopeNodeCounts[ScopeSnapCustomScope.ScopeId], Is.EqualTo(1));
    }

    [Test]
    public void Snap_key_format_remains_master_compatible()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ScopeSnapLayer())
            .Build();

        SnapDocument document = runtime.FullSnap.Serialize();

        Assert.That(document.Sections.Keys, Does.Contain("LayerBase.Test.ScopeSnapLayer_FullSnap"));
        Assert.That(document.Sections.Keys, Does.Contain("LayerBase.Test.ScopeSnapScopedService_FullSnap"));
        Assert.That(document.Sections.Keys, Has.None.StartsWith($"{ScopeSnapCustomScope.ScopeId}:"));
    }

    [Test]
    public void Duplicate_snap_key_fails_build()
    {
        Assert.That(
            () => LayerHub.CreateLayers()
                .Push(new DuplicateSnapLayer())
                .Push(new DuplicateSnapLayer())
                .Build(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Duplicate FullSnap key"));
    }

    [Test]
    public void Main_scope_only_sync_snap_still_works()
    {
        var layer = new ScopeSnapLayer();
        LayerRuntime runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        layer.LayerValue = 10;
        ScopeSnapScopedService.LastInstance!.ServiceValue = 20;
        SnapDocument document = runtime.FullSnap.Serialize();

        layer.LayerValue = 0;
        ScopeSnapScopedService.LastInstance!.ServiceValue = 0;
        runtime.FullSnap.Deserialize(document);

        Assert.That(layer.LayerValue, Is.EqualTo(10));
        Assert.That(ScopeSnapScopedService.LastInstance!.ServiceValue, Is.EqualTo(20));
    }

    [Test]
    public async Task Worker_scope_requires_async_snap_api()
    {
        using var runtime = new LayerRuntime(2401);
        using ScopeRuntimeHost host = CreateWorkerHost(runtime, ScopeSnapWorkerScope.ScopeId);
        var fullSnap = new FullSnapRuntime(runtime, host);
        fullSnap.FreezePlans();
        host.StartWorkers();

        Assert.That(
            () => fullSnap.Serialize(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("SerializeAsync"));

        SnapDocument document = await fullSnap.SerializeAsync().WithTimeout(TimeSpan.FromSeconds(2));

        Assert.That(document.Sections, Is.Empty);
    }

    [Test]
    public async Task Worker_snap_node_runs_on_worker_owner_thread()
    {
        using var runtime = new LayerRuntime(2402);
        using ScopeRuntimeHost host = CreateWorkerHost(runtime, ScopeSnapWorkerScope.ScopeId);
        var fullSnap = new FullSnapRuntime(runtime, host);
        var node = new ThreadRecordingSnapNode("worker-node");
        fullSnap.Register(
            ScopeSnapWorkerScope.ScopeId,
            new ScopeSnapNodePlan(0, 0, node));
        fullSnap.FreezePlans();
        int mainThreadId = Environment.CurrentManagedThreadId;
        host.StartWorkers();

        SnapDocument document = await fullSnap.SerializeAsync().WithTimeout(TimeSpan.FromSeconds(2));

        Assert.That(document.Sections.Keys, Does.Contain("worker-node"));
        Assert.That(node.WriteThreadId, Is.Not.EqualTo(mainThreadId));
    }

    [Test]
    public async Task Command_buffer_is_flushed_before_snapshot_write()
    {
        using var runtime = new LayerRuntime(2403);
        using ScopeRuntimeHost host = CreateWorkerHost(runtime, ScopeSnapWorkerScope.ScopeId);
        ScopeRuntime worker = host.Scopes.Single(static scope => scope.ScopeId == ScopeSnapWorkerScope.ScopeId);
        var fullSnap = new FullSnapRuntime(runtime, host);
        var node = new ThreadRecordingSnapNode(
            "flush-node",
            () => worker.EcsScheduler.CommandBuffer.Size);
        fullSnap.Register(
            ScopeSnapWorkerScope.ScopeId,
            new ScopeSnapNodePlan(0, 0, node));
        fullSnap.FreezePlans();
        host.StartWorkers();

        await worker.RequestEnterSafePointAsync().WithTimeout(TimeSpan.FromSeconds(2));
        worker.EcsScheduler.CommandBuffer.Create(Array.Empty<Arch.Core.ComponentType>());
        await worker.RequestExitSafePointAsync().WithTimeout(TimeSpan.FromSeconds(2));

        await fullSnap.SerializeAsync().WithTimeout(TimeSpan.FromSeconds(2));

        Assert.That(node.CommandBufferSizeAtWrite, Is.EqualTo(0));
    }

    [Test]
    public void Plain_object_clip_snap_still_works()
    {
        var carrier = new MultiClipCarrier();

        carrier.Clip<MoveClip>().Deserialize(new MoveClip(1.5f, 2.5f));

        Assert.That(carrier.Clip<MoveClip>().Serialize().X, Is.EqualTo(1.5f));
        Assert.That(carrier.TryClip<HealthClip>(out _), Is.True);
    }

    private static ScopeRuntimeHost CreateWorkerHost(LayerRuntime runtime, int scopeId)
    {
        return ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(scopeId, nameof(ScopeSnapWorkerScope), typeof(ScopeSnapWorkerScope)),
                    ScopeOptions.Worker(tickRateHz: 100))
            },
            runtime.Id,
            runtime.Generation);
    }

    private static ScopeRuntimeHost CreateInlineHost(LayerRuntime runtime, int scopeId)
    {
        return ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                new ScopeExecutionPlan(
                    new ScopeDescriptor(scopeId, nameof(ScopeSnapCustomScope), typeof(ScopeSnapCustomScope)),
                    ScopeOptions.Inline)
            },
            runtime.Id,
            runtime.Generation);
    }

    private sealed class ThreadRecordingSnapNode : IGeneratedFullSnapNode
    {
        private readonly Func<int>? _commandBufferSizeProvider;

        public ThreadRecordingSnapNode(string key, Func<int>? commandBufferSizeProvider = null)
        {
            __SnapKey = key;
            _commandBufferSizeProvider = commandBufferSizeProvider;
        }

        public int WriteThreadId { get; private set; }

        public int CommandBufferSizeAtWrite { get; private set; } = -1;

        public string __SnapKey { get; }

        public int __SnapVersion => 1;

        public void WriteFullSnap(ref SnapWriter writer)
        {
            WriteThreadId = Environment.CurrentManagedThreadId;
            if (_commandBufferSizeProvider != null)
                CommandBufferSizeAtWrite = _commandBufferSizeProvider();
        }

        public void ReadFullSnap(ref SnapReader reader)
        {
        }
    }
}

public readonly struct ScopeSnapCustomScope : IScopeDefinition
{
    public const int ScopeId = 240;
}

public readonly struct ScopeSnapWorkerScope : IScopeDefinition
{
    public const int ScopeId = 241;
}

public partial class ScopeSnapLayer : Layer, IFullSnap
{
    public int LayerValue { get; set; }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("layerValue", LayerValue);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        LayerValue = reader.ReadInt32("layerValue");
    }
}

[OwnerLayer(typeof(ScopeSnapLayer))]
public partial class ScopeSnapScopedService : IService, IFullSnap
{
    public ScopeSnapScopedService()
    {
        LastInstance = this;
    }

    public static ScopeSnapScopedService? LastInstance { get; private set; }

    public static void ResetForTest()
    {
        LastInstance = null;
    }

    public int ServiceValue { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("serviceValue", ServiceValue);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        ServiceValue = reader.ReadInt32("serviceValue");
    }
}

public partial class DuplicateSnapLayer : Layer
{
}

[OwnerLayer(typeof(DuplicateSnapLayer))]
public partial class DuplicateSnapService : IService, IFullSnap
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
    }
}
