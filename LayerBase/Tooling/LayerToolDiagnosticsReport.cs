namespace LayerBase.Tooling;

public sealed class LayerToolDiagnosticsReport
{
    public LayerToolDiagnosticsReport(
        IReadOnlyList<LayerToolEntryInfo> entries,
        IReadOnlyList<LayerToolWarning> warnings,
        int cachedEntryCount)
    {
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        CachedEntryCount = cachedEntryCount;
    }

    public IReadOnlyList<LayerToolEntryInfo> Entries { get; }

    public IReadOnlyList<LayerToolWarning> Warnings { get; }

    public int CachedEntryCount { get; }
}
