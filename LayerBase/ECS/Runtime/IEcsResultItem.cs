namespace LayerBase.ECS.Runtime;

internal interface IEcsResultItem
{
    string DebugName { get; }

    void Apply(LayerRuntime runtime);
}
