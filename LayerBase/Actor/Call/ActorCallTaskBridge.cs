using LayerBase.Async;

namespace LayerBase.Actor;

internal static class ActorCallTaskBridge
{
    public static void Forward<TResponse>(
        LBTask<TResponse> task,
        LBTaskCompletionSource<TResponse> target)
        where TResponse : struct
    {
        var awaiter = task.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            CompleteImmediately(awaiter, target);
            return;
        }

        awaiter.OnCompleted(() =>
        {
            CompleteImmediately(task.GetAwaiter(), target);
        });
    }

    private static void CompleteImmediately<TResponse>(
        LBTask<TResponse>.Awaiter awaiter,
        LBTaskCompletionSource<TResponse> target)
        where TResponse : struct
    {
        try
        {
            target.SetResult(awaiter.GetResult());
        }
        catch (OperationCanceledException exception)
        {
            target.SetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            target.SetException(exception);
        }
    }
}
