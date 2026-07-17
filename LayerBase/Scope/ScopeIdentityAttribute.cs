namespace LayerBase.Scope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScopeIdentityAttribute : Attribute
{
    public ScopeIdentityAttribute(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Scope identity is required.", nameof(value));

        Value = value.Trim();
    }

    public string Value { get; }
}
