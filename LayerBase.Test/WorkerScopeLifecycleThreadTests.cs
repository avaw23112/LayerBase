using System.Collections.Concurrent;
using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Scope;

namespace LayerBase.Test;

[TestFixture]
[Category("ProductionHardening")]
public sealed class WorkerScopeLifecycleThreadTests
{
    private ConcurrentQueue<string> _lifecycleLog = new();

    [SetUp]
    public void SetUp()
    {
        _lifecycleLog = new ConcurrentQueue<string>();
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Worker_scope_initialize_runs_on_owner_thread()
    {
        using var runtime = BuildWithWorkerScope();

        var log = _lifecycleLog.ToArray();
        Assert.That(log, Has.Some.Contains("Initialize"));
    }

    [Test]
    public void Worker_scope_post_build_runs_on_owner_thread()
    {
        using var runtime = BuildWithWorkerScope();

        var log = _lifecycleLog.ToArray();
        Assert.That(log, Has.Some.Contains("PostBuild"));
    }

    [Test]
    public void Worker_scope_runtime_start_runs_on_owner_thread()
    {
        using var runtime = BuildWithWorkerScope();

        var log = _lifecycleLog.ToArray();
        Assert.That(log, Has.Some.Contains("RuntimeStart"));
    }

    [Test]
    public void Worker_scope_services_are_constructed_on_owner_thread()
    {
        using var runtime = LayerHub.CreateLayers()
            .Push(new WorkerScopeConstructorThreadLayer(_lifecycleLog))
            .Build();

        var byName = _lifecycleLog
            .Select(static line => line.Split(':'))
            .ToDictionary(static parts => parts[0], static parts => int.Parse(parts[1]));

        Assert.That(byName["Ctor"], Is.EqualTo(byName["RuntimeStart"]));
    }

    [Test]
    public void Worker_scope_runtime_stop_runs_on_owner_thread()
    {
        var runtime = BuildWithWorkerScope();
        runtime.Dispose();

        var log = _lifecycleLog.ToArray();
        Assert.That(log, Has.Some.Contains("RuntimeStop"));
    }

    [Test]
    public void Worker_scope_dispose_runs_on_owner_thread()
    {
        var runtime = BuildWithWorkerScope();
        runtime.Dispose();

        var log = _lifecycleLog.ToArray();
        Assert.That(log, Has.Some.Contains("Dispose"));
    }

    [Test]
    public void Each_lifecycle_callback_runs_exactly_once()
    {
        var lifecycleLog = new ConcurrentQueue<string>();
        var runtime = LayerHub.CreateLayers()
            .Push(new WorkerScopeLifecycleLayer(lifecycleLog))
            .Build();
        runtime.Dispose();

        var lifecycleCallbacks = lifecycleLog
            .Select(line => line.Split(':')[0])
            .Where(name => name is "Initialize" or "PostBuild" or "RuntimeStart")
            .GroupBy(name => name)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.That(lifecycleCallbacks.GetValueOrDefault("Initialize"), Is.EqualTo(1));
        Assert.That(lifecycleCallbacks.GetValueOrDefault("PostBuild"), Is.EqualTo(1));
        Assert.That(lifecycleCallbacks.GetValueOrDefault("RuntimeStart"), Is.EqualTo(1));
    }

    private LayerRuntime BuildWithWorkerScope()
    {
        return LayerHub.CreateLayers()
            .Push(new WorkerScopeLifecycleLayer(_lifecycleLog))
            .Build();
    }

    private sealed class WorkerScopeLifecycleLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly ConcurrentQueue<string> _lifecycleLog;

        public WorkerScopeLifecycleLayer(ConcurrentQueue<string> lifecycleLog)
        {
            _lifecycleLog = lifecycleLog;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 777,
                    identity: "scope:test:WorkerScope",
                    scopeType: typeof(WorkerScope),
                    factory: static () => new WorkerScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            RegisterService(
                typeof(LifecycleTrackingService),
                new LifecycleTrackingService(_lifecycleLog),
                typeof(WorkerScope));
        }
    }

    private sealed class WorkerScopeConstructorThreadLayer : Layer, IGeneratedScopeDefinitionProvider
    {
        private readonly ConcurrentQueue<string> _lifecycleLog;

        public WorkerScopeConstructorThreadLayer(ConcurrentQueue<string> lifecycleLog)
        {
            _lifecycleLog = lifecycleLog;
        }

        public GeneratedScopeDefinition[] __GetScopeDefinitions()
        {
            return new[]
            {
                new GeneratedScopeDefinition(
                    scopeId: 778,
                    identity: "scope:test:WorkerCtorScope",
                    scopeType: typeof(WorkerCtorScope),
                    factory: static () => new WorkerCtorScope())
            };
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            using var _ = ((ServiceCollection)services).PushRegistrationScope(
                registrationScopeId: 0,
                ownerScopeId: 778);

            services.AddSingleton<ConstructorThreadService>(_ => new ConstructorThreadService(_lifecycleLog));
        }
    }

    private sealed class WorkerCtorScope : IScopeDefinition
    {
        public const int ScopeId = 778;
        public ScopeOptions Options => ScopeOptions.Worker();
    }

    private sealed class ConstructorThreadService : IService, IRuntimeStart
    {
        private readonly ConcurrentQueue<string> _log;

        public ConstructorThreadService(ConcurrentQueue<string> log)
        {
            _log = log;
            _log.Enqueue($"Ctor:{Environment.CurrentManagedThreadId}");
        }

        public void ConfigureServices(IServiceCollection services) { }

        public void RuntimeStart()
        {
            _log.Enqueue($"RuntimeStart:{Environment.CurrentManagedThreadId}");
        }
    }

    private sealed class WorkerScope : IScopeDefinition
    {
        public const int ScopeId = 777;
        public ScopeOptions Options => ScopeOptions.Worker();
    }

    private sealed class LifecycleTrackingService : IService,
        IInitializable, IPostBuild, IRuntimeStart, IRuntimeStop, IDisposable
    {
        private readonly ConcurrentQueue<string> _log;

        public LifecycleTrackingService(ConcurrentQueue<string> log)
        {
            _log = log;
        }

        public void ConfigureServices(IServiceCollection services) { }

        public void Initialize()
        {
            _log.Enqueue($"Initialize:{Environment.CurrentManagedThreadId}");
        }

        public void PostBuild()
        {
            _log.Enqueue($"PostBuild:{Environment.CurrentManagedThreadId}");
        }

        public void RuntimeStart()
        {
            _log.Enqueue($"RuntimeStart:{Environment.CurrentManagedThreadId}");
        }

        public void RuntimeStop()
        {
            _log.Enqueue($"RuntimeStop:{Environment.CurrentManagedThreadId}");
        }

        public void Dispose()
        {
            _log.Enqueue($"Dispose:{Environment.CurrentManagedThreadId}");
        }
    }
}
