namespace LayerBase.ECS;

[AttributeUsage(
    AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class InputAttribute : Attribute
{
}
