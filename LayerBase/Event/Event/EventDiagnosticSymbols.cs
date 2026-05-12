using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace LayerBase.Core.Event;

/// <summary>
/// EventCenter 专用诊断符号表。
/// 它把重复诊断字符串压缩成整数 ID。
/// </summary>
internal static class EventDiagnosticSymbols
{
    /// <summary>
    /// 保护符号登记过程的锁。
    /// 符号登记发生在注册、Rebuild 或首次诊断路径，不在每次事件派发中发生。
    /// </summary>
    private static readonly object s_lock = new();

    /// <summary>
    /// 诊断文本到符号 ID 的映射。
    /// key 是原始字符串，例如 "BattleLayer"。
    /// value 是该字符串对应的整数 ID。
    /// </summary>
    private static readonly Dictionary<string, int> s_textToId = new(StringComparer.Ordinal);

    /// <summary>
    /// 符号 ID 到诊断文本的映射。
    /// 数组下标就是符号 ID。
    /// 0 号位保留，不对应真实字符串。
    /// </summary>
    private static string?[] s_idToText = new string?[256];

    /// <summary>
    /// 当前已经分配到的最大符号 ID。
    /// 0 保留不用，因此第一个真实符号 ID 是 1。
    /// </summary>
    private static int s_nextId;

    /// <summary>
    /// 登记一个诊断字符串，并返回它的符号 ID。
    /// </summary>
    /// <param name="text">
    /// 要登记的诊断字符串。
    /// 可以是事件名、Layer 名、handler 名。
    /// null 或空字符串会被当作 Unknown。
    /// </param>
    /// <returns>
    /// text 对应的符号 ID。
    /// 同一个 text 在同一次运行中会返回同一个 ID。
    /// </returns>
    public static int Intern(string? text)
    {
        // 0 表示未知符号，避免为 null 或空字符串分配真实 ID。
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        lock (s_lock)
        {
            // 如果字符串已经登记过，直接返回已有 ID。
            if (s_textToId.TryGetValue(text, out var existingId))
            {
                return existingId;
            }

            // 分配新的符号 ID。
            var newId = ++s_nextId;

            // 确保 id -> text 数组足够容纳 newId。
            EnsureCapacity(newId);

            // 建立双向映射。
            s_textToId[text] = newId;
            s_idToText[newId] = text;

            return newId;
        }
    }

    /// <summary>
    /// 根据符号 ID 还原诊断字符串。
    /// </summary>
    /// <param name="id">
    /// 要还原的符号 ID。
    /// 0 或越界 ID 会被还原为 "Unknown"。
    /// </param>
    /// <returns>
    /// 符号 ID 对应的诊断字符串。
    /// </returns>
    public static string Resolve(int id)
    {
        // Volatile.Read 用于读取最新发布的数组引用。
        // 这样扩容后其他线程能看到新的数组。
        var table = Volatile.Read(ref s_idToText);

        // 使用 uint 比较可以同时处理 id < 0 和 id >= Length。
        if ((uint)id >= (uint)table.Length)
        {
            return "Unknown";
        }

        return table[id] ?? "Unknown";
    }

    /// <summary>
    /// 确保 id -> text 数组能容纳指定 ID。
    /// </summary>
    /// <param name="id">
    /// 即将写入的符号 ID。
    /// 如果 id 超出当前容量，就按 2 倍扩容。
    /// </param>
    private static void EnsureCapacity(int id)
    {
        if (id < s_idToText.Length)
        {
            return;
        }

        var newLength = s_idToText.Length;

        // 持续翻倍，直到新数组能容纳 id。
        while (newLength <= id)
        {
            newLength *= 2;
        }

        var next = new string?[newLength];

        // 复制旧映射到新数组。
        Array.Copy(s_idToText, next, s_idToText.Length);

        // 发布新数组引用。
        Volatile.Write(ref s_idToText, next);
    }
}

/// <summary>
/// 为事件类型提供诊断名称符号 ID。
/// 它只用于异常日志、调试面板、诊断报告，不参与事件派发。
/// </summary>
/// <typeparam name="TEvent">
/// 事件结构体类型。
/// </typeparam>
internal static class EventTypeSymbol<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前事件类型名称对应的符号 ID。
    /// 只在异常路径中通过 EventDiagnosticSymbols.Resolve 还原成字符串。
    /// </summary>
    public static readonly int NameId =
        EventDiagnosticSymbols.Intern(typeof(TEvent).FullName ?? typeof(TEvent).Name);
}

internal static class HandlerNameSymbol
{
    /// <summary>
    /// 获取委托 handler 的诊断名称符号 ID。
    /// </summary>
    /// <param name="handler">
    /// 事件 handler 委托。
    /// 该委托可以是实例方法、静态方法、lambda 或闭包。
    /// </param>
    /// <returns>
    /// handler 诊断名称对应的符号 ID。
    /// </returns>
    public static int FromDelegate(Delegate handler)
    {
        // Method 表示委托绑定的方法。
        var method = handler.Method;

        // DeclaringType 表示声明该方法的类型。
        // 如果它为空，则尝试使用 Target 的运行时类型。
        var typeName =
            method.DeclaringType?.FullName ??
            handler.Target?.GetType().FullName ??
            "Global";

        // lambda 或闭包方法常见名称类似 "<MethodName>b__0_0"。
        // 这种名称过长且不稳定，所以可以折叠为 "lambda"。
        var methodName = NormalizeMethodName(method);

        // 只在注册或 Rebuild 阶段拼接字符串。
        // 正常事件派发阶段不会再次拼接。
        return EventDiagnosticSymbols.Intern($"{typeName}.{methodName}");
    }

    /// <summary>
    /// 获取接口 handler 对象的诊断名称符号 ID。
    /// </summary>
    /// <param name="handler">
    /// 实现 IEventHandler&lt;T&gt; 或类似接口的 handler 实例。
    /// </param>
    /// <returns>
    /// handler 类型名称对应的符号 ID。
    /// </returns>
    public static int FromInstance(object handler)
    {
        var type = handler.GetType();

        // 对接口式 handler 来说，类型名通常比方法名更有诊断意义。
        return EventDiagnosticSymbols.Intern(type.FullName ?? type.Name);
    }

    /// <summary>
    /// 规范化方法名，避免 lambda 或闭包名污染诊断输出。
    /// </summary>
    /// <param name="method">
    /// 需要规范化名称的方法信息。
    /// </param>
    /// <returns>
    /// 更适合日志展示的方法名。
    /// </returns>
    private static string NormalizeMethodName(MethodInfo method)
    {
        var name = method.Name;

        // 编译器生成方法通常包含尖括号。
        // 这里做保守处理，避免日志里出现过长的编译器内部名。
        if (name.StartsWith("<", StringComparison.Ordinal) &&
            name.Contains('>', StringComparison.Ordinal))
        {
            return "lambda";
        }

        return name;
    }
}

internal static class LayerErrorReporter
{
    /// <summary>
    /// 将基于符号 ID 的异常报告转换为旧版字符串报告。
    /// </summary>
    /// <param name="layerIndex">
    /// 抛出异常的 Layer 下标。
    /// </param>
    /// <param name="handlerNameId">
    /// handler 名称符号 ID。
    /// </param>
    /// <param name="eventNameId">
    /// 事件名称符号 ID。
    /// </param>
    /// <param name="exception">
    /// handler 抛出的异常。
    /// </param>
    public static void ReportBySymbolId(
        int       layerIndex,
        int       handlerNameId,
        int       eventNameId,
        Exception exception)
    {
        var handlerName = EventDiagnosticSymbols.Resolve(handlerNameId);
        var eventName = EventDiagnosticSymbols.Resolve(eventNameId);

        LayerBase.LayerHub.ReportLayerEventError(
            layerIndex,
            handlerName,
            eventName,
            exception);
    }
}