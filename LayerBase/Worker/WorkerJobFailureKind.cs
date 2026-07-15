namespace LayerBase.Worker;

public enum WorkerJobFailureKind : byte
{
    ExecutionFault = 0,
    Cancelled = 1,
    ResultScopeEventRejected = 2,
    OriginScopeStopped = 3
}
