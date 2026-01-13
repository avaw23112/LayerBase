using System;
using Events.Core.ResponsibilityChain;
using System.Collections.Generic;
using Events.LayerChain;

namespace Events.EventHub
{
	public struct LayerBuilder
	{
		private ResponsibilityChain rc;
		
		internal LayerBuilder(ResponsibilityChain rc)
		{
			this.rc = rc;
		}

		public LayerBuilder Push(Node node)
		{
			rc.AddLast(node);
			return this;
		}

	}
	public static class EventHub
	{
		private static List<ResponsibilityChain> m_responsibilityChains = new List<ResponsibilityChain>(4);
		
		/// <summary>
		/// 创建责任链
		/// </summary>
		/// <returns></returns>
		public static LayerBuilder CreateHub()
		{
			var rcToken = RcOwnerToken.CreateId();
			var rc = new ResponsibilityChain(rcToken);
			m_responsibilityChains.Add(rc);
			return new LayerBuilder(rc);
		}
		
		/// <summary>
		/// 处理所有层级的Buffer Events
		/// </summary>
		/// <exception cref="Exception"></exception>
		public static void Pump()
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