using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.Scope;

public enum ScopeObjectKind
{
    Service,
    Context
}

public readonly struct LayerMembership
{
    public static LayerMembership Empty { get; } = new(-1, 0);

    public LayerMembership(int start, int count)
    {
        Start = start;
        Count = count;
    }

    public int Start { get; }

    public int Count { get; }
}

public sealed class ScopeObjectBinding
{
    public ScopeObjectBinding(
        LayerRuntime?    runtime,
        ScopeRuntime     scope,
        int              serviceSlot,
        int              contextSlot,
        LayerMembership  membership,
        ScopeObjectKind  kind)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Runtime = runtime;
        ServiceSlot = serviceSlot;
        ContextSlot = contextSlot;
        Membership = membership;
        Kind = kind;
    }

    public LayerRuntime? Runtime { get; }

    public ScopeRuntime Scope { get; }

    public int RuntimeId => Runtime?.Id ?? -1;

    public int ScopeId => Scope.ScopeId;

    public int ServiceSlot { get; }

    public int ContextSlot { get; }

    public LayerMembership Membership { get; }

    public ScopeObjectKind Kind { get; }
}

public interface IScopeObjectBindingAccessor
{
    ScopeObjectBinding? __ScopeObjectBinding { get; set; }
}

public sealed class UnboundScopeObjectException : InvalidOperationException
{
    public UnboundScopeObjectException(Type objectType)
        : base($"Scope-bound object '{objectType?.FullName ?? "<unknown>"}' has not been attached to a ScopeRuntime.")
    {
    }
}

internal static class ScopeObjectBinder
{
    private static readonly ConditionalWeakTable<object, ScopeObjectBinding> Bindings = new();

    public static void Attach(object value, ScopeObjectBinding binding)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (binding == null)
        {
            throw new ArgumentNullException(nameof(binding));
        }

        if (value is IScopeObjectBindingAccessor accessor)
        {
            accessor.__ScopeObjectBinding = binding;
            return;
        }

        Bindings.Remove(value);
        Bindings.Add(value, binding);
    }

    public static void Detach(object value)
    {
        if (value == null)
        {
            return;
        }

        if (value is IScopeObjectBindingAccessor accessor)
        {
            accessor.__ScopeObjectBinding = null;
        }

        Bindings.Remove(value);
    }

    public static bool TryGet(object value, [NotNullWhen(true)] out ScopeObjectBinding? binding)
    {
        if (value == null)
        {
            binding = null;
            return false;
        }

        if (value is IScopeObjectBindingAccessor accessor &&
            accessor.__ScopeObjectBinding != null)
        {
            binding = accessor.__ScopeObjectBinding;
            return true;
        }

        return Bindings.TryGetValue(value, out binding);
    }

    public static ScopeObjectBinding? Get(object value)
    {
        return TryGet(value, out ScopeObjectBinding? binding)
            ? binding
            : null;
    }

    public static ScopeObjectBinding Require(object value)
    {
        if (TryGet(value, out ScopeObjectBinding? binding))
        {
            return binding;
        }

        throw new UnboundScopeObjectException(value.GetType());
    }
}
