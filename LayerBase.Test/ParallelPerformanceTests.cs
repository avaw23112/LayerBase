using System.Diagnostics;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class ParallelPerformanceTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
        LayerHub.InitializeJobScheduler(4);
    }

    [Test]
    public void Parallel_Events_Dispatch_And_Fuse_Test()
    {
        var layer = new ParallelLayer();
        var rt = LayerHub.CreateLayers().Push(layer).Build();

        for (var i = 0; i < 100; i++) rt.Send(new WorkEvent());
        
        // 发送报错事件，验证熔断
        rt.Send(new FaultEvent());
        
        // 验证后续事件依然能正常发送（且报错不会阻塞主线程）
        for (var i = 0; i < 10; i++) rt.Send(new WorkEvent());
    }

    private class ParallelLayer : Layer
    {
        public ParallelLayer()
        {
            SubscribeParallel((in WorkEvent e) => { return EventHandledState.Continue; });
            SubscribeParallel((in FaultEvent e) => throw new Exception("Parallel error"));
        }
    }

    public struct WorkEvent { }
    public struct FaultEvent { }
}