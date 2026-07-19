namespace LayerBase.DI;

public static class ServiceProviderExtensions
{
    public static T Get<T>(this IServiceProvider services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        return services.Get<T>();
    }
}
