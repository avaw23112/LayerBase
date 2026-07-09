namespace LayerBase.Tooling;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LayerToolAttribute : Attribute
{
    public LayerToolAttribute(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            throw new ArgumentException("Tool id cannot be null or empty.", nameof(toolId));
        }

        ToolId = toolId;
    }

    public string ToolId { get; }

    public Type? Contract { get; set; }

    public string? DefaultKeyProperty { get; set; } = "Key";

    public bool AllowCache { get; set; } = true;
}
