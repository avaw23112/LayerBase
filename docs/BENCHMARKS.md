# LayerBase Benchmarks

Detailed performance benchmarks for LayerBase.

## ECS Async Benchmark Boundaries

Async ECS benchmark results must be read by scenario. Do not merge these metrics into a single "EndToEnd" number.

| Benchmark family | What it measures | What it does not prove |
|:--|:--|:--|
| `EcsExecutionModeBenchmarks.Async_PlainQuery_SubmitOnly` | Public `Query<T>().ForEach(ref job)` submit cost, averaged across 1024 submits. Cleanup waits the submitted fence outside the measured body. | Worker execution, drain, actor post, renderer/game-loop interference, or cold worker wake latency. |
| `EcsExecutionModeBenchmarks.Async_PlainQuery_EndToEnd` | Warm-worker PlainQuery submit, one flush, current-fence wait. | Cold idle wake behavior. |
| `EcsAsyncSubmitBoundaryBenchmarks.WarmWorkerEndToEndBenchmark` | Focused warm-worker fence cost for a single PlainQuery. | Full game-frame stability. |
| `EcsAsyncSubmitBoundaryBenchmarks.ColdWorkerWakeLatencyBenchmark` | Platform/OS cost after the ECS worker has parked. | ECS hot-path cost. Report this separately from warm-worker EndToEnd. |
| `EcsAsyncBringBenchmarks` | Bring query execution, result drain, and actor event posting through `Pump`. | PlainQuery submit cost or cold worker wake behavior. |
| `EcsFrameBatchBenchmarks.Async_FrameBatch_SubmitFlushOnly` | One frame submitting 1/10/100/1000 PlainQueries and flushing once; this is the frame-level SubmissionBatch amortization test. | Worker execution completion time. Cleanup waits the fence outside the measured body. |
| `EcsFrameBatchBenchmarks.Async_FrameBatch_WarmWorkerEndToEnd` | One frame submitting 1/10/100/1000 PlainQueries, flushing once, waiting the current fence, then pumping. | Unity/Godot/render-thread scheduling stability or GC pressure from a real game. |

Current ECS Async benchmark coverage proves that the core async query chain can be low-allocation and low-latency in controlled warm-worker scenarios. It does not prove that a complete game runtime is always stable. A real game can still introduce additional jitter from multiple systems submitting work in the same frame, Bring event return flow, ActorWorld drain, main-thread contention, long-running worker state, GC pressure, and engine/render threads.

When reporting ECS async performance:

- Use `WarmWorker EndToEnd` for ECS async execution performance.
- Use `ColdWorkerWakeLatency` for parked-worker platform scheduling cost.
- Use `FrameBatch SubmitManyQueries FlushOnce` for per-frame submit/flush amortization.
- Use Bring-specific benchmarks for `SubmitBringQuery`, `ActorEventBatch<TEvent>`, `DrainResults`, and `ActorWorld.PostTo` paths.
- Do not use old single-invocation SubmitOnly numbers as submit-cost evidence.

## Cross-Framework Growth Comparison (Events & Fan-out)

Data taken from `LayerBase.BenchMark.Compare/bin/Release/net8.0/BenchmarkDotNet.Artifacts/results`.

### Growth by Event Kind Count (fixed batch, multi-event)

**2 subscribers per event**

| Event Kinds | Delegate Batch Cost | MessagePipe Batch Cost | LayerBase Batch Cost | LayerBase Avg/Event | LayerBase Scale vs 32 | MessagePipe Scale vs 32 | LayerBase vs MessagePipe |
|:-----|----------:|-----------------:|-----------------:|----------------:|---------------------:|-----------------------:|-------------------------:|
| 32   |  6.192 ns |       207.639 ns |   **121.643 ns** |    **3.801 ns** |                1.00x |                  1.00x |                   +41.4% |
| 128  | 27.760 ns |     1,283.180 ns | **1,118.160 ns** |    **8.735 ns** |                9.19x |                  6.18x |                   +12.8% |
| 256  | 57.890 ns |     2,828.560 ns | **2,337.390 ns** |    **9.130 ns** |               19.21x |                 13.62x |                   +17.3% |

**3 subscribers per event**

| Event Kinds | Delegate Batch Cost | MessagePipe Batch Cost | LayerBase Batch Cost | LayerBase Avg/Event | LayerBase Scale vs 32 | MessagePipe Scale vs 32 | LayerBase vs MessagePipe |
|:-----|----------:|-----------------:|-----------------:|----------------:|---------------------:|-----------------------:|-------------------------:|
| 32   | 11.033 ns |       246.203 ns |   **148.563 ns** |    **4.642 ns** |                1.00x |                  1.00x |                   +39.6% |
| 128  | 94.160 ns |     1,480.110 ns | **1,222.960 ns** |    **9.554 ns** |                8.23x |                  6.01x |                   +17.3% |
| 256  | 378.22 ns |     3,051.310 ns | **2,586.260 ns** |   **10.102 ns** |               17.40x |                 12.39x |                   +15.2% |

### Growth by Subscriber Count for a Single Event (1M Notify calls)

| Subscribers | C# event Cost/Notify | MessagePipe Cost/Notify | LayerBase Cost/Notify | C# event Scale vs 1 | MessagePipe Scale vs 1 | LayerBase Scale vs 1 | LayerBase vs MessagePipe |
|:------|--------------:|-----------------:|---------------:|-------------------:|----------------------:|--------------------:|-------------------------:|
| 1     |     0.3607 ns |        1.8591 ns |  **1.6582 ns** |              1.00x |                 1.00x |               1.00x |                   +10.8% |
| 4     |    11.5578 ns |        2.9315 ns |  **2.6933 ns** |             32.04x |                 1.57x |               1.62x |                    +8.1% |
| 8     |    20.5293 ns |        5.0653 ns |  **3.4854 ns** |             56.91x |                 2.72x |               2.10x |                   +31.2% |
| 16    |    35.9193 ns |        9.6484 ns |  **6.1484 ns** |            103.34x |                 5.38x |               3.81x |                   +36.3% |

### Request/Response Performance Comparison (100k Calls)

| Method                          | Total Cost (100k) |    Avg/Call | Scale vs Direct | Memory |
|:----------------------------|--------------:|------------:|---------:|-----:|
| Direct LBTask Struct Call   |  **29.21 μs** | **0.29 ns** |    1.00x |  0 B |
| MessagePipe IRequestHandler |  **50.48 μs** | **0.50 ns** |    1.73x |  0 B |
| **LayerBase CallAsync**     | **108.15 μs** | **1.08 ns** |    3.70x |  0 B |

---

*Note: Benchmark results vary based on environment, .NET version, CPU, and JIT configuration. Please refer to the BenchmarkDotNet output in the repository for your specific hardware.*
