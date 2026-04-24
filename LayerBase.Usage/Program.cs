namespace LayerBase.Usage;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== LayerBase Framework Showcase ===\n");

        BasicUsage.Run();
        Console.WriteLine();

        AsyncUsage.Run();
        Console.WriteLine();

        ParallelUsage.Run();
        Console.WriteLine();

        PropagationUsage.Run();
        Console.WriteLine();

        DelayUsage.Run();
        Console.WriteLine();

        ServiceUsage.Run();
        Console.WriteLine();

        CallUsage.Run();
        Console.WriteLine();

        SharedFieldUsage.Run();
        Console.WriteLine();

        Console.WriteLine("=== Showcase Finished ===");
    }
}
