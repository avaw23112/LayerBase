# 🚀 LayerBase.Generator: 工业级架构源生成器

**LayerBase.Generator** 是 [LayerBase 高性能架构总线](https://github.com/avaw23112/LayerBase)的专属 **C# 源生成器（Source Generator）** 插件。

它负责将高层抽象的依赖注入（DI）和事件订阅特性，在**编译期**转化为直接且连续的底层委托（SOA 数组布局），从而帮助 LayerBase 实现了**零运行期反射开销**和高达 1.5 亿次/秒的极限吞吐量。

---

## 🎯 核心功能与特性

通过挂载简单的特性，`LayerBase.Generator` 将在后台静默生成所有的样板代码：

1. **自动依赖注入与层级挂载 (`[OwnerLayer]`)**
   - 只需给您的 `Service` 或切片式的 `EventHandler` 打上 `[OwnerLayer(typeof(YourLayer))]` 特性，生成器便会生成自动注册逻辑。
   - `LayerHub.CreateLayers().Build()` 能够全自动扫描并在后台完成服务依赖、实例创建与层级挂载。
2. **事件总线零反射绑定 (`[Subscribe]`, `[SubscribeAsync]`)**
   - 不再需要手动维护繁琐的 `EventBus.Subscribe<T>(Method)`。
   - 只需在您的 `Manager` (继承 `ILayerContext`) 的事件处理方法上挂载 `[Subscribe]` 或 `[SubscribeAsync]`。
   - 源生成器会直接提取函数的底层委托，生成高密度的包装类并注入全局总线，**彻底消除运行时反射查找。**
3. **全局异常与元数据观察 (`EventMetaData<T>`)**
   - 对于网络同步包或核心状态事件，只需定义一个继承自 `EventMetaData<T>` 的类。
   - 生成器会自动将它与 `partial struct` 事件绑定，建立一个**全局级别的、零侵入的异常拦截点**，任何对该事件处理所产生的未捕获异常都会流向这里进行统一监控。

---

## 📦 如何安装与配置

由于本包属于编译期分析器（Analyzer），在引入它时请务必**配置引用类型**，以防将其作为运行库打包进项目中。

1. **NuGet 快速安装**：
   ```bash
   dotnet add package LayerBase.Generator --version 1.3.2
   ```
2. **正确配置 .csproj 引用**：
   当您在项目中使用该生成器时，确保它的 `OutputItemType` 设置为 `Analyzer`，且不输出运行时程序集：
   ```xml
   <ItemGroup>
       <PackageReference Include="LayerBase.Generator" Version="1.3.2" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
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

### 为什么我的 [Subscribe] 报错或没有生效？
1. 请确保您的类继承了 `ILayerContext` 接口（通常是各种 `Manager`）。
2. 请确保您的类被标记为 `partial`，因为生成器需要在该类的同名分部中写入 `Initialize` 接口的实现逻辑。
3. 请确保您的处理方法的参数前面使用了 `in` 修饰符（如 `in DamageEvent e`），以享受结构体的零分配传递。

---

## 🔗 关于 LayerBase
`LayerBase.Generator` 是 LayerBase 极限性能生态的最后一块拼图。结合 `LBTask` 的零分配异步和底层的 SOA 零分支引擎，构建您的工业级架构。

项目主页：[LayerBase GitHub 仓库](https://github.com/avaw23112/LayerBase)