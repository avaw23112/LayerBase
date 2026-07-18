using System.Diagnostics;

namespace LayerBase.Scope;

internal readonly struct ShutdownDeadline
{
    private readonly long _deadlineTimestamp;

    private ShutdownDeadline(long deadlineTimestamp)
    {
        _deadlineTimestamp = deadlineTimestamp;
    }

    public bool IsExpired =>
        Stopwatch.GetTimestamp() >= _deadlineTimestamp;

    public int RemainingMilliseconds
    {
        get
        {
            long remaining = _deadlineTimestamp - Stopwatch.GetTimestamp();
            if (remaining <= 0)
                return 0;

            long milliseconds = remaining * 1000L / Stopwatch.Frequency;
            if (milliseconds <= 0)
                return 1;

            return milliseconds >= int.MaxValue
                ? int.MaxValue
                : (int)milliseconds;
        }
    }

    public static ShutdownDeadline Start(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            return new ShutdownDeadline(Stopwatch.GetTimestamp());

        double rawTicks = timeout.TotalSeconds * Stopwatch.Frequency;
        long timeoutTicks = rawTicks >= long.MaxValue
            ? long.MaxValue
            : Math.Max(1L, (long)rawTicks);

        long now = Stopwatch.GetTimestamp();
        long deadline;

        try
        {
            deadline = checked(now + timeoutTicks);
        }
        catch (OverflowException)
        {
            deadline = long.MaxValue;
        }

        return new ShutdownDeadline(deadline);
    }
}
