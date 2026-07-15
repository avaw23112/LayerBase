namespace LayerBase.Scope;

public enum ScopePostStatus
{
    Accepted = 0,
    RuntimeDisposed = 1,
    StaleEndpoint = 2,
    QueueFull = 3,
    Rejected = 4
}

public readonly struct ScopePostResult
{
    public ScopePostResult(ScopePostStatus status)
    {
        Status = status;
    }

    public ScopePostStatus Status { get; }

    public bool IsAccepted => Status == ScopePostStatus.Accepted;

    public static ScopePostResult Accepted => new(ScopePostStatus.Accepted);

    public static ScopePostResult RuntimeDisposed => new(ScopePostStatus.RuntimeDisposed);

    public static ScopePostResult StaleEndpoint => new(ScopePostStatus.StaleEndpoint);

    public static ScopePostResult QueueFull => new(ScopePostStatus.QueueFull);

    public static ScopePostResult Rejected => new(ScopePostStatus.Rejected);
}
