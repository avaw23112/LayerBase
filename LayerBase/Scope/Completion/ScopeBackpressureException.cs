using System;

namespace LayerBase.Scope.Completion;

public sealed class ScopeBackpressureException : InvalidOperationException
{
    public ScopeBackpressureException(string message)
        : base(message)
    {
    }
}
