using LayerBase;
using LayerBase.Async;
using LayerBase.Call;
using LayerBase.Layers;
using LayerBase.Scope;

namespace EventsTest;

[TestFixture]
public sealed class ScopeFaultPropagationTests
{
    [Test]
    public void Update_exception_delivers_fault_through_completion_inbox_to_main_scope()
    {
        using var runtime = new LayerRuntime(9201);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, ScopeOptions.Inline)
            },
            runtimeId: 9201,
            generation: 1);

        ScopeFaultInfo? fault = null;
        runtime.Faulted += info => { fault = info; };

        ScopeRuntime customScope = host.Scopes[1];

        Assert.DoesNotThrow(() => customScope.PumpUpdate(0.016f));
        Assert.That(host.MainScope.Transport.CompletionInbox.Count, Is.EqualTo(1));

        host.MainScope.PumpIngress();

        Assert.That(fault, Is.Not.Null);
        Assert.That(fault!.Value.Record.SourceScopeId, Is.EqualTo(1));
        Assert.That(fault.Value.Record.Phase, Is.EqualTo(ScopeFaultPhase.ServiceUpdate));
    }

    [Test]
    public void Main_scope_pump_invokes_runtime_faulted_event_on_main_thread()
    {
        using var runtime = new LayerRuntime(9202);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, ScopeOptions.Inline)
            },
            runtimeId: 9202,
            generation: 1);

        var mainThreadId = Environment.CurrentManagedThreadId;
        var faultedThreadId = 0;
        ScopeFaultInfo? fault = null;
        runtime.Faulted += info =>
        {
            faultedThreadId = Environment.CurrentManagedThreadId;
            fault = info;
        };

        host.Scopes[1].PumpUpdate(0.016f);
        host.MainScope.PumpIngress();

        Assert.That(fault, Is.Not.Null);
        Assert.That(faultedThreadId, Is.EqualTo(mainThreadId));
        Assert.That(fault!.Value.Record.SourceScopeId, Is.EqualTo(1));
        Assert.That(fault.Value.Record.Phase, Is.EqualTo(ScopeFaultPhase.ServiceUpdate));
    }

    [Test]
    public void Faulted_callback_exception_is_isolated_from_main_scope_pump()
    {
        using var runtime = new LayerRuntime(9207);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, ScopeOptions.Inline)
            },
            runtimeId: 9207,
            generation: 1);

        runtime.Faulted += _ => throw new InvalidOperationException("callback failed");

        host.Scopes[1].PumpUpdate(0.016f);

        Assert.DoesNotThrow(() => host.MainScope.PumpIngress());
        Assert.That(host.MainScope.Transport.EventInbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Non_main_scope_fault_delivered_via_completion_inbox_when_main_event_inbox_full()
    {
        using var runtime = new LayerRuntime(9208);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, ScopeOptions.Inline)
            },
            runtimeId: 9208,
            generation: 1);

        ScopeFaultInfo? fault = null;
        runtime.Faulted += info => { fault = info; };

        ScopeRuntime customScope = host.Scopes[1];
        ScopeRuntime mainScope = host.MainScope;

        for (int i = 0; i < 1024; i++)
        {
            ScopePostResult result = mainScope.Transport.EnqueueEvent(
                routeId: 9999,
                ScopeEventClass.Critical,
                new FillEvent(i));
            Assert.That(result, Is.EqualTo(ScopePostResult.Accepted));
        }

        ScopePostResult rejected = mainScope.Transport.EnqueueEvent(
            routeId: 9999,
            ScopeEventClass.Critical,
            new FillEvent(9999));
        Assert.That(rejected, Is.EqualTo(ScopePostResult.QueueFull));

        customScope.PumpUpdate(0.016f);

        Assert.That(mainScope.Transport.CompletionInbox.Count, Is.EqualTo(1));

        mainScope.PumpIngress();

        Assert.That(fault, Is.Not.Null);
        Assert.That(fault!.Value.Record.SourceScopeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Call_handler_exception_is_not_double_reported_as_fault_event()
    {
        using var runtime = new LayerRuntime(9203);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain()
            },
            runtimeId: 9203,
            generation: 1);

        ScopeRuntime scope = host.MainScope;
        scope.LocalCalls.Register(new ScopeLocalCallRouteEntry(
            ScopeDefinitionIds.Main,
            ScopeLocalCallRouteId<FaultCallRequest, FaultCallResponse>.Id,
            typeof(FaultCallRequest),
            typeof(FaultCallResponse),
            typeof(FaultCallHandler),
            typeof(FaultCallLayer),
            new ScopeLocalCallInvoker<FaultCallRequest, FaultCallResponse>(
                static (_, _) => throw new InvalidOperationException("call failed")),
            new ScopeLocalCallDispatcher<FaultCallRequest, FaultCallResponse>(
                static (_, _) => throw new InvalidOperationException("call failed"))));

        var callTask = scope.EnqueueCall<FaultCallRequest, FaultCallResponse>(new FaultCallRequest());
        scope.PumpIngress();

        Assert.ThrowsAsync<InvalidOperationException>(async () => await callTask);
        Assert.That(host.MainScope.Transport.EventInbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public async Task Faulted_worker_still_accepts_stop_and_dispose()
    {
        using var runtime = new LayerRuntime(9204);
        var stopScopeOptions = new ScopeOptions(
            ScopeThreadingMode.Worker,
            ScopeClockMode.FixedRate,
            tickRateHz: 100,
            ScopeFaultPolicy.StopScope);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, stopScopeOptions)
            },
            runtimeId: 9204,
            generation: 1);

        ScopeRuntime workerScope = host.Scopes[1];
        host.StartWorkers();

        Assert.That(WaitUntil(() => workerScope.State == ScopeRuntimeState.Stopping || workerScope.State == ScopeRuntimeState.Stopped), Is.True);

        var stopTask = workerScope.RequestStopAsync();
        Assert.That(WaitUntil(() => stopTask.GetAwaiter().IsCompleted), Is.True);
        ScopeStopResponse stopResponse = await stopTask;
        Assert.That(stopResponse.State, Is.EqualTo(ScopeControlResult.Succeeded));

        var disposeTask = workerScope.RequestDisposeAsync();
        Assert.That(WaitUntil(() => disposeTask.GetAwaiter().IsCompleted), Is.True);
        ScopeDisposeResponse disposeResponse = await disposeTask;
        Assert.That(disposeResponse.State, Is.EqualTo(ScopeControlResult.Succeeded));
        Assert.That(workerScope.State, Is.EqualTo(ScopeRuntimeState.Disposed));
    }

    [Test]
    public void Stop_scope_policy_uses_control_call()
    {
        using var runtime = new LayerRuntime(9205);
        var stopScopeOptions = new ScopeOptions(
            ScopeThreadingMode.Inline,
            ScopeClockMode.RuntimePump,
            tickRateHz: 0,
            ScopeFaultPolicy.StopScope);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, stopScopeOptions)
            },
            runtimeId: 9205,
            generation: 1);

        ScopeRuntime customScope = host.Scopes[1];
        customScope.PumpUpdate(0.016f);
        host.MainScope.PumpIngress();

        Assert.That(customScope.Transport.CallInbox.TryDequeue(out var envelope), Is.True);
        Assert.That(envelope.Class, Is.EqualTo(ScopeCallClass.Control));
        Assert.That(envelope.RouteId, Is.EqualTo(ScopeLifecycleRouteIds.Stop));
    }

    [Test]
    public void Stop_runtime_policy_uses_main_scope_control_call()
    {
        using var runtime = new LayerRuntime(9206);
        var stopRuntimeOptions = new ScopeOptions(
            ScopeThreadingMode.Inline,
            ScopeClockMode.RuntimePump,
            tickRateHz: 0,
            ScopeFaultPolicy.StopRuntime);
        using var host = ScopeRuntimeHost.Create(
            runtime,
            new[]
            {
                ScopeExecutionPlan.CreateMain(),
                CreateThrowingUpdateScopePlan(scopeId: 1, stopRuntimeOptions)
            },
            runtimeId: 9206,
            generation: 1);

        host.Scopes[1].PumpUpdate(0.016f);

        Assert.That(host.MainScope.Transport.CallInbox.TryDequeue(out var envelope), Is.True);
        Assert.That(envelope.Class, Is.EqualTo(ScopeCallClass.Control));
        Assert.That(envelope.RouteId, Is.EqualTo(ScopeLifecycleRouteIds.Stop));
    }

    [Test]
    public void No_exception_queue_or_exception_hub_exists()
    {
        var forbiddenTypes = typeof(ScopeRuntime).Assembly
            .GetTypes()
            .Where(static type =>
                type.Name.Contains("ExceptionQueue", StringComparison.Ordinal) ||
                type.Name.Contains("ExceptionHub", StringComparison.Ordinal))
            .Select(static type => type.FullName)
            .ToArray();

        Assert.That(forbiddenTypes, Is.Empty);
    }

    private static ScopeExecutionPlan CreateThrowingUpdateScopePlan(
        int scopeId,
        ScopeOptions options)
    {
        var update = new UpdateInvoker[]
        {
            _ => throw new InvalidOperationException("update failed")
        };
        var layers = new[]
        {
            new ScopeLayerLifecycleSlice(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)
        };

        return new ScopeExecutionPlan(
            new ScopeDescriptor(scopeId, nameof(FaultScope), typeof(FaultScope)),
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

    private sealed class FaultScope : IScopeDefinition
    {
    
    public ScopeOptions Options => ScopeOptions.Inline;
    }

    private readonly struct FaultCallRequest
    {
    }

    private readonly struct FaultCallResponse
    {
    }

    private readonly struct FillEvent
    {
        public FillEvent(int value) { Value = value; }
        public int Value { get; }
    }

    private sealed class FaultCallHandler
    {
    }

    private sealed class FaultCallLayer : Layer
    {
    }

    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            Thread.Sleep(10);
        }

        return condition();
    }
}
