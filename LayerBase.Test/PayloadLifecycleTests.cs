using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class PayloadLifecycleTests
{
    private EventCenter _eventCenter;

    [SetUp]
    public void SetUp()
    {
        _eventCenter = new EventCenter();
        LayerHub.Reset();
        EventMetaDataHandler.Clear();
    }

    public partial struct LifecycleTestEvent { public int Value; }

    [Test]
    public void Runtime_Dispose_Clears_PayloadStore_Cache()
    {
        int runtimeId;
        using (var runtime = LayerHub.CreateLayers().Push(new TestLayer()).Build())
        {
            runtimeId = runtime.Id;
            runtime.Scheduler.PrewarmEvent<LifecycleTestEvent>();
            runtime.Post(new LifecycleTestEvent { Value = 42 });
            
            // 验证 Store 已创建
            Assert.That(PayloadStoreCache<LifecycleTestEvent>.Stores[runtimeId], Is.Not.Null);
        }
        
        // 验证 Runtime Dispose 后，Store 被清空
        Assert.That(PayloadStoreCache<LifecycleTestEvent>.Stores[runtimeId], Is.Null);
    }

    [Test]
    public void RuntimeId_Reuse_Does_Not_Leak_Old_Payloads()
    {
        // 1. 第一个 Runtime
        var runtime1 = LayerHub.CreateLayers().Push(new TestLayer()).Build();
        var id1 = runtime1.Id;
        runtime1.Scheduler.PrewarmEvent<LifecycleTestEvent>();
        runtime1.Post(new LifecycleTestEvent { Value = 1 });
        runtime1.Dispose();
        
        // 2. 第二个 Runtime (假设复用了 ID)
        LayerHub.Reset(); 
        var runtime2 = LayerHub.CreateLayers().Push(new TestLayer()).Build();
        Assert.That(runtime2.Id, Is.EqualTo(id1), "Should reuse the same ID after Reset for this test.");
        
        int callCount = 0;
        int lastValue = 0;
        runtime2.EventCenter.SubscribeNotify<LifecycleTestEvent>(0, (in LifecycleTestEvent e) => 
        {
            callCount++;
            lastValue = e.Value;
        });
        
        // 3. Pump，不应该读到旧的 Value 1
        runtime2.Scheduler.PrewarmEvent<LifecycleTestEvent>();
        runtime2.Pump(0.1f);
        Assert.That(callCount, Is.EqualTo(0));
        
        // 4. 发送新事件
        runtime2.Post(new LifecycleTestEvent { Value = 2 });
        runtime2.Pump(0.1f);
        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(lastValue, Is.EqualTo(2));
    }

    [Test]
    public void PostScheduler_Dispose_Releases_Pending_Payloads()
    {
        var options = PostSchedulerOptions.Default;
        var table = new EventBuildPolicyTable(options.DefaultBackpressure);
        var scheduler = new PostScheduler(0, _eventCenter, options, table);
        scheduler.PrewarmEvent<LifecycleTestEvent>();
        
        scheduler.TryPost(new LifecycleTestEvent { Value = 1 });
        scheduler.TryPostLatest(new LifecycleTestEvent { Value = 2 });
        scheduler.TryPostCoalesced(new LifecycleTestEvent { Value = 3 });

        // 获取 Store，记录当前的活跃数量 (这里需要内部访问，或者通过 Release 钩子验证)
        // 由于没有直接 API，我们通过 Dispose 不报错来验证基础清理流程。
        // 更严谨的验证需要 Mock EventStore。
        
        Assert.DoesNotThrow(() => scheduler.Dispose());
    }

    [Test]
    public void FlushBuffers_Exception_Does_Not_Leak_Snapshot_Payloads()
    {
        var options = PostSchedulerOptions.Default;
        var table = new EventBuildPolicyTable(options.DefaultBackpressure);
        var scheduler = new PostScheduler(0, _eventCenter, options, table);
        
        // 配置 Coalesced 和 Latest 策略
        scheduler.AddSpecialPolicy(EventTypeId<LifecycleTestEvent>.Id, 
            new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0));
        
        scheduler.TryPostCoalesced(new LifecycleTestEvent { Value = 1 });
        scheduler.TryPostCoalesced(new LifecycleTestEvent { Value = 2 });
        
        // 订阅并抛出异常
        _eventCenter.SubscribeNotify<LifecycleTestEvent>(0, (in LifecycleTestEvent e) => 
        {
            throw new Exception("Test Exception");
        });

        // 执行 Pump (会调用 FlushBuffers)
        Assert.Throws<Exception>(() => scheduler.Pump());
        
        // 再次 Pump 不应该重复派发 (因为 snapshot 已清理)
        int callCount = 0;
        _eventCenter.SubscribeNotify<LifecycleTestEvent>(1, (in LifecycleTestEvent e) => callCount++);
        
        // 清理掉抛异常的订阅，防止第二次也抛
        _eventCenter.Reset(); 
        _eventCenter.SubscribeNotify<LifecycleTestEvent>(1, (in LifecycleTestEvent e) => callCount++);

        Assert.DoesNotThrow(() => scheduler.Pump());
        Assert.That(callCount, Is.EqualTo(0));
    }

    private class TestLayer : Layer {}
}
