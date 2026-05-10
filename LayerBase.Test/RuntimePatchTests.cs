using System;
using LayerBase;
using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Test;

public struct IdentityEvent { public int Value; }

[EventIdentity(1001, "Test.IdentityEvent", 2)]
public partial struct IdentityEventWithAttr { public int Value; }

public partial struct NoIdentityEvent { public int Value; }

[TestFixture]
public class RuntimePatchTests
{
    [Test]
    public void TestEventIdentityRegistration()
    {
        LayerHub.Reset();
        var identity = EventIdentityRegistry.GetOrCreate<IdentityEventWithAttr>();
        
        Assert.That(identity.StableId, Is.EqualTo(1001));
        Assert.That(identity.StableKey, Is.EqualTo("Test.IdentityEvent"));
        Assert.That(identity.Version, Is.EqualTo(2));
        
        var noIdentity = EventIdentityRegistry.GetOrCreate<NoIdentityEvent>();
        Assert.That(noIdentity.StableId, Is.EqualTo(0));
        Assert.That(noIdentity.StableKey, Does.Contain("NoIdentityEvent"));
        Assert.That(noIdentity.Version, Is.EqualTo(1));
    }

    [Test]
    public void TestMergeFailurePolicy_Reject()
    {
        var runtime = new LayerRuntime(1);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new TestLayer());
        
        var options = new PostSchedulerOptions(1024, 1024, 0, 0, 1, 64, BackpressurePolicy.RejectNew);
        builder.SetPostOptions(options);
        
        runtime.BuildServiceProvider(); 
        runtime.InitializeScheduler(options);
        
        var policy = new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0, MergeFailurePolicy.Reject);
        runtime.PolicyTable.SetMetaData(EventTypeId<CoalescedTestEvent>.Id, new CoalescedTestEventMetaData());
        runtime.Scheduler.AddSpecialPolicy(EventTypeId<CoalescedTestEvent>.Id, policy);

        runtime.TryPost(new CoalescedTestEvent { Id = 1, Value = 10 });
        var result = runtime.TryPost(new CoalescedTestEvent { Id = 1, Value = -1 });
        
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void TestMergeFailurePolicy_FallbackToLatest()
    {
        var runtime = new LayerRuntime(1);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new TestLayer());
        
        var options = PostSchedulerOptions.Default;
        runtime.InitializeScheduler(options);

        var policy = new EventPostPolicy(PostDeliveryMode.Coalesced, BackpressurePolicy.RejectNew, 0, MergeFailurePolicy.FallbackToLatest);
        runtime.PolicyTable.SetMetaData(EventTypeId<CoalescedTestEvent>.Id, new CoalescedTestEventMetaData());
        runtime.Scheduler.AddSpecialPolicy(EventTypeId<CoalescedTestEvent>.Id, policy);

        runtime.TryPost(new CoalescedTestEvent { Id = 1, Value = 10 });
        var result = runtime.TryPost(new CoalescedTestEvent { Id = 1, Value = -1 });
        
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void TestCompletionQueueExceptionPolicy()
    {
        var queue = new MainThreadCompletionQueue();
        int errorCount = 0;
        
        queue.Enqueue(() => throw new Exception("Test Exception"));
        queue.Enqueue(() => { });

        Assert.Throws<Exception>(() => queue.Drain(0, CompletionExceptionPolicy.Throw, null));

        // Clear queue
        queue.Drain(0, CompletionExceptionPolicy.ReportAndContinue, null);

        queue.Enqueue(() => throw new Exception("Test Exception"));
        queue.Enqueue(() => { });
        
        var stats = queue.Drain(0, CompletionExceptionPolicy.ReportAndContinue, ex => errorCount++);
        
        Assert.That(stats.Errors, Is.EqualTo(1));
        Assert.That(stats.Processed, Is.EqualTo(1));
        Assert.That(errorCount, Is.EqualTo(1));
    }

    public class LifecycleLayer : Layer, IPostBuild, IRuntimeStart, IRuntimeStop, IFixedUpdate
    {
        public bool PostBuildCalled;
        public bool RuntimeStartCalled;
        public bool RuntimeStopCalled;
        public int FixedUpdateCount;

        public void PostBuild() => PostBuildCalled = true;
        public void RuntimeStart() => RuntimeStartCalled = true;
        public void RuntimeStop() => RuntimeStopCalled = true;
        public void FixedUpdate(float fixedDeltaTime) => FixedUpdateCount++;
    }

    [Test]
    public void TestLifecycleHooks()
    {
        var runtime = new LayerRuntime(1);
        var layer = new LifecycleLayer();
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(layer);
        builder.SetFixedUpdateOptions(new FixedUpdateOptions(true, 0.01f, 4));
        
        var builtRuntime = builder.Build();
        
        Assert.That(layer.PostBuildCalled, Is.True);
        Assert.That(layer.RuntimeStartCalled, Is.True);
        
        builtRuntime.Pump(0.025f);
        Assert.That(layer.FixedUpdateCount, Is.EqualTo(2));
        
        builtRuntime.Dispose();
        Assert.That(layer.RuntimeStopCalled, Is.True);
    }

    [Test]
    public void TestPolicyDump()
    {
        var runtime = new LayerRuntime(1);
        var builder = new LayerRuntime.LayersBuilder(runtime);
        builder.Push(new TestLayer());
        builder.Build();
        
        var markdown = runtime.GetPolicyMarkdown();
        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Does.Contain("RuntimeId"));
    }

    private class TestLayer : Layer {}
}
