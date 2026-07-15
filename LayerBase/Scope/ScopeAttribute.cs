namespace LayerBase.Scope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeAttribute<TScope> : Attribute
    where TScope : IScopeDefinition
{
}
