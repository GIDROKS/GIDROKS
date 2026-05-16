# codec-bench

Self-contained BenchmarkDotNet project behind the LZ4 vs Brotli table in the repo root README.

## Run

From the repo root:

```bash
dotnet run -c Release --project docs/perf/codec-bench -- --filter '*Codec*'
```

Or, locally inside this folder:

```bash
dotnet run -c Release -- --filter '*Codec*'
```

BDN handles warmup, repetition, outlier removal and produces a markdown summary you can paste back into the README.

## What it measures

- `Compress_LZ4` vs `Compress_Brotli_Q4` — wall time and bytes-out.
- `Decompress_LZ4` vs `Decompress_Brotli` — wall time on the pre-compressed payload prepared in `[GlobalSetup]`.
- Four buckets: **50 / 500 / 5 000 / 50 000 B**, parameterised by `PayloadBytes`.
- `[MemoryDiagnoser]` reports allocations; `[ThreadingDiagnoser]` flags lock contention.

## What the corpus actually is

A deterministic, MessagePack-encoded "snapshot" stream resembling gameplay traffic — entity ids, recurring kind strings, float clusters. Generated with a fixed seed so results are reproducible across machines.

**Random bytes are not used on purpose:** random input is incompressible and would make both codecs look identical, which would invalidate the comparison.

## Expected output shape

```
| Method              | PayloadBytes | Mean      | Error    | StdDev   | Allocated |
|-------------------- |------------- |----------:|---------:|---------:|----------:|
| Compress_LZ4        | 50           |   0.42 us | 0.01 us  | 0.01 us  |      0 B  |
| Compress_Brotli_Q4  | 50           |   0.38 us | 0.01 us  | 0.01 us  |    312 B  |
| Decompress_LZ4      | 50           |   0.31 us | 0.01 us  | 0.01 us  |      0 B  |
| Decompress_Brotli   | 50           |   0.46 us | 0.02 us  | 0.02 us  |    248 B  |
| ...                                                                                |
```

Absolute numbers will vary by CPU, OS power policy, and .NET runtime version. The ratios should track.

## Environment used for the README table

- Ryzen 7 5800X · 32 GB DDR4-3200 · Windows 11 24H2
- .NET 9 · Server GC · `DOTNET_TieredCompilation=1` · `DOTNET_ReadyToRun=1`
- `K4os.Compression.LZ4` 1.3.x (Frame format, fast mode)
- `System.IO.Compression.BrotliStream` at quality 4 (`CompressionLevel.Fastest`)
