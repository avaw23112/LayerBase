using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using LayerBase.Core.Event;
using LayerBase.Core.EventCatalogue;
using LayerBase.Core.EventHandler;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.DI.Options;
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

	[Test]
	public void IService_that_implements_IUpdate_is_pumped()
	{
		var layer = new ServiceUpdateLayer();
		LayerHub.CreateLayers().Push(layer).Build();

		var updater = layer.GetService<UpdatingService>();
		Assert.That(updater.TickCount, Is.EqualTo(0));

		LayerHub.Pump(0.02f);

		Assert.That(updater.TickCount, Is.GreaterThanOrEqualTo(1));
	}

	[Test]
	public void Layer_GetService_is_thread_safe_in_parallel_handlers()
	{
		var layer = new ConcurrentServiceLayer();
		try
		{
			layer.Build();

			const int concurrency = 16;
			const int perWorkerIterations = 64;
			var scopedRefs = new ConcurrentBag<IConcurrentScopedService>();
			var singletonRefs = new ConcurrentBag<IConcurrentSingletonService>();
			var errors = new ConcurrentQueue<Exception>();

			Parallel.For(0, concurrency, _ =>
			{
				try
				{
					for (int i = 0; i < perWorkerIterations; i++)
					{
						scopedRefs.Add(layer.GetService<IConcurrentScopedService>());
						singletonRefs.Add(layer.GetService<IConcurrentSingletonService>());
					}
				}
				catch (Exception ex)
				{
					errors.Enqueue(ex);
				}
			});

			Assert.That(errors, Is.Empty);

			var scopedArray = scopedRefs.ToArray();
			var singletonArray = singletonRefs.ToArray();
			Assert.That(scopedArray.Length, Is.EqualTo(concurrency * perWorkerIterations));
			Assert.That(singletonArray.Length, Is.EqualTo(concurrency * perWorkerIterations));
			Assert.That(scopedArray.All(item => ReferenceEquals(item, scopedArray[0])), Is.True);
			Assert.That(singletonArray.All(item => ReferenceEquals(item, singletonArray[0])), Is.True);
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

internal partial class ConcurrentServiceLayer : Layer
{
}

[OwnerLayer(typeof(ConcurrentServiceLayer))]
internal sealed class ConcurrentServiceModule : IService
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<IConcurrentScopedService, ConcurrentScopedService>();
		services.AddSingleton<IConcurrentSingletonService, ConcurrentSingletonService>();
	}
}

internal interface IConcurrentScopedService
{
}

internal sealed class ConcurrentScopedService : IConcurrentScopedService
{
}

internal interface IConcurrentSingletonService
{
}

internal sealed class ConcurrentSingletonService : IConcurrentSingletonService
{
}

internal partial class ServiceEventLayer : Layer
{
	public List<int> Received { get; } = new();

	public ServiceEventLayer()
	{
		Subscribe<ServiceRaisedEvent>(Handle);
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

internal partial class ServiceUpdateLayer : Layer
{
}

[OwnerLayer(typeof(ServiceUpdateLayer))]
internal sealed class UpdatingService : IService, IUpdate
{
	public int TickCount { get; private set; }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<UpdatingService>(_ => this);
	}

	public void Update()
	{
		TickCount++;
	}
}
