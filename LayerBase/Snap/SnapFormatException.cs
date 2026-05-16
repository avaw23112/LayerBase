namespace LayerBase.Snap;

public sealed class SnapFormatException : Exception
{
    public SnapFormatException(string message)
        : base(message)
    {
    }

    public SnapFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
