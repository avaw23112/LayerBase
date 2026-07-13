namespace LayerBase.Snap;

public sealed class SnapLimitExceededException : Exception
{
    public SnapLimitExceededException(string message)
        : base(message)
    {
    }
}
