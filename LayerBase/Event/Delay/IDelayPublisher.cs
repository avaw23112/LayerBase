namespace LayerBase.Event.Delay
{
    /// <summary>
    /// Delay event cache reader contract.
    /// </summary>
    public interface IDelayPublisher
    {
        bool HasValue { get; }
    }

    public interface IDelayPublisher<T> : IDelayPublisher where T : struct
    {
        /// <summary>Try get the latest cached value without consuming it.</summary>
        bool TryGet(out T value);

        /// <summary>Try get the latest cached value and consume it on success.</summary>
        bool TryTake(out T value);

        /// <summary>Direction marker of the most recent published delay value.</summary>
        DelayDirection Direction { get; }
    }
}
