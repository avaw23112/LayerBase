using System.Text.Json;

namespace LayerBase.Snap;

public static class JsonSnapCodec
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true
    };

    public static string EncodeToString(SnapDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return JsonSerializer.Serialize(document, options ?? DefaultOptions);
    }

    public static SnapDocument DecodeFromString(string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(json));
        }

        try
        {
            SnapDocument? document = JsonSerializer.Deserialize<SnapDocument>(
                json,
                options ?? DefaultOptions);

            if (document == null)
            {
                throw new SnapFormatException("SnapDocument decode failed.");
            }

            if (document.Sections == null)
            {
                document.Sections = new Dictionary<string, SnapSection>();
            }

            return document;
        }
        catch (SnapFormatException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SnapFormatException("SnapDocument decode failed.", ex);
        }
    }
}
