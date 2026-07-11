using System.Runtime.CompilerServices;
using LayerBase.DI;

namespace LayerBase.Scope;

public interface IGeneratedScopeServiceBinding
{
    void BindScope(ScopeRuntime ownerScope, int serviceId);
}

internal interface IServiceScopeBinding
{
    void BindScope(ScopeRuntime ownerScope, int serviceId);
}

internal static class ScopeServiceOwnerRegistry
{
    private static readonly ConditionalWeakTable<IService, ScopeRuntime> Owners = new();

    public static void Bind(IService service, ScopeRuntime ownerScope)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (ownerScope == null)
        {
            throw new ArgumentNullException(nameof(ownerScope));
        }

        Owners.Remove(service);
        Owners.Add(service, ownerScope);
    }

    public static bool TryGet(IService service, out ScopeRuntime ownerScope)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return Owners.TryGetValue(service, out ownerScope!);
    }

    public static void Unbind(IService service)
    {
        if (service == null)
        {
            return;
        }

        Owners.Remove(service);
    }
}
