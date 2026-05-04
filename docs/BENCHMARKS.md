# LayerBase Benchmarks

Detailed performance benchmarks for LayerBase.

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
