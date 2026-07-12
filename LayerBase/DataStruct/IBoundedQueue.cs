namespace LayerBase.Core.DataStruct;

public interface IBoundedQueue<T>
{
    int Count { get; }

    int Capacity { get; }

    bool IsClosed { get; }

    bool TryEnqueue(T item);

    bool TryDequeue(out T item);

    void Close();

    void Clear();
}
