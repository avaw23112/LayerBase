namespace LayerBase.Core.ResponsibilityChain;

public abstract class Node
{
    internal Node? Next;
    internal Node? Prev;
    public Node? Previous => Prev;
    public Node? NextNode => Next;

    public RcOwnerToken OwnerToken { get; set; } = RcOwnerToken.Zero;
}