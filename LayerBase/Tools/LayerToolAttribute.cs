using System;

namespace LayerBase.Tools;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LayerToolAttribute : Attribute
{
    public LayerToolAttribute(string toolId, Type layer, Type ownerScope)
    {
        ToolId = string.IsNullOrWhiteSpace(toolId)
            ? throw new ArgumentException("Layer tool id is required.", nameof(toolId))
            : toolId;
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        OwnerScope = ownerScope ?? throw new ArgumentNullException(nameof(ownerScope));
    }

    public string ToolId { get; }

    public Type Layer { get; }

    public Type OwnerScope { get; }

    public Type? Contract { get; set; }

    public string? DefaultKeyProperty { get; set; } = "Key";

    public bool AllowCache { get; set; } = true;
}
