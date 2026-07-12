namespace LayerBase.Scope.Lifecycle;

internal enum ScopeRuntimeState
{
    Created = 0,
    Starting = 1,
    Running = 2,
    StopRequested = 3,
    Stopping = 4,
    Stopped = 5,
    Disposing = 6,
    Disposed = 7
}
