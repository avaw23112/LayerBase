namespace LayerBase.Core.EventCatalogue;

public static class EventCatalogue
{
    private static readonly Dictionary<string, CatalogueNode> s_mapCatalogueNodes = new();

    private static readonly Dictionary<EventCategoryToken, CatalogueNode> s_mapTokenWithNode = new();

    private static readonly Dictionary<string, EventCategoryToken> s_mapCategoryNameWithToken = new();

    public static bool IsSameCategory(EventCategoryToken origin, string categoryName)
    {
        if (!s_mapCategoryNameWithToken.TryGetValue(categoryName, out var token)) return false;

        return IsSameCategory(origin, token);
    }

    /// <summary>
    ///     查找两个Token是否有同一个父节点
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsSameCategory(EventCategoryToken origin, EventCategoryToken target)
    {
        if (!s_mapTokenWithNode.TryGetValue(origin, out var originNode) ||
            !s_mapTokenWithNode.TryGetValue(target, out var targetNode))
            return false;

        if (origin == target)
            return true;

        if (originNode.lastNode == null || targetNode.lastNode == null) return false;

        return originNode.lastNode.eventCategoryToken == targetNode.lastNode.eventCategoryToken;
    }

    /// <summary>
    ///     查找origin是否是categoryName目录的子节点
    /// </summary>
    /// <param name="categoryName"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsBelongCategory(EventCategoryToken origin, string categoryName)
    {
        if (!s_mapCategoryNameWithToken.TryGetValue(categoryName, out var token)) return false;

        return IsBelongCategory(token, origin);
    }

    /// <summary>
    ///     查找target是否是origin的子节点
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsBelongCategory(EventCategoryToken origin, EventCategoryToken target)
    {
        if (!s_mapTokenWithNode.TryGetValue(target, out var targetNode)) return false;

        var currentNode = targetNode;
        while (currentNode != null)
        {
            if (currentNode.eventCategoryToken == origin) return true;

            currentNode = currentNode.lastNode;
        }

        return false;
    }

    public static CatalogueNode Path(string originalCatalogue)
    {
        var catalogue = originalCatalogue;
        if (string.IsNullOrEmpty(originalCatalogue)) throw new Exception("错误目录");

        if (s_mapCatalogueNodes.TryGetValue(catalogue, out var node)) return node;

        var catalogueNode = new CatalogueNode();
        catalogueNode.Catalogue = catalogue;
        catalogueNode.eventCategoryToken = new EventCategoryToken(catalogueNode.GetHashCode());
        catalogueNode.lastNode = null;

        RegisterNode(catalogueNode);
        return catalogueNode;
    }

    internal static void RegisterNode(CatalogueNode node)
    {
        s_mapCatalogueNodes.Add(node.Catalogue, node);
        s_mapTokenWithNode.Add(node.GetToken(), node);
        s_mapCategoryNameWithToken.Add(node.Catalogue, node.GetToken());
    }
}

