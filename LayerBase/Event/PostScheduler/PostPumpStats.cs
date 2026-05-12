namespace LayerBase.Core.Event;

public readonly struct PostPumpStats
{
    public readonly int ProcessedCount;
    public readonly double ElapsedMilliseconds;
    public readonly int RemainingCount;
    public readonly int WavesProcessed;

    public PostPumpStats(int processedCount, double elapsedMilliseconds, int remainingCount, int wavesProcessed)
    {
        ProcessedCount = processedCount;
        ElapsedMilliseconds = elapsedMilliseconds;
        RemainingCount = remainingCount;
        WavesProcessed = wavesProcessed;
    }
}