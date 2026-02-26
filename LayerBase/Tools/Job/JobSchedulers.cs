using System.Threading;

namespace LayerBase.Tools.Job;

/// <summary>
/// 全局 JobScheduler 管理器。
/// </summary>
public static class JobSchedulers
{
    private static JobScheduler s_default = new();

    public static JobScheduler Default => Volatile.Read(ref s_default);

    public static void ConfigureDefault(int workerCount = 0, int queueCapacity = 0)
    {
        var scheduler = new JobScheduler(workerCount, queueCapacity);
        var previous = Interlocked.Exchange(ref s_default, scheduler);
        previous.Dispose();
    }

    public static void ResetDefault()
    {
        ConfigureDefault();
    }
}
