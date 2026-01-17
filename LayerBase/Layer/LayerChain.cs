using LayerBase.Core.EventStateTrace;
using LayerBase.Core.ResponsibilityChain;
using LayerBase.Event.EventMetaData;

namespace LayerBase.Layers;

internal sealed class LayerChain
{
    private readonly ResponsibilityChain responsibilityChain;
    private EventStateTracer? eventStateTracer;
    private EventLogTracer? _eventLogTracer;
    private Action<string>? logger;
    internal LayerChain(ResponsibilityChain chain)
    {
        responsibilityChain = chain;
    }
    internal ResponsibilityChain Chain => responsibilityChain;
    internal void AddNode(Node node)
    {
        responsibilityChain.AddLast(node);
        if (eventStateTracer != null && node is Layer layer)
        {
            layer.SetEventTracer(eventStateTracer);
        }
    }

    internal void Build(int slabSize = 512)
    {
        eventStateTracer = new EventStateTracer(slabSize);
        
        // 注册事件元数据创建事件
        eventStateTracer.OnClassicEventCompleted = EventMetaDataHandler.OnClassicEventDestroyed;
        eventStateTracer.OnClassicEventCreated = EventMetaDataHandler.OnClassicEventCreated;
        
        // 构建层级
        foreach (var node in responsibilityChain)
        {
            (node as Layer)?.SetEventTracer(eventStateTracer);
            (node as Layer)?.Build();
        }
    }
    internal void SetLogTracing(Action<string>? logger = null,int logQueueCapacity = 256)
    {
        this.logger = logger;
        _eventLogTracer = new EventLogTracer(logQueueCapacity);
        
        foreach (var node in responsibilityChain)
        {
            (node as Layer)?.SetEventLogTracer(_eventLogTracer);
        }
    }

    internal void Pump()
    {
        if (eventStateTracer == null)
        {
            return;
        }

        foreach (var node in responsibilityChain)
        {
            (node as Layer)?.Pump();
        }
    }

    internal void PrintLog()
    {
        if (_eventLogTracer == null)
        {
            return;
        }

        if (logger == null)
        {
            throw new Exception("未设置日志处理器");
        }

        var logQueue = _eventLogTracer.Logs;
        while (logQueue.Count > 0)
        {
            if (logQueue.TryDequeue(out string log))
            {
                logger(log);
            }
        }
    }
}