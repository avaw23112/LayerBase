using System.Collections.Concurrent;
using LayerBase;
using LayerBase.DI;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class ScopeServiceDisposalTests
{
    private readonly List<string> _disposeLog = new();

    [SetUp]
    public void SetUp()
    {
        _disposeLog.Clear();
        RetryDisposableService.DisposeCount = 0;
        ThrowOnceDisposableService.DisposeCount = 0;
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Secondary_scope_service_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(_disposeLog, Has.Exactly(1).Matches<string>(s => s.Contains("SecondaryService")));
    }

    [Test]
    public void Main_scope_service_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.Dispose();

        Assert.That(_disposeLog, Has.Some.Contains("MainService"));
    }

    [Test]
    public void Same_instance_registered_as_multiple_interfaces_is_disposed_once()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new MultiInterfaceLayer(_disposeLog))
            .Build();

        runtime.Dispose();

        Assert.That(_disposeLog, Has.Exactly(1).Matches<string>(s => s.Contains("MultiService")));
    }

    [Test]
    public void Disposing_secondary_scope_does_not_dispose_main_service()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();

        Assert.That(_disposeLog, Has.None.Matches<string>(s => s.Contains("MainService")));
    }

    [Test]
    public void Runtime_dispose_after_secondary_dispose_does_not_double_dispose()
    {
        var runtime = LayerHub.CreateLayers()
            .Push(new ServiceDisposalLayer(_disposeLog))
            .Build();

        runtime.ScopeHost.Scopes[1].Dispose();
        runtime.Dispose();

        Assert.That(_disposeLog, Has.Some.Contains("MainService"));
        Assert.That(_disposeLog, Has.Some.Contains("SecondaryService"));
    }

    [Test]
    public void Scope_service_provider_wrong_thread_dispose_can_be_retried_on_owner_thread()
    {
        using var runtime = new LayerRuntime(60101);
        var host = ScopeRuntimeHost.CreateMain(runtime, runtimeId: 60101, generation: 1);
        ScopeRuntime scope = host.MainScope;
        using var ownerThread = new OwnerThread(scope);
        var provider = CreateSingleServiceProvider<RetryDisposableService>(scope, runtime);

        ownerThread.Invoke(() => provider.Get<RetryDisposableService>());

        Assert.Throws<InvalidOperationException>(() => provider.Dispose());

        ownerThread.Invoke(() => provider.Dispose());

        Assert.That(RetryDisposableService.DisposeCount, Is.EqualTo(1));

        ownerThread.Invoke(() => scope.Dispose());
    }

    [Test]
    public void Scope_service_provider_dispose_reports_resource_errors()
    {
        using var runtime = new LayerRuntime(60102);
        using var host = ScopeRuntimeHost.CreateMain(runtime, runtimeId: 60102, generation: 1);
        ScopeRuntime scope = host.MainScope;
        scope.InstallSynchronizationContext();
        var provider = CreateSingleServiceProvider<ThrowingDisposableService>(scope, runtime);

        provider.Get<ThrowingDisposableService>();

        Assert.Throws<AggregateException>(() => provider.Dispose());
        Assert.That(provider.IsDisposed, Is.False);
    }

    [Test]
    public void Layer_dispose_failure_can_be_retried()
    {
        var layer = new ThrowOnceDisposeLayer();
        var runtime = LayerHub.CreateLayers()
            .Push(layer)
            .Build();

        try
        {
            Assert.Throws<AggregateException>(() => layer.Dispose());

            Assert.DoesNotThrow(() => layer.Dispose());

            Assert.That(ThrowOnceDisposableService.DisposeCount, Is.EqualTo(2));
        }
        finally
        {
            runtime.Dispose();
        }
    }

    private static ScopeServiceProvider CreateSingleServiceProvider<TService>(
        ScopeRuntime scope,
        LayerRuntime runtime)
        where TService : class
    {
        var descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            ServiceLifetime.Singleton,
            null,
            null,
            ownerScopeId: scope.ScopeId);
        var plan = ScopeServicePlan.Compile(scope.ScopeId, new[] { descriptor });
        var layer = new MultiInterfaceLayer(new List<string>());
        layer.AttachToContext(runtime);
        return new ScopeServiceProvider(scope, plan, layer);
    }

    private sealed class TracedDisposable : IService, IDisposable
    {
        private readonly List<string> _log;
        private readonly string _name;
        private int _disposeCount;

        public TracedDisposable(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public void ConfigureServices(IServiceCollection services) { }

        public void Dispose()
        {
            int count = Interlocked.Increment(ref _disposeCount);
            _log.Add($"{_name}:{count}");
        }
    }

    private sealed class RetryDisposableService : IDisposable
    {
        public static int DisposeCount;

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
        }
    }

    private sealed class ThrowingDisposableService : IDisposable
    {
        public void Dispose()
        {
            throw new InvalidOperationException("dispose failed");
        }
    }

    private sealed class ThrowOnceDisposableService : IDisposable
    {
        public static int DisposeCount;

        public void Dispose()
        {
            if (Interlocked.Increment(ref DisposeCount) == 1)
                throw new InvalidOperationException("first dispose failed");
        }
    }

    private sealed class ThrowOnceDisposeLayer : Layer
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new ThrowOnceDisposableService());
        }
    }

    private sealed class OwnerThread : IDisposable
    {
        private readonly BlockingCollection<Action> _actions = new();
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _ready = new(false);

        public OwnerThread(ScopeRuntime scope)
        {
            _thread = new Thread(() =>
            {
                scope.InstallSynchronizationContext();
                _ready.Set();
                foreach (Action action in _actions.GetConsumingEnumerable())
                    action();
            })
            {
                IsBackground = true
            };
            _thread.Start();
            _ready.Wait(TimeSpan.FromSeconds(2));
        }

        public void Invoke(Action action)
        {
            Exception? error = null;
            using var done = new ManualResetEventSlim(false);
            _actions.Add(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    done.Set();
                }
            });

            Assert.That(done.Wait(TimeSpan.FromSeconds(2)), Is.True);
            if (error != null)
                throw error;
        }

        public void Dispose()
        {
            _actions.CompleteAdding();
            _thread.Join(TimeSpan.FromSeconds(2));
            _actions.Dispose();
            _ready.Dispose();
        }
    }

    private sealed class ServiceDisposalLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly List<string> _log;

        public ServiceDisposalLayer(List<string> log)
        {
            _log = log;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:SecondaryScope",
                    scopeType: typeof(SecondaryScope),
                    factory: static () => new SecondaryScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(IService),
                new TracedDisposable(_log, "MainService"),
                typeof(MainScope));

            RegisterService(
                typeof(IService),
                new TracedDisposable(_log, "SecondaryService"),
                typeof(SecondaryScope));
        }
    }

    private sealed class MultiInterfaceLayer : Layer
    {
        private readonly List<string> _log;

        public MultiInterfaceLayer(List<string> log)
        {
            _log = log;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            var instance = new TracedDisposable(_log, "MultiService");
            RegisterService(typeof(IService), instance);
        }
    }

    private sealed class SecondaryScope : IScopeDefinition
    {
        public const int ScopeId = 777;
        public ScopeOptions Options => ScopeOptions.Inline;
    }
}
