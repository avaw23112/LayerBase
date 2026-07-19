using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public static class JsonSnapCodec
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false
    };

    public static string EncodeToString(SnapDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return JsonSerializer.Serialize(document, options ?? DefaultOptions);
    }

    public static byte[] EncodeToUtf8Bytes(SnapDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return JsonSerializer.SerializeToUtf8Bytes(document, options ?? DefaultOptions);
    }

    public static SnapDocument DecodeFromString(string json, JsonSerializerOptions? options = null)
    {
        return DecodeFromString(json, FullSnapLimits.Default, options);
    }

    public static SnapDocument DecodeFromString(
        string json,
        FullSnapLimits limits,
        JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(json));
        }

        limits.ThrowIfInvalid();
        int byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > limits.MaxTotalBytes)
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxTotalBytes ({byteCount} > {limits.MaxTotalBytes}).");
        }

        try
        {
            using var parsed = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    MaxDepth = limits.MaxJsonDepth
                });
            ValidateNoDuplicateProperties(parsed.RootElement);

            SnapDocument? document = JsonSerializer.Deserialize<SnapDocument>(
                json,
                options ?? DefaultOptions);

            return NormalizeDecodedDocument(document);
        }
        catch (SnapFormatException)
        {
            throw;
        }
        catch (JsonException ex) when (IsDepthFailure(ex, limits.MaxJsonDepth))
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxJsonDepth ({limits.MaxJsonDepth}).",
                ex);
        }
        catch (Exception ex)
        {
            throw new SnapFormatException("SnapDocument decode failed.", ex);
        }
    }

    public static SnapDocument DecodeFromUtf8Bytes(
        ReadOnlySpan<byte> utf8Json,
        FullSnapLimits limits,
        JsonSerializerOptions? options = null)
    {
        limits.ThrowIfInvalid();
        if (utf8Json.IsEmpty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(utf8Json));
        }

        if (utf8Json.Length > limits.MaxTotalBytes)
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxTotalBytes ({utf8Json.Length} > {limits.MaxTotalBytes}).");
        }

        try
        {
            byte[] bytes = utf8Json.ToArray();
            using var stream = new MemoryStream(bytes);
            using var parsed = JsonDocument.Parse(
                stream,
                new JsonDocumentOptions
                {
                    MaxDepth = limits.MaxJsonDepth
                });
            ValidateNoDuplicateProperties(parsed.RootElement);

            SnapDocument? document = JsonSerializer.Deserialize<SnapDocument>(
                bytes,
                options ?? DefaultOptions);

            return NormalizeDecodedDocument(document);
        }
        catch (SnapFormatException)
        {
            throw;
        }
        catch (JsonException ex) when (IsDepthFailure(ex, limits.MaxJsonDepth))
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxJsonDepth ({limits.MaxJsonDepth}).",
                ex);
        }
        catch (Exception ex)
        {
            throw new SnapFormatException("SnapDocument decode failed.", ex);
        }
    }

    internal static int GetDocumentByteCount(SnapDocument document)
    {
        return EncodeToUtf8Bytes(document).Length;
    }

    internal static int GetSectionByteCount(SnapSection section)
    {
        return JsonSerializer.SerializeToUtf8Bytes(section, DefaultOptions).Length;
    }

    internal static int GetJsonDepth(JsonNode? node)
    {
        if (node == null)
        {
            return 0;
        }

        if (node is JsonObject obj)
        {
            int max = 1;
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                max = Math.Max(max, 1 + GetJsonDepth(property.Value));
            }

            return max;
        }

        if (node is JsonArray array)
        {
            int max = 1;
            foreach (JsonNode? item in array)
            {
                max = Math.Max(max, 1 + GetJsonDepth(item));
            }

            return max;
        }

        return 1;
    }

    private static SnapDocument NormalizeDecodedDocument(SnapDocument? document)
    {
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

    private static void ValidateNoDuplicateProperties(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new SnapFormatException(
                            $"Duplicate JSON property `{property.Name}` in FullSnap JSON.");
                    }

                    ValidateNoDuplicateProperties(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    ValidateNoDuplicateProperties(item);
                }

                break;
        }
    }

    private static bool IsDepthFailure(JsonException exception, int maxDepth)
    {
        return exception.Message.Contains("depth", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains(maxDepth.ToString(), StringComparison.Ordinal);
    }
}
