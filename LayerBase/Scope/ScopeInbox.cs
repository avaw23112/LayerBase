namespace LayerBase.Scope;

public readonly struct ScopeCallInboxOptions
{
    public ScopeCallInboxOptions(int capacity, int reservedForResponseAndControl)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        if (reservedForResponseAndControl < 0 || reservedForResponseAndControl > capacity)
            throw new ArgumentOutOfRangeException(
                nameof(reservedForResponseAndControl),
                "Reserved capacity must be between zero and capacity.");

        Capacity = capacity;
        ReservedForResponseAndControl = reservedForResponseAndControl;
    }

    public int Capacity { get; }

    public int ReservedForResponseAndControl { get; }
}

public readonly struct ScopeEventInboxOptions
{
    public ScopeEventInboxOptions(int capacity, int reservedForInternal, int reservedForCritical)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        if (reservedForInternal < 0)
            throw new ArgumentOutOfRangeException(nameof(reservedForInternal), "Reserved capacity cannot be negative.");
        if (reservedForCritical < 0)
            throw new ArgumentOutOfRangeException(nameof(reservedForCritical), "Reserved capacity cannot be negative.");
        if (reservedForInternal + reservedForCritical > capacity)
            throw new ArgumentOutOfRangeException(
                nameof(reservedForInternal),
                "Combined reserved capacity cannot exceed capacity.");

        Capacity = capacity;
        ReservedForInternal = reservedForInternal;
        ReservedForCritical = reservedForCritical;
    }

    public int Capacity { get; }

    public int ReservedForInternal { get; }

    public int ReservedForCritical { get; }
}

internal enum ScopeAdmissionClass
{
    Business = 0,
    Internal = 1,
    Critical = 2,
    Response = 3,
    Control = 4
}

internal enum ScopeEnqueueResult
{
    Accepted = 0,
    Full = 1,
    BusinessClosed = 2,
    Closed = 3,
    StaleEndpoint = 4
}

internal interface IScopeInbox<T>
{
    ScopeEnqueueResult TryEnqueue(in T item, ScopeAdmissionClass admission);

    bool TryDequeue(out T item);

    void CloseBusinessAdmission();

    void CloseAllAdmission();
}

internal sealed class ScopeBoundedInbox<T> : IScopeInbox<T>
{
    private readonly object _gate = new();
    private readonly T[] _items;
    private readonly int _businessLimit;
    private readonly int _internalLimit;
    private int _head;
    private int _count;
    private long _accepted;
    private long _rejected;
    private int _highWatermark;
    private bool _businessClosed;
    private bool _allClosed;

    private ScopeBoundedInbox(int capacity, int businessLimit, int internalLimit)
    {
        _items = new T[capacity];
        _businessLimit = businessLimit;
        _internalLimit = internalLimit;
    }

    public static ScopeBoundedInbox<T> CreateCallInbox(ScopeCallInboxOptions options)
    {
        var businessLimit = options.Capacity - options.ReservedForResponseAndControl;
        return new ScopeBoundedInbox<T>(
            options.Capacity,
            businessLimit,
            options.Capacity);
    }

    public static ScopeBoundedInbox<T> CreateEventInbox(ScopeEventInboxOptions options)
    {
        var businessLimit = options.Capacity - options.ReservedForInternal - options.ReservedForCritical;
        var internalLimit = options.Capacity - options.ReservedForCritical;
        return new ScopeBoundedInbox<T>(
            options.Capacity,
            businessLimit,
            internalLimit);
    }

    public ScopeEnqueueResult TryEnqueue(in T item, ScopeAdmissionClass admission)
    {
        lock (_gate)
        {
            if (_allClosed)
            {
                _rejected++;
                return ScopeEnqueueResult.Closed;
            }

            if (admission == ScopeAdmissionClass.Business && _businessClosed)
            {
                _rejected++;
                return ScopeEnqueueResult.BusinessClosed;
            }

            var limit = GetLimit(admission);
            if (_count >= limit)
            {
                _rejected++;
                return ScopeEnqueueResult.Full;
            }

            var tail = (_head + _count) % _items.Length;
            _items[tail] = item;
            _count++;
            _accepted++;
            if (_count > _highWatermark)
                _highWatermark = _count;
            return ScopeEnqueueResult.Accepted;
        }
    }

    public ScopeInboxDiagnosticsSnapshot CaptureDiagnostics()
    {
        lock (_gate)
        {
            return new ScopeInboxDiagnosticsSnapshot(
                _count,
                _items.Length,
                _accepted,
                _rejected,
                _highWatermark);
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _items[_head];
            _items[_head] = default!;
            _head = (_head + 1) % _items.Length;
            _count--;
            return true;
        }
    }

    public void CloseBusinessAdmission()
    {
        lock (_gate)
        {
            _businessClosed = true;
        }
    }

    public void CloseAllAdmission()
    {
        lock (_gate)
        {
            _allClosed = true;
            _businessClosed = true;
        }
    }

    private int GetLimit(ScopeAdmissionClass admission)
    {
        return admission switch
        {
            ScopeAdmissionClass.Business => _businessLimit,
            ScopeAdmissionClass.Internal => _internalLimit,
            _ => _items.Length
        };
    }
}

internal readonly struct ScopeInboxDiagnosticsSnapshot
{
    public ScopeInboxDiagnosticsSnapshot(int count, int capacity, long accepted, long rejected, int highWatermark)
    {
        Count = count;
        Capacity = capacity;
        Accepted = accepted;
        Rejected = rejected;
        HighWatermark = highWatermark;
    }

    public int Count { get; }

    public int Capacity { get; }

    public long Accepted { get; }

    public long Rejected { get; }

    public int HighWatermark { get; }
}
