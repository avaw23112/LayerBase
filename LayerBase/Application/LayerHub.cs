using LayerBase.Async;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using LayerBase.Layers.LayerMetaData;
using LayerBase.Tools.Timer;

namespace LayerBase.LayerHub
{
	public enum LayerType
	{
		Singleton,
		Scope
	}
	public struct LayersBuilder
	{
		private LayerChain _chain;
		private Action<string>? _logger;
		private int _logQueueCapacity = 256;
		private int _eventStateSlabSize = 256;
		private bool _releaseMode;
		
		internal LayersBuilder(LayerChain chain)
		{
			this._chain = chain;
		}
		public LayersBuilder Push(Node node,LayerType layerType = LayerType.Scope)
		{
			this._chain.AddNode(node);
			if (LayerType.Singleton == layerType && node is Layer layer)
			{
				LayerHub.PushInstanceLayer(layer);
			}
			return this;
		}
		public LayersBuilder SetLogTracing(Action<string>? logger = null,int logQueueCapacity = 256)
		{
			this._logger = logger;
			this._logQueueCapacity = logQueueCapacity;
			return this;
		}

		public LayersBuilder SetRelease(bool release = true)
		{
			_releaseMode = release;
			return this;
		}

		public LayersBuilder SetEventStateSlabSize(int eventStateSlabSize = 256)
		{
			this._eventStateSlabSize = eventStateSlabSize;
			return this;
		}

		public void Build()
		{
			_chain.Build(_eventStateSlabSize, _releaseMode);
			if (!_releaseMode)
			{
				_chain.SetLogTracing(_logger,_logQueueCapacity);
			}
		}
	}

	public static class LayerHub
	{
		private static List<LayerChain> s_responsibilityChains = new List<LayerChain>(4);
		private static LayerBaseSynchronizationContext s_Context = LayerBaseSynchronizationContext.InstallAsCurrent();
		
        public static Dictionary<Type,Layer> InstanceLayers = new Dictionary<Type, Layer>();

		/// <summary>
		/// Test hook: reset global state between test runs.
		/// </summary>
		public static void Reset()
		{
			EventMetaDataHandler.Clear();
			LayerMetaData.Clear();
			s_responsibilityChains.Clear();
			TimerSchedulers.Clear();
		}

		/// <summary>
		/// 创建责任链
		/// </summary>
		/// <returns></returns>
		public static LayersBuilder CreateLayers(int EventStateSlabSize = 512)
		{
			var rcToken = RcOwnerToken.CreateId();
			var rc = new ResponsibilityChain(rcToken);
			
			var chainBundle = new LayerChain(rc);
			
			s_responsibilityChains.Add(chainBundle);
			return new LayersBuilder(chainBundle);
		}
		
		internal static void PushInstanceLayer<T>(T layer) where T : Layer
		{
			var layerType = typeof(T);
			if (InstanceLayers.ContainsKey(layerType))
			{
				throw new Exception($"{layerType} has already been pushed.");
			}
			InstanceLayers.Add(layerType, layer);
		}
		public static Layer ResolveInstance<T>() where T : Layer
		{
			if (!InstanceLayers.TryGetValue(typeof(T), out Layer layer))
			{
				throw new ArgumentException($"{typeof(T).Name}不存在");
			}
			return layer;
		}

		public static void Pump(float deltaTime)
		{
			PumpLayers();
			PumpAsyncEvents();
			PumpEventLogs();
			TimerSchedulers.TickAll(deltaTime);
		}
		
		
		//---------------内部方法-------------------------------
		
		private static void PumpAsyncEvents()
		{
			s_Context.Update();
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
