using LayerBase.Async;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Event.Delay;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Layers.LayerMetaData;
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
            if (layerType == LayerType.Singleton && node is Layer layer)
            {
                LayerHub.PushInstanceLayer(layer);
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
            if (!_releaseMode)
            {
                _chain.SetLogTracing(_logger, _logQueueCapacity);
            }
        }
    }

    public static class LayerHub
    {
        private static readonly List<LayerChain> s_responsibilityChains = new(4);
        private static LayerBaseSynchronizationContext s_context = LayerBaseSynchronizationContext.InstallAsCurrent();

        public static event Action<LayerEventErrorInfo>? OnLayerEventError;

        public static Dictionary<Type, Layer> InstanceLayers = new();

        /// <summary>
        /// Test hook: reset global state between test runs.
        /// </summary>
        public static void Reset()
        {
            EventMetaDataHandler.Clear();
            LayerMetaData.Clear();
            s_responsibilityChains.Clear();
            OnLayerEventError = null;
            InstanceLayers.Clear();
            TimerSchedulers.Clear();
            JobSchedulers.ResetDefault();
            DelayPublisherManager.Instance.Clear();
            s_context.Dispose();
        }

        /// <summary>
        /// Initialize the global scheduler for parallel handlers.
        /// </summary>
        public static void InitializeJobScheduler(int workerCount = 0, int queueCapacity = 0)
        {
            JobSchedulers.ConfigureDefault(workerCount, queueCapacity);
        }

        /// <summary>
        /// Create a new layer chain.
        /// </summary>
        public static LayersBuilder CreateLayers(int eventStateSlabSize = 512)
        {
            var rcToken = RcOwnerToken.CreateId();
            var rc = new ResponsibilityChain(rcToken);
            var chainBundle = new LayerChain(rc);

            s_responsibilityChains.Add(chainBundle);
            return new LayersBuilder(chainBundle).SetEventStateSlabSize(eventStateSlabSize);
        }

        internal static void PushInstanceLayer<T>(T layer) where T : Layer
        {
            var layerType = layer.GetType();
            if (InstanceLayers.ContainsKey(layerType))
            {
                throw new Exception($"{layerType} has already been pushed.");
            }

            InstanceLayers.Add(layerType, layer);
        }

        public static Layer ResolveInstance<T>()
        {
            if (!InstanceLayers.TryGetValue(typeof(T), out Layer layer))
            {
                throw new ArgumentException($"{typeof(T).Name} does not exist.");
            }

            return layer;
        }

        public static void Pump(float deltaTime)
        {
            s_context.Update();
            DelayPublisherManager.Instance.Update(deltaTime);
            PumpLayers();
            EventMetaDataHandler.PumpExpectations();
            PumpEventLogs();
            TimerSchedulers.TickAll(deltaTime);
        }

        internal static void ReportLayerEventError(
            string layerFullName,
            string handlerFullName,
            string eventFullName,
            Exception exception)
        {
            var callbacks = OnLayerEventError;
            if (callbacks == null)
            {
                return;
            }

            var errorInfo = new LayerEventErrorInfo(layerFullName, handlerFullName, eventFullName, exception);
            foreach (Action<LayerEventErrorInfo> callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback(errorInfo);
                }
                catch
                {
                    // Error observers should not impact event dispatch.
                }
            }
        }

        private static void PumpEventLogs()
        {
            foreach (var chainBundle in s_responsibilityChains)
            {
                chainBundle.PrintLog();
            }
        }

        private static void PumpLayers()
        {
            foreach (var chainBundle in s_responsibilityChains)
            {
                chainBundle.Pump();
            }
        }
    }
}
