using LayerBase;
using LayerBase.DI;
using LayerBase.DI.Options;
using LayerBase.Layers;

namespace EventsTest;

[TestFixture]
public class LifecycleTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        _trace = new List<string>();
    }

    private List<string> _trace;

    [Test]
    public void Lifecycle_Phases_Execute_In_Correct_Order()
    {
        var layer = new LifecycleLayer();
        layer.RegisterService(new LifecycleTestService(_trace));

        // 运行构建流
        LayerHub.CreateLayers().Push(layer).Build();

        // 验证执行顺序：1. AutoBind -> 2. Initialize
        Assert.That(_trace, Is.EqualTo(new[] { "AutoBind_A", "AutoBind_B", "Init_A", "Init_B" }),
            "Should bind all first, then initialize all, maintaining registration order.");

        // 验证 Update 顺序
        LayerHub.Pump(0.02f);

        var updateA = _trace.IndexOf("Update_A");
        var updateB = _trace.IndexOf("Update_B");
        Assert.That(updateA, Is.LessThan(updateB), "Update_A should execute before Update_B");
    }

    private class LifecycleLayer : Layer
    {
    }

    private class LifecycleTestService : IService
    {
        private readonly List<string> _trace;

        public LifecycleTestService(List<string> trace)
        {
            _trace = trace;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // 注册顺序：A 先于 B
            services.AddSingleton<ManagerA>(new ManagerA(_trace));
            services.AddSingleton(new ManagerB(_trace));
        }
    }

    // --- Mock Managers ---

    public class ManagerA : ILayerContext, IAutoSubscribe, IInitializable, IUpdate
    {
        private readonly List<string> _trace;

        public ManagerA(List<string> trace)
        {
            _trace = trace;
        }

        public int LayerIndex { get; set; }

        public void AutoBind(Layer l)
        {
            _trace.Add("AutoBind_A");
        }

        public IEnumerable<EventDependency> GetEventDependencies()
        {
            return Enumerable.Empty<EventDependency>();
        }

        public IEnumerable<Type> GetSubscribedEvents()
        {
            return Enumerable.Empty<Type>();
        }

        public void Initialize()
        {
            _trace.Add("Init_A");
        }

        public void Update()
        {
            _trace.Add("Update_A");
        }
    }

    public class ManagerB : ILayerContext, IAutoSubscribe, IInitializable, IUpdate
    {
        private readonly List<string> _trace;

        public ManagerB(List<string> trace)
        {
            _trace = trace;
        }

        public int LayerIndex { get; set; }

        public void AutoBind(Layer l)
        {
            _trace.Add("AutoBind_B");
        }

        public IEnumerable<EventDependency> GetEventDependencies()
        {
            return Enumerable.Empty<EventDependency>();
        }

        public IEnumerable<Type> GetSubscribedEvents()
        {
            return Enumerable.Empty<Type>();
        }

        public void Initialize()
        {
            _trace.Add("Init_B");
        }

        public void Update()
        {
            _trace.Add("Update_B");
        }
    }
}