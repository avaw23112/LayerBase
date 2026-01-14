using LayerBase.Async;
using LayerBase.Core.EventStateTrace;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.LayerChain;

namespace LayerBase.LayerHub
{
	internal sealed class ResponsibilityChainBundle
	{
		private readonly ResponsibilityChain responsibilityChain;
		private EventStateTracer? eventStateTracer;
		private Action<string>? logger;

		internal ResponsibilityChainBundle(ResponsibilityChain chain)
		{
			responsibilityChain = chain;
		}

		internal ResponsibilityChain Chain => responsibilityChain;

		internal void AddNode(Node node)
		{
			responsibilityChain.AddLast(node);
			if (eventStateTracer != null && node is Layer layer)
			{
				layer.SetEventTracer(eventStateTracer);
			}
		}

		internal void SetEventTracing(Action<string>? logger = null, int slabSize = 512, int logCapacity = 256)
		{
			eventStateTracer = new EventStateTracer(slabSize, logCapacity)
			{
				Enabled = true
			};
			this.logger = logger;
			foreach (var node in responsibilityChain)
			{
				(node as Layer)?.SetEventTracer(eventStateTracer);
			}
		}

		internal void PumpEventLogs()
		{
			if (eventStateTracer == null)
			{
				return;
			}

			foreach (var node in responsibilityChain)
			{
				(node as Layer)?.PumpEventLog();
			}
		}

		internal void PrintLog()
		{
			if (eventStateTracer == null)
			{
				return;
			}

			if (logger == null)
			{
				throw new Exception("未设置日志处理器");
			}

			var logQueue = eventStateTracer.Logs;
			while (logQueue.Count > 0)
			{
				if (logQueue.TryDequeue(out string log))
				{
					logger(log);
				}
			}
		}
	}

	public struct LayersBuilder
	{
		private ResponsibilityChainBundle chainBundle;
		internal LayersBuilder(ResponsibilityChainBundle chainBundle)
		{
			this.chainBundle = chainBundle;
		}

		public LayersBuilder Push(Node node)
		{
			chainBundle.AddNode(node);
			return this;
		}

		public LayersBuilder SetEventTracing(Action<string>? logger = null, int slabSize = 512, int logCapacity = 256)
		{
			chainBundle.SetEventTracing(logger, slabSize, logCapacity);
			return this;
		}

		public void PrintLog()
		{
			chainBundle.PrintLog();
		}
	}

	public static class LayerHub
	{
		private static List<ResponsibilityChainBundle> s_responsibilityChains = new List<ResponsibilityChainBundle>(4);
		private static LayerBaseSynchronizationContext s_Context = LayerBaseSynchronizationContext.InstallAsCurrent();

		/// <summary>
		/// 创建责任链
		/// </summary>
		/// <returns></returns>
		public static LayersBuilder CreateLayers()
		{
			var rcToken = RcOwnerToken.CreateId();
			var rc = new ResponsibilityChain(rcToken);
			var chainBundle = new ResponsibilityChainBundle(rc);
			s_responsibilityChains.Add(chainBundle);
			return new LayersBuilder(chainBundle);
		}

		/// <summary>
		/// 打印日志
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static void PrintLog()
		{
			foreach (var chainBundle in s_responsibilityChains)
			{
				chainBundle.PrintLog();
			}
		}
		
		// ----------------------事件日志----------------------------------------
		
		/// <summary>
		/// 处理所有层级的Buffer Events,
		/// 处理所有异步事件
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static void Pump()
		{
			PumpBufferEvents();
			PumpAsyncEvents();
			PumpEventLogs();
		}
		
		private static void PumpAsyncEvents()
		{
			s_Context.Update();
		}
		private static void PumpEventLogs()
		{
			foreach (var chainBundle in s_responsibilityChains)
			{
				chainBundle.PumpEventLogs();
			}
		}
		private static void PumpBufferEvents()
		{
			foreach (var chainBundle in s_responsibilityChains)
			{
				foreach (var node in chainBundle.Chain)
				{
					(node as Layer)?.Pump();
				}
			}
		}
	}
}
