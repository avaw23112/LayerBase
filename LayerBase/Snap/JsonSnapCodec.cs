using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

public static class JsonSnapCodec
{
    private static readonly JsonSerializerOptions s_defaultOptionsTemplate = new()
    {
        WriteIndented = false
    };

    public static JsonSerializerOptions DefaultOptions => CreateOptions(null);

    public static string EncodeToString(SnapDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return JsonSerializer.Serialize(document, CreateOptions(options));
    }

    public static byte[] EncodeToUtf8Bytes(SnapDocument document, JsonSerializerOptions? options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return JsonSerializer.SerializeToUtf8Bytes(document, CreateOptions(options));
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
        return DecodeFromString(json, limits.ToReadLimits(), limits.MinFormatVersion, limits.MaxFormatVersion, options);
    }

    public static SnapDocument DecodeFromString(
        string json,
        SnapReadLimits limits,
        JsonSerializerOptions? options = null)
    {
        return DecodeFromString(json, limits, FullSnapLimits.Default.MinFormatVersion, FullSnapLimits.Default.MaxFormatVersion, options);
    }

    private static SnapDocument DecodeFromString(
        string json,
        SnapReadLimits limits,
        int minFormatVersion,
        int maxFormatVersion,
        JsonSerializerOptions? options)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(json));
        }

        limits.ThrowIfInvalid();
        int byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > limits.MaxJsonBytes)
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxJsonBytes ({byteCount} > {limits.MaxJsonBytes}).");
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
            ValidateSnapJsonShape(parsed.RootElement, limits, minFormatVersion, maxFormatVersion);

            SnapDocument? document = parsed.RootElement.Deserialize<SnapDocument>(CreateOptions(options));

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
        return DecodeFromUtf8Bytes(utf8Json, limits.ToReadLimits(), limits.MinFormatVersion, limits.MaxFormatVersion, options);
    }

    public static SnapDocument DecodeFromUtf8Bytes(
        ReadOnlySpan<byte> utf8Json,
        SnapReadLimits limits,
        JsonSerializerOptions? options = null)
    {
        return DecodeFromUtf8Bytes(utf8Json, limits, FullSnapLimits.Default.MinFormatVersion, FullSnapLimits.Default.MaxFormatVersion, options);
    }

    private static SnapDocument DecodeFromUtf8Bytes(
        ReadOnlySpan<byte> utf8Json,
        SnapReadLimits limits,
        int minFormatVersion,
        int maxFormatVersion,
        JsonSerializerOptions? options)
    {
        limits.ThrowIfInvalid();
        if (utf8Json.IsEmpty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(utf8Json));
        }

        if (utf8Json.Length > limits.MaxJsonBytes)
        {
            throw new SnapFormatException(
                $"FullSnap JSON exceeds MaxJsonBytes ({utf8Json.Length} > {limits.MaxJsonBytes}).");
        }

        try
        {
            var reader = new Utf8JsonReader(
                utf8Json,
                new JsonReaderOptions
                {
                    MaxDepth = limits.MaxJsonDepth
                });
            using var parsed = JsonDocument.ParseValue(ref reader);
            if (reader.Read())
                throw new SnapFormatException("FullSnap JSON must contain a single root value.");

            ValidateNoDuplicateProperties(parsed.RootElement);
            ValidateSnapJsonShape(parsed.RootElement, limits, minFormatVersion, maxFormatVersion);

            SnapDocument? document = parsed.RootElement.Deserialize<SnapDocument>(CreateOptions(options));

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
        return JsonSerializer.SerializeToUtf8Bytes(section, CreateOptions(null)).Length;
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

    private static JsonSerializerOptions CreateOptions(JsonSerializerOptions? options)
    {
        return new JsonSerializerOptions(options ?? s_defaultOptionsTemplate);
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

    private static void ValidateSnapJsonShape(
        JsonElement root,
        SnapReadLimits limits,
        int minFormatVersion,
        int maxFormatVersion)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new SnapFormatException("FullSnap JSON root must be an object.");

        if (!root.TryGetProperty(nameof(SnapDocument.FormatVersion), out JsonElement formatVersionElement) ||
            !formatVersionElement.TryGetInt32(out int formatVersion))
        {
            throw new SnapFormatException("FullSnap FormatVersion must be an integer.");
        }

        if (formatVersion < minFormatVersion || formatVersion > maxFormatVersion)
        {
            throw new SnapFormatException(
                $"FullSnap FormatVersion {formatVersion} is outside supported range {minFormatVersion}-{maxFormatVersion}.");
        }

        if (!root.TryGetProperty(nameof(SnapDocument.Sections), out JsonElement sectionsElement) ||
            sectionsElement.ValueKind != JsonValueKind.Object)
        {
            throw new SnapFormatException("FullSnap Sections must be a JSON object.");
        }

        int sectionCount = 0;
        long totalSectionBytes = 0;
        foreach (JsonProperty sectionProperty in sectionsElement.EnumerateObject())
        {
            sectionCount++;
            if (sectionCount > limits.MaxSections)
            {
                throw new SnapFormatException(
                    $"FullSnap section count exceeds MaxSections ({sectionCount} > {limits.MaxSections}).");
            }

            ValidateSectionJson(sectionProperty, limits, ref totalSectionBytes);
        }
    }

    private static void ValidateSectionJson(
        JsonProperty sectionProperty,
        SnapReadLimits limits,
        ref long totalSectionBytes)
    {
        string dictionaryKey = sectionProperty.Name;
        if (string.IsNullOrWhiteSpace(dictionaryKey))
            throw new SnapFormatException("FullSnap section key cannot be empty.");

        JsonElement sectionElement = sectionProperty.Value;
        if (sectionElement.ValueKind != JsonValueKind.Object)
            throw new SnapFormatException($"FullSnap section `{dictionaryKey}` must be a JSON object.");

        int sectionBytes = Encoding.UTF8.GetByteCount(sectionElement.GetRawText());
        if (sectionBytes > limits.MaxSectionBytes)
        {
            throw new SnapFormatException(
                $"FullSnap section `{dictionaryKey}` exceeds MaxSectionBytes ({sectionBytes} > {limits.MaxSectionBytes}).");
        }

        totalSectionBytes += sectionBytes;
        if (totalSectionBytes > limits.MaxTotalSectionBytes)
        {
            throw new SnapFormatException(
                $"FullSnap sections exceed MaxTotalSectionBytes ({totalSectionBytes} > {limits.MaxTotalSectionBytes}).");
        }

        if (!sectionElement.TryGetProperty(nameof(SnapSection.Key), out JsonElement keyElement) ||
            keyElement.ValueKind != JsonValueKind.String)
        {
            throw new SnapFormatException($"FullSnap section `{dictionaryKey}` key must be a string.");
        }

        string? payloadKey = keyElement.GetString();
        if (string.IsNullOrWhiteSpace(payloadKey))
            throw new SnapFormatException("FullSnap section key cannot be empty.");

        if (!string.Equals(dictionaryKey, payloadKey, StringComparison.Ordinal))
        {
            throw new SnapFormatException(
                $"FullSnap section dictionary key `{dictionaryKey}` does not match payload key `{payloadKey}`.");
        }

        if (!sectionElement.TryGetProperty(nameof(SnapSection.Version), out JsonElement versionElement) ||
            !versionElement.TryGetInt32(out int version) ||
            version <= 0)
        {
            throw new SnapFormatException($"FullSnap section `{dictionaryKey}` version must be a positive integer.");
        }

        if (!sectionElement.TryGetProperty(nameof(SnapSection.Data), out JsonElement dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            throw new SnapFormatException($"FullSnap section `{dictionaryKey}` data must be a JSON object.");
        }
    }

    private static bool IsDepthFailure(JsonException exception, int maxDepth)
    {
        return exception.Message.Contains("depth", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains(maxDepth.ToString(), StringComparison.Ordinal);
    }
}
