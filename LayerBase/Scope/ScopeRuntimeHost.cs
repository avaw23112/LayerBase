using LayerBase.Async;
using LayerBase.Actor;

namespace LayerBase.Scope;

internal sealed class ScopeRuntimeHost : IDisposable
{
    private readonly ScopeRuntimeDirectory _directory;
    private readonly ScopeRuntime[] _inlineScopes;
    private readonly ScopeWorker[] _workers;
    private bool _workersStarted;
    private int _shutdownStarted;
    private int _disposed;
    private int _nextInlineScopeIndex;

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
            scopes[i].BindHost(this);
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
        try
        {
            for (int i = 0; i < plans.Count; i++)
            {
                scopes[i] = new ScopeRuntime(runtime, plans[i], runtimeId, generation);
                if (plans[i].Options.Threading == ScopeThreadingMode.Worker)
                    workers.Add(new ScopeWorker(scopes[i]));
            }

            var directory = new ScopeRuntimeDirectory(scopes);
            return new ScopeRuntimeHost(directory, workers.ToArray());
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

        if (_workersStarted)
            return;

        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i].Start(in deadline);
        }

        _workersStarted = true;
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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
            return;

        var deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(15));

        try
        {
            if (!_workersStarted)
            {
                DisposeUnstartedScopes();
                return;
            }

            RequestStopForAllScopes(in deadline);

            RequestDisposeForAllScopes(in deadline);

            StopWorkers(in deadline);
        }
        finally
        {
            Volatile.Write(ref _disposed, 1);
        }
    }

    private void RequestStopForAllScopes(in ShutdownDeadline deadline)
    {
        ScopeRuntime[] runtimes = _directory.Runtimes;

        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            ScopeRuntime scope = runtimes[i];

            if (scope.State is
                ScopeRuntimeState.Stopped or
                ScopeRuntimeState.Disposed)
            {
                continue;
            }

            if (scope.Options.Threading != ScopeThreadingMode.Worker)
            {
                scope.StopOnOwnerThread();
            }
            else
            {
                var task = scope.RequestStopAsync();
                WaitForControl(scope, task, in deadline);
            }
        }
    }

    private void DisposeUnstartedScopes()
    {
        ScopeRuntime[] runtimes = _directory.Runtimes;

        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            runtimes[i].DisposeUnstarted();
        }

        for (int i = _workers.Length - 1; i >= 0; i--)
        {
            _workers[i].Stop(ShutdownDeadline.Start(TimeSpan.Zero));
        }
    }

    private void RequestDisposeForAllScopes(in ShutdownDeadline deadline)
    {
        ScopeRuntime[] runtimes = _directory.Runtimes;

        for (int i = runtimes.Length - 1; i >= 0; i--)
        {
            ScopeRuntime scope = runtimes[i];

            if (scope.State == ScopeRuntimeState.Disposed)
                continue;

            if (scope.Options.Threading != ScopeThreadingMode.Worker)
            {
                scope.Dispose();
            }
            else
            {
                try
                {
                    ScopeDisposeResponse response =
                        ScopeControlBarrier.Wait(
                            scope.RequestDisposeAsync(),
                            in deadline,
                            $"{scope.Descriptor.Name}.Dispose");

                    ScopeControlBarrier.EnsureSucceeded(
                        response.State,
                        "Dispose",
                        scope);
                }
                catch (Exception ex)
                {
                    scope.ReportFatalFault(ex, ScopeFaultPhase.WorkerLoop);
                }
            }
        }
    }

    private bool StopWorkers(in ShutdownDeadline deadline)
    {
        bool anyTimedOut = false;

        for (int i = _workers.Length - 1; i >= 0; i--)
        {
            ScopeWorkerShutdownResult result = _workers[i].Stop(in deadline);

            if (result == ScopeWorkerShutdownResult.TimedOut)
            {
                anyTimedOut = true;

                ScopeRuntime runtime = _workers[i].Runtime;

                runtime.ReportFatalFault(
                    new TimeoutException(
                        $"Scope worker `{runtime.Descriptor.Name}` exceeded the shutdown deadline."),
                    ScopeFaultPhase.WorkerLoop);
            }
        }

        return anyTimedOut;
    }

    private static bool WaitForControl<T>(
        ScopeRuntime scope,
        LayerBase.Async.LBTask<T> task,
        in ShutdownDeadline deadline)
    {
        var awaiter = task.GetAwaiter();

        while (!awaiter.IsCompleted)
        {
            if (scope.Options.Threading == ScopeThreadingMode.Inline)
            {
                scope.PumpIngress();

                if (awaiter.IsCompleted)
                    break;
            }

            if (deadline.IsExpired)
                return false;

            Thread.Yield();
        }

        try
        {
            _ = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            scope.ReportFatalFault(ex, ScopeFaultPhase.Shutdown);
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(ScopeRuntimeHost));
    }
}
