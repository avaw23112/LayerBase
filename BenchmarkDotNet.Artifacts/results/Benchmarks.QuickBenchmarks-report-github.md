```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5335/23H2/2023Update/SunValley3)
12th Gen Intel Core i7-12650H 2.30GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Method                                   | Mean     | Error     | StdDev    | Allocated |
|----------------------------------------- |---------:|----------:|----------:|----------:|
| &#39;⚡ 典型中重度负载 (5层: 1层100订阅, 4层各20订阅) - 1万次&#39; | 5.354 ms | 0.0994 ms | 0.1714 ms | 117.03 KB |
