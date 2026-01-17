using LayerBase.DI;
using LayerBase.Layers;
using NUnit.Framework.Legacy;

namespace EventsTest;

public class ServiceRegistrationTests
{
	[Test]
	public void GeneratedServices_are_resolvable_with_expected_lifetimes()
	{
		var layer = new ServiceDemoLayer();
		try
		{
			layer.Build();

			var greeter1 = layer.GetService<IGreetingService>();
			var greeter2 = layer.GetService<IGreetingService>();
			var greeting = greeter1.Greet("Tests");

			StringAssert.Contains("Tests", greeting);
			Assert.That(greeter2, Is.Not.SameAs(greeter1), "Transient service should create new instance");

			var counter1 = layer.GetService<ICounterService>();
			var counter2 = layer.GetService<ICounterService>();

			Assert.That(counter1, Is.SameAs(counter2), "Scoped service should reuse instance within layer");
			// GreetingService already increments the scoped counter once
			Assert.That(counter1.Next(), Is.EqualTo(2));
			Assert.That(counter1.Next(), Is.EqualTo(3));
		}
		finally
		{
			layer.Dispose();
		}
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
