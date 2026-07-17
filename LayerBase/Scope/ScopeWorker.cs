using System.Diagnostics;

namespace LayerBase.Scope;

internal sealed class ScopeWorker : IDisposable
{
    private readonly ScopeRuntime _runtime;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new(false);
    private readonly AutoResetEvent _workSignal = new(initialState: false);
    private bool _startedThread;
    private bool _disposed;

    public ScopeWorker(ScopeRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _runtime.BindWorkerWakeSignal(() => _workSignal.Set());
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = $"LayerBase.Scope.{runtime.Descriptor.Name}"
        };
    }

    public void Start()
    {
        if (_startedThread)
            return;

        _startedThread = true;
        _thread.Start();
        _started.Wait();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_startedThread && _runtime.State != ScopeRuntimeState.Disposed)
            _ = _runtime.RequestDisposeAsync();

        _workSignal.Set();

        if (_thread.IsAlive && !ReferenceEquals(Thread.CurrentThread, _thread))
            _thread.Join();

        if (!_startedThread)
            _runtime.Dispose();

        _workSignal.Dispose();
    }

    private void Run()
    {
        _started.Set();

        SynchronizationContext? previousContext =
            SynchronizationContext.Current;

        try
        {
            _runtime.InstallSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(
                _runtime.SynchronizationContext);

            ScopeTickOptions tick =
                _runtime.Options.Tick;

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
                   ScopeRuntimeState.Disposed)
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