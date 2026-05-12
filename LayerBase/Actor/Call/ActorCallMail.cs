using LayerBase.Async;

namespace LayerBase.Actor;

internal readonly struct ActorCallMail<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
    public readonly TRequest Request;
    public readonly CancellationToken CancellationToken;
    public readonly LBTaskCompletionSource<TResponse> Source;

    public ActorCallMail(
        in TRequest                       request,
        CancellationToken                 cancellationToken,
        LBTaskCompletionSource<TResponse> source)
    {
        Request = request;
        CancellationToken = cancellationToken;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
}