namespace LayerBase.Lifetime;

internal sealed class TerminalCleanupRunner
{
    private readonly List<Exception> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public void Run(string resourceName, Action cleanup)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            _errors.Add(new ResourceCleanupException(resourceName, exception));
        }
    }

    public AggregateException? BuildException()
    {
        return _errors.Count == 0
            ? null
            : new AggregateException(_errors);
    }
}

internal sealed class ResourceCleanupException : Exception
{
    public ResourceCleanupException(string resourceName, Exception inner)
        : base($"Resource cleanup failed for `{resourceName}`.", inner)
    {
        ResourceName = resourceName;
    }

    public string ResourceName { get; }
}
