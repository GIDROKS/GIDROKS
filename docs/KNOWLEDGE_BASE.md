# Knowledge base (GIDROKS)

## Purpose

Facts about this repository’s **documentation and benchmarks**, aimed at contributors or future-you. Product/engineering narrative for hiring lives in the root [README.md](../README.md).

## Performance docs

| Resource | Contents |
|----------|----------|
| [perf/README.md](perf/README.md) | Full performance case studies (LZ4/Brotli, SignalR GC, routing locks, Unity transition hitch), methodology, toolbox |
| [perf/codec-bench/README.md](perf/codec-bench/README.md) | What the BDN project measures, run syntax, corpus assumptions |

**Inputs:** MessagePack-style snapshot payloads (fixed seed) in the benchmark project; not random bytes.

**Outputs:** Console tables and BenchmarkDotNet markdown summaries; optional artifacts under `BenchmarkDotNet.Artifacts/` (gitignored).

**Config / env:** Documented in [perf/README.md](perf/README.md) (bench host, .NET version, key package versions). Absolute µs numbers vary by CPU and power policy; ratios should track.

**Where to look for errors:** Terminal output from `dotnet run`; BDN logs under `BenchmarkDotNet.Artifacts/` if enabled.

## Images

[images/](images/) holds PNGs referenced from [perf/README.md](perf/README.md) (historical ad-hoc benchmark screenshots).
