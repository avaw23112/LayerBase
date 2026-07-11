namespace LayerBase.Modules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyModuleAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ModuleIgnoreAttribute : Attribute
{
}
