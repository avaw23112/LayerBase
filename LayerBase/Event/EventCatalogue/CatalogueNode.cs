namespace LayerBase.Core.EventCatalogue;

public class CatalogueNode
{
    private Dictionary<string, CatalogueNode>? _children;
    public EventCategoryToken eventCategoryToken = EventCategoryToken.Empty;
    public string Catalogue { get; set; } = string.Empty;
    public CatalogueNode? lastNode;

    public CatalogueNode Combine(string subCatalogue)
    {
        if (string.IsNullOrEmpty(subCatalogue))
        {
            throw new Exception("错误目录");
        }
        
        _children ??= new Dictionary<string, CatalogueNode>(4);

        var node = new CatalogueNode
        {
            Catalogue = subCatalogue,
            lastNode = this,
            eventCategoryToken = new EventCategoryToken(subCatalogue.GetHashCode()),
        };
        
        _children.Add(subCatalogue, node);
        EventCatalogue.RegisterNode(node);
        return node;
    }

    public EventCategoryToken GetToken() => eventCategoryToken;
}
