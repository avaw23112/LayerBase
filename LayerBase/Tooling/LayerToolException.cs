namespace LayerBase.Tooling;

public sealed class LayerToolException : InvalidOperationException
{
    public LayerToolException(string message)
        : base(message)
    {
    }
}
