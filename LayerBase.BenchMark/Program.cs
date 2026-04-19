using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;
using LayerBase.Core.EventHandler;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--full", StringComparison.OrdinalIgnoreCase))
            {
                BenchmarkRunner.Run<FullBenchmarks>();
            }
            else if (args.Length > 0 && args[0].Equals("--compare", StringComparison.OrdinalIgnoreCase))
            {
                BenchmarkRunner.Run<EvolutionBenchmarks>();
            }
            else
            {
                BenchmarkRunner.Run<QuickBenchmarks>();
            }
        }
    }

    [MemoryDiagnoser]
    [HideColumns("Error", "StdDev", "Median", "RatioSD")]
    public class EvolutionBenchmarks
    {
        private const int Iterations = 1_000_000;

        [Benchmark(Baseline = true, Description = "标准模式 (100万次)")]
        public void Standard_Dispatch()
        {
            LayerHub.Reset();
            var layer = new BenchLayer();
            // 使用 Lambda 订阅 (走传统委托路径)
            layer.Subscribe((in BenchEvent _) => { return EventHandledState.Continue; });
            LayerHub.CreateLayers().Push(layer).Build();
            
            for (var i = 0; i < Iterations; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "透明桥接模式 (100万次)")]
        public unsafe void Optimized_Dispatch()
        {
            LayerHub.Reset();
            var layer = new OptimizedBenchLayer();
            LayerHub.CreateLayers().Push(layer).Build();
            
            // 🚀 手动模拟 Generator 注入 (确保在 Benchmark 项目中生效)
            delegate*<object, in BenchEvent, EventHandledState> ptr = &OptimizedBenchLayer.StaticBridge;
            layer.SubscribeOptimized<BenchEvent>((IntPtr)ptr, layer, "OptimizedBenchLayer.OnRecv");

            // 强制 Rebuild 位图
            LayerHub.Send(new BenchEvent());

            for (var i = 0; i < Iterations; i++) LayerHub.Send(new BenchEvent());
        }
    }

    [MemoryDiagnoser]
    public class QuickBenchmarks
    {
        private const int ChallengeCount = 10_000;

        [Benchmark(Description = "⚡ 典型中重度负载 - 1万次")]
        public void Typical_5Layers_HeavyLoad_Quick()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 5; i++)
            {
                var layer = new BenchLayer();
                int subCount = (i == 2) ? 100 : 20;
                for (int j = 0; j < subCount; j++)
                {
                    layer.Subscribe((in BenchEvent _) => { return EventHandledState.Continue; });
                }
                builder.Push(layer);
            }
            builder.Build();

            for (var i = 0; i < ChallengeCount; i++) LayerHub.Send(new BenchEvent());
        }
    }

    [MemoryDiagnoser]
    public class FullBenchmarks
    {
        private const int EventCount = 1_000_000;

        [Benchmark(Description = "单层低压 - 100万次")]
        public void SingleLayer_LowDensity()
        {
            LayerHub.Reset();
            var layer = new BenchLayer();
            layer.Subscribe((in BenchEvent _) => { return EventHandledState.Continue; });
            LayerHub.CreateLayers().Push(layer).Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }
    }

    public class BenchLayer : Layer { }

    public partial class OptimizedBenchLayer : Layer 
    {
        [Subscribe]
        internal EventHandledState OnRecv(in BenchEvent e) => EventHandledState.Continue;

        internal static EventHandledState StaticBridge(object ins, in BenchEvent e) => ((OptimizedBenchLayer)ins).OnRecv(in e);
    }

    public struct BenchEvent {}
}
