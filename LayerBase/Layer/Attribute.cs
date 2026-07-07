namespace LayerBase.Layers;

/// <summary>
/// 标记一个 Layer 类型的所属关系，允许多个 Layer 类型共享同一个标记。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class OwnerLayerAttribute : Attribute
{
    public OwnerLayerAttribute(Type layerType)
    {
        LayerType = layerType;
    }

    public Type LayerType { get; }
}