using LayerBase.Async;
using LayerBase.Core.Event;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Event.Delay;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Tools.Job;
using LayerBase.Tools.Timer;

namespace LayerBase.LayerHub
{
    public readonly struct LayerEventErrorInfo
    {
        public LayerEventErrorInfo(string layerFullName, string handlerFullName, string eventFullName, Exception exception)
        {
            LayerFullName = layerFullName;
            HandlerFullName = handlerFullName;
            EventFullName = eventFullName;
            Exception = exception;
        }

        public string LayerFullName { get; }
        public string HandlerFullName { get; }
        public string EventFullName { get; }
        public Exception Exception { get; }
    }

    public enum LayerType
    {
        Singleton,
        Scope
    }

    public struct LayersBuilder
    {
        private LayerChain _chain;
        private Action<string>? _logger;
        private int _logQueueCapacity;
        private int _eventStateSlabSize;
        private bool _releaseMode;

        internal LayersBuilder(LayerChain chain)
        {
            _chain = chain;
            _logger = null;
            _logQueueCapacity = 256;
            _eventStateSlabSize = 256;
            _releaseMode = false;
        }

        public LayersBuilder Push(Node node, LayerType layerType = LayerType.Scope)
        {
            _chain.AddNode(node);
            if (node is Layer layer)
            {
                if (layer.RouteIndex == -1)
                {
                    int index = LayerHub.GetNextLayerIndex();
                    layer.SetRouteIndex(index);
                    LayerHub.EventCenter.EnsureSlots(index + 1, layer.GetType().Name);
                }

                if (layerType == LayerType.Singleton)
                {
                    LayerHub.PushInstanceLayer(layer);
                }
            }

            return this;
        }

        public LayersBuilder SetLogTracing(Action<string>? logger = null, int logQueueCapacity = 256)
        {
            _logger = logger;
            _logQueueCapacity = logQueueCapacity;
            return this;
        }

        public LayersBuilder SetRelease(bool release = true)
        {
            _releaseMode = release;
            return this;
        }

        public LayersBuilder SetEventStateSlabSize(int eventStateSlabSize = 256)
        {
            _eventStateSlabSize = eventStateSlabSize;
            return this;
        }

        public void Build()
        {
            _chain.Build(_eventStateSlabSize, _releaseMode);
            _chain.SetLogTracing(_logger, _logQueueCapacity);
        }
    }

    public static class LayerHub
    {
        private static GlobalEventCenter? s_eventCenter;
        internal static GlobalEventCenter EventCenter => s_eventCenter ??= new();

        private static readonly List<LayerChain> s_responsibilityChains = new(4);
        private static LayerBaseSynchronizationContext s_context = LayerBaseSynchronizationContext.InstallAsCurrent();
        private static int s_nextLayerIndex = 0;
        
        public static event Action<LayerEventErrorInfo>? OnLayerEventError;

        public static Dictionary<Type, Layer> InstanceLayers = new();

        /// <summary>
        /// Test hook: reset global state between test runs.
        /// </summary>
        public static void Reset()
        {
            EventMetaDataHandler.Clear();
            s_eventCenter = null;
            s_responsibilityChains.Clear();
            OnLayerEventError = null;
            InstanceLayers.Clear();
            TimerSchedulers.Clear();
            JobSchedulers.ResetDefault();
            DelayPublisherManager.Instance.Clear();
            s_context.Dispose();
            s_nextLayerIndex = 0;
        }

        internal static int GetNextLayerIndex() => Interlocked.Increment(ref s_nextLayerIndex) - 1;

        // -----------------Global Event APIs-------------------

        /// <summary>
        /// 同步发送全局广播事件（从 Layer 0 开始）。
        /// </summary>
        public static void Send<T>(T value) where T : struct
        {
            EventCenter.Send(value, 0, Propagation.Global);
        }

        /// <summary>
        /// 异步入队全局广播事件（从 Layer 0 开始）。
        /// </summary>
        public static void Post<T>(T value) where T : struct
        {
            EventCenter.Post(value, 0, Propagation.Global);
        }
    
        public static void InitializeJobScheduler(int workerCount = 0, int queueCapacity = 0)
        {
            JobSchedulers.ConfigureDefault(workerCount, queueCapacity);
        }

        public static LayersBuilder CreateLayers()
        {
            var rc = new ResponsibilityChain(RcOwnerToken.CreateId());
            var chainBundle = new LayerChain(rc);
            s_responsibilityChains.Add(chainBundle);
            return new LayersBuilder(chainBundle);
        }

        public static void Pump(float deltaTime)
        {
            s_context.Update();
            PumpLayers();
            TimerSchedulers.TickAll(deltaTime);
            DelayPublisherManager.Instance.Update(deltaTime);
        }

        internal static void PushInstanceLayer(Layer layer)
        {
            InstanceLayers[layer.GetType()] = layer;
        }

        internal static void ReportLayerEventError(int layerIndex, string handlerFullName, string eventFullName, Exception exception)
        {
            string layerName = GetLayerNameByIndex(layerIndex);
            OnLayerEventError?.Invoke(new LayerEventErrorInfo(layerName, handlerFullName, eventFullName, exception));
        }

        internal static string GetLayerNameByIndex(int index)
        {
            return EventCenter.GetLayerName(index);
        }

        private static void PumpLayers()
        {
            for (int i = 0; i < s_responsibilityChains.Count; i++)
            {
                s_responsibilityChains[i].Pump();
            }
        }
    }
}
