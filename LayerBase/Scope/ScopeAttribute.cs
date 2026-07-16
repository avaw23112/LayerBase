namespace LayerBase.Scope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ScopeAttribute : Attribute
{
    public ScopeAttribute(Type scopeType)
    {
        ScopeType = scopeType ?? throw new ArgumentNullException(nameof(scopeType));
    }

    public Type ScopeType { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeAttribute<TScope> : ScopeAttribute
    where TScope : IScopeDefinition
{
    public ScopeAttribute()
        : base(typeof(TScope))
    {
    }
}
