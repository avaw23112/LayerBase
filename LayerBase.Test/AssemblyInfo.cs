// 由于 LayerHub 是基于静态单例构建的，且涉及后台 JobScheduler 线程，
// 并行执行测试类会导致资源竞争和异常泄露（Child tests had errors）。
// 强制 NUnit 顺序执行所有测试类以确保测试环境的原子性和稳定性。

[assembly: Parallelizable(ParallelScope.None)]
[assembly: LevelOfParallelism(1)]