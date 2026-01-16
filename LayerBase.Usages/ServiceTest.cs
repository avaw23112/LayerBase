using LayerBase.DI;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace LayerBase.Usages
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var layer = new ServiceDemoLayer();

            LayerHub.LayerHub.CreateLayers()
                    .Push(layer)
                    .Build();

            var greeter = layer.GetService<IGreetingService>();
            Console.WriteLine(greeter.Greet("LayerBase DI"));

            var counter = layer.GetService<ICounterService>();
            Console.WriteLine($"Scoped counter -> {counter.Next()}, {counter.Next()}");
            Console.WriteLine($"Scoped reuse -> {ReferenceEquals(counter, layer.GetService<ICounterService>())}");
            Console.WriteLine($"Transient greeter reuse -> {ReferenceEquals(greeter, layer.GetService<IGreetingService>())}");

            layer.Dispose();
        }
    }

    internal partial class ServiceDemoLayer : Layer
    {
    }

    [OwnerLayer(typeof(ServiceDemoLayer))]
    internal sealed class DemoServiceModule : IService
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITimeProvider, SystemTimeProvider>();
            services.AddScoped<ICounterService, CounterService>();
            services.AddTransient<IGreetingService, GreetingService>();
        }
    }

    internal interface IGreetingService
    {
        string Greet(string name);
    }

    internal sealed class GreetingService : IGreetingService
    {
        private readonly ITimeProvider _timeProvider;
        private readonly ICounterService _counter;

        public GreetingService(ITimeProvider timeProvider, ICounterService counter)
        {
            _timeProvider = timeProvider;
            _counter = counter;
        }

        public string Greet(string name)
        {
            var count = _counter.Next();
            return $"[{_timeProvider.Now:T}] #{count} Hello, {name}!";
        }
    }

    internal interface ICounterService
    {
        int Next();
    }

    internal sealed class CounterService : ICounterService
    {
        private int _count;

        public int Next() => ++_count;
    }

    internal interface ITimeProvider
    {
        DateTime Now { get; }
    }

    internal sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
