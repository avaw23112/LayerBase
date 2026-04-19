using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            // 运行所有定义的基准测试类
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    // 定义一个公共基类，开启内存诊断
    [MemoryDiagnoser]
    [HideColumns("Error", "StdDev", "Median", "RatioSD")]
    public abstract class EventBenchmarkBase
    {
        protected const int OneMillion = 1_000_000;
        protected const int TenThousand = 10_000;
    }

    public class SingleLayer_Low_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var l = new BenchLayer(); l.RegisterService(new OptimizedManager());
            LayerHub.CreateLayers().Push(l).Build(); 
        }
        [Benchmark(Description = "单层低压 (1层/1订阅) - 100万次")]
        public void Run() { for (int i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent()); }
    }

    public class SingleLayer_High_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var l = new BenchLayer(); for (int i = 0; i < 10; i++) l.RegisterService(new OptimizedManager());
            LayerHub.CreateLayers().Push(l).Build(); 
        }
        [Benchmark(Description = "单层高压 (1层/10订阅) - 100万次")]
        public void Run() { for (int i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent()); }
    }

    public class MultiLayer_Low_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 9; i++) builder.Push(new BenchLayer());
            var tail = new BenchLayer(); tail.RegisterService(new OptimizedManager());
            builder.Push(tail).Build(); 
        }
        [Benchmark(Description = "多层低压 (10层/尾订阅) - 100万次")]
        public void Run() { for (int i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent()); }
    }

    public class MultiLayer_Full_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 10; i++) { var l = new BenchLayer(); l.RegisterService(new OptimizedManager()); builder.Push(l); }
            builder.Build(); 
        }
        [Benchmark(Description = "多层高压 (10层/全订阅) - 100万次")]
        public void Run() { for (int i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent()); }
    }

    public class Extreme_Empty_64_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 64; i++) builder.Push(new BenchLayer());
            builder.Build(); 
        }
        [Benchmark(Description = "极限空负载 (64层/0订阅) - 100万次")]
        public void Run() { for (int i = 0; i < OneMillion; i++) LayerHub.Send(new BenchEvent()); }
    }

    public class Typical_Heavy_180_Bench : EventBenchmarkBase {
        [GlobalSetup] public void Setup() { 
            LayerHub.Reset(); 
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 5; i++) {
                var l = new BenchLayer();
                int count = (i == 0) ? 100 : 20;
                for (int j = 0; j < count; j++) l.RegisterService(new OptimizedManager());
                builder.Push(l);
            }
            builder.Build(); 
        }
        [Benchmark(Description = "中重度实战 (5层/180订阅) - 1万次")]
        public void Run() { for (int i = 0; i < TenThousand; i++) LayerHub.Send(new BenchEvent()); }
    }

    // 辅助类
    public partial class OptimizedManager : LayerBase.DI.IService {
        public void ConfigureServices(LayerBase.DI.IServiceCollection s) => s.AddSingleton(this);
        [Subscribe] public EventHandledState Handle(in BenchEvent e) => EventHandledState.Continue;
    }
    public class BenchLayer : Layer { }
    public struct BenchEvent { }
}
