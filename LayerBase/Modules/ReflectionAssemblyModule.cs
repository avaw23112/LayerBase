using System.Reflection;
using LayerBase.Call;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Modules;

public sealed class ReflectionAssemblyModule : IAssemblyModule
{
    private ReflectionAssemblyModule(AssemblyModuleId id, AssemblyModuleManifest manifest)
    {
        Id = id;
        Manifest = manifest;
    }

    public AssemblyModuleId Id { get; }

    public AssemblyModuleManifest Manifest { get; }

    public static ReflectionAssemblyModule Build(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var moduleId = new AssemblyModuleId(assembly.GetName().Name ?? assembly.FullName ?? nameof(ReflectionAssemblyModule));
        var services = new List<ServiceContribution>();
        var contexts = new List<ContextContribution>();
        var localCalls = new List<LocalCallContribution>();
        var eventHandlers = new List<EventHandlerContribution>();

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (ShouldIgnore(type) || !IsConcreteClass(type))
                continue;

            AddOwnerLayerContributions(type, services, localCalls);
            AddOwnerServiceContributions(type, contexts, eventHandlers);
        }

        return new ReflectionAssemblyModule(
            moduleId,
            new AssemblyModuleManifest(
                moduleId,
                services.ToArray(),
                contexts.ToArray(),
                localCalls.ToArray(),
                eventHandlers.ToArray(),
                Array.Empty<LayerToolContribution>()));
    }

    private static void AddOwnerLayerContributions(
        Type type,
        List<ServiceContribution> services,
        List<LocalCallContribution> localCalls)
    {
        var ownerLayers = GetOwnerLayerTypes(type);
        if (ownerLayers.Length == 0)
            return;

        var ownerScopeType = GetOwnerScopeType(type);
        foreach (var ownerLayerType in ownerLayers)
        {
            if (typeof(IService).IsAssignableFrom(type))
            {
                services.Add(ServiceContribution.ForTypes(
                    type,
                    type,
                    ownerLayerType,
                    ownerScopeType,
                    ServiceLifetime.Singleton));
            }

            foreach (var localCallInterface in GetClosedInterfaces(type, typeof(IScopeLocalCallHandler<,>)))
            {
                var arguments = localCallInterface.GetGenericArguments();
                localCalls.Add(LocalCallContribution.ForTypes(
                    arguments[0],
                    arguments[1],
                    type,
                    ownerLayerType,
                    ownerScopeType));
            }
        }
    }

    private static void AddOwnerServiceContributions(
        Type type,
        List<ContextContribution> contexts,
        List<EventHandlerContribution> eventHandlers)
    {
        var ownerServices = type.GetCustomAttributes<OwnerServiceAttribute>(false)
                                .Select(static attribute => attribute.ServiceType)
                                .Where(static serviceType => serviceType != null)
                                .Distinct()
                                .OrderBy(static serviceType => serviceType.FullName, StringComparer.Ordinal)
                                .ToArray();

        if (ownerServices.Length == 0)
            return;

        var handlerEventTypes = GetEventTypes(type).ToArray();
        var contributesContext = typeof(ILayerContext).IsAssignableFrom(type);
        if (!contributesContext && handlerEventTypes.Length == 0)
            return;

        foreach (var ownerServiceType in ownerServices)
        {
            if (!typeof(IService).IsAssignableFrom(ownerServiceType) || ShouldIgnore(ownerServiceType))
                continue;

            var ownerLayers = GetOwnerLayerTypes(ownerServiceType);
            if (ownerLayers.Length == 0)
                continue;

            var ownerScopeType = GetOwnerScopeType(ownerServiceType);
            foreach (var ownerLayerType in ownerLayers)
            {
                if (contributesContext)
                {
                    contexts.Add(ContextContribution.ForTypes(
                        type,
                        ownerServiceType,
                        ownerLayerType,
                        ownerScopeType));
                }

                foreach (var eventType in handlerEventTypes)
                {
                    eventHandlers.Add(EventHandlerContribution.ForTypes(
                        eventType,
                        type,
                        ownerServiceType,
                        ownerLayerType,
                        ownerScopeType));
                }
            }
        }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                           .OrderBy(static type => type.FullName, StringComparer.Ordinal)
                           .ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                     .Where(static type => type != null)
                     .Cast<Type>()
                     .OrderBy(static type => type.FullName, StringComparer.Ordinal)
                     .ToArray();
        }
    }

    private static Type[] GetOwnerLayerTypes(Type type)
    {
        return type.GetCustomAttributes<OwnerLayerAttribute>(false)
                   .Select(static attribute => attribute.LayerType)
                   .Where(static layerType => layerType != null)
                   .Distinct()
                   .OrderBy(static layerType => layerType.FullName, StringComparer.Ordinal)
                   .ToArray();
    }

    private static Type GetOwnerScopeType(Type type)
    {
        foreach (var attribute in type.GetCustomAttributes(false))
        {
            var attributeType = attribute.GetType();
            if (attributeType.IsGenericType &&
                attributeType.GetGenericTypeDefinition() == typeof(ScopeAttribute<>))
            {
                return attributeType.GetGenericArguments()[0];
            }
        }

        return typeof(MainScope);
    }

    private static IEnumerable<Type> GetEventTypes(Type type)
    {
        foreach (var handlerInterface in GetClosedInterfaces(type, typeof(IEventHandler<>)))
            yield return handlerInterface.GetGenericArguments()[0];

        foreach (var handlerInterface in GetClosedInterfaces(type, typeof(IEventHandlerAsync<>)))
            yield return handlerInterface.GetGenericArguments()[0];
    }

    private static IEnumerable<Type> GetClosedInterfaces(Type type, Type openGenericInterface)
    {
        return type.GetInterfaces()
                   .Where(candidate => candidate.IsGenericType &&
                                       candidate.GetGenericTypeDefinition() == openGenericInterface)
                   .OrderBy(static candidate => candidate.FullName, StringComparer.Ordinal);
    }

    private static bool IsConcreteClass(Type type)
    {
        return type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false };
    }

    private static bool ShouldIgnore(Type type)
    {
        return type.IsDefined(typeof(ModuleIgnoreAttribute), false);
    }
}
