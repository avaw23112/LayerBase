using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using NUnit.Framework;
using LayerBase;
using LayerBase.Layers;

namespace EventsTest;

public partial struct TestPostEvent
{
    public int Value;
}

public partial struct CoalescedTestEvent { public int Value; }
public class CoalescedEventMetaData : EventMetaData<CoalescedTestEvent>
{
    public override EventPostPolicy? PostPolicy => new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0);
}

public partial struct LatestTestEvent { public int Value; }
public class LatestEventMetaData : EventMetaData<LatestTestEvent>
{
    public override EventPostPolicy? PostPolicy => new EventPostPolicy(PostDeliveryMode.Latest, BackpressurePolicy.RejectNew, 0);
}

public partial struct MaxPendingTestEvent { public int Value; }
public class MaxPendingEventMetaData : EventMetaData<MaxPendingTestEvent>
{
    public override EventPostPolicy? PostPolicy => new EventPostPolicy(PostDeliveryMode.Normal, BackpressurePolicy.RejectNew, 2);
}

public partial struct DropOldestTestEvent { public int Value; }

public class TestPostLayer : Layer {}

[TestFixture]
public class PostSchedulerTests
{
    private GlobalEventCenter _eventCenter;
    
    [SetUp]
    public void SetUp()
    {
        _eventCenter = new GlobalEventCenter();
        EventMetaDataHandler.Clear();
    }

    [Test]
    public void Basic_Post_And_Pump()
    {
        var options = PostSchedulerOptions.Default;
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        int callCount = 0;
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => callCount++);
        
        scheduler.TryPost(new TestPostEvent());
        Assert.That(callCount, Is.EqualTo(0));
        
        scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Wave_Isolation_Post_During_Pump_Goes_To_Next_Wave()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        int callCount = 0;
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => 
        {
            callCount++;
            scheduler.TryPost(new TestPostEvent()); 
        });
        
        scheduler.TryPost(new TestPostEvent());
        
        var stats = scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
        Assert.That(stats.RemainingCount, Is.EqualTo(1));
        Assert.That(stats.WavesProcessed, Is.EqualTo(1));
        
        stats = scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(2));
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
        Assert.That(stats.WavesProcessed, Is.EqualTo(1));
    }
    
    [Test]
    public void MaxWavesPerPump_Processes_Multiple_Waves()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 2, 64, BackpressurePolicy.RejectNew);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        int callCount = 0;
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => 
        {
            callCount++;
            if (callCount == 1) scheduler.TryPost(new TestPostEvent());
        });
        
        scheduler.TryPost(new TestPostEvent());
        
        var stats = scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(2));
        Assert.That(stats.ProcessedCount, Is.EqualTo(2));
        Assert.That(stats.WavesProcessed, Is.EqualTo(2));
    }

    [Test]
    public void EventCount_Budget_Limits_Processing()
    {
        var options = new PostSchedulerOptions(1024, 1024, 5, 0, 1, 64, BackpressurePolicy.RejectNew);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        int callCount = 0;
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => callCount++);
        
        for (int i = 0; i < 10; i++) scheduler.TryPost(new TestPostEvent());
        
        var stats = scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(5));
        Assert.That(stats.ProcessedCount, Is.EqualTo(5));
        Assert.That(stats.RemainingCount, Is.EqualTo(5));
        Assert.That(stats.WavesProcessed, Is.EqualTo(1));
    }

    [Test]
    public void Time_Budget_Limits_Processing()
    {
        var options = new PostSchedulerOptions(1024, 1024, 0, 1.0, 1, 1, BackpressurePolicy.RejectNew);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        int callCount = 0;
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => 
        {
            callCount++;
            Thread.Sleep(10); 
        });
        
        for (int i = 0; i < 200; i++) scheduler.TryPost(new TestPostEvent());
        
        var stats = scheduler.Pump();
        Assert.That(callCount, Is.LessThan(200));
        Assert.That(stats.ProcessedCount, Is.LessThan(200));
    }

    [Test]
    public void Backpressure_RejectNew()
    {
        var options = new PostSchedulerOptions(3, 3, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        
        Assert.That(scheduler.TryPost(new TestPostEvent()).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new TestPostEvent()).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new TestPostEvent()).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new TestPostEvent()).IsSuccess, Is.False);
    }

    [Test]
    public void Backpressure_DropOldest()
    {
        var options = new PostSchedulerOptions(3, 3, 0, 0, 1, 64, BackpressurePolicy.DropOldest);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        var received = new List<int>();
        
        _eventCenter.SubscribeNotify<DropOldestTestEvent>(0, (in DropOldestTestEvent e) => received.Add(e.Value));
        
        Assert.That(scheduler.TryPost(new DropOldestTestEvent { Value = 1 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new DropOldestTestEvent { Value = 2 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new DropOldestTestEvent { Value = 3 }).IsSuccess, Is.True);
        Assert.That(scheduler.TryPost(new DropOldestTestEvent { Value = 4 }).IsSuccess, Is.True); 
        
        scheduler.Pump();
        Assert.That(received, Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void Backpressure_DropNewest()
    {
        var options = new PostSchedulerOptions(3, 3, 0, 0, 1, 64, BackpressurePolicy.DropNewest);
        var scheduler = new PostScheduler(_eventCenter, options, new EventRuntimePolicyTable(options.DefaultBackpressure));
        var received = new List<int>();
        
        _eventCenter.SubscribeNotify<TestPostEvent>(0, (in TestPostEvent e) => received.Add(e.Value));
        
        scheduler.TryPost(new TestPostEvent { Value = 1 });
        scheduler.TryPost(new TestPostEvent { Value = 2 });
        scheduler.TryPost(new TestPostEvent { Value = 3 });
        scheduler.TryPost(new TestPostEvent { Value = 4 }); 
        
        scheduler.Pump();
        Assert.That(received, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Coalesced_Mode_Processes_Only_Once_Per_Pump()
    {
        EventMetaDataRegistry.RegisterMetaData<CoalescedTestEvent>(new CoalescedEventMetaData());
        
        var runtime = LayerHub.CreateLayers().Push(new TestPostLayer()).Build();
        int callCount = 0;
        runtime.EventCenter.SubscribeNotify<CoalescedTestEvent>(0, (in CoalescedTestEvent e) => callCount++);
        
        runtime.Post(new CoalescedTestEvent());
        runtime.Post(new CoalescedTestEvent());
        runtime.Post(new CoalescedTestEvent());
        
        var stats = runtime.Scheduler.Pump();
        Assert.That(callCount, Is.EqualTo(1));
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
    }

    [Test]
    public void Latest_Mode_Processes_Only_Last_Value()
    {
        EventMetaDataRegistry.RegisterMetaData<LatestTestEvent>(new LatestEventMetaData());
        
        var runtime = LayerHub.CreateLayers().Push(new TestPostLayer()).Build();
        int lastValue = -1;
        runtime.EventCenter.SubscribeNotify<LatestTestEvent>(0, (in LatestTestEvent e) => lastValue = e.Value);
        
        runtime.Post(new LatestTestEvent { Value = 1 });
        runtime.Post(new LatestTestEvent { Value = 2 });
        runtime.Post(new LatestTestEvent { Value = 3 });
        
        var stats = runtime.Scheduler.Pump();
        Assert.That(lastValue, Is.EqualTo(3));
        Assert.That(stats.ProcessedCount, Is.EqualTo(1));
    }
    
    [Test]
    public void MaxPending_Rejects_New_Events()
    {
        EventMetaDataRegistry.RegisterMetaData<MaxPendingTestEvent>(new MaxPendingEventMetaData());
        
        var runtime = LayerHub.CreateLayers().Push(new TestPostLayer()).Build();
        
        Assert.That(runtime.Scheduler.TryPost(new MaxPendingTestEvent()).IsSuccess, Is.True);
        Assert.That(runtime.Scheduler.TryPost(new MaxPendingTestEvent()).IsSuccess, Is.True);
        Assert.That(runtime.Scheduler.TryPost(new MaxPendingTestEvent()).IsSuccess, Is.False); 
        
        runtime.Scheduler.Pump();
        
        Assert.That(runtime.Scheduler.TryPost(new MaxPendingTestEvent()).IsSuccess, Is.True);
    }
}
