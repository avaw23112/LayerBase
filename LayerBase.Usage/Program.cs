using Usage;

namespace LayerBase.Usage;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== LayerBase Framework All-in-One Showcase ===\n");

        // 1. 基础分发
        BasicUsage.Run();
        Console.WriteLine();

        // 2. 异步处理
        AsyncUsage.Run();
        Console.WriteLine();

        // 3. 并行隔离
        ParallelUsage.Run();
        Console.WriteLine();

        // 4. 传播控制
        PropagationUsage.Run();
        Console.WriteLine();

        // 5. 延迟分发
        DelayUsage.Run();
        Console.WriteLine();

        // 6. 服务 DI
        ServiceUsage.Run();
        Console.WriteLine();

        Console.WriteLine("\n=== Showcase Finished ===");
    }
}