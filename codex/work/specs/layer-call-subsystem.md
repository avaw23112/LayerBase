# Task Spec: Layer Call Subsystem

## Goal

为 LayerBase 新增一套独立于 Event 传播体系的 `Call` 子系统，提供按层寻址、单目标、可等待、明确返回值的 request/response 调用能力。

## Required External Shape

```csharp
await LayerHub.For<TLayer>()
    .CallAsync<TRequest, TResponse>(request, cancellationToken);
```

## Required Business Interface

```csharp
public interface ILayerCallHandler<TRequest, TResponse>
{
    LBTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
```

## Constraints

- `Call` 不参与 Event 的 Bubble / Drop / Global / Handled 传播语义。
- 调用方不能直接跨层拿服务。
- `CallHandler` 必须能方便获取当前层服务，业务使用体验至少支持 `this.Get<TService>()`。
- `CallHandler` 的所属层继续通过 `[OwnerLayer(typeof(SomeLayer))]` 声明，并由源生成器自动注册。
- 不修改 `ILayerContext` 相关契约。
- 保持 diff 小、实现可逆、测试可验证。

## Non-goals

- 不把 `LayerHub` 变成全局 service locator。
- 不实现多播调用或传播型调用。
- 不改变现有 Event 子系统语义。

## Verification Targets

- `CallAsync` 路由成功并返回正确结果。
- 目标层缺失时明确失败。
- 路由缺失时明确失败。
- handler 能访问当前层服务。
- handler 不能直接拿到其他层 scoped 服务。
- `[OwnerLayer]` 自动注册有效。
- 同层重复 `(TRequest, TResponse)` 注册在构建阶段明确失败。
- `CancellationToken` 能传递到 handler。
- handler 异常会传播到调用方。
