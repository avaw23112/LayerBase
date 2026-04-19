using System.Diagnostics;
using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class PerformanceBenchmarks
{
    private const int EventCount = 100_000;

    [Test]
    public void Massive_Event_Dispatch_Bench()
    {
        var layer = new BenchLayer();
        var rt = LayerHub.CreateLayers().Push(layer).Build();

        // 预热
        for (var i = 0; i < 1000; i++) rt.Send(new BenchEvent());

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < EventCount; i++) rt.Send(new BenchEvent());
        sw.Stop();

        TestContext.Out.WriteLine($"[100k Dispatch] Total: {sw.ElapsedMilliseconds}ms, Avg: {sw.Elapsed.TotalMilliseconds * 1000 / EventCount:F2}us/op");
    }

    private class BenchLayer : Layer
    {
        public BenchLayer()
        {
            Subscribe<BenchEvent>(Handle);
        }

        private EventHandledState Handle(in BenchEvent e)
        {
            return EventHandledState.Continue;
        }
    }

    public struct BenchEvent { }
}