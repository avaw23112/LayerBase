using System;

namespace LayerBase.Tools;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LayerToolAttribute : Attribute
{
    public LayerToolAttribute(string toolId)
    {
        ToolId = string.IsNullOrWhiteSpace(toolId)
            ? throw new ArgumentException("Layer tool id is required.", nameof(toolId))
            : toolId;
    }

    public string ToolId { get; }

    public Type? Contract { get; set; }

    public string? DefaultKeyProperty { get; set; } = "Key";

    public bool AllowCache { get; set; } = true;
}
