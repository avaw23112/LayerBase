using System.Diagnostics;

namespace LayerBase.Scope;

internal enum ScopeWorkerShutdownResult
{
    Stopped,
    TimedOut,
    AlreadyStopped
}

internal sealed class ScopeWorker : IDisposable
{
    private readonly ScopeRuntime _runtime;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private Exception? _startupException;
    private readonly AutoResetEvent _workSignal = new(initialState: false);
    private bool _startedThread;
    private bool _disposed;
    private int _startWaitCompleted;
    private int _threadExited;
    private int _resourcesReleased;
    private ScopeWorkerShutdownResult _shutdownResult;

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

    public ScopeWorkerShutdownResult ShutdownResult => _shutdownResult;

    internal ScopeRuntime Runtime => _runtime;

    public void Start(in ShutdownDeadline deadline)
    {
        if (_startedThread)
            return;

        _startedThread = true;
        _thread.Start();

        try
        {
            int remaining = deadline.RemainingMilliseconds;

            if (remaining <= 0 || !_ready.Wait(remaining))
            {
                throw new TimeoutException(
                    $"Scope worker `{_runtime.Descriptor.Name}` did not become ready before the build deadline.");
            }

            Exception? startupException =
                Volatile.Read(ref _startupException);

            if (startupException != null)
            {
                throw new InvalidOperationException(
                    $"Scope worker `{_runtime.Descriptor.Name}` failed during startup.",
                    startupException);
            }
        }
        finally
        {
            Volatile.Write(ref _startWaitCompleted, 1);
            TryReleaseResourcesAfterExit();
        }
    }

    internal ScopeWorkerShutdownResult Stop(in ShutdownDeadline deadline)
    {
        if (!_startedThread)
        {
            _runtime.DisposeUnstarted();
            ReleaseResources();
            return ScopeWorkerShutdownResult.AlreadyStopped;
        }

        try
        {
            _workSignal.Set();
        }
        catch (ObjectDisposedException)
        {
        }

        if (!_thread.IsAlive)
        {
            ReleaseResources();
            return ScopeWorkerShutdownResult.AlreadyStopped;
        }

        if (ReferenceEquals(Thread.CurrentThread, _thread))
        {
            return ScopeWorkerShutdownResult.TimedOut;
        }

        int remaining = deadline.RemainingMilliseconds;

        if (remaining <= 0 || !_thread.Join(remaining))
        {
            _thread.IsBackground = true;

            return ScopeWorkerShutdownResult.TimedOut;
        }

        ReleaseResources();

        return ScopeWorkerShutdownResult.Stopped;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        var deadline = ShutdownDeadline.Start(TimeSpan.FromSeconds(5));

        _shutdownResult = Stop(in deadline);
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
                _ready.Set();
                return;
            }

            ScopeTickOptions tick =
                _runtime.Options.Tick;

            RunWorkerLoop(in tick);
        }
        finally
        {
            try
            {
                if (_runtime.State !=
                    ScopeRuntimeState.Disposed)
                {
                    _runtime.RunRuntimeStop();
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(
                    previousContext);
                Volatile.Write(ref _threadExited, 1);
                TryReleaseResourcesAfterExit();
            }
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

        while (_runtime.State !=
               ScopeRuntimeState.Disposed &&
               _runtime.State !=
               ScopeRuntimeState.Faulted)
        {
            try
            {
                _runtime.PumpWorkerImmediateWork();

                if (_runtime.State ==
                    ScopeRuntimeState.Disposed)
                {
                    break;
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

                    continue;
                }

                if (_runtime.HasImmediateWork)
                    continue;

                int waitMilliseconds =
                    CalculateWaitMilliseconds(
                        now,
                        nextTickDeadline);

                _workSignal.WaitOne(
                    waitMilliseconds);
            }
            catch (Exception ex)
            {
                _runtime.ReportFault(
                    ex,
                    ScopeFaultPhase.WorkerLoop);
            }
        }
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