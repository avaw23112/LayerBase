namespace LayerBase.ECS.Runtime;

public readonly struct EcsDrainStats
{
    public EcsDrainStats(int drained, int failed)
    {
        Drained = drained;
        Failed = failed;
    }

    public int Drained { get; }

    public int Failed { get; }
}
