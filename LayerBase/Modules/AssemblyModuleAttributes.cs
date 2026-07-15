namespace LayerBase.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyModuleAttribute : Attribute
{
    public AssemblyModuleAttribute(string? id = null)
    {
        Id = id;
    }

    public string? Id { get; }
}

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ModuleIgnoreAttribute : Attribute
{
}
