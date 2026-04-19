using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LayerBase.Core.Event;
using LayerBase.LayerHub;
using LayerBase.Layers;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class EventBusBenchmarks
    {
        private const int EventCount = 1_000_000;
        
        [GlobalSetup]
        public void Setup()
        {
            LayerHub.Reset();
        }

        [Benchmark]
        public void HighDensity_10Layers_AllSubscribe()
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

        [Benchmark]
        public void LowDensity_10Layers_SingleSubscribe()
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
        
        [Benchmark]
        public void Challenge_10k_Events_3Layers()
        {
            LayerHub.Reset();
            var builder = LayerHub.CreateLayers();
            for (int i = 0; i < 3; i++) {
                var l = new BenchLayer();
                l.Subscribe((in BenchEvent _) => { l.HandledCount++; return EventHandledState.Continue; });
                builder.Push(l);
            }
            builder.Build();

            for (var i = 0; i < 10_000; i++) LayerHub.Send(new BenchEvent());
        }
    }

    class BenchLayer : Layer { public int HandledCount; }
    public struct BenchEvent {}

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<EventBusBenchmarks>();
        }
    }
}