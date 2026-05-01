using LayerBase.Async;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class MultiWorldTaskTests
{
    [Test]
    public async Task MultiWorld_NextFrame_Isolation()
    {
        var worldA = LayerBase.LayerHub.CreateLayers()
            .Push(new TestLayer())
            .Build();
        
        var worldB = LayerBase.LayerHub.CreateLayers()
            .Push(new TestLayer())
            .Build();

        bool taskACompleted = false;
        bool taskBCompleted = false;

        // Start task in World A
        _ = TaskA();

        // Start task in World B
        _ = TaskB();

        async LBTask TaskA()
        {
            await worldA.Tasks!.NextFrame();
            taskACompleted = true;
        }

        async LBTask TaskB()
        {
            await worldB.Tasks!.NextFrame();
            taskBCompleted = true;
        }

        // Pump World A
        worldA.Pump(0.1f);
        Assert.That(taskACompleted, Is.True);
        Assert.That(taskBCompleted, Is.False);

        // Pump World B
        worldB.Pump(0.1f);
        Assert.That(taskBCompleted, Is.True);
        
        await Task.CompletedTask; // Silence async warning
    }

    [Test]
    public async Task MultiWorld_Delay_ContextCapture()
    {
        var worldA = LayerBase.LayerHub.CreateLayers()
            .Push(new TestLayer())
            .Build();

        bool taskCompleted = false;
        SynchronizationContext? capturedContext = null;

        _ = StartTask();

        async LBTask StartTask()
        {
            // Enter world scope manually or use world.Tasks.Delay
            await worldA.Tasks!.Delay(TimeSpan.FromMilliseconds(10));
            capturedContext = SynchronizationContext.Current;
            taskCompleted = true;
            await Task.CompletedTask;
        }

        // Wait for timer to fire
        Thread.Sleep(50);

        // Task should not be completed yet because World A hasn't pumped its sync context
        Assert.That(taskCompleted, Is.False);

        // Pump World A
        worldA.Pump(0.1f);
        
        Assert.That(taskCompleted, Is.True);
        
        var contextField = worldA.Tasks!.GetType().GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(contextField, Is.Not.Null);
        Assert.That(contextField!.GetValue(worldA.Tasks), Is.SameAs(capturedContext));
        
        worldA.Dispose();
    }

    [Test]
    public async Task MultiWorld_RunOnMainThread_Consistency()
    {
        var worldA = LayerBase.LayerHub.CreateLayers().Push(new TestLayer()).Build();
        
        bool actionExecuted = false;
        bool continuationExecuted = false;
        SynchronizationContext? actionContext = null;
        SynchronizationContext? continuationContext = null;

        _ = StartTask();

        async LBTask StartTask()
        {
            // Run on worldA from current thread
            await worldA.Tasks!.RunOnMainThread(() =>
            {
                actionExecuted = true;
                actionContext = SynchronizationContext.Current;
            });
            
            continuationExecuted = true;
            continuationContext = SynchronizationContext.Current;
            await Task.CompletedTask;
        }

        // Action and continuation shouldn't run yet
        Assert.That(actionExecuted, Is.False);
        Assert.That(continuationExecuted, Is.False);

        // Pump World A
        worldA.Pump(0.1f);

        Assert.That(actionExecuted, Is.True);
        Assert.That(continuationExecuted, Is.True);
        
        var expectedContext = GetWorldContext(worldA);
        Assert.That(actionContext, Is.SameAs(expectedContext));
        Assert.That(continuationContext, Is.SameAs(expectedContext));
        
        worldA.Dispose();
    }

    [Test]
    public async Task MultiWorld_AsyncMethod_WorldCapture()
    {
        var worldA = LayerBase.LayerHub.CreateLayers().Push(new TestLayer()).Build();
        
        bool innerCompleted = false;
        SynchronizationContext? innerContext = null;

        // Simulate being inside World A's update loop
        using (var scope = GetWorldContext(worldA).EnterScope())
        {
            _ = AsyncMethod();
        }

        async LBTask AsyncMethod()
        {
            // Suspends here. Should capture World A context because we are in scope.
            await LBTask.NextFrame(); 
            
            innerContext = SynchronizationContext.Current;
            innerCompleted = true;
            await Task.CompletedTask;
        }

        // Pump World A
        worldA.Pump(0.1f);

        Assert.That(innerCompleted, Is.True);
        Assert.That(innerContext, Is.SameAs(GetWorldContext(worldA)));
        
        worldA.Dispose();
    }

    private static LayerBaseSynchronizationContext GetWorldContext(LayerBase.LayerRuntime world)
    {
        return (LayerBaseSynchronizationContext)world.Tasks!.GetType()
            .GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(world.Tasks)!;
    }

    private class TestLayer : LayerBase.Layers.Layer { }
}
