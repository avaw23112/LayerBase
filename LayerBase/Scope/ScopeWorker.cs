using System.Diagnostics;

namespace LayerBase.Scope;

internal enum ScopeWorkerStartState : byte
{
    Created,
    Starting,
    Running,
    StartFailed,
    Exited
}

internal sealed class ScopeWorker : IDisposable
{
    private const int MaxConsecutiveWorkerLoopFaults = 3;

    private readonly ScopeRuntime _runtime;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private Exception? _startupException;
    private readonly AutoResetEvent _workSignal = new(initialState: false);
    private bool _startedThread;
    private int _startWaitCompleted;
    private int _threadExited;
    private int _resourcesReleased;
    private int _consecutiveWorkerLoopFaults;
    private int _exitRequested;
    private ScopeWorkerStartState _startState;

    public ScopeWorker(ScopeRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _runtime.BindWorkerWakeSignal(SignalWork);
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"LayerBase.Scope.{runtime.Descriptor.Name}"
        };
    }

    public ScopeWorkerStartState StartState => _startState;

    internal ScopeRuntime Runtime => _runtime;

    public void Start(in ShutdownDeadline deadline)
    {
        if (_startedThread)
            return;

        _startState = ScopeWorkerStartState.Starting;
        _startedThread = true;
        _thread.Start();

        try
        {
            int remaining = deadline.RemainingMilliseconds;

            if (remaining <= 0 || !_ready.Wait(remaining))
            {
                _startState = ScopeWorkerStartState.StartFailed;
                throw new TimeoutException(
                    $"Scope worker `{_runtime.Descriptor.Name}` did not become ready before the build deadline.");
            }

            Exception? startupException =
                Volatile.Read(ref _startupException);

            if (startupException != null)
            {
                _startState = ScopeWorkerStartState.StartFailed;
                throw new InvalidOperationException(
                    $"Scope worker `{_runtime.Descriptor.Name}` failed during startup.",
                    startupException);
            }

            _startState = ScopeWorkerStartState.Running;
        }
        finally
        {
            Volatile.Write(ref _startWaitCompleted, 1);
            TryReleaseResourcesAfterExit();
        }
    }

    internal void RequestExitAfterScopeStopped()
    {
        if (_runtime.State != ScopeRuntimeState.Stopped)
        {
            throw new InvalidOperationException(
                "Worker exit requires a stopped Scope.");
        }

        if (Interlocked.Exchange(ref _exitRequested, 1) == 0)
            SignalWork();
    }

    internal bool WaitForExit(in ShutdownDeadline deadline)
    {
        if (!_startedThread)
        {
            _runtime.DisposeUnstarted();
            ReleaseResources();
            return true;
        }
        if (!_thread.IsAlive)
        {
            _startState = ScopeWorkerStartState.Exited;
            ReleaseResources();
            return true;
        }

        if (ReferenceEquals(Thread.CurrentThread, _thread))
        {
            return false;
        }

        int remaining = deadline.RemainingMilliseconds;

        if (remaining <= 0 || !_thread.Join(remaining))
        {
            _thread.IsBackground = true;
            return false;
        }   

        _startState = ScopeWorkerStartState.Exited;
        ReleaseResources();

        return true;
    }

    public void Dispose()
    {
        if (_startedThread && Volatile.Read(ref _threadExited) == 0)
        {
            throw new InvalidOperationException(
                "ScopeWorker cannot be disposed before thread exit.");
        }

        ReleaseResources();
    }

    internal bool ResourcesReleased =>
        Volatile.Read(ref _resourcesReleased) != 0;

    private void SignalWork()
    {
        if (Volatile.Read(ref _resourcesReleased) != 0)
            return;

        try
        {
            _workSignal.Set();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void TryReleaseResourcesAfterExit()
    {
        if (Volatile.Read(ref _startWaitCompleted) == 0 ||
            Volatile.Read(ref _threadExited) == 0)
        {
            return;
        }

        ReleaseResources();
    }

    internal void ForceReleaseResources()
    {
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        if (Interlocked.Exchange(ref _resourcesReleased, 1) != 0)
            return;

        _ready.Dispose();
        _workSignal.Dispose();
    }

    private void Run()
    {
        SynchronizationContext? previousContext =
            SynchronizationContext.Current;

        try
        {
            try
            {
                _runtime.InstallSynchronizationContext();

                SynchronizationContext.SetSynchronizationContext(
                    _runtime.SynchronizationContext);
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _startupException, ex);
                _startState = ScopeWorkerStartState.StartFailed;
                _ready.Set();
                return;
            }

            ScopeTickOptions tick =
                _runtime.Options.Tick;

            RunWorkerLoop(in tick);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(
                previousContext);
            Volatile.Write(ref _threadExited, 1);
            _startState = ScopeWorkerStartState.Exited;
            TryReleaseResourcesAfterExit();
        }
    }

    private void RunWorkerLoop(in ScopeTickOptions tick)
    {
        _ready.Set();

        long intervalTimestampTicks =
            CalculateIntervalTimestampTicks(
                tick.RateHz);

        long nextTickDeadline =
            Stopwatch.GetTimestamp();

        float fixedDeltaTime =
            tick.RateHz > 0
                ? 1f / tick.RateHz
                : 0f;

        while (true)
        {
            try
            {
                switch (_runtime.State)
                {
                    case ScopeRuntimeState.Created:
                    case ScopeRuntimeState.Ready:
                        _runtime.PumpIngress();
                        if (!_runtime.HasImmediateWork)
                            _workSignal.WaitOne(1);
                        break;

                    case ScopeRuntimeState.Running:
                        PumpRunningScope(
                            in tick,
                            intervalTimestampTicks,
                            fixedDeltaTime,
                            ref nextTickDeadline);
                        break;

                    case ScopeRuntimeState.StopRequested:
                    case ScopeRuntimeState.Draining:
                        _runtime.PumpTerminalDrainStep();
                        if (!_runtime.HasImmediateWork)
                            _workSignal.WaitOne(1);
                        break;

                    case ScopeRuntimeState.Stopped:
                        _runtime.PumpIngress();
                        if (_runtime.State == ScopeRuntimeState.Disposed)
                            return;

                        if (Volatile.Read(ref _exitRequested) == 0)
                        {
                            _workSignal.WaitOne();
                            break;
                        }

                        _runtime.DisposeStoppedOnOwnerThread();
                        return;

                    case ScopeRuntimeState.Disposed:
                    case ScopeRuntimeState.Faulted:
                        return;

                    default:
                        _runtime.PumpWorkerImmediateWork();
                        if (!_runtime.HasImmediateWork)
                            _workSignal.WaitOne(1);
                        break;
                }

                _consecutiveWorkerLoopFaults = 0;
            }
            catch (Exception ex)
            {
                _consecutiveWorkerLoopFaults++;
                if (_consecutiveWorkerLoopFaults >= MaxConsecutiveWorkerLoopFaults)
                {
                    _runtime.ReportFatalFault(
                        ex,
                        ScopeFaultPhase.WorkerLoop);
                    break;
                }

                _runtime.ReportFault(
                    ex,
                    ScopeFaultPhase.WorkerLoop);
            }
        }
    }

    private void PumpRunningScope(
        in ScopeTickOptions tick,
        long intervalTimestampTicks,
        float fixedDeltaTime,
        ref long nextTickDeadline)
    {
        _runtime.PumpWorkerImmediateWork();

        if (_runtime.State != ScopeRuntimeState.Running)
        {
            return;
        }

        long now =
            Stopwatch.GetTimestamp();

        if (now >= nextTickDeadline)
        {
            PumpDueTicks(
                in tick,
                intervalTimestampTicks,
                fixedDeltaTime,
                ref nextTickDeadline);

            return;
        }

        if (_runtime.HasImmediateWork)
            return;

        int waitMilliseconds =
            CalculateWaitMilliseconds(
                now,
                nextTickDeadline);

        _workSignal.WaitOne(
            waitMilliseconds);
    }

    private static long CalculateIntervalTimestampTicks(
        int tickRateHz)
    {
        if (tickRateHz <= 0)
            return long.MaxValue;

        return Math.Max(
            1L,
            Stopwatch.Frequency / tickRateHz);
    }

    private static int CalculateWaitMilliseconds(
        long now,
        long deadline)
    {
        long remaining =
            deadline - now;

        if (remaining <= 0)
            return 0;

        long numerator;

        try
        {
            numerator = checked(
                remaining * 1000L +
                Stopwatch.Frequency -
                1L);
        }
        catch (OverflowException)
        {
            return int.MaxValue;
        }

        long milliseconds =
            numerator / Stopwatch.Frequency;

        if (milliseconds <= 0)
            return 1;

        return milliseconds >= int.MaxValue
            ? int.MaxValue
            : (int)milliseconds;
    }

    private void PumpDueTicks(
        in ScopeTickOptions tick,
        long intervalTimestampTicks,
        float fixedDeltaTime,
        ref long nextTickDeadline)
    {
        int executionLimit =
            tick.OverrunPolicy ==
            ScopeTickOverrunPolicy.CatchUpLimited
                ? tick.MaxCatchUpTicks
                : 1;

        int executed = 0;
        long now = Stopwatch.GetTimestamp();

        while (now >= nextTickDeadline &&
               executed < executionLimit &&
               _runtime.State !=
                   ScopeRuntimeState.Disposed)
        {
            _runtime.PumpWorkerScheduledTick(
                fixedDeltaTime);

            nextTickDeadline +=
                intervalTimestampTicks;

            executed++;
            now = Stopwatch.GetTimestamp();
        }

        if (now < nextTickDeadline)
            return;

        long overdue =
            now - nextTickDeadline;

        long skippedIntervals =
            overdue /
            intervalTimestampTicks + 1L;

        nextTickDeadline +=
            skippedIntervals *
            intervalTimestampTicks;
    }
}
