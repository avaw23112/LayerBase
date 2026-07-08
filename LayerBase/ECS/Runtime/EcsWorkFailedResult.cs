namespace LayerBase.ECS.Runtime;

internal sealed class EcsWorkFailedResult : IEcsResultItem
{
    private readonly Exception _exception;

    public EcsWorkFailedResult(string debugName, Exception exception)
    {
        DebugName = debugName;
        _exception = exception;
    }

    public string DebugName { get; }

    public void Apply(LayerRuntime runtime)
    {
        runtime.ReportLayerEventError(-1, "EcsWorker", DebugName, _exception);
    }
}
