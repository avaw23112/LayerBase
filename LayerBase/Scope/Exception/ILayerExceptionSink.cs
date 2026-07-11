namespace LayerBase.Scope;

/// <summary>
/// 统一异常回调接口。
/// </summary>
public interface ILayerExceptionSink
{
    void OnException(in LayerExceptionRecord record);

    void OnExceptionQueueOverflow(int droppedCount, in LayerExceptionRecord lastRecord);
}
