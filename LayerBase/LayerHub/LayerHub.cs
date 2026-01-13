using LayerBase.Async;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.LayerChain;

namespace LayerBase.LayerHub
{
	public struct LayersBuilder
	{
		private ResponsibilityChain rc;

		internal LayersBuilder(ResponsibilityChain rc)
		{
			this.rc = rc;
		}

		public LayersBuilder Push(Node node)
		{
			rc.AddLast(node);
			return this;
		}
	}

	public static class LayerHub
	{
		private static List<ResponsibilityChain> m_responsibilityChains = new List<ResponsibilityChain>(4);
		private static LayerBaseSynchronizationContext m_Context = LayerBaseSynchronizationContext.InstallAsCurrent();

		/// <summary>
		/// 创建责任链
		/// </summary>
		/// <returns></returns>
		public static LayersBuilder CreateLayers()
		{
			var rcToken = RcOwnerToken.CreateId();
			var rc = new ResponsibilityChain(rcToken);
			m_responsibilityChains.Add(rc);
			return new LayersBuilder(rc);
		}

		/// <summary>
		/// 处理所有层级的Buffer Events,
		/// 处理所有异步事件
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static void Pump()
		{
			PumpBufferEvents();
			PumpAsyncEvents();
		}

		private static void PumpAsyncEvents()
		{
			m_Context.Update();
		}

		private static void PumpBufferEvents()
		{
			foreach (var VARIABLE in m_responsibilityChains)
			{
				//遍历链表将每一层的事件都Pump掉
				var Node = VARIABLE.Head;
				while (Node != VARIABLE.Tail)
				{
					Layer layer = Node as Layer;
					if (layer == null)
					{
						throw new Exception("存在空层级");
					}
					layer.Pump();
					Node = Node.NextNode;
				}

				//尾节点Pump
				if (Node != null)
				{
					Layer layer = Node as Layer;
					if (layer == null)
					{
						throw new Exception("存在空层级");
					}
					layer.Pump();
				}
			}
		}
	}
}