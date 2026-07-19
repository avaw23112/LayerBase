using LayerBase.Async;

namespace LayerBase.Scope;

internal static class ScopeControlBarrier
{
    public static T Wait<T>(
        LBTask<T> task,
        in ShutdownDeadline deadline,
        string operation)
        where T : struct
    {
        var awaiter = task.GetAwaiter();
        var spinner = new SpinWait();

        while (!awaiter.IsCompleted)
        {
            if (deadline.IsExpired)
            {
                throw new TimeoutException(
                    $"Scope control operation `{operation}` exceeded its deadline.");
            }

            spinner.SpinOnce();
        }

        return awaiter.GetResult();
    }

    public static void EnsureSucceeded(
        ScopeControlResult result,
        string operation,
        ScopeRuntime scope)
    {
        if (result == ScopeControlResult.Succeeded)
            return;

        throw new InvalidOperationException(
            $"Scope lifecycle operation `{operation}` failed for " +
            $"`{scope.Descriptor.Name}` with result `{result}`.");
    }
}
