namespace LayerBase.Usage;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== LayerBase Framework Showcase ===\n");

        BasicUsage.Run();
        Console.WriteLine();

        AsyncUsage.Run();
        Console.WriteLine();

        ParallelUsage.Run();
        Console.WriteLine();

        DelayUsage.Run();
        Console.WriteLine();

        ServiceUsage.Run();
        Console.WriteLine();

        CallUsage.Run();
        Console.WriteLine();

        SharedFieldUsage.Run();
        Console.WriteLine();

        ActorRuntimeUsage.Run();
        Console.WriteLine();

        await ScopeUsage.Run();
        Console.WriteLine();

        EcsQueryUsage.Run();
        Console.WriteLine();

        ExceptionHandlingUsage.Run();
        Console.WriteLine();

        Console.WriteLine("=== Showcase Finished ===");
        GeneratorVerification.Run();
    }
}
