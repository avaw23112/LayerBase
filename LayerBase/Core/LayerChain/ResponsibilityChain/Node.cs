namespace LayerBase.Core.ResponsibilityChain
{
	/// <summary>
	/// 链表节点（双向）：持有处理器，并能看到前后节点。
	/// </summary>
	public abstract class Node
	{
		internal Node Prev;
		internal Node Next;
		public Node Previous => Prev;       // ^ Previous：对外只读访问前驱节点
		public Node NextNode => Next;       // ^ NextNode：对外只读访问后继节点

		public RcOwnerToken OwnerToken { get; set; } = RcOwnerToken.Zero;
	}
}