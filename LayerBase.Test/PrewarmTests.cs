using NUnit.Framework;
using LayerBase.Core.Event;
using LayerBase.Core.EventHandler;
using LayerBase.Layers;
using System;

namespace LayerBase.Test;

[TestFixture]
public partial class PrewarmTests
{
    private LayerRuntime _runtime;

    [SetUp]
    public void Setup()
    {
        _runtime = LayerHub.CreateLayers()
                           .Push(new TestLayer())
                           .Build();
    }

    [TearDown]
    public void Teardown()
    {
        _runtime.Dispose();
    }

    [Test]
    public void TestDefaultPrewarm()
    {
        // 验证默认预热不报错。
        // 它会调用生成的 Registry，由于 TestLayer 有订阅，所以会预热 TestEvent。
        Assert.DoesNotThrow(() => _runtime.Prewarm());
    }

    [Test]
    public void TestAllPrewarm()
    {
        // 验证全量预热不报错。
        Assert.DoesNotThrow(() => _runtime.Prewarm(new LayerPrewarmOptions(LayerPrewarmTargets.All)));
    }

    [Test]
    public void TestManualPrewarmEvent()
    {
        // 验证手动调用 PrewarmEvent 不报错。
        Assert.DoesNotThrow(() => _runtime.EventCenter.PrewarmEvent<TestEvent>(LayerPrewarmOptions.Default));
    }

    [Test]
    public void RegisterEventType_should_enable_non_generic_subscription_path()
    {
        EventCenter.RegisterEventType<ManualPrewarmEvent>();
        int received = 0;
        EventNotifyDelegate<ManualPrewarmEvent> handler = (in ManualPrewarmEvent value) => received = value.Value;

        _runtime.EventCenter.Subscribe(0, handler, typeof(ManualPrewarmEvent));
        _runtime.EventCenter.Send(new ManualPrewarmEvent { Value = 17 });

        Assert.That(received, Is.EqualTo(17));
    }

    [Test]
    public void Non_generic_subscription_reflection_fallback_should_be_observable_once_per_event_type()
    {
        int callbackCount = 0;
        Type? fallbackType = null;
        _runtime.EventCenter.OnReflectionFallback += type =>
        {
            callbackCount++;
            fallbackType = type;
        };
        EventNotifyDelegate<UnregisteredFallbackEvent> handler = (in UnregisteredFallbackEvent _) => { };

        _runtime.EventCenter.Subscribe(0, handler, typeof(UnregisteredFallbackEvent));
        _runtime.EventCenter.Subscribe(1, handler, typeof(UnregisteredFallbackEvent));

        Assert.That(_runtime.EventCenter.ReflectionFallbackCount, Is.EqualTo(1));
        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(fallbackType, Is.EqualTo(typeof(UnregisteredFallbackEvent)));
    }
}

public partial class TestLayer : Layer
{
    [Subscribe]
    public void OnTestEvent(in TestEvent e)
    {
    }
}

public struct TestEvent
{
    public int Value;
}

[PrewarmEvent]
public struct ManualPrewarmEvent
{
    public int Value;
}

public struct UnregisteredFallbackEvent
{
    public int Value;
}
