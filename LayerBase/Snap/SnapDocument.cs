namespace LayerBase.Snap;

public sealed class SnapDocument
{
    public int FormatVersion { get; init; } = 1;

    public Dictionary<string, SnapSection> Sections { get; set; } = new();

    public void AddSection(SnapSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException(nameof(section));
        }

        if (string.IsNullOrWhiteSpace(section.Key))
        {
            throw new SnapFormatException("Snap section key cannot be empty.");
        }

        Sections[section.Key] = section;
    }

    public bool TryGetSection(string key, out SnapSection? section)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        return Sections.TryGetValue(key, out section);
    }
}
