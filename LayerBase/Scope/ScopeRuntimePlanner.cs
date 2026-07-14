using System.Reflection;
using LayerBase.DI;

namespace LayerBase.Scope;

public sealed class ScopeRuntimePlan
{
    internal ScopeRuntimePlan(
        ScopeDescriptor descriptor,
        Type? scopeType,
        IService[] services,
        ScopeRuntimeOptions? runtimeOptions = null)
    {
        Descriptor = descriptor;
        ScopeType = scopeType;
        Services = services;
        RuntimeOptions = runtimeOptions ?? ScopeRuntimeOptions.Default;
    }

    public ScopeDescriptor Descriptor { get; }

    public Type? ScopeType { get; }

    public IService[] Services { get; }

    public ScopeRuntimeOptions RuntimeOptions { get; }
}

public readonly struct ScopeRuntimeServiceScopeInfo
{
    public ScopeRuntimeServiceScopeInfo(Type scopeType, ScopeDescriptor descriptor)
    {
        ScopeType = scopeType ?? throw new ArgumentNullException(nameof(scopeType));
        Descriptor = descriptor;
    }

    public Type ScopeType { get; }

    public ScopeDescriptor Descriptor { get; }
}

public delegate bool ScopeRuntimeServiceScopeResolver(
    Type serviceType,
    out ScopeRuntimeServiceScopeInfo scopeInfo);

public static class ScopeRuntimePlanner
{
    public static bool IsScopedServiceType(Type serviceType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return GetServiceScopeType(serviceType) != null;
    }

    public static IReadOnlyList<ScopeRuntimePlan> Build(IReadOnlyList<IService> services)
    {
        return Build(services, resolver: null);
    }

    public static IReadOnlyList<ScopeRuntimePlan> Build(
        IReadOnlyList<IService> services,
        ScopeRuntimeServiceScopeResolver? resolver)
    {
        return Build(services, resolver, mainScopeRuntimeOptions: null);
    }

    public static IReadOnlyList<ScopeRuntimePlan> Build(
        IReadOnlyList<IService> services,
        ScopeRuntimeServiceScopeResolver? resolver,
        ScopeRuntimeOptions? mainScopeRuntimeOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var mainServices = new List<IService>();
        var scopedServices = new List<ScopedServiceBucket>();
        var scopedIndexes = new Dictionary<Type, int>();

        for (int i = 0; i < services.Count; i++)
        {
            IService service = services[i] ?? throw new ArgumentException("Service list cannot contain null.", nameof(services));
            if (!TryGetServiceScopeInfo(service.GetType(), resolver, out ScopeRuntimeServiceScopeInfo scopeInfo))
            {
                mainServices.Add(service);
                continue;
            }

            Type scopeType = scopeInfo.ScopeType;
            if (!scopedIndexes.TryGetValue(scopeType, out int scopedIndex))
            {
                scopedIndex = scopedServices.Count;
                scopedIndexes.Add(scopeType, scopedIndex);
                ResolvedScopeOption scopeOption = scopeInfo.Descriptor.ScopeId == 0
                    ? CreateDefaultScopeOption(scopeType, scopedIndex + 1)
                    : ScopeOptionResolver.Resolve(scopeType, scopeInfo.Descriptor.ScopeId, scopeInfo.Descriptor);
                scopedServices.Add(new ScopedServiceBucket(scopeType, scopeOption.Descriptor, scopeOption.RuntimeOptions));
            }

            scopedServices[scopedIndex].Services.Add(service);
        }

        var plans = new List<ScopeRuntimePlan>(scopedServices.Count + 1)
        {
            new(
                ScopeDescriptors.Main,
                null,
                mainServices.ToArray(),
                mainScopeRuntimeOptions ?? ScopeOptionResolver.ResolveMain().RuntimeOptions)
        };

        for (int i = 0; i < scopedServices.Count; i++)
        {
            ScopedServiceBucket bucket = scopedServices[i];
            plans.Add(new ScopeRuntimePlan(
                bucket.Descriptor,
                bucket.ScopeType,
                bucket.Services.ToArray(),
                bucket.RuntimeOptions));
        }

        return plans;
    }

    private static bool TryGetServiceScopeInfo(
        Type serviceType,
        ScopeRuntimeServiceScopeResolver? resolver,
        out ScopeRuntimeServiceScopeInfo scopeInfo)
    {
        if (resolver != null && resolver(serviceType, out scopeInfo))
        {
            return true;
        }

        Type? scopeType = GetServiceScopeType(serviceType);
        if (scopeType == null)
        {
            scopeInfo = default;
            return false;
        }

        scopeInfo = new ScopeRuntimeServiceScopeInfo(
            scopeType,
            ScopeDescriptors.Main);
        return true;
    }

    private static Type? GetServiceScopeType(Type serviceType)
    {
        Type? scopeType = null;
        object[] attributes = serviceType.GetCustomAttributes(inherit: true);
        for (int i = 0; i < attributes.Length; i++)
        {
            Type attributeType = attributes[i].GetType();
            if (!attributeType.IsGenericType ||
                attributeType.GetGenericTypeDefinition() != typeof(ScopeAttribute<>))
            {
                continue;
            }

            if (scopeType != null)
            {
                throw new InvalidOperationException(
                    $"Service '{serviceType.FullName}' cannot declare more than one scope.");
            }

            scopeType = attributeType.GetGenericArguments()[0];
        }

        return scopeType;
    }

    private static ResolvedScopeOption CreateDefaultScopeOption(Type scopeType, int scopeId)
    {
        var options = scopeType.GetCustomAttribute<ScopeOptionsAttribute>(inherit: false);
        if (options == null)
        {
            return ScopeOptionResolver.ResolveDefault(scopeType, scopeId);
        }

        var descriptor = new ScopeDescriptor(
            scopeId,
            scopeType.Name,
            options.Threading,
            options.Clock,
            options.TickRateHz,
            options.StopPolicy);

        return ScopeOptionResolver.Resolve(scopeType, scopeId, descriptor);
    }

    private sealed class ScopedServiceBucket
    {
        public ScopedServiceBucket(
            Type scopeType,
            ScopeDescriptor descriptor,
            ScopeRuntimeOptions runtimeOptions)
        {
            ScopeType = scopeType;
            Descriptor = descriptor;
            RuntimeOptions = runtimeOptions;
        }

        public Type ScopeType { get; }

        public ScopeDescriptor Descriptor { get; }

        public ScopeRuntimeOptions RuntimeOptions { get; }

        public List<IService> Services { get; } = new();
    }
}
