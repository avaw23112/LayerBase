```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.5335/23H2/2023Update/SunValley3)
12th Gen Intel Core i7-12650H 2.30GHz, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Method                              | Mean         | Error      | StdDev     | Gen0   | Gen1   | Allocated |
|------------------------------------ |-------------:|-----------:|-----------:|-------:|-------:|----------:|
| HighDensity_10Layers_AllSubscribe   | 16,810.57 μs | 328.414 μs | 530.327 μs |      - |      - |  67.75 KB |
| LowDensity_10Layers_SingleSubscribe |  6,381.23 μs | 126.830 μs | 189.833 μs |      - |      - |   59.4 KB |
| Challenge_10k_Events_3Layers        |     91.41 μs |   1.802 μs |   2.806 μs | 1.7090 | 0.1221 |  21.28 KB |
