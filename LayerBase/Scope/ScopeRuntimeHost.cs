using System.Linq;
using LayerBase.Async;
using LayerBase.Actor;
using LayerBase.Core.Event;
using LayerBase.Lifetime;

namespace LayerBase.Scope;

internal sealed class ScopeRuntimeHost : ILifetimeParticipant, IDisposable
{
    private readonly ScopeRuntimeDirectory _directory;
    private readonly ScopeRuntime[] _inlineScopes;
    private readonly ScopeWorker[] _workers;
    private int _shutdownStarted;
    private int _disposed;
    private int _stopRequested;
    private int _nextInlineScopeIndex;

    string ILifetimeParticipant.LifetimeName => "ScopeRuntimeHost";

    private ScopeRuntimeHost(ScopeRuntimeDirectory directory, ScopeWorker[] workers)
    {
        var scopes = directory.Runtimes;
        var inlineScopes = new List<ScopeRuntime>();

        for (int i = 0; i < scopes.Length; i++)
        {
            if (scopes[i].Options.Threading == ScopeThreadingMode.Inline)
                inlineScopes.Add(scopes[i]);
        }

        _directory = directory;
        _inlineScopes = inlineScopes.ToArray();
        _workers = workers ?? Array.Empty<ScopeWorker>();

        var mainEndpoint = scopes[0].Endpoint;
        for (int i = 0; i < scopes.Length; i++)
        {
            scopes[i].BindMainActorEndpoint(mainEndpoint);
            scopes[i].BindScopeEndpoints(_directory);
        }
    }

    public ScopeRuntime MainScope => _directory.MainScope;

    public IReadOnlyList<ScopeRuntime> Scopes => _directory.Runtimes;

    public bool HasWorkerScopes => _workers.Length > 0;

    public bool TryGetRuntime(int scopeId, out ScopeRuntime scope)
    {
        ThrowIfDisposed();

        if (_directory.TryGetRuntime(scopeId, out var runtime))
        {
            scope = runtime;
            return true;
        }

        scope = null!;
        return false;
    }

    public static ScopeRuntimeHost CreateMain(LayerRuntime runtime, int runtimeId, int generation)
    {
        return Create(runtime, new[] { ScopeExecutionPlan.CreateMain() }, runtimeId, generation);
    }

    public static ScopeRuntimeHost Create(
        LayerRuntime runtime,
        IReadOnlyList<ScopeExecutionPlan> plans,
        int runtimeId,
        int generation)
    {
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));
        if (plans == null)
            throw new ArgumentNullException(nameof(plans));
        if (plans.Count == 0)
            throw new ArgumentException("Scope host requires at least MainScope.", nameof(plans));

        var scopes = new ScopeRuntime[plans.Count];
        var workers = new List<ScopeWorker>();
        ScopeRuntimeHost? host = null;
        ScopeFaultHandler fault = (in ScopeFaultRecord record) =>
        {
            runtime.ReportScopeFault(in record);
            host?.ApplyFaultPolicy(in record);
        };
        ScopeDelayRegistryChangedHandler delayRegistryChanged = scopeId =>
        {
            if (scopeId == ScopeDefinitionIds.Main)
                runtime.MarkDelayDirty();
        };
        ScopeSystemCallHandler systemCall = (ScopeRuntime scope, in ScopeCallEnvelope envelope, EventPayloadStorage payloadStorage) =>
            scope.ScopeId == ScopeDefinitionIds.Main &&
            runtime.MainActorRuntime.DispatchCallRoute(
                envelope.RouteId,
                runtimeId,
                envelope,
                payloadStorage);
        ScopeSystemEventHandler systemEvent = (ScopeRuntime scope, in ScopeEventEnvelope envelope, EventPayloadStorage payloadStorage) =>
            scope.ScopeId == ScopeDefinitionIds.Main &&
            (runtime.MainActorRuntime.DispatchProjectionRoute(
                 envelope.RouteId,
                 scope,
                 runtimeId,
                 envelope.Payload,
                 payloadStorage) ||
             runtime.MainActorRuntime.DispatchCommandRoute(
                 envelope.RouteId,
                 runtimeId,
                 envelope.Payload,
                 payloadStorage));
        ScopeRuntimeCallbacks CreateCallbacks()
        {
            return new ScopeRuntimeCallbacks(
                fault,
                delayRegistryChanged,
                runtime.ReportLayerEventError,
                runtime.DisposeScopeServices,
                systemCall,
                systemEvent,
                () => runtime.HasToolRegistry ? runtime.Tools.CaptureDiagnostics() : default);
        }

        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].Descriptor.ScopeId == ScopeDefinitionIds.Main)
                {
                    scopes[i] = new ScopeRuntime(
                        plans[i],
                        runtimeId,
                        generation,
                        runtime.WorkerExecutor,
                        CreateCallbacks(),
                        runtime.MainActorRuntime.Client,
                        runtime.MainActorRuntime.Factory,
                        runtime.MainActorRuntime.ProjectedActorCommandSink,
                        runtime.MainActorRuntime.BindProjectionWorld);
                }
                else
                {
                    scopes[i] = new ScopeRuntime(
                        plans[i],
                        runtimeId,
                        generation,
                        runtime.WorkerExecutor,
                        CreateCallbacks());
                }

                if (plans[i].Options.Threading == ScopeThreadingMode.Worker)
                    workers.Add(new ScopeWorker(scopes[i]));
            }

            var directory = new ScopeRuntimeDirectory(scopes);
            host = new ScopeRuntimeHost(directory, workers.ToArray());
            return host;
        }
        catch
        {
            for (int i = 0; i < scopes.Length; i++)
                scopes[i]?.DisposeUnstarted();
            foreach (var worker in workers)
                worker.Dispose();
            throw;
        }
    }

    public bool HasAnyWorkerStarted => _workers.Length > 0 &&
        _workers.Any(
            w => w.StartState is
                ScopeWorkerStartState.Starting or
                ScopeWorkerStartState.Running or
                ScopeWorkerStartState.StartFailed or
                ScopeWorkerStartState.Exited);

    public void StartWorkers()
    {
        var defaultDeadline = ShutdownDeadline.Start(
            TimeSpan.FromSeconds(15));

        StartWorkers(in defaultDeadline);
    }

    public void StartWorkers(
        in ShutdownDeadline deadline)
    {
        if (Volatile.Read(ref _shutdownStarted) != 0)
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));

        var started = new List<ScopeWorker>();

        for (int i = 0; i < _workers.Length; i++)
        {
            ScopeWorker worker = _workers[i];
            try
            {
                switch (worker.StartState)
                {
                    case ScopeWorkerStartState.Created:
                        worker.Start(in deadline);
                        started.Add(worker);
                        break;

                    case ScopeWorkerStartState.Running:
                        break;

                    case ScopeWorkerStartState.Starting:
                        throw new InvalidOperationException(
                            $"Scope worker `{worker.Runtime.Descriptor.Name}` is already starting.");

                    case ScopeWorkerStartState.StartFailed:
                    case ScopeWorkerStartState.Exited:
                        throw new InvalidOperationException(
                            $"Scope worker `{worker.Runtime.Descriptor.Name}` cannot be restarted after `{worker.StartState}`.");

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(worker.StartState),
                            worker.StartState,
                            "Unknown scope worker start state.");
                }
            }
            catch
            {
                RollbackStartedWorkers(started, in deadline);
                throw;
            }
        }
    }

    private void RollbackStartedWorkers(
        List<ScopeWorker> started,
        in ShutdownDeadline deadline)
    {
        for (int i = started.Count - 1; i >= 0; i--)
        {
            ScopeWorker worker = started[i];
            if (worker.Runtime.State is ScopeRuntimeState.Disposed or ScopeRuntimeState.Faulted)
                continue;

            if (WaitForScopeStop(worker.Runtime, in deadline))
            {
                worker.RequestExitAfterScopeStopped();
                _ = worker.WaitForExit(in deadline);
            }
        }
    }

    public void ApplyFaultPolicy(in ScopeFaultRecord record)
    {
        if (!_directory.TryGetRuntime(
                record.SourceScopeId,
                out ScopeRuntime sourceScope))
        {
            return;
        }

        switch (sourceScope.Options.FaultPolicy)
        {
            case ScopeFaultPolicy.ReportAndContinue:
                return;

            case ScopeFaultPolicy.StopScope:
                _ = sourceScope.RequestStopAsync();
                return;

            case ScopeFaultPolicy.StopRuntime:
                _ = MainScope.RequestStopAsync();
                return;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool TryGetScope<TScope>(out ScopeRef<TScope> scope)
        where TScope : IScopeDefinition
    {
        ThrowIfDisposed();
        var scopeType = typeof(TScope);
        var runtimes = _directory.Runtimes;
        for (int i = 0; i < runtimes.Length; i++)
        {
            if (runtimes[i].Descriptor.ScopeType == scopeType)
            {
                scope = new ScopeRef<TScope>(runtimes[i].Endpoint);
                return true;
            }
        }

        scope = default;
        return false;
    }

    public bool HasInlineScopes =>
        _inlineScopes.Length > 0;

    internal ScopeRuntimeDirectory Directory => _directory;

    public void PumpInlineScopes(
        float deltaTime,
        ref RuntimeFrameBudget budget,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>? reportException)
    {
        ThrowIfDisposed();

        if (_inlineScopes.Length == 0)
            return;

        int startIndex = _nextInlineScopeIndex % _inlineScopes.Length;

        for (int i = 0; i < _inlineScopes.Length; i++)
        {
            int idx = (startIndex + i) % _inlineScopes.Length;
            _inlineScopes[idx].PumpScopeResources(
                deltaTime,
                ref budget,
                exceptionPolicy,
                reportException);
        }

        budget.StartingScopeIndex = (startIndex + 1) % _inlineScopes.Length;
        _nextInlineScopeIndex = (startIndex + 1) % _inlineScopes.Length;
    }

    public void CloseAdmission()
    {
        RequestStop();
    }

    public void RequestStop()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
            return;

        ScopeRuntime[] runtimes = _directory.Runtimes;
        for (int i = runtimes.Length - 1; i >= 1; i--)
        {
            ScopeRuntime scope = runtimes[i];
            if (scope.State is ScopeRuntimeState.Stopped or ScopeRuntimeState.Disposed) continue;
            _ = scope.RequestStopAsync();
        }
    }

    public LifetimeDrainResult Drain(in ShutdownDeadline deadline)
    {
        RequestStop();

        bool timedOut = !DrainNonMainScopes(in deadline);
        if (!timedOut)
            timedOut = !DrainMainScope(in deadline);

        if (!timedOut)
            RequestDisposeForAllScopes(in deadline);

        return timedOut
            ? LifetimeDrainResult.TimedOut
            : LifetimeDrainResult.Drained;
    }

    public void Release(TerminalCleanupRunner cleanup)
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0) return;
        var deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));
        try
        {
            if (!HasAnyWorkerStarted)
            {
                for (int i = _directory.Runtimes.Length - 1; i >= 0; i--)
                    cleanup.Run(_directory.Runtimes[i].Descriptor.Name, () => _directory.Runtimes[i].DisposeUnstarted());
                return;
            }
            RequestDisposeForAllScopes(in deadline);
        }
        finally { Volatile.Write(ref _disposed, 1); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
            return;
        var deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(15));
        try
        {
            if (!HasAnyWorkerStarted)
            {
                for (int i = _directory.Runtimes.Length - 1; i >= 0; i--)
                    _directory.Runtimes[i].DisposeUnstarted();
                for (int i = _workers.Length - 1; i >= 0; i--)
                    _workers[i].ForceReleaseResources();
                return;
            }
            Drain(in deadline);
            RequestDisposeForAllScopes(in deadline);
        }
        finally { Volatile.Write(ref _disposed, 1); }
    }

    private bool DrainNonMainScopes(in ShutdownDeadline deadline)
    {
        ScopeRuntime[] runtimes = _directory.Runtimes;
        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            ScopeRuntime scope = runtimes[i];
            if (scope.ScopeId == ScopeDefinitionIds.Main)
                continue;

            if (!WaitForScopeStop(scope, in deadline))
                return false;

            if (scope.Options.Threading == ScopeThreadingMode.Worker &&
                TryGetWorker(scope.ScopeId, out ScopeWorker worker))
            {
                if (scope.State == ScopeRuntimeState.Stopped)
                    worker.RequestExitAfterScopeStopped();
                if (!worker.WaitForExit(in deadline))
                    return false;
            }
        }

        return true;
    }

    private bool DrainMainScope(in ShutdownDeadline deadline)
    {
        return WaitForScopeStop(MainScope, in deadline);
    }

    private bool WaitForScopeStop(
        ScopeRuntime scope,
        in ShutdownDeadline deadline)
    {
        if (scope.State is ScopeRuntimeState.Stopped or ScopeRuntimeState.Disposed)
            return true;

        try
        {
            if (!WaitForControl(
                    scope,
                    scope.RequestStopAsync(),
                    in deadline,
                    $"{scope.Descriptor.Name}.Stop",
                    out ScopeStopResponse response))
            {
                scope.ReportFault(
                    new TimeoutException($"Scope `{scope.Descriptor.Name}` exceeded shutdown deadline."),
                    ScopeFaultPhase.Shutdown);
                return false;
            }

            ScopeControlBarrier.EnsureSucceeded(response.Result, "Stop", scope);
            if (!response.Snapshot.IsEmpty)
            {
                throw new InvalidOperationException(
                    $"Scope `{scope.Descriptor.Name}` returned successful Stop with pending work.");
            }

            return true;
        }
        catch (Exception ex)
        {
            scope.ReportFault(ex, ScopeFaultPhase.Shutdown);
            return false;
        }
    }

    private void RequestDisposeForAllScopes(in ShutdownDeadline deadline)
    {
        ScopeRuntime[] runtimes = _directory.Runtimes;
        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            ScopeRuntime scope = runtimes[i];
            if (scope.State == ScopeRuntimeState.Disposed) continue;
            try
            {
                if (!WaitForControl(
                        scope,
                        scope.RequestDisposeAsync(),
                        in deadline,
                        $"{scope.Descriptor.Name}.Dispose",
                        out ScopeDisposeResponse response))
                {
                    scope.ReportFault(
                        new TimeoutException($"Scope `{scope.Descriptor.Name}` exceeded dispose deadline."),
                        ScopeFaultPhase.Shutdown);
                    continue;
                }

                ScopeControlBarrier.EnsureSucceeded(response.State, "Dispose", scope);
            }
            catch (Exception ex)
            {
                scope.ReportFault(ex, ScopeFaultPhase.Shutdown);
            }
        }
    }

    private bool TryGetWorker(int scopeId, out ScopeWorker worker)
    {
        for (int i = 0; i < _workers.Length; i++)
        {
            if (_workers[i].Runtime.ScopeId == scopeId)
            {
                worker = _workers[i];
                return true;
            }
        }

        worker = null!;
        return false;
    }

    private static bool WaitForControl<T>(
        ScopeRuntime scope,
        LayerBase.Async.LBTask<T> task,
        in ShutdownDeadline deadline,
        string operationName,
        out T response)
    {
        var awaiter = task.GetAwaiter();

        while (!awaiter.IsCompleted)
        {
            if (scope.Options.Threading != ScopeThreadingMode.Worker)
            {
                scope.PumpIngress();

                if (awaiter.IsCompleted)
                    break;
            }

            if (deadline.IsExpired)
            {
                response = default!;
                return false;
            }

            Thread.Yield();
        }

        try
        {
            response = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            scope.ReportFault(ex, ScopeFaultPhase.Shutdown);
            response = default!;
            return false;
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
    }
}
