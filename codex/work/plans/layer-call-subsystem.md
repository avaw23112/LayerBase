# Plan: Layer Call Subsystem

## Workstreams

1. 运行时接口
   - 新增 `ILayerCallHandler<TRequest, TResponse>`
   - 新增 `LayerHub.For<TLayer>()` 和目标层 `CallAsync`
   - 新增当前层服务访问扩展 `this.Get<T>()`

2. 层内路由
   - 在 `Layer` 内维护 `(TRequest, TResponse)` -> handler 路由表
   - 构建阶段注册 call handlers
   - 明确处理缺层、缺路由、重复注册

3. 源生成器
   - 为 `[OwnerLayer] + ILayerCallHandler<,>` 生成自动注册代码
   - 让现有 `LayerServiceGenerator` 忽略 CallHandler，避免误报必须实现 `IService`
   - 确保构建阶段真正应用 `LayerServiceRegistry`

4. 单元测试
   - 新增 `CallTests`
   - 覆盖需求稿列出的核心行为

5. 设计边界强约束
   - Analyze 时明确提醒：`Call` 只适合单目标功能切片，不应用来掩盖跨层功能编排
   - 任何 “同一 Request -> 多 layer / 多 response / 多重绑定” 的声明都直接报编译错误

## Expected Files

- `LayerBase/Call/*`
- `LayerBase/Application/LayerHub.cs`
- `LayerBase/Layer/Layer.cs`
- `LayerBase/Layer/LayerChain.cs`
- `LayerBase/Layer/LayerServiceRegistry.cs`
- `LayerBase/DI/ServiceContracts.cs`
- `LayerBase.Generator/LayerBase.Generator/*`
- `LayerBase.Test/CallTests.cs`

## Risks

- `[OwnerLayer]` 现有注册链路此前没有真正应用 `LayerServiceRegistry.Apply(...)`，补上后会激活原本未生效的生成注册逻辑。
- 同类型 Layer 多实例下 `LayerHub.For<TLayer>()` 可能出现歧义，需要明确错误语义。
- 生成器需要避免把 CallHandler 当成普通 `IService` 报错。
- “同一 Request 指向多个 layer / response” 这条限制是**刻意保留**的设计警示：不要把 `Call` 误用成完整功能或跨层流程编排的承载面。
