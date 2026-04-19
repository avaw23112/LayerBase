```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5335/23H2/2023Update/SunValley3)
12th Gen Intel Core i7-12650H 2.30GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Type                    | Method                      | Mean         | Allocated |
|------------------------ |---------------------------- |-------------:|----------:|
| Classic_1ms_Bench       | &#39;经典 1ms 挑战 (3层全订阅) - 1万次&#39;   |     60.96 μs |         - |
| Extreme_Empty_64_Bench  | &#39;极限空负载 (64层/0订阅) - 100万次&#39;   |  1,561.16 μs |         - |
| MultiLayer_Full_Bench   | &#39;多层高压 (10层/全订阅) - 100万次&#39;    |  9,515.23 μs |         - |
| MultiLayer_Low_Bench    | &#39;多层低压 (10层/仅尾层) - 100万次&#39;    |  4,687.65 μs |         - |
| MultiLayer_Random_Bench | &#39;多层随机负载 (10层/5层订阅) - 100万次&#39; |  7,289.42 μs |         - |
| SingleLayer_High_Bench  | &#39;单层高压 (1层/10订阅) - 100万次&#39;    | 10,795.11 μs |         - |
| SingleLayer_Low_Bench   | &#39;单层低压 (1层/1订阅) - 100万次&#39;     |  5,181.26 μs |         - |
| Typical_Heavy_180_Bench | &#39;中重度负载 (180订阅) - 1万次&#39;       |  1,062.48 μs |         - |
