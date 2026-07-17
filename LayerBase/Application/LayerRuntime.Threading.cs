using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    private int _ownerThreadId;

    internal void BindOwnerThreadForBuild()
    {
        int currentThreadId = Environment.CurrentManagedThreadId;

        int existing = Interlocked.CompareExchange(
            ref _ownerThreadId,
            currentThreadId,
            comparand: 0);

        if (existing != 0 && existing != currentThreadId)
        {
            throw new InvalidOperationException(
                $"LayerRuntime {Id} build must run on owner thread {existing}, " +
                $"but current thread is {currentThreadId}.");
        }
    }

    [Conditional("DEBUG")]
    internal void RequireOwnerThreadDebug([CallerMemberName] string memberName = "")
    {
        int ownerThreadId = Volatile.Read(ref _ownerThreadId);

        int currentThreadId = Environment.CurrentManagedThreadId;

        if (ownerThreadId == 0)
        {
            Interlocked.CompareExchange(
                ref _ownerThreadId,
                currentThreadId,
                comparand: 0);

            ownerThreadId = Volatile.Read(ref _ownerThreadId);
        }

        if (ownerThreadId != currentThreadId)
        {
            throw new InvalidOperationException(
                $"LayerRuntime {Id}.{memberName} is owner-thread-only. " +
                $"OwnerThread={ownerThreadId}, CurrentThread={currentThreadId}.");
        }
    }
}
