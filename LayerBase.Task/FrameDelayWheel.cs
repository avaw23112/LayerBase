namespace LayerBase.Async;

internal sealed class FrameDelayWheel<T>
{
    private const int WheelSize = 256;
    private const int WheelMask = WheelSize - 1;

    private readonly List<ScheduledItem>?[] _buckets =
        new List<ScheduledItem>?[WheelSize];

    private readonly Action<T> _ready;
    private long _currentFrame;
    private int _count;

    public FrameDelayWheel(Action<T> ready)
    {
        _ready = ready
            ?? throw new ArgumentNullException(nameof(ready));
    }

    public int Count => _count;

    public void Schedule(T value, int frames)
    {
        int normalizedFrames = Math.Max(frames, 1);
        long dueFrame = _currentFrame + normalizedFrames;
        int bucketIndex = (int)(dueFrame & WheelMask);

        List<ScheduledItem> bucket =
            _buckets[bucketIndex] ??=
                new List<ScheduledItem>();

        bucket.Add(new ScheduledItem(dueFrame, value));
        _count++;
    }

    public void Advance()
    {
        _currentFrame++;

        int bucketIndex =
            (int)(_currentFrame & WheelMask);

        List<ScheduledItem>? bucket =
            _buckets[bucketIndex];

        if (bucket == null || bucket.Count == 0)
            return;

        for (int i = bucket.Count - 1; i >= 0; i--)
        {
            ScheduledItem item = bucket[i];

            if (item.DueFrame > _currentFrame)
                continue;

            int lastIndex = bucket.Count - 1;
            bucket[i] = bucket[lastIndex];
            bucket.RemoveAt(lastIndex);
            _count--;

            _ready(item.Value);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _buckets.Length; i++)
            _buckets[i]?.Clear();

        _count = 0;
    }

    private readonly struct ScheduledItem
    {
        public ScheduledItem(long dueFrame, T value)
        {
            DueFrame = dueFrame;
            Value = value;
        }

        public long DueFrame { get; }

        public T Value { get; }
    }
}
