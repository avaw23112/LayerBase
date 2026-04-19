```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5335/23H2/2023Update/SunValley3)
12th Gen Intel Core i7-12650H 2.30GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Type                    | Method                      | Mean        | Error      | StdDev     | Allocated |
|------------------------ |---------------------------- |------------:|-----------:|-----------:|----------:|
| Classic_1ms_Bench       | &#39;经典 1ms 挑战 (3层全订阅) - 1万次&#39;   |    61.32 μs |   1.223 μs |   2.415 μs |         - |
| Extreme_Empty_64_Bench  | &#39;极限空负载 (64层/0订阅) - 100万次&#39;   | 1,516.91 μs |  30.175 μs |  64.955 μs |         - |
| MultiLayer_Full_Bench   | &#39;多层高压 (10层/全订阅) - 100万次&#39;    | 9,170.90 μs | 180.520 μs | 306.536 μs |         - |
| MultiLayer_Low_Bench    | &#39;多层低压 (10层/仅尾层) - 100万次&#39;    | 4,598.08 μs |  91.886 μs | 197.794 μs |         - |
| MultiLayer_Random_Bench | &#39;多层随机负载 (10层/5层订阅) - 100万次&#39; | 6,222.94 μs | 123.069 μs | 272.714 μs |         - |
| SingleLayer_High_Bench  | &#39;单层高压 (1层/10订阅) - 100万次&#39;    | 9,053.39 μs | 174.913 μs | 460.791 μs |         - |
| SingleLayer_Low_Bench   | &#39;单层低压 (1层/1订阅) - 100万次&#39;     | 4,453.57 μs |  87.153 μs | 169.985 μs |         - |
| Typical_Heavy_180_Bench | &#39;中重度负载 (180订阅) - 1万次&#39;       |   894.46 μs |  17.772 μs |  44.260 μs |         - |
