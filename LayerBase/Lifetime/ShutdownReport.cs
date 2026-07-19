namespace LayerBase.Lifetime;

internal readonly struct ShutdownReport
{
    public ShutdownReport(
        LifetimeState state,
        bool hasCleanupErrors,
        AggregateException? cleanupException)
    {
        State = state;
        HasCleanupErrors = hasCleanupErrors;
        CleanupException = cleanupException;
    }

    public LifetimeState State { get; }

    public bool HasCleanupErrors { get; }

    public AggregateException? CleanupException { get; }

    public bool IsDrained => State == LifetimeState.Released ||
                             State == LifetimeState.ReleasedWithErrors;
}
