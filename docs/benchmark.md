# LayerBase Benchmark Guide

本指南描述如何复现 LayerBase 的基准测试，以及如何解读测试结果。

## 运行环境要求

- **.NET SDK**: 推荐使用 .NET 8.0 或更高版本。
- **构建模式**: 必须使用 `Release` 模式运行，以确保 JIT 优化开启。
- **基准测试工具**: 本项目使用 [BenchmarkDotNet](https://benchmarkdotnet.org/)。

## 如何运行

进入 Benchmark 项目目录：

```bash
cd LayerBase.BenchMark
dotnet run -c Release -- --filter *
```

## 测试场景说明

- **基础事件派发**: 测量单线程/多线程下简单结构体的事件派发性能。
- **层间路由**: 测量跨 Layer 的 Call 调用与 Event 传播延迟。
- **资源分配**: 验证核心路径中的零分配（Zero-Allocation）特性。

## 理解结果

LayerBase 的核心设计目标并非简单的吞吐量峰值，而是：
- **低分配 (Low Allocation)**: 在高频逻辑中最小化 GC 负担。
- **低抖动 (Low Jitter)**: 确保执行路径的可预测性。
- **可预测执行**: 避免不可控的上下文切换或内存分配。

如果你的场景对延迟极度敏感，请关注测试结果中的 `Gen 0/1/2` 分配数据。

## 历史记录

原始基准测试报告存放在 `docs/benchmarks/results/` 目录下。
