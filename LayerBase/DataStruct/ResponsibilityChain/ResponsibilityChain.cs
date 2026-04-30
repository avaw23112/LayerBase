using System.Collections;

namespace LayerBase.Core.ResponsibilityChain;

internal sealed class ResponsibilityChain : IEnumerable<Node>
{
    private readonly RcOwnerToken m_OwnerToken;

    public ResponsibilityChain(RcOwnerToken token)
    {
        m_OwnerToken = token;
    }

    public Node? Head { get; private set; }

    public Node? Tail { get; private set; }

    IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
    {
        return new Enumerator(Head);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(Head);
    }

    public Node AddLast(Node node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        DetermineOwned(node);

        if (Tail == null)
        {
            Head = Tail = node;
            return node;
        }

        node.Prev = Tail;
        Tail.Next = node;
        Tail = node;
        ValidateAcyclic();
        return node;
    }

    public Node AddFirst(Node node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        DetermineOwned(node);

        if (Head == null)
        {
            Head = Tail = node;
            return node;
        }

        node.Next = Head;
        Head.Prev = node;
        Head = node;
        ValidateAcyclic();
        return node;
    }

    public Node InsertBefore(Node anchor, Node target)
    {
        if (anchor == null) throw new ArgumentNullException(nameof(anchor));
        if (target == null) throw new ArgumentNullException(nameof(target));
        EnsureOwned(anchor);

        if (anchor == Head)
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

        if (anchor == Tail)
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

        if (prev != null) prev.Next = next;
        else Head = next;
        if (next != null) next.Prev = prev;
        else Tail = prev;

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


        Detach(node);


        if (anchor == Head)
        {
            node.Prev = null;
            node.Next = Head;
            Head!.Prev = node;
            Head = node;
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

        if (anchor == Tail)
        {
            node.Next = null;
            node.Prev = Tail;
            Tail!.Next = node;
            Tail = node;
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

        if (prev != null) prev.Next = next;
        else Head = next;
        if (next != null) next.Prev = prev;
        else Tail = prev;

        node.Prev = null;
        node.Next = null;
        ValidateAcyclic();
    }

    private void EnsureOwned(Node node)
    {
        if (!node.OwnerToken.Equals(m_OwnerToken))
            throw new InvalidOperationException("Node does not belong to this chain.");
    }

    private void DetermineOwned(Node node)
    {
        if (!node.OwnerToken.Equals(m_OwnerToken)) node.OwnerToken = m_OwnerToken;
    }

    private void ValidateAcyclic()
    {
        if (Head != null && Head.Prev != null)
            throw new InvalidOperationException("Invalid chain: Head.Prev must be null.");

        if (Tail != null && Tail.Next != null)
            throw new InvalidOperationException("Invalid chain: Tail.Next must be null.");

        var slow = Head;
        var fast = Head;

        while (fast != null && fast.Next != null)
        {
            slow = slow!.Next;
            fast = fast.Next.Next;

            if (ReferenceEquals(slow, fast))
                throw new InvalidOperationException("Cycle detected in responsibility chain.");
        }

        Node? prev = null;
        var cur = Head;

        while (cur != null)
        {
            if (!cur.OwnerToken.Equals(m_OwnerToken))
                throw new InvalidOperationException("Invalid chain: node.Owner mismatch.");

            if (!ReferenceEquals(cur.Prev, prev))
                throw new InvalidOperationException("Invalid chain: Prev/Next symmetry broken.");


            if (ReferenceEquals(cur.Next, cur))
                throw new InvalidOperationException("Invalid chain: self-loop detected (node.Next == node).");

            prev = cur;
            cur = cur.Next;
        }

        if (!ReferenceEquals(prev, Tail))
            throw new InvalidOperationException("Invalid chain: Tail pointer mismatch.");
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(Head);
    }

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
                m_current = m_start;
            else
                m_current = m_current.Next;
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