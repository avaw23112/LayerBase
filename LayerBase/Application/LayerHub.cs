using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LayerBase.Core.Event;
using LayerBase.Layers;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Tools.Job;
using LayerBase.DI;

namespace LayerBase.LayerHub
{
    public enum LayerEventInfoType { Debug, Info, Warning, Error }

    public readonly struct LayerEventInfo
    {
        public readonly int LayerIndex;
        public readonly string Source;
        public readonly string EventName;
        public readonly string Message;
        public readonly Exception? Exception;
        public readonly LayerEventInfoType Type;

        public LayerEventInfo(int layerIndex, string source, string eventName, string message, LayerEventInfoType type, Exception? exception = null)
        {
            LayerIndex = layerIndex; Source = source; EventName = eventName; Message = message; Type = type; Exception = exception;
        }
        public override string ToString() => $"[{Type}] [Layer {LayerIndex}] {Source} -> {EventName}: {Message}";
    }

    public static class LayerHub
    {
        private static LayerChain? s_chain;
        
        /// <summary>
        /// 核心事件中心。设为可写是为了支持测试环境的物理断代重置。
        /// </summary>
        internal static GlobalEventCenter EventCenter { get; private set; } = new();
        
        private static int s_layerIndexCounter = 0;
        public static bool IsDebugMode { get; private set; }
        public static event Action<LayerEventInfo>? OnLayerEventInfo;

        internal static int GetNextLayerIndex() => s_layerIndexCounter++;
        public static LayersBuilder CreateLayers() => new();
        public static void Pump(float deltaTime) => s_chain?.Pump(deltaTime);

        public static void Reset()
        {
            s_chain = null;
            s_layerIndexCounter = 0;
            // 核心修复：物理断代重置，不留任何静态残余
            EventCenter = new GlobalEventCenter();
            ServiceLayerBinder.Reset();
            OnLayerEventInfo = null;
            IsDebugMode = false;
        }

        internal static void ReportInfo(LayerEventInfo info) => OnLayerEventInfo?.Invoke(info);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReportLayerEventError(int layerIndex, string source, string eventName, Exception ex)
        {
            ReportInfo(new LayerEventInfo(layerIndex, source, eventName, ex.Message, LayerEventInfoType.Error, ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReportWarning(int layerIndex, string source, string eventName, string message)
        {
            ReportInfo(new LayerEventInfo(layerIndex, source, eventName, message, LayerEventInfoType.Warning));
        }

        public sealed class LayersBuilder
        {
            private readonly LayerBase.Core.ResponsibilityChain.ResponsibilityChain _chain = new(new RcOwnerToken());
            private bool _debugMode = false;

            public LayersBuilder Push(Layer layer)
            {
                if (s_chain == null) s_chain = new LayerChain(_chain);
                s_chain.AddNode(layer);
                return this;
            }

            public LayersBuilder SetDebugMode(bool enabled)
            {
                _debugMode = enabled;
                LayerHub.IsDebugMode = enabled;
                return this;
            }

            public void Build()
            {
                if (s_chain == null) throw new InvalidOperationException("No layers added.");
                s_chain.Build(1024, true);
                if (_debugMode) ReportTopology();
            }

            private void ReportTopology()
            {
                if (s_chain == null) return;
                string summary = s_chain.GetTopologySummary();
                LayerHub.ReportInfo(new LayerEventInfo(-1, "System", "Topology", summary, LayerEventInfoType.Info));
            }
        }

        public static void InitializeJobScheduler(int workerCount) => JobSchedulers.ConfigureDefault(workerCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventHandledState Send<T>(in T value) where T : struct 
            => EventCenter.Send(value, 0, Propagation.Global);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Post<T>(in T value) where T : struct 
            => EventCenter.Post(value, 0, Propagation.Global);
    }
}
