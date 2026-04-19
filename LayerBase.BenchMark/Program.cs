using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--full", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("🚀 Running Full Benchmarks...");
                BenchmarkRunner.Run<FullBenchmarks>();
            }
            else
            {
                Console.WriteLine("⚡ Running Quick Benchmarks...");
                BenchmarkRunner.Run<QuickBenchmarks>();
            }
        }
    }

    [MemoryDiagnoser]
    public class QuickBenchmarks
    {
        private const int ChallengeCount = 10_000;

        [Benchmark(Description = "⚡ 典型中重度负载 (5层: 1层100订阅, 4层各20订阅) - 1万次")]
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
                    layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
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
        private const int ChallengeCount = 10_000;

        [Benchmark(Description = "单层低压 (1层, 1个订阅) - 100万次")]
        public void SingleLayer_LowDensity()
        {
            LayerHub.Reset();
            var layer = new BenchLayer();
            layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
            LayerHub.CreateLayers().Push(layer).Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "单层高压 (1层, 10个订阅) - 100万次")]
        public void SingleLayer_HighDensity()
        {
            LayerHub.Reset();
            var layer = new BenchLayer();
            for(int i=0; i<10; i++) layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
            LayerHub.CreateLayers().Push(layer).Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "多层低压 (10层, 仅尾层订阅) - 100万次")]
        public void MultiLayer_LowDensity_TailLoad()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 10; i++) {
                var l = new BenchLayer();
                if (i == 9) l.Subscribe((in BenchEvent _) => { l.HandledCount++; return EventHandledState.Continue; });
                builder.Push(l);
            }
            builder.Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "多层高压/全负载 (10层, 层层订阅) - 100万次")]
        public void MultiLayer_HighDensity_FullLoad()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (var i = 0; i < 10; i++)
            {
                var layer = new BenchLayer();
                layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
                builder.Push(layer);
            }
            builder.Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "多层随机负载 (10层, 随机5层订阅) - 100万次")]
        public void MultiLayer_RandomLoad()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            var activeLayers = new HashSet<int> { 1, 4, 5, 7, 8 };
            for (var i = 0; i < 10; i++)
            {
                var layer = new BenchLayer();
                if (activeLayers.Contains(i)) {
                    layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
                }
                builder.Push(layer);
            }
            builder.Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "极限空负载 (64层, 0订阅) - 100万次")]
        public void MaxLayers_EmptyLoad()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (var i = 0; i < 64; i++) builder.Push(new BenchLayer());
            builder.Build();
            for (var i = 0; i < EventCount; i++) LayerHub.Send(new BenchEvent());
        }

        [Benchmark(Description = "典型中重度负载 (5层: 1层100订阅, 4层各20订阅) - 1万次")]
        public void Typical_5Layers_HeavyLoad()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 5; i++)
            {
                var layer = new BenchLayer();
                int subCount = (i == 2) ? 100 : 20;
                for (int j = 0; j < subCount; j++)
                {
                    layer.Subscribe((in BenchEvent _) => { layer.HandledCount++; return EventHandledState.Continue; });
                }
                builder.Push(layer);
            }
            builder.Build();
            for (var i = 0; i < ChallengeCount; i++) LayerHub.Send(new BenchEvent());
        }
    }

    class BenchLayer : Layer { public int HandledCount; }
    public struct BenchEvent {}
}
