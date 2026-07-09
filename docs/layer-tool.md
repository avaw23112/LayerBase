# LayerTool

LayerTool provides generated registration for runtime-created tool objects without runtime reflection.

## Define a Tool Attribute

```csharp
using LayerBase.Tooling;

public interface IUiView
{
}

[LayerTool("ui.view", Contract = typeof(IUiView))]
[AttributeUsage(AttributeTargets.Class)]
public sealed class UiViewAttribute : Attribute
{
    public UiViewAttribute(string key)
    {
        Key = key;
    }

    public string Key { get; }
    public string? Path { get; set; }
    public bool Cache { get; set; }
    public Type? Factory { get; set; }
    public Type? Layer { get; set; }
    public Type? Service { get; set; }
    public Type? Manager { get; set; }
}
```

## Register Generated Tools

```csharp
using LayerBase;

using var runtime = LayerHub.CreateLayers()
    .Push(new UiLayer())
    .UseGeneratedLayerTools()
    .Build();

var view = runtime.Tools.GetOrCreate<IUiView>("Inventory");
```

The generator emits direct calls to `LayerToolRegistry.Register<TContract, TImplementation>()`.
It does not use `Activator.CreateInstance`, constructor lookup, attribute lookup, or assembly type scanning.

## Creation Priority

For each marked implementation, creation is selected in this order:

1. A valid static method marked with `[LayerToolFactory]`.
2. An external factory from the tool attribute's `Factory = typeof(...)` property.
3. A public parameterless constructor.

External factories implement:

```csharp
public sealed class InventoryViewFactory : ILayerToolFactory<InventoryView>
{
    public InventoryView Create(LayerToolCreateContext context, LayerToolEntry entry)
    {
        return new InventoryView();
    }
}
```

External factory types are resolved through `LayerToolCreateContext.GetFactory<T>()`, so they must be registered in the
Runtime service container.

`LayerToolCreateContext` exposes the current registry and, when created by `LayerRuntime`, `Runtime`, `GetService<T>()`, and `GetFactory<T>()`.

## Registry Diagnostics

`LayerToolRegistry` supports entry queries and cache inspection:

```csharp
var all = runtime.Tools.GetEntries();
var views = runtime.Tools.GetEntries<IUiView>();
var uiTools = runtime.Tools.GetEntriesByToolId("ui.view");
var cached = runtime.Tools.GetCachedEntries();
var report = runtime.Tools.CreateDiagnosticsReport();
```

Cache management:

```csharp
runtime.Tools.ClearCache<InventoryView>();
runtime.Tools.ClearCache<IUiView>("Inventory");
runtime.Tools.ClearAllCaches();
```

## Analyzer Diagnostics

| Id | Meaning |
| :--- | :--- |
| LBTOOL001 | `[LayerTool]` is not applied to an Attribute type. |
| LBTOOL002 | The `[LayerTool]` target does not inherit `System.Attribute`. |
| LBTOOL003 | `Contract` is not an interface or class. |
| LBTOOL004 | A marked implementation does not implement the configured contract. |
| LBTOOL005 | The resolved key is empty. |
| LBTOOL006 | The same contract has duplicate keys. |
| LBTOOL007 | No valid creation path exists. |
| LBTOOL008 | `[LayerToolFactory]` has an invalid signature. |
| LBTOOL009 | More than one `[LayerToolFactory]` method exists on the same implementation. |
| LBTOOL010 | The tool attribute `Cache` property is not `bool`. |
| LBTOOL011 | The tool attribute `Path` property is not `string`. |
| LBTOOL012 | The tool attribute `Factory` property is not `System.Type`. |
| LBTOOL013 | The external factory does not implement `ILayerToolFactory<TImplementation>`. |
