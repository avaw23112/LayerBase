namespace LayerBase.Scope;

public sealed class ScopeResourceClosedException : InvalidOperationException
{
    public ScopeResourceClosedException(string message) : base(message)
    {
    }
}

public sealed class ScopeResourceGenerationException : InvalidOperationException
{
    public ScopeResourceGenerationException(string message) : base(message)
    {
    }
}
