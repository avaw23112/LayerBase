using Arch.Core;

namespace LayerBase.ECS.Runtime.Submission;

internal interface IEcsWorkExecutor
{
    string DebugName { get; }

    void Execute(World world, in EcsWorkRecord record, EcsSubmissionBatch batch);
}

internal static class EcsExecutorRegistry
{
    private static readonly List<IEcsWorkExecutor> Executors = new();
    private static readonly object Sync = new();

    public static int Register(IEcsWorkExecutor executor)
    {
        lock (Sync)
        {
            int id = Executors.Count;
            Executors.Add(executor);
            return id;
        }
    }

    public static string GetDebugName(int executorId)
    {
        lock (Sync)
        {
            return Executors[executorId].DebugName;
        }
    }

    public static void Execute(
        int executorId,
        World world,
        in EcsWorkRecord record,
        EcsSubmissionBatch batch)
    {
        IEcsWorkExecutor executor;
        lock (Sync)
        {
            executor = Executors[executorId];
        }

        executor.Execute(world, in record, batch);
    }
}
