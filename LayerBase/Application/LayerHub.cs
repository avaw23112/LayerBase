using LayerBase.Async;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Layers;

namespace LayerBase.LayerHub
{
	public struct LayersBuilder
	{
		private LayerChain _chain;
		internal LayersBuilder(LayerChain chain)
		{
			this._chain = chain;
		}
		public LayersBuilder Push(Node node)
		{
			_chain.AddNode(node);
			return this;
		}
		public LayersBuilder SetLogTracing(Action<string>? logger = null,int logQueueCapacity = 256)
		{
			_chain.SetLogTracing(logger,logQueueCapacity);
			return this;
		}
	}

	public static class LayerHub
	{
		private static List<LayerChain> s_responsibilityChains = new List<LayerChain>(4);
		private static LayerBaseSynchronizationContext s_Context = LayerBaseSynchronizationContext.InstallAsCurrent();

		/// <summary>
		/// 创建责任链
		/// </summary>
		/// <returns></returns>
		public static LayersBuilder CreateLayers(int EventStateSlabSize = 512)
		{
			var rcToken = RcOwnerToken.CreateId();
			var rc = new ResponsibilityChain(rcToken);
			
			var chainBundle = new LayerChain(rc);
			chainBundle.Build(EventStateSlabSize);
			
			s_responsibilityChains.Add(chainBundle);
			return new LayersBuilder(chainBundle);
		}

		public static void Pump()
		{
			PumpLayers();
			PumpAsyncEvents();
			PumpEventLogs();
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
