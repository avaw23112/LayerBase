using System;

namespace LayerBase.Tools;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class LayerToolAttribute : Attribute
{
    public LayerToolAttribute(Type ownerLayerType, Type contractType, string localKey = "default")
    {
        OwnerLayerType = ownerLayerType ?? throw new ArgumentNullException(nameof(ownerLayerType));
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        LocalKey = string.IsNullOrWhiteSpace(localKey)
            ? throw new ArgumentException("Layer tool local key is required.", nameof(localKey))
            : localKey;
    }

    public Type OwnerLayerType { get; }

    public Type ContractType { get; }

    public string LocalKey { get; }

    public bool Cache { get; set; } = true;
}
