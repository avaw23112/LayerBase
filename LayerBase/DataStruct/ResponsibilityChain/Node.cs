namespace LayerBase.Core.ResponsibilityChain;

/// <summary>
/// 责任链节点的抽象基类。包含前后指针和所属令牌。
/// Layer 类继承自 Node，使其可以被组织到责任链中。
/// </summary>
public abstract class Node
{
    internal Node? Next;
    internal Node? Prev;
    public Node? Previous => Prev;
    public Node? NextNode => Next;

    public RcOwnerToken OwnerToken { get; set; } = RcOwnerToken.Zero;
}