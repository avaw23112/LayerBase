```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5335/23H2/2023Update/SunValley3)
12th Gen Intel Core i7-12650H 2.30GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Method           | Mean     | Ratio | Allocated | Alloc Ratio |
|----------------- |---------:|------:|----------:|------------:|
| &#39;标准模式 (100万次)&#39;   | 8.119 ms |  1.09 |   9.67 KB |        1.00 |
| &#39;透明桥接模式 (100万次)&#39; | 7.821 ms |  1.05 |   9.35 KB |        0.97 |
