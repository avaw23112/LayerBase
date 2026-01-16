namespace LayerBase.Core.EventCatalogue;

public class CatalogueNode
{
    private  Dictionary<string, CatalogueNode>? nextLayer;
    public EventCategoryToken eventCategoryToken;
    public string Catalogue;
    public CatalogueNode? lastNode;

    public CatalogueNode Combine(string subCatalogue)
    {
        if (string.IsNullOrEmpty(subCatalogue))
        {
            throw new Exception("错误目录");
        }
        
        if (nextLayer == null)
        {
            nextLayer = new Dictionary<string, CatalogueNode>(4);
        }

        CatalogueNode catalogueNode = new CatalogueNode();
        catalogueNode.Catalogue = subCatalogue;
        catalogueNode.lastNode = this;
        catalogueNode.eventCategoryToken = new EventCategoryToken(catalogueNode.GetHashCode());
        
        nextLayer.Add(subCatalogue,catalogueNode);
        EventCatalogue.RegisterNode(catalogueNode);
        return catalogueNode;
    }

    public EventCategoryToken GetToken() => eventCategoryToken;
}