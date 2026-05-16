using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CodecBench;

// Release-only, server GC, single short job — keep runs reproducible and quick.
var config = ManualConfig
    .CreateMinimumViable()
    .AddJob(Job.Default
        .WithRuntime(CoreRuntime.Core90)
        .WithGcServer(true)
        .WithGcConcurrent(true)
        .WithWarmupCount(5)
        .WithIterationCount(15));

BenchmarkSwitcher
    .FromAssembly(typeof(CodecBenchmarks).Assembly)
    .Run(args, config);
