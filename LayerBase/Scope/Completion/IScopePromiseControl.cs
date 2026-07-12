using System;

namespace LayerBase.Scope.Completion;

internal interface IScopePromiseControl
{
    bool TrySetResult(object? result);
    bool TrySetException(Exception exception);
    bool IsCompleted { get; }
    bool IsCancelled { get; }
}
