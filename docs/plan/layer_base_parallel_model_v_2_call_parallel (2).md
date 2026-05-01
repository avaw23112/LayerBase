# LayerBase 并发模型设计文档 v2

## 1. 设计结论

LayerBase 的并发模型不应做成一套复杂的通用并发框架。

为了保证可用性，最终并发能力收缩为两条能力线：

```text
SubscribeParallel
    简单后台订阅。
    事件触发后，框架把 handler 投递到后台线程执行。
    框架不收集业务结果。
    框架不保证线程安全。
    用户自己负责锁、共享状态和副作用安全。

CallParallel
    强约束并行 Call。
    外部传入 request。
    request 提供线性 payload。
    框架自动切片、并行执行、收集结果。
    返回 LBTask<TResponse>。
    外部 await 后得到综合 response。
```

不新增 `[ParallelSliceHandler]`、`[ParallelCall]` 等新特性。

并发 Call 的注册继续使用项目现有风格：

```text
OwnerService / OwnerLayer
    负责声明归属。

接口实现
    负责声明 handler 类型。

源生成器
    根据接口类型生成注册代码和异步包装器。
```

一句话总结：

```text
SubscribeParallel 给粗放后台执行。
CallParallel 给安全、易用、可 await 的并行数据计算。
```

---

## 2. 为什么要收缩并发模型

之前讨论过的完整并发模型包括：

```text
SerialConsumer
JobPerEvent
SliceTask
CompletionReceiver
ResultStore
```

这套模型能力完整，但概念过多，用户使用成本高。

LayerBase 的定位是游戏架构框架，不是通用并发库。

因此并发能力应该遵循：

```text
少概念
强约束
高可用
和现有 Call / Owner / Source Generator 风格一致
```

最终选择：

```text
保留 SubscribeParallel 的简单语义。
新增 CallParallel 作为真正的安全并行计算入口。
```

---

## 3. SubscribeParallel 的定位

`SubscribeParallel` 保留为简单后台订阅。

它的语义是：

```text
事件到来。
框架把对应 parallel handler 投递给后台线程池。
handler 自己执行。
框架最多捕获异常并报告。
框架不等待结果。
框架不收集 response。
框架不做自动切片。
```

适合：

```text
日志写入
后台打点
异步缓存刷新
用户自己加锁保护的后台副作用
不关心返回值的后台任务
```

不适合：

```text
需要 await 的计算
需要返回综合结果的计算
需要自动切片的批量计算
需要框架保证 payload 边界的并行数据处理
```

文档中必须明确：

```text
SubscribeParallel 是 fire-and-forget。
fire-and-forget 表示发出去后不等待结果。
它不是安全并行计算模型。
```

---

## 4. CallParallel 的定位

`CallParallel` 是新的主要并发能力。

它是普通 `Call` 语义的并行扩展：

```text
普通 Call:
    request -> response

CallParallel:
    request -> LBTask<response>
```

外部使用方式应尽量简单：

```csharp
var response = await LayerHub.CallParallel(request);
```

用户只需要关心：

```text
request 里有什么输入数据。
response 里会返回什么结果。
```

用户不需要关心：

```text
StartIndex
EndIndex
sliceCount
RemainingParts
ThreadPool
CompletionQueue
LBTaskCompletionSource
```

---

## 5. CallParallel 的核心执行流程

```text
外部调用：
    await LayerHub.CallParallel(request)

内部执行：
    1. 根据 TRequest 找到生成器注册的 parallel call route。
    2. 调用 generated asyncHandler。
    3. asyncHandler 读取 request 中的线性 payload。
    4. 框架根据 payload 长度和切片策略自动计算切片边界。
    5. 框架分配 output 数组。
    6. 框架提交多个后台切片任务。
    7. 每个后台任务只处理自己的 ReadOnlySpan<TInput> 和 Span<TOutput>。
    8. 所有切片完成后，调用 handler.Complete 生成 TResponse。
    9. response 通过主线程 completion 队列完成 LBTask<TResponse>。
    10. await 方拿到 response。
```

重要边界：

```text
ParallelHandler 本身不是 async。
ParallelHandler 只做同步切片计算。
异步能力由源生成器生成的 asyncHandler 提供。
```

---

## 6. 不新增特性：使用 Owner + Interface 注册

并发 handler 不通过新特性注册。

不引入：

```csharp
[ParallelSliceHandler]
[ParallelCall]
```

继续使用：

```text
OwnerService
OwnerLayer
```

并通过接口区分 handler 种类。

类比现有异步事件 handler 的接口注册方式：

```text
IEventHandlerAsync<TEvent>
    表示异步事件 handler。

IParallelTransformHandler<TRequest,TInput,TOutput,TResponse>
    表示并行 Transform Call handler。
```

也就是说：

```text
OwnerService / OwnerLayer
    说明这个 handler 属于哪个 Service 或 Layer。

IParallelTransformHandler
    说明这个 handler 是一个并行 Call handler。
```

---

## 7. 第一版只支持 Transform Parallel Call

第一版只做一种并行 Call：

```text
Transform Parallel Call
```

模型：

```text
ReadOnlyMemory<TInput>
    -> 自动切片
    -> 多个 ReadOnlySpan<TInput>
    -> 多个 Span<TOutput>
    -> 完整 TOutput[]
    -> TResponse
```

也就是：

```text
输入只读。
输出由框架分配。
每个切片只写自己的 output 范围。
所有切片完成后生成 response。
```

暂时不做：

```text
ReadOnlySlice + TPartial + Reduce
InPlaceSlice
多个 parallel handler 聚合
ResultStore + Handle
嵌套并发
自定义完成线程
```

原因：第一版必须好用、好解释、好落地。

---

## 8. 核心接口：IParallelCallHandler

```csharp
public interface IParallelCallHandler<TRequest, TResponse>
    where TRequest : struct
    where TResponse : struct
{
}
```

说明：

```text
IParallelCallHandler 是标记性接口。
它本身不定义方法。
它用于告诉源生成器：
    这是一个 CallParallel handler。
    TRequest 是 request 类型。
    TResponse 是 response 类型。
```

`IParallelCallHandler` 不直接给用户实现。

用户实现更具体的接口，例如：

```text
IParallelTransformHandler<TRequest,TInput,TOutput,TResponse>
```

---

## 9. 核心接口：IParallelTransformHandler

```csharp
public interface IParallelTransformHandler<TRequest, TInput, TOutput, TResponse>
    : IParallelCallHandler<TRequest, TResponse>
    where TRequest : struct
    where TInput : struct
    where TOutput : struct
    where TResponse : struct
{
    // GetInput 表示从 request 中取出完整线性输入 payload。
    // request 参数：外部传入的并行请求。
    // 返回值：完整输入数据。框架会根据返回值的 Length 自动切片。
    ReadOnlyMemory<TInput> GetInput(in TRequest request);

    // ExecuteSlice 表示执行一个切片。
    // input 参数：当前切片的只读输入数据。
    // output 参数：当前切片的可写输出数据。
    // 该方法在后台线程中同步执行。
    // 该方法不允许访问 Layer、Service、World 或主线程对象。
    void ExecuteSlice(
        ReadOnlySpan<TInput> input,
        Span<TOutput> output);

    // Complete 表示所有切片执行完成后生成最终 response。
    // request 参数：原始请求。
    // output 参数：所有切片写完后的完整输出数据。
    // 返回值：CallParallel 最终返回给 await 方的 response。
    TResponse Complete(
        in TRequest request,
        ReadOnlyMemory<TOutput> output);
}
```

设计理由：

```text
GetInput
    让 request 自己决定 payload 存在哪里。

ExecuteSlice
    强制 handler 面向线性切片。
    不暴露 start/end。
    不暴露完整数组。

Complete
    所有切片完成后统一生成 response。
```

---

## 10. Handler 示例

### 10.1 输入数据

```csharp
public readonly struct DamageInput
{
    // Hp 表示原始血量。
    public readonly float Hp;

    // Damage 表示本次伤害值。
    public readonly float Damage;

    // hp 参数：原始血量。
    // damage 参数：本次伤害值。
    public DamageInput(float hp, float damage)
    {
        Hp = hp;
        Damage = damage;
    }
}
```

### 10.2 输出数据

```csharp
public readonly struct DamageOutput
{
    // FinalHp 表示计算后的血量。
    public readonly float FinalHp;

    // finalHp 参数：计算后的血量。
    public DamageOutput(float finalHp)
    {
        FinalHp = finalHp;
    }
}
```

### 10.3 Request

```csharp
public readonly struct DamageParallelRequest
{
    // Input 表示完整线性输入数据。
    // CallParallel 会根据 Input.Length 自动切片。
    public readonly ReadOnlyMemory<DamageInput> Input;

    // input 参数：完整线性输入数据。
    public DamageParallelRequest(ReadOnlyMemory<DamageInput> input)
    {
        Input = input;
    }
}
```

### 10.4 Response

```csharp
public readonly struct DamageParallelResponse
{
    // Output 表示并行计算后的完整输出数据。
    // 第一版为了可用性，允许 response 直接持有 ReadOnlyMemory<T>。
    public readonly ReadOnlyMemory<DamageOutput> Output;

    // output 参数：并行计算后的完整输出数据。
    public DamageParallelResponse(ReadOnlyMemory<DamageOutput> output)
    {
        Output = output;
    }
}
```

### 10.5 Handler

```csharp
[OwnerService(typeof(BattleService))]
public sealed partial class DamageParallelHandler :
    IParallelTransformHandler<
        DamageParallelRequest,
        DamageInput,
        DamageOutput,
        DamageParallelResponse>
{
    // GetInput 从 request 中取出完整输入数据。
    // request 参数：外部传入的 DamageParallelRequest。
    // 返回值：完整输入 payload。
    public ReadOnlyMemory<DamageInput> GetInput(
        in DamageParallelRequest request)
    {
        return request.Input;
    }

    // ExecuteSlice 在后台线程中执行。
    // input 参数：当前切片的只读输入。
    // output 参数：当前切片的可写输出。
    // input.Length 和 output.Length 应该一致。
    public void ExecuteSlice(
        ReadOnlySpan<DamageInput> input,
        Span<DamageOutput> output)
    {
        for (var i = 0; i < input.Length; i++)
        {
            var item = input[i];
            var finalHp = Math.Max(0f, item.Hp - item.Damage);

            output[i] = new DamageOutput(finalHp);
        }
    }

    // Complete 在所有切片完成后执行。
    // request 参数：原始请求。
    // output 参数：完整输出数据。
    // 返回值：最终 response。
    public DamageParallelResponse Complete(
        in DamageParallelRequest request,
        ReadOnlyMemory<DamageOutput> output)
    {
        return new DamageParallelResponse(output);
    }
}
```

### 10.6 调用

```csharp
var request = new DamageParallelRequest(inputs);

DamageParallelResponse response =
    await LayerHub.CallParallel(request);

ReadOnlyMemory<DamageOutput> output = response.Output;
```

用户视角：

```text
像普通 Call 一样调用。
只是返回值是 LBTask<TResponse>。
```

---

## 11. 源生成器职责

源生成器扫描：

```text
OwnerService / OwnerLayer
    +
IParallelTransformHandler<TRequest,TInput,TOutput,TResponse>
```

然后生成以下内容。

---

### 11.1 路由注册

生成：

```text
TRequest -> TResponse -> GeneratedAsyncHandler
```

示意：

```csharp
registry.RegisterParallelCall<DamageParallelRequest, DamageParallelResponse>(
    new DamageParallelGeneratedAsyncHandler(
        owner: ownerInstance,
        handler: new DamageParallelHandler()));
```

说明：

```text
RegisterParallelCall 将 request 类型映射到生成的 asyncHandler。
CallParallel 时通过 TRequest 找到对应 handler。
```

第一版建议：

```text
一个 TRequest 只允许注册一个 ParallelCall handler。
```

避免多个 handler 的 response 合并问题。

---

### 11.2 生成 asyncHandler

用户写的是同步切片 handler。

源生成器生成异步包装器。

示意：

```csharp
internal sealed class DamageParallelGeneratedAsyncHandler
{
    // _handler 表示用户实现的切片 handler。
    private readonly DamageParallelHandler _handler;

    // _runtime 表示当前并发运行时。
    // 它负责后台任务调度、主线程完成回收和 LBTask 完成。
    private readonly ParallelRuntime _runtime;

    // handler 参数：用户实现的 DamageParallelHandler。
    // runtime 参数：当前 LayerRuntime 所属的 ParallelRuntime。
    public DamageParallelGeneratedAsyncHandler(
        DamageParallelHandler handler,
        ParallelRuntime runtime)
    {
        _handler = handler;
        _runtime = runtime;
    }

    // CallAsync 是源生成器生成的异步包装器。
    // request 参数：外部传入的并行 request。
    // 返回值：可 await 的 LBTask<DamageParallelResponse>。
    public LBTask<DamageParallelResponse> CallAsync(
        in DamageParallelRequest request)
    {
        var input = _handler.GetInput(in request);
        var output = new DamageOutput[input.Length];

        return _runtime.RunTransform(
            request: request,
            input: input,
            output: output,
            executeSlice: _handler.ExecuteSlice,
            complete: _handler.Complete);
    }
}
```

上面的代码是结构示意。

实际生成器应避免多余委托分配，可以生成静态调用路径。

---

## 12. ParallelRuntime 的职责

`ParallelRuntime` 不对外暴露复杂 API。

它主要供生成器生成的 asyncHandler 使用。

职责：

```text
1. 计算切片边界。
2. 提交后台任务。
3. 维护 RemainingParts。
4. 捕获异常。
5. 调用 Complete。
6. 把 response 投递回主线程 completion 队列。
7. 在主线程 Pump 中完成 LBTask<TResponse>。
```

示意接口：

```csharp
public sealed class ParallelRuntime
{
    // RunTransform 执行 Transform 型并行 Call。
    // request 参数：原始 request。
    // input 参数：完整输入数据。
    // output 参数：完整输出数组，由框架或生成器分配。
    // executeSlice 参数：用户实现的同步切片计算方法。
    // complete 参数：用户实现的最终 response 生成方法。
    // 返回值：可 await 的 LBTask<TResponse>。
    public LBTask<TResponse> RunTransform<TRequest, TInput, TOutput, TResponse>(
        in TRequest request,
        ReadOnlyMemory<TInput> input,
        TOutput[] output,
        SliceExecute<TInput, TOutput> executeSlice,
        TransformComplete<TRequest, TOutput, TResponse> complete)
        where TRequest : struct
        where TInput : struct
        where TOutput : struct
        where TResponse : struct
    {
        throw new NotImplementedException();
    }
}

public delegate void SliceExecute<TInput, TOutput>(
    ReadOnlySpan<TInput> input,
    Span<TOutput> output)
    where TInput : struct
    where TOutput : struct;

public delegate TResponse TransformComplete<TRequest, TOutput, TResponse>(
    in TRequest request,
    ReadOnlyMemory<TOutput> output)
    where TRequest : struct
    where TOutput : struct
    where TResponse : struct;
```

说明：

```text
SliceExecute 是切片计算委托。
TransformComplete 是全部切片完成后的 response 生成委托。
实际生成代码可以避免委托分配，这里只是表达职责。
```

---

## 13. 自动切片策略

用户不处理 `StartIndex` 和 `EndIndex`。

框架内部维护切片范围。

```csharp
internal readonly struct ParallelSliceRange
{
    // StartIndex 表示当前切片起始位置。
    public readonly int StartIndex;

    // Count 表示当前切片包含多少个元素。
    public readonly int Count;

    // startIndex 参数：切片起始位置。
    // count 参数：切片元素数量。
    public ParallelSliceRange(int startIndex, int count)
    {
        StartIndex = startIndex;
        Count = count;
    }
}
```

构建切片：

```csharp
private static int BuildRanges(
    int length,
    int workerCount,
    int maxParts,
    Span<ParallelSliceRange> ranges)
{
    // length 参数：输入 payload 的总长度。
    // workerCount 参数：可用于并行执行的 worker 数量。
    // maxParts 参数：最大切片数。
    // ranges 参数：写入切片范围的缓冲区。
    // 返回值：实际生成的切片数量。

    if (length <= 0)
    {
        return 0;
    }

    var targetParts = Math.Min(
        maxParts,
        Math.Max(1, workerCount * 4));

    var partCount = Math.Min(length, targetParts);
    var chunkSize = (length + partCount - 1) / partCount;

    var count = 0;

    for (var start = 0; start < length; start += chunkSize)
    {
        if (count >= ranges.Length)
        {
            break;
        }

        var size = Math.Min(chunkSize, length - start);
        ranges[count] = new ParallelSliceRange(start, size);
        count++;
    }

    return count;
}
```

默认策略建议：

```text
sliceCount ≈ workerCount * 4
```

原因：

```text
切片太少，部分 worker 可能空闲。
切片太多，调度开销变大。
workerCount * 4 是相对稳妥的默认值。
```

---

## 14. Worker 执行方式

由于 `Span<T>` 不能跨线程保存，也不能被闭包长期捕获，因此任务对象中不保存 Span。

任务对象只保存：

```text
input memory
output array
startIndex
count
```

worker 执行时临时构造 Span：

```csharp
private static void ExecutePart<TInput, TOutput>(
    ReadOnlyMemory<TInput> input,
    TOutput[] output,
    int startIndex,
    int count,
    SliceExecute<TInput, TOutput> executeSlice)
    where TInput : struct
    where TOutput : struct
{
    // inputSlice 表示当前切片的只读输入。
    // 它只在当前方法调用栈中存在，不会被保存到字段里。
    ReadOnlySpan<TInput> inputSlice =
        input.Span.Slice(startIndex, count);

    // outputSlice 表示当前切片的可写输出。
    // 不同切片的 outputSlice 范围不重叠。
    Span<TOutput> outputSlice =
        output.AsSpan(startIndex, count);

    executeSlice(inputSlice, outputSlice);
}
```

这保证：

```text
用户 handler 只接收当前切片 Span。
用户 handler 不需要知道 start/end。
框架仍能安全地在线程池任务中保存边界信息。
```

---

## 15. LBTask 完成策略

`CallParallel` 返回 `LBTask<TResponse>`。

不建议后台线程直接完成 await continuation。

推荐：

```text
后台线程完成计算。
最后一个切片调用 Complete 得到 response。
将 response 写入 ParallelCallCompletionQueue。
主线程 Pump drain completion。
主线程完成 LBTask<TResponse>。
```

原因：

```text
await CallParallel 后的继续执行位置更符合主线程直觉。
避免 continuation 在后台线程执行后访问主线程对象。
```

流程：

```text
CallParallel
    -> 返回未完成 LBTask<TResponse>

后台完成所有切片
    -> Complete 生成 response
    -> Enqueue completion

LayerRuntime.Pump
    -> Drain ParallelCallCompletionQueue
    -> SetResult(response)
```

---

## 16. ParallelCallCompletionQueue

`ParallelCallCompletionQueue` 是后台线程到主线程的完成队列。

它可以和其他 parallel completion 共用 MPSC 实现。

第一版建议只用于完成 `LBTask<TResponse>`。

```csharp
internal readonly struct ParallelCallCompletionItem
{
    // PromiseHandle 表示要完成的 LBTask promise。
    public readonly ParallelPromiseHandle PromiseHandle;

    // ResponsePayloadHandle 表示 response 在 payload storage 中的位置。
    // 这样可以避免把不同 TResponse 装箱成 object。
    public readonly PayloadHandle ResponsePayloadHandle;

    // ResponseTypeId 表示 TResponse 的类型编号。
    public readonly int ResponseTypeId;

    // promiseHandle 参数：要完成的 LBTask promise。
    // responsePayloadHandle 参数：response payload 句柄。
    // responseTypeId 参数：response 类型编号。
    public ParallelCallCompletionItem(
        ParallelPromiseHandle promiseHandle,
        PayloadHandle responsePayloadHandle,
        int responseTypeId)
    {
        PromiseHandle = promiseHandle;
        ResponsePayloadHandle = responsePayloadHandle;
        ResponseTypeId = responseTypeId;
    }
}
```

第一版也可以为了可用性直接在内部用泛型 completion 对象。

但长期低 GC 方向应使用：

```text
TypeId + PayloadHandle
```

---

## 17. 异常处理

后台切片异常必须被捕获。

默认策略：

```text
任意切片异常
    -> 标记 batch faulted
    -> 不再调用 Complete
    -> 主线程 Pump 时让 LBTask<TResponse> 进入异常状态
```

示意：

```csharp
try
{
    ExecutePart(...);
}
catch (Exception ex)
{
    // ReportPartFault 表示记录切片异常。
    // 它应保证只有第一次异常会完成 promise。
    batch.ReportPartFault(ex);
}
```

第一版建议使用 FailFast：

```text
FailFast
    任意切片失败后，整个 CallParallel 失败。
```

暂时不做：

```text
部分失败仍返回部分结果。
```

这会让语义复杂很多。

---

## 18. 取消策略

第一版可以先不公开复杂取消接口。

但内部建议预留：

```text
CancellationToken
```

未来可以支持：

```csharp
var response = await LayerHub.CallParallel(request, cancellationToken);
```

第一版如果暂不支持取消，也应在接口设计中避免阻塞未来扩展。

如果未来支持取消，语义应为：

```text
取消请求发生后：
    不再提交新切片。
    已经运行的切片只能协作式停止。
    LBTask<TResponse> 进入取消状态。
```

注意：C# 不能安全强杀线程。

取消只能是协作式。

---

## 19. 背压策略

`CallParallel` 可能提交大量切片任务。

需要限制：

```text
最大并行 Call 数
最大飞行中切片数
最大 completion 队列容量
```

第一版建议简单策略：

```text
如果超过限制：
    CallParallel 直接返回 failed LBTask。
```

不要默认阻塞调用方。

可配置项：

```csharp
public readonly struct ParallelCallOptions
{
    // MaxConcurrentCalls 表示最多允许多少个 CallParallel 同时运行。
    public readonly int MaxConcurrentCalls;

    // MaxInFlightParts 表示最多允许多少个切片任务处于未完成状态。
    public readonly int MaxInFlightParts;

    // MaxPartsPerCall 表示单次 CallParallel 最多切出多少个分片。
    public readonly int MaxPartsPerCall;

    // CompletionQueueCapacity 表示后台完成队列容量。
    public readonly int CompletionQueueCapacity;

    // maxConcurrentCalls 参数：最大并行 Call 数。
    // maxInFlightParts 参数：最大飞行中切片数。
    // maxPartsPerCall 参数：单次 Call 最大切片数。
    // completionQueueCapacity 参数：完成队列容量。
    public ParallelCallOptions(
        int maxConcurrentCalls,
        int maxInFlightParts,
        int maxPartsPerCall,
        int completionQueueCapacity)
    {
        MaxConcurrentCalls = maxConcurrentCalls;
        MaxInFlightParts = maxInFlightParts;
        MaxPartsPerCall = maxPartsPerCall;
        CompletionQueueCapacity = completionQueueCapacity;
    }
}
```

---

## 20. Payload 类型约束

为了可用性第一，第一版建议：

```csharp
where TInput : struct
where TOutput : struct
```

不强制：

```csharp
where TInput : unmanaged
where TOutput : unmanaged
```

但文档必须警告：

```text
TInput / TOutput 应尽量是纯值类型数据。
不要在其中存放可变引用对象。
不要在 ExecuteSlice 中修改外部对象。
```

后续可以增加严格模式：

```text
StrictParallelPayload
    要求 TInput / TOutput 是 unmanaged。
```

`unmanaged` 是 C# 泛型约束，表示类型不能包含托管引用字段。

它更安全，但限制更强。

---

## 21. 生成器校验规则

源生成器应校验：

```text
1. handler 类型必须有 OwnerService 或 OwnerLayer 归属。
2. handler 必须实现 IParallelTransformHandler<TRequest,TInput,TOutput,TResponse>。
3. TRequest / TResponse / TInput / TOutput 必须是 struct。
4. ExecuteSlice 必须是同步方法。
5. ExecuteSlice 不允许返回 Task / ValueTask / LBTask。
6. ExecuteSlice 参数必须是 ReadOnlySpan<TInput> 和 Span<TOutput>。
7. Complete 返回值必须是 TResponse。
8. 同一个 TRequest 第一版只允许一个 parallel call route。
9. handler 类型不能继承 Layer 或 Service。
10. handler 构造函数不应要求业务对象依赖。
```

这些校验的目的：

```text
防止并行 handler 变成普通业务对象方法。
防止用户在后台线程访问主线程状态。
防止异步逻辑嵌入 ExecuteSlice。
```

---

## 22. 与 OwnerService / OwnerLayer 的关系

`OwnerService / OwnerLayer` 只表达归属，不表达执行方式。

例如：

```csharp
[OwnerService(typeof(BattleService))]
public sealed partial class DamageParallelHandler :
    IParallelTransformHandler<
        DamageParallelRequest,
        DamageInput,
        DamageOutput,
        DamageParallelResponse>
{
}
```

含义：

```text
这个并行 handler 归属于 BattleService。
生成器可以把它纳入 BattleService 的注册范围、拓扑报告和生命周期管理。

但 DamageParallelHandler 本身不是 BattleService 的方法。
它不应该访问 BattleService 实例状态。
```

如果是 Layer 归属：

```csharp
[OwnerLayer(typeof(BattleLayer))]
public sealed partial class DamageParallelHandler :
    IParallelTransformHandler<...>
{
}
```

含义类似。

---

## 23. CallParallel 路由表

运行时应有独立的并行 Call 路由表。

```text
ParallelCallRoute<TRequest,TResponse>
    -> generated asyncHandler
```

底层可使用静态泛型 ID：

```text
ParallelCallRouteId<TRequest,TResponse>.Id
```

类似普通 Call 路由。

第一版规则：

```text
一个 TRequest 只能对应一个 TResponse。
一个 TRequest 只能对应一个 parallel handler。
```

避免歧义。

---

## 24. 与普通 Call 的区别

| 能力 | Call | CallParallel |
|---|---|---|
| 返回值 | `TResponse` 或现有 Call 语义 | `LBTask<TResponse>` |
| 执行线程 | 主线程 / 同步 | 后台切片 + 主线程完成 |
| handler 类型 | Service / Layer handler | 独立接口 handler |
| 是否自动切片 | 否 | 是 |
| 是否允许访问业务对象 | 是 | ExecuteSlice 不应访问 |
| 典型用途 | 业务请求响应 | 纯数据批量计算 |

---

## 25. 与 SubscribeParallel 的区别

| 能力 | SubscribeParallel | CallParallel |
|---|---|---|
| 触发方式 | 事件通知 | request/response 调用 |
| 返回值 | 无 | `LBTask<TResponse>` |
| 结果收集 | 无 | 自动收集 |
| 自动切片 | 无 | 有 |
| 线程安全责任 | 用户负责 | 框架通过切片约束降低风险 |
| handler 形态 | 现有订阅 handler | 独立接口 handler |
| 适合场景 | 后台副作用 | 纯数据并行计算 |

---

## 26. LayerRuntime.Pump 顺序

推荐 Pump 顺序：

```text
LayerRuntime.Pump(deltaTime)
    1. TimeScheduler.Tick(deltaTime)
    2. DelayBufferSystem.Tick(deltaTime)
    3. ParallelCallCompletionQueue.Drain()
    4. PostScheduler.Pump()
    5. EventMetaDataHandler.PumpExpectations()
```

`ParallelCallCompletionQueue.Drain()` 必须在主线程执行。

它负责：

```text
完成 LBTask<TResponse>
抛出或传递后台异常
释放已完成 call 的内部状态
```

---

## 27. 第一版实施路线

### P0：保留 SubscribeParallel

不破坏当前行为。

只在文档中明确：

```text
SubscribeParallel 是粗放后台执行能力。
```

---

### P1：定义接口

新增：

```text
IParallelCallHandler<TRequest,TResponse>
IParallelTransformHandler<TRequest,TInput,TOutput,TResponse>
```

不新增特性。

---

### P2：源生成器扫描接口

生成：

```text
ParallelCall route
Generated asyncHandler
注册代码
```

---

### P3：实现 ParallelRuntime.RunTransform

实现：

```text
input 读取
output 分配
自动切片
后台任务提交
RemainingParts 计数
Complete 调用
completion 入队
```

---

### P4：实现 LBTask 主线程完成

实现：

```text
ParallelCallCompletionQueue
主线程 Drain
promise.SetResult / SetException
```

---

### P5：补充诊断

包括：

```text
重复路由诊断
payload 类型警告
handler 形态错误
并行任务异常
超过 MaxInFlight
```

---

## 28. 暂不实现的能力

第一版不做：

```text
自定义 Reduce handler
InPlace handler
多个 parallel handler 聚合一个 response
ResultStore + Handle
work-stealing scheduler
嵌套 CallParallel
后台 continuation 直接执行
复杂取消语义
```

这些不是不重要，而是会降低第一版可用性。

第一版目标是：

```text
让用户能像普通 Call 一样使用并行数据计算。
```

---

## 29. 最终总结

LayerBase 并发模型 v2 的最终形态：

```text
SubscribeParallel
    简单后台执行。
    用户自己负责线程安全。
    不返回结果。

CallParallel
    强约束并行数据计算。
    使用 OwnerService / OwnerLayer 归属。
    使用接口注册 handler。
    源生成器生成 asyncHandler。
    外部 await LBTask<TResponse>。
    内部自动切片、并行执行、收集结果。
```

真正的并发重点不在 `SubscribeParallel`，而在：

```text
CallParallel + IParallelTransformHandler + Source Generator async wrapper
```

一句话：

```text
用户只写纯数据切片处理逻辑。
也就是：给定当前切片的 ReadOnlySpan<TInput>，计算后写入对应的 Span<TOutput>。
这部分不应该写普通业务流程，不应该访问 Service / Layer / World，也不应该操作主线程对象。
源生成器负责生成异步包装器。
运行时负责并行执行、切片分配、结果收集和主线程完成。
外部像普通 Call 一样 await 结果。
```

