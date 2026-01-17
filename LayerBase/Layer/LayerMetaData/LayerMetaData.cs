using LayerBase.Core.EventCatalogue;

namespace LayerBase.Layers.LayerMetaData;


public enum LayerDispatchStrategy
{
    /// <summary>
    /// 抛弃事件:将事件在此层抛弃
    /// </summary>
    Throw,
    
    /// <summary>
    /// 积压事件:不处理,但不抛弃,也不推送给其他层
    /// </summary>
    Post,
    
    /// <summary>
    /// 忽略事件:不处理,但推送给其他层
    /// </summary>
    Ignore,
    
    /// <summary>
    /// 正常处理:处理,且推送给其他层
    /// </summary>
    None
}

public static class LayerMetaData
{
    
    private static Dictionary<Type, Dictionary<EventCategoryToken, LayerDispatchStrategy>> m_dispatchStrategyByType = new();
    
    public static LayerDispatchStrategy GetDispatchStrategy(Type layerType, EventCategoryToken category)
    {
        LayerDispatchStrategy strategy = LayerDispatchStrategy.None;
        if (!m_dispatchStrategyByType.TryGetValue(layerType,
                out Dictionary<EventCategoryToken, LayerDispatchStrategy> dispatchStrategy))
        {
            dispatchStrategy = new Dictionary<EventCategoryToken, LayerDispatchStrategy>();
            m_dispatchStrategyByType.Add(layerType, dispatchStrategy);
        }
        else
        {
            dispatchStrategy.TryGetValue(category, out strategy);
        }
        return strategy;
    }
    public static LayerDispatchStrategy GetDispatchStrategy<Layer>(EventCategoryToken category) where Layer : LayerBase.Layers.Layer
    {
        LayerDispatchStrategy strategy = LayerDispatchStrategy.None;
        if (!m_dispatchStrategyByType.TryGetValue(typeof(Layer),
                out Dictionary<EventCategoryToken, LayerDispatchStrategy> dispatchStrategy))
        {
            dispatchStrategy = new Dictionary<EventCategoryToken, LayerDispatchStrategy>();
            m_dispatchStrategyByType.Add(typeof(Layer), dispatchStrategy);
        }
        else
        {
            dispatchStrategy.TryGetValue(category, out strategy);
        }
        return strategy;
    }
    
    public static void SetDispatchStrategy<Layer>(EventCategoryToken category, LayerDispatchStrategy strategy) where Layer : LayerBase.Layers.Layer
    {
        if (!m_dispatchStrategyByType.TryGetValue(typeof(Layer),
                out Dictionary<EventCategoryToken, LayerDispatchStrategy> dispatchStrategy))
        {
            dispatchStrategy = new Dictionary<EventCategoryToken, LayerDispatchStrategy>();
            m_dispatchStrategyByType.Add(typeof(Layer), dispatchStrategy);
        }
        dispatchStrategy[category] = strategy;
    }
}

public class LayerMetaData<Layer> where Layer : LayerBase.Layers.Layer
{
    public static void SetDispatchStrategy(EventCategoryToken category, LayerDispatchStrategy strategy)
    {
        LayerMetaData.SetDispatchStrategy<Layer>(category, strategy);
    }
    public static LayerDispatchStrategy GetDispatchStrategy(EventCategoryToken category)
    {
        return LayerMetaData.GetDispatchStrategy<Layer>(category);
    }
}