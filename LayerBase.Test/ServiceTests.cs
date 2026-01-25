using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.LayerHub;
using LayerBase.Layers;
using LayerBase.Tools.Timer;
using NUnit.Framework.Legacy;

namespace EventsTest;

public class ServiceRegistrationTests
{
	[SetUp]
	public void SetUp()
	{
		LayerHub.Reset();
	}

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

	[Test]
	public void IService_can_access_layer_and_dispatch_events()
	{
		var layer = new ServiceEventLayer();

		LayerHub.CreateLayers().Push(layer).Build();

		var emitter = layer.GetService<IServiceEventEmitter>();
		emitter.Emit(42);

		LayerHub.Pump(0.02f);

		Assert.That(layer.Received.Count, Is.EqualTo(1));
	}


	[Test]
	public void Timer_ticks_in_normal_mode()
	{
		var layer = new ReleaseStubLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		var scheduler = TimerSchedulers.GetOrCreate("release-test-normal");
		bool fired = false;
		scheduler.FireAfter(0.01, new ReleaseTimerEvent(2), (in ReleaseTimerEvent evt) =>
		{
			fired = true;
			return EventHandledState.Handled;
		});

		LayerHub.Pump(0.02f);

		Assert.That(fired, Is.True);
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

internal partial class ServiceEventLayer : Layer
{
	public List<int> Received { get; } = new();

	public ServiceEventLayer()
	{
		Bind<ServiceRaisedEvent>(Handle);
	}

	private EventHandledState Handle(in ServiceRaisedEvent evt)
	{
		Received.Add(evt.Id);
		return EventHandledState.Handled;
	}
}

[OwnerLayer(typeof(ServiceEventLayer))]
internal sealed class ServiceEventModule : IService
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<IServiceEventEmitter, ServiceEventEmitter>();
	}
}

internal interface IServiceEventEmitter
{
	void Emit(int id);
}

internal sealed class ServiceEventEmitter : IServiceEventEmitter, IService
{
	public void ConfigureServices(IServiceCollection services)
	{
	}

	public void Emit(int id)
	{
		this.Bubble(new ServiceRaisedEvent(id));
	}
}

internal partial struct ServiceRaisedEvent(int Id)
{
	public int Id;
}

internal sealed class ServiceRaisedEventMeta : EventMetaData<ServiceRaisedEvent>
{
	private static readonly EventCategoryToken s_category = EventCatalogue.Path("service-events").GetToken();
	public override EventCategoryToken Category => s_category;
}

internal sealed class ReleaseStubLayer : Layer
{
}

internal readonly record struct ReleaseTimerEvent(int Id);
