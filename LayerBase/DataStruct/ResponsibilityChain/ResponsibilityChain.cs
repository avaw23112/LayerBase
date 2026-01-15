using System.Collections;

namespace LayerBase.Core.ResponsibilityChain
{
	/// <summary>
	/// 一个“外界可控”的双向责任链：节点知道 Prev/Next，外界可插入/删除/移动节点。
	/// </summary>
	internal sealed class ResponsibilityChain : IEnumerable<Node>
	{
		private Node m_head;
		private Node m_tail;
		private RcOwnerToken m_OwnerToken;
		public ResponsibilityChain(RcOwnerToken token)
		{
			this.m_OwnerToken = token;
		}
		public Node Head => m_head;
		public Node Tail => m_tail;
	
		public Node AddLast(Node node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			DetermineOwned(node);

			if (m_tail == null)
			{
				m_head = m_tail = node;
				return node;
			}

			node!.Prev = m_tail;       // ^ 新节点前驱 = 旧尾
			m_tail.Next = node;       // ^ 旧尾后继 = 新节点
			m_tail = node;            // ^ 更新尾指针
			
			ValidateAcyclic();
			return node;
		}

		public Node AddFirst(Node node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			DetermineOwned(node);

			if (m_head == null)
			{
				m_head = m_tail = node;
				return node;
			}

			node!.Next = m_head;       // ^ 新节点后继 = 旧头
			m_head.Prev = node;       // ^ 旧头前驱 = 新节点
			m_head = node;            // ^ 更新头指针
			ValidateAcyclic();
			return node;
		}

		public Node InsertBefore(Node anchor, Node target)
		{
			if (anchor == null) throw new ArgumentNullException(nameof(anchor));
			if (target == null) throw new ArgumentNullException(nameof(target));
			EnsureOwned(anchor);

			if (anchor == m_head)
				return AddFirst(target);

			var prev = anchor.Prev;
			target.Prev = prev;
			target.Next = anchor;

			prev!.Next = target;
			anchor.Prev = target;
			ValidateAcyclic();
			return target;
		}

		public Node InsertAfter(Node anchor, Node target)
		{
			if (anchor == null) throw new ArgumentNullException(nameof(anchor));
			if (target == null) throw new ArgumentNullException(nameof(target));
			EnsureOwned(anchor);

			if (anchor == m_tail)
				return AddLast(target);

			var next = anchor.Next;
			target.Next = next;
			target.Prev = anchor;

			next!.Prev = target;
			anchor.Next = target;
			ValidateAcyclic();
			return target;
		}

		public void Remove(Node node)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			EnsureOwned(node);
			
			node.OwnerToken.Reset();
			
			var prev = node.Prev;
			var next = node.Next;

			if (prev != null) prev.Next = next; else m_head = next;

			if (next != null) next.Prev = prev; else m_tail = prev;

			node.Prev = null;
			node.Next = null;
			ValidateAcyclic();
		}

		public void MoveBefore(Node node, Node anchor)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (anchor == null) throw new ArgumentNullException(nameof(anchor));
			EnsureOwned(node);
			EnsureOwned(anchor);

			if (node == anchor) return;
			if (node.Next == anchor) return;

			// 先摘除 node（但不清 Owner）
			Detach(node);

			// 再插入到 anchor 前
			if (anchor == m_head)
			{
				node.Prev = null;
				node.Next = m_head;
				m_head.Prev = node;
				m_head = node;
				return;
			}

			var prev = anchor.Prev;
			node.Prev = prev;
			node.Next = anchor;
			prev!.Next = node;
			anchor.Prev = node;
			ValidateAcyclic();
		}

		public void MoveAfter(Node node, Node anchor)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (anchor == null) throw new ArgumentNullException(nameof(anchor));
			EnsureOwned(node);
			EnsureOwned(anchor);

			if (node == anchor) return;
			if (node.Prev == anchor) return;

			Detach(node);

			if (anchor == m_tail)
			{
				node.Next = null;
				node.Prev = m_tail;
				m_tail.Next = node;
				m_tail = node;
				return;
			}

			var next = anchor.Next;
			node.Next = next;
			node.Prev = anchor;
			next!.Prev = node;
			anchor.Next = node;
			ValidateAcyclic();
		}

		private void Detach(Node node)
		{
			var prev = node.Prev;
			var next = node.Next;

			if (prev != null) prev.Next = next; else m_head = next;
			if (next != null) next.Prev = prev; else m_tail = prev;

			node.Prev = null;
			node.Next = null;
			ValidateAcyclic();
		}

		private void EnsureOwned(Node node)
		{
			if (node.OwnerToken.Equals(m_OwnerToken))
				throw new InvalidOperationException("Node does not belong to this chain.");
		}

		private void DetermineOwned(Node node)
		{
			if (!node.OwnerToken.Equals((m_OwnerToken)))
			{
				node.OwnerToken = m_OwnerToken;
			}
		}

		private void ValidateAcyclic()
		{
			if (m_head != null && m_head.Prev != null)
				throw new InvalidOperationException("Invalid chain: Head.Prev must be null.");

			if (m_tail != null && m_tail.Next != null)
				throw new InvalidOperationException("Invalid chain: Tail.Next must be null.");

			Node slow = m_head;
			Node fast = m_head;

			while (fast != null && fast.Next != null)
			{
				slow = slow.Next;
				fast = fast.Next.Next;

				if (ReferenceEquals(slow, fast))
					throw new InvalidOperationException("Cycle detected in responsibility chain.");
			}

			Node prev = null;
			Node cur = m_head;

			while (cur != null)
			{
				if (!cur.OwnerToken.Equals(m_OwnerToken))
					throw new InvalidOperationException("Invalid chain: node.Owner mismatch.");

				if (!ReferenceEquals(cur.Prev, prev))
					throw new InvalidOperationException("Invalid chain: Prev/Next symmetry broken.");

				// 防止自环（最常见 bug：cur.Next = cur）
				if (ReferenceEquals(cur.Next, cur))
					throw new InvalidOperationException("Invalid chain: self-loop detected (node.Next == node).");

				prev = cur;
				cur = cur.Next;
			}

			if (!ReferenceEquals(prev, m_tail))
				throw new InvalidOperationException("Invalid chain: Tail pointer mismatch.");
		}

		public Enumerator GetEnumerator() => new Enumerator(m_head);
		IEnumerator<Node> IEnumerable<Node>.GetEnumerator() => new Enumerator(m_head);
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(m_head);

		public struct Enumerator : IEnumerator<Node>
		{
			private readonly Node? m_start;
			private Node? m_current;

			internal Enumerator(Node? start)
			{
				m_start = start;
				m_current = null;
			}

			public Node Current => m_current!;
			object IEnumerator.Current => m_current!;

			public bool MoveNext()
			{
				if (m_current == null)
				{
					m_current = m_start;
				}
				else
				{
					m_current = m_current.Next;
				}
				return m_current != null;
			}

			public void Reset()
			{
				m_current = null;
			}

			public void Dispose()
			{
			}
		}
	}
}
