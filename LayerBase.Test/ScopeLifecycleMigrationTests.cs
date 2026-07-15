using System.Reflection;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeLifecycleMigrationTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    [Test]
    public void Main_scope_lifecycle_plan_contains_layer_slices_and_precomputed_update_invokers()
    {
        var trace = new List<string>();
        var runtime = LayerHub.CreateLayers()
                              .Push(new TraceLayer("L0", trace, registerUpdate: true))
                              .Push(new TraceLayer("L1", trace, registerUpdate: false))
                              .Push(new TraceLayer("L2", trace, registerUpdate: false))
                              .Build();

        var plan = runtime.ScopeHost.MainScope.LifecyclePlan;
        var planType = plan.GetType();
        var layers = GetArray(planType, plan, "Layers");
        var updates = GetArray(planType, plan, "Update");

        Assert.That(layers, Has.Length.EqualTo(3), "Empty Layer slices must preserve push LayerIndex order.");
        Assert.That(updates, Has.Length.EqualTo(1), "Update invokers should be precomputed into ScopeLifecyclePlan.");
        Assert.That(GetInt(layers.GetValue(0)!, "LayerIndex"), Is.EqualTo(0));
        Assert.That(GetInt(layers.GetValue(1)!, "LayerIndex"), Is.EqualTo(1));
        Assert.That(GetInt(layers.GetValue(2)!, "LayerIndex"), Is.EqualTo(2));
        Assert.That(GetInt(layers.GetValue(1)!, "UpdateCount"), Is.EqualTo(0));
    }

    [Test]
    public void Scope_update_runs_precomputed_layer_slices_in_push_order()
    {
        var trace = new List<string>();
        var runtime = LayerHub.CreateLayers()
                              .Push(new TraceLayer("L0", trace, registerUpdate: true))
                              .Push(new TraceLayer("L1", trace, registerUpdate: false))
                              .Push(new TraceLayer("L2", trace, registerUpdate: true, updateSlot: 1))
                              .Build();

        runtime.Pump(0.016f);

        Assert.That(trace.Where(static item => item.StartsWith("Update_", StringComparison.Ordinal)).ToArray(),
            Is.EqualTo(new[] { "Update_L0", "Update_L2" }));
    }

    [Test]
    public void Initialize_runs_through_scope_lifecycle_plan_in_forward_layer_order()
    {
        var trace = new List<string>();
        var runtime = LayerHub.CreateLayers()
                              .Push(new TraceLayer("L0", trace, registerUpdate: false, registerInitialize: true))
                              .Push(new TraceLayer("L1", trace, registerUpdate: false, registerInitialize: false))
                              .Push(new TraceLayer("L2", trace, registerUpdate: false, registerInitialize: true))
                              .Build();

        var plan = runtime.ScopeHost.MainScope.LifecyclePlan;
        var initializes = GetArray(plan.GetType(), plan, "Initialize");

        Assert.That(initializes, Has.Length.EqualTo(2),
            "Initialize invokers must be captured into the ScopeLifecyclePlan instead of running outside the scope lifecycle.");
        Assert.That(trace.Where(static item => item.StartsWith("Init_", StringComparison.Ordinal)).ToArray(),
            Is.EqualTo(new[] { "Init_L0", "Init_L2" }));
    }

    [Test]
    public void Runtime_stop_runs_in_reverse_layer_order()
    {
        var trace = new List<string>();
        var runtime = LayerHub.CreateLayers()
                              .Push(new TraceLayer("L0", trace, registerUpdate: false))
                              .Push(new TraceLayer("L1", trace, registerUpdate: false))
                              .Push(new TraceLayer("L2", trace, registerUpdate: false))
                              .Build();

        runtime.Dispose();

        Assert.That(trace.Where(static item => item.StartsWith("Stop_", StringComparison.Ordinal)).ToArray(),
            Is.EqualTo(new[] { "Stop_L2", "Stop_L1", "Stop_L0" }));
    }

    [Test]
    public void Fixed_update_accumulator_belongs_to_scope_runtime_not_layer_runtime()
    {
        var runtimeFields = typeof(LayerRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(static field => field.Name)
            .ToArray();

        var scopeRuntimeFields = typeof(ScopeRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(static field => field.Name)
            .ToArray();

        Assert.That(runtimeFields, Has.No.EqualTo("_fixedUpdateAccumulator"));
        Assert.That(scopeRuntimeFields, Has.Some.EqualTo("_fixedUpdateAccumulator"));
    }

    [Test]
    public void Layer_chain_no_longer_exposes_running_tick_entry_points()
    {
        var layerChainType = typeof(Layer).Assembly.GetType("LayerBase.Layers.LayerChain")!;
        var runningMethods = layerChainType
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(static method => method.Name is "Pump" or "PumpFixed")
            .Select(static method => method.Name)
            .ToArray();

        Assert.That(runningMethods, Is.Empty);
    }

    [Test]
    public void Inline_scope_runs_lifecycle_plan_in_layer_slice_order()
    {
        var trace = new List<string>();
        using var runtime = new LayerRuntime(9101);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateTraceScopePlan<InlineTraceScope>(
                    scopeId: 1,
                    ScopeOptions.Inline,
                    trace,
                    "I0",
                    "I2")
            },
            runtimeId: 9101,
            generation: 1);

        Assert.That(host.TryGetScope<InlineTraceScope>(out var scope), Is.True);
        Assert.That(scope.Address.ScopeId, Is.EqualTo(1));

        host.Scopes[1].PumpUpdate(0.016f);

        Assert.That(trace, Is.EqualTo(new[] { "I0", "I2" }));
    }

    [Test]
    public void Worker_scope_runs_lifecycle_plan_on_owner_thread()
    {
        var updateSignal = new ManualResetEventSlim(false);
        var stopSignal = new ManualResetEventSlim(false);
        var mainThreadId = Environment.CurrentManagedThreadId;
        var updateThreadId = 0;
        var stopThreadId = 0;

        using var runtime = new LayerRuntime(9102);
        using (var host = ScopeRuntimeHost.Create(
                   runtime,
                   new[]
                   {
                       ScopeExecutionPlan.CreateMain(),
                       CreateWorkerScopePlan(
                           updateSignal,
                           stopSignal,
                           updateThread => updateThreadId = updateThread,
                           stopThread => stopThreadId = stopThread)
                   },
                   runtimeId: 9102,
                   generation: 1))
        {
            Assert.That(host.TryGetScope<WorkerTraceScope>(out var scope), Is.True);
            Assert.That(scope.Address.ScopeId, Is.EqualTo(2));

            host.StartWorkers();

            Assert.That(updateSignal.Wait(TimeSpan.FromSeconds(2)), Is.True);
        }

        Assert.That(stopSignal.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(updateThreadId, Is.Not.EqualTo(0));
        Assert.That(stopThreadId, Is.EqualTo(updateThreadId));
        Assert.That(updateThreadId, Is.Not.EqualTo(mainThreadId));
    }

    [Test]
    public void Scope_pump_drains_owner_synchronization_context_before_update()
    {
        var trace = new List<string>();
        using var runtime = new LayerRuntime(9106);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateTraceScopePlan<InlineTraceScope>(
                    scopeId: 1,
                    ScopeOptions.Inline,
                    trace,
                    "Update_I0",
                    "Update_I2")
            },
            runtimeId: 9106,
            generation: 1);

        ScopeRuntime scope = host.Scopes[1];
        scope.InstallSynchronizationContext();
        scope.SynchronizationContext!.Post(_ => trace.Add("Continuation"), null);

        scope.PumpScopeResources(0.016f);

        Assert.That(trace, Is.EqualTo(new[] { "Continuation", "Update_I0", "Update_I2" }));
    }

    [Test]
    public async Task Disposing_scope_runs_lifecycle_dispose_in_reverse_layer_order()
    {
        var trace = new List<string>();
        using var runtime = new LayerRuntime(9107);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateDisposeScopePlan<InlineTraceScope>(scopeId: 1, trace)
            },
            runtimeId: 9107,
            generation: 1);

        ScopeRuntime scope = host.Scopes[1];
        var disposeTask = scope.RequestDisposeAsync();

        scope.PumpIngress();

        _ = await disposeTask;
        Assert.That(trace, Is.EqualTo(new[] { "Dispose_L2", "Dispose_L0" }));
    }

    [Test]
    public async Task Stop_control_is_delivered_through_scope_call_inbox()
    {
        using var runtime = new LayerRuntime(9104);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateTraceScopePlan<InlineTraceScope>(
                    scopeId: 1,
                    ScopeOptions.Inline,
                    new List<string>(),
                    "I0",
                    "I2")
            },
            runtimeId: 9104,
            generation: 1);

        ScopeRuntime scope = host.Scopes[1];

        var stopTask = scope.RequestStopAsync();

        Assert.That(stopTask.GetAwaiter().IsCompleted, Is.False,
            "Stop must be queued as a control ScopeCall and completed by the owner-thread pump.");

        scope.PumpIngress();

        ScopeStopResponse response = await stopTask;
        Assert.That(response.State, Is.EqualTo(ScopeControlResult.Succeeded));
        Assert.That(scope.State, Is.EqualTo(ScopeRuntimeState.Stopped));
    }

    [Test]
    public async Task Stopped_scope_rejects_business_but_accepts_dispose_control()
    {
        using var runtime = new LayerRuntime(9105);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateTraceScopePlan<InlineTraceScope>(
                    scopeId: 1,
                    ScopeOptions.Inline,
                    new List<string>(),
                    "I0",
                    "I2")
            },
            runtimeId: 9105,
            generation: 1);

        ScopeRuntime scope = host.Scopes[1];

        var stopTask = scope.RequestStopAsync();
        scope.PumpIngress();
        _ = await stopTask;

        var postResult = scope.EnqueueEvent(new TraceScopeEvent(7));
        Assert.That(postResult.Status, Is.EqualTo(ScopePostStatus.Rejected));

        var disposeTask = scope.RequestDisposeAsync();
        Assert.That(disposeTask.GetAwaiter().IsCompleted, Is.False);

        scope.PumpIngress();

        ScopeDisposeResponse response = await disposeTask;
        Assert.That(response.State, Is.EqualTo(ScopeControlResult.Succeeded));
        Assert.That(scope.State, Is.EqualTo(ScopeRuntimeState.Disposed));
    }

    [Test]
    public void Scope_worker_does_not_use_stop_flag_as_lifecycle_command()
    {
        Type workerType = typeof(ScopeWorker);

        Assert.That(workerType.GetMethod("RequestStop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            Is.Null,
            "Worker stop/dispose must be delivered through ScopeCall, not a direct worker command method.");

        Assert.That(workerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                              .Select(static field => field.Name),
            Has.No.EqualTo("_running"),
            "A worker-local stop flag must not carry lifecycle command semantics.");
    }

    [Test]
    public void Scope_ref_posts_to_its_own_scope_inbox()
    {
        var trace = new List<string>();
        using var runtime = new LayerRuntime(9103);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateTraceScopePlan<InlineTraceScope>(
                    scopeId: 1,
                    ScopeOptions.Inline,
                    trace,
                    "I0",
                    "I2")
            },
            runtimeId: 9103,
            generation: 1);

        Assert.That(host.TryGetScope<InlineTraceScope>(out var scope), Is.True);

        var result = scope.Post(new TraceScopeEvent(5));

        Assert.That(result.Status, Is.EqualTo(ScopePostStatus.Accepted));
        Assert.That(host.Scopes[0].Transport.EventInbox.TryDequeue(out _), Is.False);
        Assert.That(host.Scopes[1].Transport.EventInbox.TryDequeue(out var envelope), Is.True);
        Assert.That(envelope.Origin.ScopeId, Is.EqualTo(1));
    }

    private static Array GetArray(Type type, object instance, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
            return (Array)property.GetValue(instance)!;

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"ScopeLifecyclePlan should expose {name}.");
        return (Array)field!.GetValue(instance)!;
    }

    private static int GetInt(object instance, string name)
    {
        var type = instance.GetType();
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
            return (int)property.GetValue(instance)!;

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"{type.Name} should expose {name}.");
        return (int)field!.GetValue(instance)!;
    }

    private static ScopeExecutionPlan CreateTraceScopePlan<TScope>(
        int scopeId,
        ScopeOptions options,
        List<string> trace,
        string firstLayerValue,
        string thirdLayerValue)
        where TScope : IScopeDefinition
    {
        var update = new UpdateInvoker[]
        {
            _ => trace.Add(firstLayerValue),
            _ => trace.Add(thirdLayerValue)
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0),
            new ScopeLayerLifecycleSlice(1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0),
            new ScopeLayerLifecycleSlice(2, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(scopeId, typeof(TScope).Name, typeof(TScope)),
            options,
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>()));
    }

    private static ScopeExecutionPlan CreateWorkerScopePlan(
        ManualResetEventSlim updateSignal,
        ManualResetEventSlim stopSignal,
        Action<int> captureUpdateThread,
        Action<int> captureStopThread)
    {
        var update = new UpdateInvoker[]
        {
            _ =>
            {
                captureUpdateThread(Environment.CurrentManagedThreadId);
                updateSignal.Set();
            }
        };
        var runtimeStop = new LifecycleInvoker[]
        {
            () =>
            {
                captureStopThread(Environment.CurrentManagedThreadId);
                stopSignal.Set();
            }
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(2, nameof(WorkerTraceScope), typeof(WorkerTraceScope)),
            ScopeOptions.Worker(tickRateHz: 100),
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                update,
                Array.Empty<FixedUpdateInvoker>(),
                runtimeStop,
                Array.Empty<LifecycleInvoker>()));
    }

    private static ScopeExecutionPlan CreateDisposeScopePlan<TScope>(
        int scopeId,
        List<string> trace)
        where TScope : IScopeDefinition
    {
        var dispose = new LifecycleInvoker[]
        {
            () => trace.Add("Dispose_L0"),
            () => trace.Add("Dispose_L2")
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
            new ScopeLayerLifecycleSlice(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0),
            new ScopeLayerLifecycleSlice(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(scopeId, typeof(TScope).Name, typeof(TScope)),
            ScopeOptions.Inline,
            lifecyclePlan: new ScopeLifecyclePlan(
                layers,
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                Array.Empty<UpdateInvoker>(),
                Array.Empty<FixedUpdateInvoker>(),
                Array.Empty<LifecycleInvoker>(),
                dispose));
    }

    private sealed class TraceLayer : Layer, IRuntimeStop
    {
        private readonly string _name;
        private readonly List<string> _trace;
        private readonly bool _registerUpdate;
        private readonly bool _registerInitialize;
        private readonly int _updateSlot;

        public TraceLayer(
            string name,
            List<string> trace,
            bool registerUpdate,
            int updateSlot = 0,
            bool registerInitialize = false)
        {
            _name = name;
            _trace = trace;
            _registerUpdate = registerUpdate;
            _registerInitialize = registerInitialize;
            _updateSlot = updateSlot;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            if (_registerInitialize)
            {
                if (_name == "L0")
                    services.AddSingleton(new TraceInitializableServiceA(_name, _trace));
                else
                    services.AddSingleton(new TraceInitializableServiceB(_name, _trace));
            }

            if (_registerUpdate)
            {
                if (_updateSlot == 0)
                    services.AddSingleton(new TraceUpdateServiceA(_name, _trace));
                else
                    services.AddSingleton(new TraceUpdateServiceB(_name, _trace));
            }
        }

        public void RuntimeStop()
        {
            _trace.Add("Stop_" + _name);
        }
    }

    private abstract class TraceInitializableService : IService, IInitializable
    {
        private readonly string _name;
        private readonly List<string> _trace;

        public TraceInitializableService(string name, List<string> trace)
        {
            _name = name;
            _trace = trace;
        }

        public void Initialize()
        {
            _trace.Add("Init_" + _name);
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class TraceInitializableServiceA : TraceInitializableService
    {
        public TraceInitializableServiceA(string name, List<string> trace)
            : base(name, trace)
        {
        }
    }

    private sealed class TraceInitializableServiceB : TraceInitializableService
    {
        public TraceInitializableServiceB(string name, List<string> trace)
            : base(name, trace)
        {
        }
    }

    private abstract class TraceUpdateService : IService, IUpdate
    {
        private readonly string _name;
        private readonly List<string> _trace;

        public TraceUpdateService(string name, List<string> trace)
        {
            _name = name;
            _trace = trace;
        }

        public void Update()
        {
            _trace.Add("Update_" + _name);
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class TraceUpdateServiceA : TraceUpdateService
    {
        public TraceUpdateServiceA(string name, List<string> trace)
            : base(name, trace)
        {
        }
    }

    private sealed class TraceUpdateServiceB : TraceUpdateService
    {
        public TraceUpdateServiceB(string name, List<string> trace)
            : base(name, trace)
        {
        }
    }

    private readonly struct InlineTraceScope : IScopeDefinition
    {
    }

    private readonly struct WorkerTraceScope : IScopeDefinition
    {
    }

    private readonly struct TraceScopeEvent
    {
        public TraceScopeEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
