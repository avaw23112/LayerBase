using System.Text.Json;
using System.Text.Json.Nodes;

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

    public static SnapDocument DecodeFromString(
        string json,
        JsonSerializerOptions? options = null,
        SnapDecodeLimits? limits = null)
    {
        limits ??= SnapDecodeLimits.Default;
        if (json != null && json.Length > limits.MaxInputChars)
        {
            throw new SnapLimitExceededException(
                $"Snap input has {json.Length} characters, exceeding limit {limits.MaxInputChars}.");
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(json));
        }

        try
        {
            JsonSerializerOptions serializerOptions = options == null
                ? new JsonSerializerOptions(DefaultOptions)
                : new JsonSerializerOptions(options);
            if (limits.MaxDepth > 0)
            {
                serializerOptions.MaxDepth = limits.MaxDepth;
            }

            SnapDocument? document = JsonSerializer.Deserialize<SnapDocument>(
                json,
                serializerOptions);

            if (document == null)
            {
                throw new SnapFormatException("SnapDocument decode failed.");
            }

            if (document.Sections == null)
            {
                document.Sections = new Dictionary<string, SnapSection>();
            }

            ValidateDocument(document, limits);
            return document;
        }
        catch (SnapLimitExceededException)
        {
            throw;
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

    private static void ValidateDocument(SnapDocument document, SnapDecodeLimits limits)
    {
        if (document.Sections.Count > limits.MaxSections)
        {
            throw new SnapLimitExceededException(
                $"Snap document has {document.Sections.Count} sections, exceeding limit {limits.MaxSections}.");
        }

        foreach (SnapSection section in document.Sections.Values)
        {
            ValidateNode(section.Data, limits, depth: 1, "$." + section.Key);
        }
    }

    private static void ValidateNode(JsonNode? node, SnapDecodeLimits limits, int depth, string path)
    {
        if (node == null)
        {
            return;
        }

        if (depth > limits.MaxDepth)
        {
            throw new SnapLimitExceededException(
                $"Snap node '{path}' exceeds max depth {limits.MaxDepth}.");
        }

        if (node is JsonArray array)
        {
            if (array.Count > limits.MaxArrayItems)
            {
                throw new SnapLimitExceededException(
                    $"Snap array '{path}' has {array.Count} items, exceeding limit {limits.MaxArrayItems}.");
            }

            for (int i = 0; i < array.Count; i++)
            {
                ValidateNode(array[i], limits, depth + 1, $"{path}[{i}]");
            }

            return;
        }

        if (node is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> pair in obj)
            {
                ValidateNode(pair.Value, limits, depth + 1, $"{path}.{pair.Key}");
            }

            return;
        }

        if (node is JsonValue value &&
            value.TryGetValue(out string? text) &&
            text != null &&
            text.Length > limits.MaxStringChars)
        {
            throw new SnapLimitExceededException(
                $"Snap string '{path}' has {text.Length} characters, exceeding limit {limits.MaxStringChars}.");
        }
    }
}
