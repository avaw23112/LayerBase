using System.Diagnostics;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
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
        // 初始化并行调度器，使用 4 个工作线程
        LayerHub.InitializeJobScheduler(workerCount: 4, queueCapacity: 1024);
    }

    [Test]
    public void ParallelHandlers_Should_Run_Concurrently_And_Not_Block_MainThread()
    {
        var layer = new ParallelBenchLayer();
        LayerHub.CreateLayers().Push(layer).Build();

        const int eventCount = 100;
        var sw = Stopwatch.StartNew();

        // 发送事件
        for (int i = 0; i < eventCount; i++)
        {
            layer.SendGlobal(new ParallelEvent());
        }

        long sendTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"[Main Thread] Sent {eventCount} events in {sendTime}ms (Should be very low)");

        // 验证主线程没有被阻塞（100个事件，每个Handler睡10ms，如果是串行需要 100 * 10 * 4 = 4000ms）
        Assert.That(sendTime, Is.LessThan(500), "Main thread was blocked by parallel handlers!");

        // 等待所有并行任务完成
        bool finished = layer.AllHandlersFinished(eventCount, TimeSpan.FromSeconds(5));
        sw.Stop();

        Assert.That(finished, Is.True, "Parallel handlers did not finish in time");
        Console.WriteLine($"[Parallel] All tasks finished. Total Time: {sw.ElapsedMilliseconds}ms");
        
        // 如果 4 个工作线程正常工作，100 * 10ms 的任务在 4 线程下理论耗时应接近 250ms-500ms
    }

    [Test]
    public void ParallelHandlers_Fault_Isolation_Test()
    {
        var layer = new ParallelBenchLayer();
        int errorCount = 0;
        LayerHub.OnLayerEventError += (info) => Interlocked.Increment(ref errorCount);
        
        LayerHub.CreateLayers().Push(layer).Build();

        // 注册一个必然抛异常的并行 Handler
        layer.SubscribeParallel<ParallelEvent>((in ParallelEvent _) => {
            throw new Exception("Parallel Crash");
        });

        // 发送事件
        layer.SendGlobal(new ParallelEvent());

        // 等待处理
        Thread.Sleep(500);

        // 验证：
        // 1. 健康的 Handler 依然在工作
        // 2. 异常被捕获且隔离
        Assert.That(layer.HealthyHandlerCount, Is.GreaterThan(0));
        Assert.That(errorCount, Is.EqualTo(1));
        Console.WriteLine("[Isolation] Parallel fault was successfully isolated and reported.");
    }

    private class ParallelBenchLayer : Layer
    {
        private int _handledCountA = 0;
        private int _handledCountB = 0;
        
        public int HealthyHandlerCount => Volatile.Read(ref _handledCountA);

        public ParallelBenchLayer()
        {
            // 注册两个并行的 Handler
            SubscribeParallel<ParallelEvent>(HandleA);
            SubscribeParallel<ParallelEvent>(HandleB);
        }

        private EventHandledState HandleA(in ParallelEvent _)
        {
            Thread.Sleep(10); // 模拟耗时
            Interlocked.Increment(ref _handledCountA);
            return EventHandledState.Continue;
        }

        private EventHandledState HandleB(in ParallelEvent _)
        {
            Thread.Sleep(10); // 模拟耗时
            Interlocked.Increment(ref _handledCountB);
            return EventHandledState.Continue;
        }

        public bool AllHandlersFinished(int expected, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (Volatile.Read(ref _handledCountA) >= expected && 
                    Volatile.Read(ref _handledCountB) >= expected)
                    return true;
                Thread.Sleep(10);
            }
            return false;
        }
    }

    public struct ParallelEvent { }
}
