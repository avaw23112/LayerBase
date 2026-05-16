# LayerBase Snap 设计方案

## 1. 目标

LayerBase Snap 用于为运行时提供统一的快照能力。

本方案拆分为两类能力：

1. `IFullSnap`：参与 Runtime 级完整快照流程，由框架在 Build 阶段自动收集并缓存。
2. `IClipSnap<T>`：面向单独模块的片段快照能力，不进入框架 FullSnap 流程，由业务主动调用。

本方案的核心原则：

- `IFullSnap` 显式保留 `WriteFullSnap` 和 `ReadFullSnap`，让开发者决定保存哪些字段。
- `__SnapKey`、`__SnapVersion`、`SnapWriter` 创建、`SnapReader` 创建、`SnapDocument` 写入和读取由框架隐式管理。
- 不使用 `[FullSnap]` 特性。实现 `IFullSnap` 本身就是参与 FullSnap 的标记。
- `ActorWorld` 和 `EcsWorld` 不默认进入 FullSnap。
- ECS 数据由 Service 在 `WriteFullSnap` 中通过 Query 选择性批量写入。
- 重点 Actor 数据通过 `IClipSnap<T>` 或由 Manager / Service 选择性同步。
- `ProjectedActor` 默认不进入快照，因为它通常可以由 ECS 数据重新投影生成。
- `SnapWriter` / `SnapReader` 第一版基于 `System.Text.Json.Nodes` 实现。
- 保留 `SnapArrayReader`，用于数组逐项读取，不把数组 API 改为 `ReadList<T>` 或 `Span<T>`。

---

## 2. 依赖包

第一版不引入第三方序列化包，只使用 .NET 自带库。

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
```

项目目标框架为 `net8.0` 时无需额外 NuGet 包。

未来如果需要 Unity IL2CPP、二进制存档或更高性能格式，可以新增扩展包，例如：

- `LayerBase.Snap.Newtonsoft`
- `LayerBase.Snap.MessagePack`
- `LayerBase.Snap.Binary`

核心接口不直接依赖这些扩展包。

---

## 3. 目录结构

建议新增目录：

```text
LayerBase/Snap/
 ├─ IFullSnap.cs
 ├─ IClipSnap.cs
 ├─ IFullSnapRuntime.cs
 ├─ IGeneratedFullSnapNode.cs
 ├─ SnapDocument.cs
 ├─ SnapSection.cs
 ├─ SnapWriter.cs
 ├─ SnapReader.cs
 ├─ SnapArrayWriter.cs
 ├─ SnapArrayReader.cs
 ├─ SnapFormatException.cs
 ├─ JsonSnapCodec.cs
 ├─ FullSnapRuntime.cs
 └─ ClipSnapExtensions.cs
```

源生成器建议新增：

```text
LayerBase.Generator/LayerBase.Generator/FullSnapGenerator.cs
```

---

## 4. 对外接口

### 4.1 `IFullSnap`

`IFullSnap` 是 Runtime 级完整快照流程的参与者接口。

开发者只负责写入和读取当前对象关心的字段，不负责 key、version、容器组织和流程调度。

```csharp
namespace LayerBase.Snap;

/// <summary>
/// 完整快照参与接口。
/// 作用：
/// 1. 让 Layer、Service、LayerContext 等对象参与 Runtime 级完整快照流程。
/// 2. 让开发者显式选择需要保存和还原的字段。
/// 3. 不负责 SnapWriter 创建、SnapReader 创建、SnapDocument 写入、SnapSection 查找。
/// </summary>
public interface IFullSnap
{
    /// <summary>
    /// 写入完整快照字段。
    /// </summary>
    /// <param name="writer">
    /// 快照写入器。
    /// 作用：
    /// 1. 由 FullSnapRuntime 创建。
    /// 2. 封装底层 JsonObject。
    /// 3. 开发者只通过它写入当前对象需要持久化的字段。
    /// </param>
    void WriteFullSnap(ref SnapWriter writer);

    /// <summary>
    /// 读取完整快照字段。
    /// </summary>
    /// <param name="reader">
    /// 快照读取器。
    /// 作用：
    /// 1. 由 FullSnapRuntime 根据 __SnapKey 找到 SnapSection 后创建。
    /// 2. 封装底层 JsonObject。
    /// 3. 开发者只通过它读取当前对象需要还原的字段。
    /// </param>
    void ReadFullSnap(ref SnapReader reader);
}
```

---

### 4.2 `IClipSnap<T>`

`IClipSnap<T>` 是模块级片段快照接口。

它不参与 FullSnap 流程，不生成 `__SnapKey`，不写入 `SnapDocument`。

```csharp
namespace LayerBase.Snap;

/// <summary>
/// 片段快照接口。
/// 作用：
/// 1. 面向单独模块传输局部状态。
/// 2. 不受 FullSnapRuntime 管理。
/// 3. 由业务代码主动调用。
/// </summary>
/// <typeparam name="TClip">
/// 片段快照类型。
/// 作用：
/// 明确当前接口导出和导入的数据结构。
/// </typeparam>
public interface IClipSnap<TClip>
{
    /// <summary>
    /// 导出片段快照。
    /// </summary>
    /// <returns>
    /// 返回当前模块导出的片段状态。
    /// </returns>
    TClip Serialize();

    /// <summary>
    /// 导入片段快照。
    /// </summary>
    /// <param name="clip">
    /// 片段快照对象。
    /// 作用：
    /// 携带要应用回当前模块的局部状态。
    /// </param>
    void Deserialize(in TClip clip);
}
```

---

### 4.3 `IFullSnapRuntime`

`IFullSnapRuntime` 是 `LayerRuntime.FullSnap` 对外暴露的 Runtime 级入口。

```csharp
using System.Text.Json;

namespace LayerBase.Snap;

/// <summary>
/// Runtime 级完整快照入口。
/// 作用：
/// 1. 对外提供完整快照导出和导入能力。
/// 2. 内部只调度 Build 阶段缓存的 IGeneratedFullSnapNode。
/// 3. 不默认序列化 EcsWorld、ActorWorld、ProjectedActor。
/// </summary>
public interface IFullSnapRuntime
{
    /// <summary>
    /// 导出完整快照文档。
    /// </summary>
    /// <returns>
    /// 返回结构化快照文档。
    /// </returns>
    SnapDocument Serialize();

    /// <summary>
    /// 从完整快照文档还原 Runtime 状态。
    /// </summary>
    /// <param name="document">
    /// 完整快照文档。
    /// 作用：
    /// 保存所有 FullSnap 节点写出的 SnapSection。
    /// </param>
    void Deserialize(SnapDocument document);

    /// <summary>
    /// 导出完整快照 JSON 字符串。
    /// </summary>
    /// <param name="options">
    /// JSON 序列化配置。
    /// 作用：
    /// 控制缩进、转换器等行为。
    /// 传入 null 时使用默认配置。
    /// </param>
    /// <returns>
    /// 返回 JSON 字符串。
    /// </returns>
    string SerializeJson(JsonSerializerOptions? options = null);

    /// <summary>
    /// 从 JSON 字符串还原完整快照。
    /// </summary>
    /// <param name="json">
    /// JSON 字符串。
    /// 作用：
    /// 作为完整快照数据来源。
    /// </param>
    /// <param name="options">
    /// JSON 反序列化配置。
    /// 作用：
    /// 控制反序列化行为。
    /// 传入 null 时使用默认配置。
    /// </param>
    void DeserializeJson(
        string json,
        JsonSerializerOptions? options = null);
}
```

---

## 5. 框架内部接口

### 5.1 `IGeneratedFullSnapNode`

`IGeneratedFullSnapNode` 由源生成器自动挂载。

开发者不应该手动实现该接口。

```csharp
namespace LayerBase.Snap;

/// <summary>
/// 源生成器生成的完整快照节点接口。
/// 作用：
/// 1. 给 FullSnapRuntime 提供稳定快照键。
/// 2. 给 FullSnapRuntime 提供区块版本号。
/// 3. 让 Build 阶段可以把 FullSnap 对象缓存成可调度节点。
/// </summary>
internal interface IGeneratedFullSnapNode : IFullSnap
{
    /// <summary>
    /// 快照区块键。
    /// 作用：
    /// 1. 序列化时作为 SnapDocument.Sections 的 key。
    /// 2. 反序列化时用于查找对应 SnapSection。
    /// </summary>
    string __SnapKey { get; }

    /// <summary>
    /// 快照区块版本。
    /// 作用：
    /// 让 ReadFullSnap 可以根据版本做兼容读取。
    /// </summary>
    int __SnapVersion { get; }
}
```

---

## 6. 快照容器

### 6.1 `SnapDocument`

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 完整快照文档。
/// 作用：
/// 1. 保存一次 Runtime FullSnap 的所有区块。
/// 2. 每个区块由源生成器生成的 __SnapKey 定位。
/// 3. 最后可以通过 JsonSnapCodec 导出成 JSON 字符串。
/// </summary>
public sealed class SnapDocument
{
    /// <summary>
    /// 快照文档格式版本。
    /// 作用：
    /// 1. 标记 SnapDocument 自身结构版本。
    /// 2. 如果未来 sections 组织方式变化，可以用它做兼容。
    /// </summary>
    public int FormatVersion { get; init; } = 1;

    /// <summary>
    /// 快照区块表。
    /// key：由源生成器生成的 __SnapKey。
    /// value：对应对象写出的快照区块。
    /// </summary>
    public Dictionary<string, SnapSection> Sections { get; init; } = new();

    /// <summary>
    /// 添加快照区块。
    /// </summary>
    /// <param name="section">
    /// 快照区块。
    /// 作用：
    /// 1. 包含 Key、Version、Data。
    /// 2. 会根据 section.Key 写入 Sections。
    /// </param>
    public void AddSection(SnapSection section)
    {
        if (string.IsNullOrWhiteSpace(section.Key))
        {
            throw new SnapFormatException("Snap section key cannot be empty.");
        }

        Sections[section.Key] = section;
    }

    /// <summary>
    /// 尝试获取快照区块。
    /// </summary>
    /// <param name="key">
    /// 快照区块 key。
    /// 作用：
    /// 反序列化时用 IGeneratedFullSnapNode.__SnapKey 查找对应区块。
    /// </param>
    /// <param name="section">
    /// 输出参数。
    /// 作用：
    /// 找到时返回对应 SnapSection；找不到时返回 null。
    /// </param>
    /// <returns>
    /// true 表示找到区块。
    /// false 表示没有找到。
    /// </returns>
    public bool TryGetSection(
        string key,
        out SnapSection? section)
    {
        return Sections.TryGetValue(key, out section);
    }
}
```

---

### 6.2 `SnapSection`

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 快照区块。
/// 作用：
/// 1. 表示一个 FullSnap 节点写出的数据。
/// 2. Key 由源生成器生成。
/// 3. Version 由源生成器生成。
/// 4. Data 保存开发者通过 SnapWriter 写入的字段。
/// </summary>
public sealed class SnapSection
{
    /// <summary>
    /// 区块 key。
    /// 作用：
    /// 用于在 SnapDocument.Sections 中定位当前区块。
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// 区块版本。
    /// 作用：
    /// 1. 标记当前 FullSnap 节点的数据结构版本。
    /// 2. 后续字段变化时，可以在 ReadFullSnap 中根据版本做兼容。
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// 区块数据。
    /// 作用：
    /// 保存 SnapWriter 写入的 JSON 对象。
    /// </summary>
    public JsonObject Data { get; init; } = new();
}
```

---

## 7. `SnapWriter`

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 快照写入器。
/// 作用：
/// 1. 封装 JsonObject。
/// 2. 给 IFullSnap.WriteFullSnap 提供稳定写入 API。
/// 3. 让业务代码不直接依赖 JsonObject。
/// 4. 禁止随意写入 object，避免格式不可控。
/// </summary>
public readonly struct SnapWriter
{
    private readonly JsonObject _data;
    private readonly string _path;

    /// <summary>
    /// 创建快照写入器。
    /// </summary>
    /// <param name="data">
    /// 底层 JSON 对象。
    /// 作用：
    /// 保存当前 FullSnap 节点写入的字段。
    /// </param>
    /// <param name="path">
    /// 当前写入路径。
    /// 作用：
    /// 1. 用于错误提示。
    /// 2. 嵌套对象或数组中可以显示更清晰的位置。
    /// </param>
    internal SnapWriter(
        JsonObject data,
        string path = "$")
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _path = path;
    }

    public void WriteInt32(string key, int value) => WriteValue(key, value);

    public void WriteInt64(string key, long value) => WriteValue(key, value);

    public void WriteSingle(string key, float value) => WriteValue(key, value);

    public void WriteDouble(string key, double value) => WriteValue(key, value);

    public void WriteBoolean(string key, bool value) => WriteValue(key, value);

    public void WriteString(string key, string? value)
    {
        ValidateKey(key);
        _data[key] = value;
    }

    public void WriteEnum<TEnum>(string key, TEnum value)
        where TEnum : struct, Enum
    {
        ValidateKey(key);
        _data[key] = value.ToString();
    }

    public SnapWriter WriteObject(string key)
    {
        ValidateKey(key);

        var child = new JsonObject();
        _data[key] = child;

        return new SnapWriter(
            data: child,
            path: $"{_path}.{key}");
    }

    public SnapArrayWriter WriteArray(string key)
    {
        ValidateKey(key);

        var array = new JsonArray();
        _data[key] = array;

        return new SnapArrayWriter(
            array: array,
            path: $"{_path}.{key}");
    }

    private void WriteValue<T>(string key, T value)
    {
        ValidateKey(key);
        _data[key] = JsonValue.Create(value);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SnapFormatException("Snap field key cannot be empty.");
        }
    }
}
```

---

## 8. `SnapArrayWriter`

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 快照数组写入器。
/// 作用：
/// 1. 封装 JsonArray。
/// 2. 用于写入列表数据，例如物品列表、实体列表、Buff 列表。
/// </summary>
public readonly struct SnapArrayWriter
{
    private readonly JsonArray _array;
    private readonly string _path;

    /// <summary>
    /// 创建数组写入器。
    /// </summary>
    /// <param name="array">
    /// 底层 JSON 数组。
    /// 作用：
    /// 保存当前数组里的元素。
    /// </param>
    /// <param name="path">
    /// 当前数组路径。
    /// 作用：
    /// 用于错误提示。
    /// </param>
    internal SnapArrayWriter(
        JsonArray array,
        string path)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array));
        _path = path;
    }

    public void AddInt32(int value) => _array.Add(value);

    public void AddInt64(long value) => _array.Add(value);

    public void AddSingle(float value) => _array.Add(value);

    public void AddDouble(double value) => _array.Add(value);

    public void AddBoolean(bool value) => _array.Add(value);

    public void AddString(string? value) => _array.Add(value);

    public void AddEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        _array.Add(value.ToString());
    }

    public SnapWriter AddObject()
    {
        var child = new JsonObject();
        _array.Add(child);

        return new SnapWriter(
            data: child,
            path: $"{_path}[{_array.Count - 1}]");
    }
}
```

---

## 9. `SnapReader`

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 快照读取器。
/// 作用：
/// 1. 封装 JsonObject。
/// 2. 给 IFullSnap.ReadFullSnap 提供稳定读取 API。
/// 3. 提供字段缺失和类型错误提示。
/// </summary>
public readonly struct SnapReader
{
    private readonly JsonObject _data;
    private readonly string _path;

    /// <summary>
    /// 当前区块版本。
    /// 作用：
    /// 让 ReadFullSnap 可以根据旧版本做兼容读取。
    /// </summary>
    public int Version { get; }

    internal SnapReader(
        JsonObject data,
        int version,
        string path = "$")
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Version = version;
        _path = path;
    }

    public int ReadInt32(string key) => ReadRequiredValue<int>(key);

    public long ReadInt64(string key) => ReadRequiredValue<long>(key);

    public float ReadSingle(string key) => ReadRequiredValue<float>(key);

    public double ReadDouble(string key) => ReadRequiredValue<double>(key);

    public bool ReadBoolean(string key) => ReadRequiredValue<bool>(key);

    public string ReadString(string key) => ReadRequiredValue<string>(key);

    public bool TryReadInt32(string key, out int value) => TryReadValue(key, out value);

    public bool TryReadSingle(string key, out float value) => TryReadValue(key, out value);

    public int ReadInt32OrDefault(string key, int defaultValue = default)
    {
        return TryReadInt32(key, out int value)
            ? value
            : defaultValue;
    }

    public float ReadSingleOrDefault(string key, float defaultValue = default)
    {
        return TryReadSingle(key, out float value)
            ? value
            : defaultValue;
    }

    public TEnum ReadEnum<TEnum>(string key)
        where TEnum : struct, Enum
    {
        string text = ReadString(key);

        if (Enum.TryParse(text, out TEnum value))
        {
            return value;
        }

        throw new SnapFormatException(
            $"Field '{BuildPath(key)}' cannot parse enum {typeof(TEnum).Name} from '{text}'.");
    }

    public SnapReader ReadObject(string key)
    {
        JsonNode node = GetRequiredNode(key);

        if (node is JsonObject obj)
        {
            return new SnapReader(
                data: obj,
                version: Version,
                path: BuildPath(key));
        }

        throw new SnapFormatException(
            $"Field '{BuildPath(key)}' is not a JSON object.");
    }

    public SnapArrayReader ReadArray(string key)
    {
        JsonNode node = GetRequiredNode(key);

        if (node is JsonArray array)
        {
            return new SnapArrayReader(
                array: array,
                version: Version,
                path: BuildPath(key));
        }

        throw new SnapFormatException(
            $"Field '{BuildPath(key)}' is not a JSON array.");
    }

    private T ReadRequiredValue<T>(string key)
    {
        JsonNode node = GetRequiredNode(key);

        try
        {
            return node.GetValue<T>();
        }
        catch (Exception ex)
        {
            throw new SnapFormatException(
                $"Field '{BuildPath(key)}' cannot be read as {typeof(T).Name}.",
                ex);
        }
    }

    private bool TryReadValue<T>(string key, out T value)
    {
        value = default!;

        if (!_data.TryGetPropertyValue(key, out JsonNode? node) ||
            node == null)
        {
            return false;
        }

        try
        {
            value = node.GetValue<T>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private JsonNode GetRequiredNode(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SnapFormatException("Snap field key cannot be empty.");
        }

        if (!_data.TryGetPropertyValue(key, out JsonNode? node) ||
            node == null)
        {
            throw new SnapFormatException(
                $"Missing required snap field '{BuildPath(key)}'.");
        }

        return node;
    }

    private string BuildPath(string key)
    {
        return $"{_path}.{key}";
    }
}
```

---

## 10. `SnapArrayReader`

`SnapArrayReader` 保留为数组逐项读取 API。

它适合读取：

- ECS 批量实体快照
- 背包物品列表
- Buff 列表
- 任务列表
- 技能冷却列表

```csharp
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// 快照数组读取器。
/// 作用：
/// 1. 封装 JsonArray。
/// 2. 给 IFullSnap.ReadFullSnap 提供数组读取能力。
/// 3. 用于读取实体列表、物品列表、Buff 列表、任务列表等批量数据。
/// </summary>
public readonly struct SnapArrayReader
{
    private readonly JsonArray _array;
    private readonly string _path;

    /// <summary>
    /// 当前快照区块版本。
    /// 作用：
    /// 1. 让数组元素读取逻辑可以根据版本做兼容。
    /// 2. 例如旧版本没有某个字段时，可以在业务代码中根据 Version 使用默认值。
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// 数组元素数量。
    /// 作用：
    /// 让业务层可以用 for 循环逐个读取数组元素。
    /// </summary>
    public int Count => _array.Count;

    internal SnapArrayReader(
        JsonArray array,
        int version,
        string path)
    {
        _array = array ?? throw new ArgumentNullException(nameof(array));
        Version = version;
        _path = path;
    }

    public SnapReader ReadObject(int index)
    {
        JsonNode node = GetRequiredNode(index);

        if (node is JsonObject obj)
        {
            return new SnapReader(
                data: obj,
                version: Version,
                path: BuildPath(index));
        }

        throw new SnapFormatException(
            $"Array element '{BuildPath(index)}' is not a JSON object.");
    }

    public int ReadInt32(int index) => ReadRequiredValue<int>(index);

    public long ReadInt64(int index) => ReadRequiredValue<long>(index);

    public float ReadSingle(int index) => ReadRequiredValue<float>(index);

    public double ReadDouble(int index) => ReadRequiredValue<double>(index);

    public bool ReadBoolean(int index) => ReadRequiredValue<bool>(index);

    public string ReadString(int index) => ReadRequiredValue<string>(index);

    public TEnum ReadEnum<TEnum>(int index)
        where TEnum : struct, Enum
    {
        string text = ReadString(index);

        if (Enum.TryParse(text, out TEnum value))
        {
            return value;
        }

        throw new SnapFormatException(
            $"Array element '{BuildPath(index)}' cannot parse enum {typeof(TEnum).Name} from '{text}'.");
    }

    public bool TryReadInt32(int index, out int value) => TryReadValue(index, out value);

    public bool TryReadSingle(int index, out float value) => TryReadValue(index, out value);

    public bool TryReadString(int index, out string value) => TryReadValue(index, out value);

    private JsonNode GetRequiredNode(int index)
    {
        ValidateIndex(index);

        JsonNode? node = _array[index];

        if (node == null)
        {
            throw new SnapFormatException(
                $"Array element '{BuildPath(index)}' is null.");
        }

        return node;
    }

    private TValue ReadRequiredValue<TValue>(int index)
    {
        JsonNode node = GetRequiredNode(index);

        try
        {
            return node.GetValue<TValue>();
        }
        catch (Exception ex)
        {
            throw new SnapFormatException(
                $"Array element '{BuildPath(index)}' cannot be read as {typeof(TValue).Name}.",
                ex);
        }
    }

    private bool TryReadValue<TValue>(int index, out TValue value)
    {
        value = default!;

        if ((uint)index >= (uint)_array.Count)
        {
            return false;
        }

        JsonNode? node = _array[index];

        if (node == null)
        {
            return false;
        }

        try
        {
            value = node.GetValue<TValue>();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_array.Count)
        {
            throw new SnapFormatException(
                $"Array index out of range: {BuildPath(index)}, count = {_array.Count}.");
        }
    }

    private string BuildPath(int index)
    {
        return $"{_path}[{index}]";
    }
}
```

---

## 11. `SnapFormatException`

```csharp
namespace LayerBase.Snap;

/// <summary>
/// 快照格式异常。
/// 作用：
/// 1. 字段缺失时抛出。
/// 2. 字段类型错误时抛出。
/// 3. JSON 结构不符合预期时抛出。
/// </summary>
public sealed class SnapFormatException : Exception
{
    public SnapFormatException(string message)
        : base(message)
    {
    }

    public SnapFormatException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
```

---

## 12. `JsonSnapCodec`

```csharp
using System.Text.Json;

namespace LayerBase.Snap;

/// <summary>
/// JSON 快照编解码器。
/// 作用：
/// 1. 把 SnapDocument 编码成 JSON 字符串。
/// 2. 把 JSON 字符串解码成 SnapDocument。
/// </summary>
public static class JsonSnapCodec
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true
    };

    public static string EncodeToString(
        SnapDocument document,
        JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(
            document,
            options ?? DefaultOptions);
    }

    public static SnapDocument DecodeFromString(
        string json,
        JsonSerializerOptions? options = null)
    {
        SnapDocument? document = JsonSerializer.Deserialize<SnapDocument>(
            json,
            options ?? DefaultOptions);

        if (document == null)
        {
            throw new SnapFormatException("SnapDocument decode failed.");
        }

        return document;
    }
}
```

---

## 13. `FullSnapRuntime`

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LayerBase.Snap;

/// <summary>
/// Runtime 级 FullSnap 执行器。
/// 作用：
/// 1. 缓存 Build 阶段收集到的 FullSnap 节点。
/// 2. 序列化时依次调用 WriteFullSnap。
/// 3. 反序列化时根据 __SnapKey 找到对应区块，再调用 ReadFullSnap。
/// </summary>
internal sealed class FullSnapRuntime : IFullSnapRuntime
{
    private readonly LayerRuntime _runtime;
    private readonly List<IGeneratedFullSnapNode> _nodes = new();

    public FullSnapRuntime(LayerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public void Register(IGeneratedFullSnapNode node)
    {
        _nodes.Add(node ?? throw new ArgumentNullException(nameof(node)));
    }

    public SnapDocument Serialize()
    {
        var document = new SnapDocument();

        for (int i = 0; i < _nodes.Count; i++)
        {
            IGeneratedFullSnapNode node = _nodes[i];

            var data = new JsonObject();
            var writer = new SnapWriter(data);

            node.WriteFullSnap(ref writer);

            document.AddSection(new SnapSection
            {
                Key = node.__SnapKey,
                Version = node.__SnapVersion,
                Data = data
            });
        }

        return document;
    }

    public void Deserialize(SnapDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            IGeneratedFullSnapNode node = _nodes[i];

            if (!document.TryGetSection(node.__SnapKey, out SnapSection? section))
            {
                continue;
            }

            var reader = new SnapReader(
                data: section.Data,
                version: section.Version);

            node.ReadFullSnap(ref reader);
        }
    }

    public string SerializeJson(JsonSerializerOptions? options = null)
    {
        return JsonSnapCodec.EncodeToString(
            Serialize(),
            options);
    }

    public void DeserializeJson(
        string json,
        JsonSerializerOptions? options = null)
    {
        SnapDocument document = JsonSnapCodec.DecodeFromString(
            json,
            options);

        Deserialize(document);
    }
}
```

---

## 14. `IClipSnap<T>` 扩展方法

```csharp
namespace LayerBase.Snap;

/// <summary>
/// ClipSnap 扩展方法。
/// 作用：
/// 1. 避免调用方手写强制类型转换。
/// 2. 让 Service、LayerContext、Actor 等对象都可以通过 target.Clip&lt;T&gt;() 获取片段能力。
/// </summary>
public static class ClipSnapExtensions
{
    public static ClipSnapHandle<TClip> Clip<TClip>(this object target)
    {
        if (target is IClipSnap<TClip> clipSnap)
        {
            return new ClipSnapHandle<TClip>(clipSnap);
        }

        throw new InvalidOperationException(
            $"Object '{target.GetType().Name}' does not implement IClipSnap<{typeof(TClip).Name}>.");
    }

    public static bool TryClip<TClip>(
        this object target,
        out ClipSnapHandle<TClip> handle)
    {
        if (target is IClipSnap<TClip> clipSnap)
        {
            handle = new ClipSnapHandle<TClip>(clipSnap);
            return true;
        }

        handle = default;
        return false;
    }
}

/// <summary>
/// ClipSnap 句柄。
/// 作用：
/// 1. 包装 IClipSnap&lt;TClip&gt;。
/// 2. 提供 Serialize 和 Deserialize 调用入口。
/// 3. 避免调用方手写显式接口转换。
/// </summary>
/// <typeparam name="TClip">
/// 片段快照类型。
/// 作用：
/// 标记当前句柄操作的是哪一种片段数据。
/// </typeparam>
public readonly struct ClipSnapHandle<TClip>
{
    private readonly IClipSnap<TClip>? _snap;

    public ClipSnapHandle(IClipSnap<TClip> snap)
    {
        _snap = snap;
    }

    public TClip Serialize()
    {
        if (_snap == null)
        {
            throw new InvalidOperationException(
                $"ClipSnapHandle<{typeof(TClip).Name}> is not initialized.");
        }

        return _snap.Serialize();
    }

    public void Deserialize(in TClip clip)
    {
        if (_snap == null)
        {
            throw new InvalidOperationException(
                $"ClipSnapHandle<{typeof(TClip).Name}> is not initialized.");
        }

        _snap.Deserialize(in clip);
    }
}
```

---

## 15. `LayerRuntime` 接入方案

### 15.1 新增字段和属性

插入位置：

```text
LayerBase/Application/LayerRuntime.cs
```

建议放在 `Scheduler`、`Timer` 等 Runtime 能力属性附近。

```csharp
private LayerBase.Snap.FullSnapRuntime? _fullSnap;

/// <summary>
/// Runtime 完整快照入口。
/// 作用：
/// 1. 对外暴露完整快照导出和导入能力。
/// 2. 只调度 Build 阶段缓存的 FullSnap 节点。
/// </summary>
public LayerBase.Snap.IFullSnapRuntime FullSnap =>
    _fullSnap ?? throw new InvalidOperationException("Runtime not built.");
```

---

### 15.2 新增 `BuildFullSnapCache`

插入位置：

```text
LayerBase/Application/LayerRuntime.cs
```

建议放在 `BuildServiceProvider()` 附近。

```csharp
internal void BuildFullSnapCache()
{
    _fullSnap = new LayerBase.Snap.FullSnapRuntime(this);

    if (_chain == null)
    {
        return;
    }

    var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

    foreach (var layer in _chain.GetNodes().OfType<Layer>())
    {
        if (layer is LayerBase.Snap.IGeneratedFullSnapNode layerNode &&
            visited.Add(layerNode))
        {
            _fullSnap.Register(layerNode);
        }

        foreach (var node in layer.GetFullSnapNodes())
        {
            if (visited.Add(node))
            {
                _fullSnap.Register(node);
            }
        }
    }
}
```

如果项目中还没有通用引用比较器，可以新增：

```csharp
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}
```

需要命名空间：

```csharp
using System.Runtime.CompilerServices;
```

---

### 15.3 修改 `LayersBuilder.Build()`

在当前构建流程中，`BuildFullSnapCache()` 应放在 Layer 和 Service 都完成构建之后。

建议位置：

```csharp
_runtime.BuildServiceProvider();
_runtime.Actors.PrepareRuntimeBuild();
_layerChain.Build(1024, true);
_runtime.Actors.CompleteRuntimeBuild();
_runtime.BuildFullSnapCache();
_runtime.PolicyTable.Freeze();
```

---

## 16. `Layer` 接入方案

当前 `Layer` 内部已经维护 `m_activeServices` 和 `m_resolvedServices`。

需要新增内部方法：

```csharp
using LayerBase.Snap;

namespace LayerBase.Layers;

public abstract partial class Layer
{
    /// <summary>
    /// 获取当前 Layer 下所有 FullSnap 节点。
    /// 作用：
    /// 1. 供 LayerRuntime.BuildFullSnapCache 在 Build 阶段收集缓存。
    /// 2. 覆盖手动 RegisterService 的服务。
    /// 3. 覆盖 ConfigureServices / DI 解析出来的服务。
    /// </summary>
    /// <returns>
    /// 返回当前 Layer 中所有实现 IGeneratedFullSnapNode 的对象。
    /// </returns>
    internal IEnumerable<IGeneratedFullSnapNode> GetFullSnapNodes()
    {
        var visited = new HashSet<object>(ObjectReferenceComparer.Instance);

        foreach (var registration in m_activeServices)
        {
            if (registration.Service is IGeneratedFullSnapNode node &&
                visited.Add(node))
            {
                yield return node;
            }
        }

        foreach (var resolved in m_resolvedServices)
        {
            if (resolved.Instance is IGeneratedFullSnapNode node &&
                visited.Add(node))
            {
                yield return node;
            }
        }
    }
}
```

如果 `Layer` 已经有私有 `ObjectReferenceComparer`，可以复用现有实现。

---

## 17. 源生成器规则

### 17.1 触发条件

源生成器扫描所有类型，满足以下条件时生成补充代码：

1. 类型是 `partial class`。
2. 类型实现 `LayerBase.Snap.IFullSnap`。
3. 类型尚未手动实现 `IGeneratedFullSnapNode`。

不需要 `[FullSnap]`。

---

### 17.2 生成内容

对于：

```csharp
namespace Game.Battle;

public sealed partial class BattleContext : ILayerContext, IFullSnap
{
    public void WriteFullSnap(ref SnapWriter writer)
    {
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
    }
}
```

生成：

```csharp
// <auto-generated />

namespace Game.Battle;

using LayerBase.Snap;

public sealed partial class BattleContext : IGeneratedFullSnapNode
{
    string IGeneratedFullSnapNode.__SnapKey => "Game.Battle.BattleContext_FullSnap";

    int IGeneratedFullSnapNode.__SnapVersion => 1;
}
```

默认 key 规则：

```text
完整命名空间.类名_FullSnap
```

第一版版本号固定为 `1`。

---

## 18. 使用示例

### 18.1 FullSnap 示例

```csharp
using LayerBase.DI;
using LayerBase.Snap;

namespace Game.Battle;

public sealed partial class BattleContext : ILayerContext, IFullSnap
{
    public int RoomId;
    public int CurrentFrame;
    public float BattleTime;

    public void WriteFullSnap(ref SnapWriter writer)
    {
        writer.WriteInt32("roomId", RoomId);
        writer.WriteInt32("currentFrame", CurrentFrame);
        writer.WriteSingle("battleTime", BattleTime);
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        RoomId = reader.ReadInt32("roomId");
        CurrentFrame = reader.ReadInt32("currentFrame");
        BattleTime = reader.ReadSingle("battleTime");
    }
}
```

### 18.2 数组写入和读取示例

```csharp
public sealed partial class InventoryService : IService, IFullSnap
{
    private readonly List<ItemStack> _items = new();

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void WriteFullSnap(ref SnapWriter writer)
    {
        SnapArrayWriter items = writer.WriteArray("items");

        for (int i = 0; i < _items.Count; i++)
        {
            SnapWriter item = items.AddObject();

            item.WriteInt32("itemId", _items[i].ItemId);
            item.WriteInt32("count", _items[i].Count);
        }
    }

    public void ReadFullSnap(ref SnapReader reader)
    {
        _items.Clear();

        SnapArrayReader items = reader.ReadArray("items");

        for (int i = 0; i < items.Count; i++)
        {
            SnapReader item = items.ReadObject(i);

            int itemId = item.ReadInt32("itemId");
            int count = item.ReadInt32("count");

            _items.Add(new ItemStack(itemId, count));
        }
    }
}

public readonly struct ItemStack
{
    public readonly int ItemId;
    public readonly int Count;

    public ItemStack(int itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }
}
```

### 18.3 ClipSnap 示例

```csharp
using LayerBase.DI;
using LayerBase.Snap;

namespace Game.Battle;

public sealed class BattleSyncService :
    IService,
    IClipSnap<MoveClip>,
    IClipSnap<HealthClip>
{
    private float _x;
    private float _y;
    private int _hp;

    public void ConfigureServices(IServiceCollection services)
    {
    }

    MoveClip IClipSnap<MoveClip>.Serialize()
    {
        return new MoveClip(_x, _y);
    }

    void IClipSnap<MoveClip>.Deserialize(in MoveClip clip)
    {
        _x = clip.X;
        _y = clip.Y;
    }

    HealthClip IClipSnap<HealthClip>.Serialize()
    {
        return new HealthClip(_hp);
    }

    void IClipSnap<HealthClip>.Deserialize(in HealthClip clip)
    {
        _hp = clip.Hp;
    }
}

public readonly struct MoveClip
{
    public readonly float X;
    public readonly float Y;

    public MoveClip(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public readonly struct HealthClip
{
    public readonly int Hp;

    public HealthClip(int hp)
    {
        Hp = hp;
    }
}
```

调用：

```csharp
MoveClip move = battleSyncService.Clip<MoveClip>().Serialize();
HealthClip health = battleSyncService.Clip<HealthClip>().Serialize();

battleSyncService.Clip<MoveClip>().Deserialize(in move);
battleSyncService.Clip<HealthClip>().Deserialize(in health);
```

---

## 19. 默认不进入 FullSnap 的对象

以下对象不默认进入 FullSnap：

- `EcsWorld`
- `ActorWorld`
- `ProjectedActor`
- Actor 邮箱
- PostScheduler 队列
- Timer 内部队列
- Delay 缓冲区
- 渲染对象
- 物理对象
- 网络连接
- C# `Task`
- 线程对象

ECS 数据由 Service Query 后选择性写入。

重点 Actor 数据由 Manager / Service 通过 `IClipSnap<T>` 选择性导出。

ProjectedActor 默认由 ECS 数据重新投影生成。

---

## 20. 测试计划

### 20.1 FullSnap 收集测试

验证点：

- 实现 `IFullSnap` 的 `LayerContext` 会被源生成器补 `IGeneratedFullSnapNode`。
- `LayerRuntime.BuildFullSnapCache()` 能收集该节点。
- `runtime.FullSnap.Serialize()` 能生成对应 section。
- section key 符合 `命名空间.类名_FullSnap`。

### 20.2 FullSnap 读写测试

验证点：

- `WriteFullSnap` 写入的字段能被 `ReadFullSnap` 正确读回。
- 字段缺失时抛出 `SnapFormatException`。
- 类型不匹配时抛出 `SnapFormatException`。

### 20.3 SnapArrayReader 测试

验证点：

- `SnapArrayWriter.AddObject()` 写入的对象数组能被 `SnapArrayReader.ReadObject(i)` 读回。
- 数组越界时抛出 `SnapFormatException`。
- 元素类型不匹配时抛出 `SnapFormatException`。

### 20.4 ClipSnap 测试

验证点：

- 同一个 Service 可以实现多个 `IClipSnap<T>`。
- `target.Clip<T>()` 能正确返回对应片段句柄。
- 未实现对应 `IClipSnap<T>` 时抛出清晰异常。

### 20.5 边界测试

验证点：

- `EcsWorld` 不会被默认序列化。
- `ActorWorld` 不会被默认序列化。
- 未实现 `IFullSnap` 的 Service 不会进入 FullSnap cache。

---

## 21. 最终结论

本方案将 LayerBase Snap 定义为业务状态快照系统，而不是 Runtime 内存镜像系统。

最终边界：

- `IFullSnap`：框架调度，开发者手写字段读写，源生成器隐式补 key/version。
- `IClipSnap<T>`：业务自调，面向局部状态传输，不进入 FullSnap。
- `SnapWriter` / `SnapReader`：LayerBase 自己封装，底层第一版使用 `System.Text.Json.Nodes`。
- `SnapArrayReader`：保留，用于逐项读取数组对象，适合复杂数组和批量实体数据。
- `EcsWorld` / `ActorWorld`：不默认进入 FullSnap，由业务通过 Service 或 ClipSnap 选择性同步。
