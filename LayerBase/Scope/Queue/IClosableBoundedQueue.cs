namespace LayerBase.Scope.Queue;

internal enum QueueEnqueueResult
{
    Accepted,
    Full,
    Closed
}

internal interface IClosableBoundedQueue<T>
{
    int Count { get; }
    int Capacity { get; }
    bool IsClosed { get; }
    QueueEnqueueResult TryEnqueue(in T item);
    bool TryDequeue(out T item);
    void Close();
    void CloseAndDrain(Action<T> drain);
}
