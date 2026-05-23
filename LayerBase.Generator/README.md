# 🚀 LayerBase.Generator: 工业级架构源生成器

**LayerBase.Generator** 是 [LayerBase 高性能架构总线](https://github.com/avaw23112/LayerBase)的专属 **C# 源生成器（Source
Generator）** 插件。

它负责将高层抽象的依赖注入（DI）和事件订阅特性，在**编译期**转化为直接且连续的底层委托（SOA 数组布局），从而帮助 LayerBase 实现了
**零运行期反射开销**和高达 1.5 亿次/秒的极限吞吐量。

---

## 🎯 核心功能与特性

通过挂载简单的特性，`LayerBase.Generator` 将在后台静默生成所有的样板代码：

1. **自动依赖注入与层级挂载 (`[OwnerLayer]` / `[OwnerService]` / `[Mount]`)**
    - 使用 `[OwnerLayer(typeof(YourLayer))]` 将 `Service` 或 `ILayerCallHandler<TRequest, TResponse>` 绑定到 Layer。
    - 使用 `[OwnerService(typeof(YourService))]` 将 `ILayerContext`、`IEventHandler<T>` 或 `IEventHandlerAsync<T>` 绑定到 Service。
    - 使用 `[Mount]` 进行父级显式装配、顺序控制、字段/属性注入，以及 interface/abstract 到实现类型的绑定。
    - `LayerHub.CreateLayers().Build()` 能够自动完成注册、实例创建、挂载与注入。
2. **Call 自动绑定与边界约束 (`[Call]`)**
    - `[Call]` 方法现在只允许定义在 `Layer` 上。
    - 如果您需要显式独立处理器，请使用 `ILayerCallHandler<TRequest, TResponse> + [OwnerLayer]`。
    - `Call` 只表示 Layer 级**单目标功能切片**，不应该被扩展为多 Layer 聚合、广播或工作流编排边界。
3. **事件总线零反射绑定 (`[SubscribeFlow]`, `[SubscribeAsync]`)**
    - 不再需要手动维护繁琐的 `EventBus.SubscribeFlow<T>(Method)`。
    - 只需在您的 `Manager` (继承 `ILayerContext`) 的事件处理方法上挂载 `[SubscribeFlow]` 或 `[SubscribeAsync]`。
    - 源生成器会直接提取函数的底层委托，生成高密度的包装类并注入全局总线，**彻底消除运行时反射查找。**
4. **全局异常与元数据观察 (`EventMetaData<T>`)**
    - 对于网络同步包或核心状态事件，只需定义一个继承自 `EventMetaData<T>` 的类。
    - 生成器会自动将它与 `partial struct` 事件绑定，建立一个**全局级别的、零侵入的异常拦截点**
      ，任何对该事件处理所产生的未捕获异常都会流向这里进行统一监控。

---

## 📦 如何安装与配置

由于本包属于编译期分析器（Analyzer），在引入它时请务必**配置引用类型**，以防将其作为运行库打包进项目中。

1. **NuGet 快速安装**：
   ```bash
   dotnet add package LayerBase.Generator --version 1.3.3
   ```
2. **正确配置 .csproj 引用**：
   当您在项目中使用该生成器时，确保它的 `OutputItemType` 设置为 `Analyzer`，且不输出运行时程序集：
   ```xml
   <ItemGroup>
       <PackageReference Include="LayerBase.Generator" Version="1.3.3" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
   *(如果您使用的是本地源码依赖，配置方法类似)*：
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```

---

## ⚙️ 常见问题 (FAQ)

### 为什么提示“事件类型必须是 partial 的”？

当您使用 `EventMetaData<T>` 为事件绑定全局异常观察者时，生成器需要向该结构体中注入静态代码，以完成无反射的底层注册。因此，您的事件必须像这样声明：

```csharp
public partial struct PlayerDeadEvent { ... }
```

### 为什么我的 [SubscribeFlow] 报错或没有生效？

1. 请确保您的类继承了 `ILayerContext` 接口（通常是各种 `Manager`）。
2. 请确保您的类被标记为 `partial`，因为生成器需要在该类的同名分部中写入 `Initialize` 接口的实现逻辑。
3. 请确保您的处理方法的参数前面使用了 `in` 修饰符（如 `in DamageEvent e`），以享受结构体的零分配传递。

### 为什么我的 `[Call]` 不能再写在 `IService` 上？

因为 `Call` 现在被收紧为 **Layer 级单目标功能切片**：

1. `[Call]` 只能定义在 `Layer` 方法上；
2. `IService` / `ILayerContext` 不再允许声明 `[Call]`；
3. 如果您需要独立处理器，请改用 `ILayerCallHandler<TRequest, TResponse> + [OwnerLayer]`。

如果一个请求想同时命中多个 Layer、做聚合、广播或流程协调，这通常说明您需要更显式的编排模型，而不是继续扩大 `Call` 的语义。

### 什么时候用 `[OwnerService]`，什么时候用 `[Mount]`？

* 用 `[OwnerService]`：当您只想声明“这个 Manager / EventHandler 属于哪个 Service 域”；
* 用 `[Mount]`：当您需要显式装配、字段顺序语义、接口实现绑定或父级注入控制。

---

## 🔗 关于 LayerBase

`LayerBase.Generator` 是 LayerBase 极限性能生态的最后一块拼图。结合 `LBTask` 的零分配异步和底层的 SOA 零分支引擎，构建您的工业级架构。

项目主页：[LayerBase GitHub 仓库](https://github.com/avaw23112/LayerBase)

---
---

# 🚀 LayerBase.Generator: Industrial-Grade Architecture Source Generator

**LayerBase.Generator** is the exclusive **C# Source Generator** plugin for
the [LayerBase High-Performance Architecture Bus](https://github.com/avaw23112/LayerBase).

It is responsible for transforming high-level abstractions like Dependency Injection (DI) and event subscription
attributes into direct, contiguous low-level delegates (**SOA Array Layout**) during **compile-time**. This enables
LayerBase to achieve **zero runtime reflection overhead** and extreme throughput of up to 150 million ops/sec.

---

## 🎯 Core Features & Highlights

By attaching simple attributes, `LayerBase.Generator` silently generates all the boilerplate code in the background:

1. **Automatic DI and ownership binding (`[OwnerLayer]` / `[OwnerService]` / `[Mount]`)**
    - Use `[OwnerLayer(typeof(YourLayer))]` to bind a `Service` or `ILayerCallHandler<TRequest, TResponse>` to a Layer.
    - Use `[OwnerService(typeof(YourService))]` to bind an `ILayerContext`, `IEventHandler<T>`, or `IEventHandlerAsync<T>` to a Service.
    - Use `[Mount]` for explicit parent-owned assembly, ordering, field/property injection, and interface/abstract implementation binding.
    - `LayerHub.CreateLayers().Build()` automatically completes registration, instance creation, mounting, and injection.
2. **Call auto-binding and boundary rules (`[Call]`)**
    - `[Call]` methods are now allowed only on `Layer`.
    - If you want an explicit standalone handler, use `ILayerCallHandler<TRequest, TResponse> + [OwnerLayer]`.
    - `Call` only represents a Layer-level **single-target functional slice**; it should not hide multi-layer aggregation, broadcast, or workflow orchestration.
3. **Zero-Reflection Event Bus Binding (`[SubscribeFlow]`, `[SubscribeAsync]`)**
    - No more manual maintenance of tedious `EventBus.SubscribeFlow<T>(Method)` calls.
    - Just attach `[SubscribeFlow]` or `[SubscribeAsync]` to the event handling methods in your `Manager` (inheriting
      from
      `ILayerContext`).
    - The Source Generator extracts the underlying delegates directly, generates high-density wrapper classes, and
      injects them into the global bus, **completely eliminating runtime reflection lookups.**
4. **Global Exception & MetaData Observation (`EventMetaData<T>`)**
    - For network sync packets or core state events, simply define a class inheriting from `EventMetaData<T>`.
    - The generator automatically binds it to your `partial struct` event, establishing a **global-level, non-intrusive
      exception interception point**. Any unhandled exceptions generated during the processing of that event will flow
      here for unified monitoring.

---

## 📦 Installation & Configuration

Since this package is a compile-time analyzer, it must be **configured as an analyzer reference** to prevent it from
being bundled into your project as a runtime library.

1. **Quick Install via NuGet**:
   ```bash
   dotnet add package LayerBase.Generator --version 1.3.3
   ```
2. **Correct .csproj Reference Configuration**:
   When using this generator in your project, ensure its `OutputItemType` is set to `Analyzer` and that it does not
   output a runtime assembly:
   ```xml
   <ItemGroup>
       <PackageReference Include="LayerBase.Generator" Version="1.3.3" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```
   *(If you are using a local source dependency, the configuration is similar)*:
   ```xml
   <ItemGroup>
       <ProjectReference Include="LayerBase.Generator\LayerBase.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```

---

## ⚙️ FAQ

### Why does it say "Event type must be partial"?

When using `EventMetaData<T>` to bind a global exception observer to an event, the generator needs to inject static code
into the struct to complete the reflection-free low-level registration. Therefore, your event must be declared like
this:

```csharp
public partial struct PlayerDeadEvent { ... }
```

### Why is my [SubscribeFlow] erroring or not taking effect?

1. Ensure your class inherits the `ILayerContext` interface (typically your various `Managers`).
2. Ensure your class is marked as `partial`, as the generator needs to write the implementation logic for the
   `Initialize` interface in a partial part of that class.
3. Ensure your handling method's parameter uses the `in` modifier (e.g., `in DamageEvent e`) to enjoy zero-allocation
   passing of structs.

### Why can’t I put `[Call]` on `IService` anymore?

Because `Call` is now intentionally narrowed to a **Layer-level single-target functional slice**:

1. `[Call]` can only be declared on `Layer` methods;
2. `IService` / `ILayerContext` must not declare `[Call]`;
3. if you need an explicit standalone handler type, use `ILayerCallHandler<TRequest, TResponse> + [OwnerLayer]`.

If one request wants to hit multiple layers, aggregate responses, broadcast, or coordinate a workflow, that usually
means you need a more explicit orchestration model instead of widening `Call` semantics.

### When should I use `[OwnerService]` vs `[Mount]`?

* Use `[OwnerService]` when you only want to declare which Service domain owns a Manager / EventHandler.
* Use `[Mount]` when you need explicit assembly, field-order semantics, interface implementation binding, or parent-controlled injection.

---

## 🔗 About LayerBase

`LayerBase.Generator` is the final piece of the LayerBase extreme performance ecosystem. Combine it with `LBTask` for
zero-allocation async and the low-level SOA branchless engine to build your industrial-grade architecture.

Project Homepage: [LayerBase GitHub Repository](https://github.com/avaw23112/LayerBase)
