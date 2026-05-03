namespace LayerBase.Async;

/// <summary>
/// 主线程 CompletionQueue 的异常处理策略。
/// </summary>
public enum CompletionExceptionPolicy
{
    /// <summary>
    /// 抛出异常。
    /// 适合 Debug 模式，方便尽早暴露问题。
    /// </summary>
    Throw,

    /// <summary>
    /// 上报异常并继续处理后续 completion。
    /// 适合 Release 模式或容错运行环境。
    /// </summary>
    ReportAndContinue
}
