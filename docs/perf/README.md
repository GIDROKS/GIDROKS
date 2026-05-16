# Performance engineering — profiling, hot paths, wire format

This document expands the **Performance engineering** section from the repository root [README.md](../../README.md). The root README keeps a single anchor case (LZ4 vs Brotli) plus reproduction commands; everything else lives here.

Performance is an engineering loop, not a heroic one-off: define a load profile, profile under it, fix the dominant cost, re-run the same fixture, accept or revert. Tooling — **JetBrains dotTrace** (CPU, wall time, lock contention), **JetBrains dotMemory** (allocations, dominant types, GC pressure), **BenchmarkDotNet** for micro-benchmarks, **Unity Profiler** + **Memory Profiler** on the client. Production claims are cross-checked against **OpenTelemetry** traces and **Grafana** percentile panels before being declared "fixed".

## How the numbers were produced

- **Fixtures, not vibes.** Every before/after runs against the same workload — fixed payload set, fixed RPS, fixed seed for synthetic load. Typical windows: 60–120 s steady traffic, or 50k–100k identical operations for handler-level work.
- **Micro-benchmarks via BenchmarkDotNet** with `[MemoryDiagnoser]` + `[ThreadingDiagnoser]`, Release config, 5 warmup × 15 measured iterations, reporting median and stddev — never single-run numbers.
- **Production correlation.** Profiler snapshots are validated against live p50/p95/p99, GC pause, and throughput panels for at least one rollout cycle before the fix is closed.

<details>
<summary><strong>Environment used for the numbers in this document</strong></summary>

- **Backend bench host:** Ryzen 7 5800X · 32 GB DDR4-3200 · Windows 11 24H2 · **.NET 9** · Server GC · `DOTNET_TieredCompilation=1` · `DOTNET_ReadyToRun=1`.
- **Unity client:** 2022.3 LTS · IL2CPP · ARM64 · reference devices Pixel 7 and iPhone 13.
- **Libraries:** `K4os.Compression.LZ4` 1.3.x (Frame format, fast mode) · `System.IO.Compression.BrotliStream` at **quality 4** (interactive default) · `MessagePack-CSharp` 2.5 · `MediatR` 12 · `MassTransit` 8 · `System.IO.Pipelines` 9.

</details>

## Case 1 — LZ4 vs Brotli for realtime gameplay packets

**Context.** Authoritative server pushing high-frequency gameplay state (vehicles, world deltas) serialized with MessagePack, typical compressed payloads from tens of bytes to tens of kilobytes. Brotli was the default on the wire; client-side decompression dominated frame budget on console-class worst-case ticks.

**Workload.** Snapshot corpus from a ~10 min capture (~14k packets), grouped into four size buckets. BenchmarkDotNet, 5 warmup × 15 measured iterations, MessagePack-encoded inputs — **not random bytes** (random input is incompressible and would invalidate the comparison).

**Result — median wall time, identical inputs for both codecs.**

| Payload | Compress · LZ4 / Brotli q=4 | Decompress · LZ4 / Brotli q=4 | Size · LZ4 / Brotli |
|--------:|----------------------------:|------------------------------:|--------------------:|
| 50 B    | 0.42 µs / 0.38 µs           | 0.31 µs / 0.46 µs             | 58 B / 47 B |
| 500 B   | 1.18 µs / 1.05 µs           | 0.62 µs / 1.23 µs             | 312 B / 248 B |
| 5 000 B | 5.40 µs / 6.80 µs           | 2.95 µs / 3.80 µs             | 2.9 KB / 2.3 KB |
| 50 000 B | 38 µs / 56 µs              | 14 µs / 62 µs                 | 29 KB / 24 KB |

**Read.** LZ4 trades ~15–20% more bytes on the wire for **2–4× faster decompression** at packet sizes that matter for realtime sync. On a 50 KB world delta, client-side decompression dropped from ~0.9 ms p99 to ~0.25 ms p99 — three frames of headroom on Switch's worst-case tick.

**Decision.** Migrated the realtime snapshot channel to LZ4; kept Brotli for cold-start asset manifests where bytes-on-wire still matter more than µs-on-client.

**What didn't work.** Trained a Zstd dictionary on the snapshot corpus — better ratio (~10–15%) but per-channel dictionary distribution complicated rollout, and the win didn't survive once delta-encoding was added upstream. Parked.

### Reproducing the LZ4 vs Brotli numbers

A self-contained BenchmarkDotNet project lives in [`codec-bench/`](./codec-bench/). From the repo root:

```bash
dotnet run -c Release --project docs/perf/codec-bench -- --filter '*Codec*'
```

Results are workload-dependent — the corpus shipped with the project is a small set of MessagePack-encoded snapshots, not random bytes; the ratios will track but the absolute numbers will vary by CPU, OS power policy, and runtime version. See [codec-bench/README.md](./codec-bench/README.md) for project-specific notes.

<details>
<summary><strong>Earlier benchmark artifacts (ad-hoc Stopwatch run, pre-BDN)</strong></summary>

These are the original screenshots from the first ad-hoc comparison that prompted the proper BDN benchmark above. Kept for historical reference — figures match within rounding.

![Compression benchmark — terminal output](../images/compression-benchmark-terminal.png)
![LZ4 vs Brotli — summary table](../images/compression-benchmark-summary-table.png)

</details>

## Case 2 — Allocations & GC in the SignalR send pipeline

**Context.** Realtime hub broadcasting small, frequent deltas (leaderboards, rewards, session state) to many concurrent connections. Under a 10k-client synthetic flood, dotMemory showed ~1.35 GB allocated and ~650 Gen0 collections over 60 s — visible stalls in the broadcast path every few seconds.

**Findings.**

- Per-send `Concat()` of two `byte[]` segments produced N temporary arrays per second.
- `BrotliStream` was constructed per call, never pooled.
- `IServiceScope` was being resolved per message in a hot path that didn't need a fresh scope.

**Fix.** Replaced `Concat` with `ArrayPool<byte>` + `Memory<byte>` slicing; pooled compressor instances behind `ObjectPool<>`; moved scope resolution to handler-level singletons where lifetimes allowed.

| Metric (60 s, 10k clients, identical fixture) | Before | After | Δ |
|---|---:|---:|---:|
| Total allocated | 1.35 GB | 0.44 GB | **−67%** |
| Gen0 collections | 648 | 117 | **−82%** |
| `byte[]` instances (top type) | 11.2 M | 1.97 M | **−82%** |
| Mean send handler wall time | 2.15 ms | 1.05 ms | **−51%** |

## Case 3 — Lock contention in the matchmaking router

**Context.** High-traffic realtime routing tier under synthetic load (many concurrent clients, sustained messages). A server-side router held `lock(this)` around a shared player→shard map. dotTrace timeline showed **68 ms/s** cumulative lock-wait across the thread pool — one hot lock serializing the whole dispatch path.

**Fix.** Replaced the mutable dictionary + lock with `ConcurrentDictionary<Guid, ShardRoute>` and split producer/consumer paths via `System.Threading.Channels` (single-writer per shard, multi-reader on the dispatcher).

| Metric (same 60 s fixture, 10k clients) | Before | After | Δ |
|---|---:|---:|---:|
| Lock wait (dotTrace, cumulative) | 68 ms/s | 0.4 ms/s | **−99%** |
| Message handling p95 | 9.5 ms | 2.1 ms | **−78%** |
| Throughput | 7.8k msg/s | 18.5k msg/s | **+137%** |

## Case 4 — Unity client: GC spike on minigame transition

**Context.** Transition between two heavy UI / gameplay modes caused a ~110 ms hitch on mid-tier Android. Unity Memory Profiler showed a 4–6 MB managed allocation burst per transition: per-element `Instantiate()` loops, `Resources.Load`, string concatenation inside localized label updates.

**Fix.** Migrated to **Addressables** with pre-warmed handles; replaced instantiation loops with `UnityEngine.Pool` object pools; switched hot label updates to cached `StringBuilder` + `TMP_TextInfo`; replaced coroutine wrappers around async APIs with **UniTask**, removing the `Task → IEnumerator` bridge allocations.

| Metric (Match-3 → Survivor transition, Pixel 7, IL2CPP, Release) | Before | After |
|---|---:|---:|
| Frame spike at transition | 108 ms | 22 ms |
| Managed allocations / transition | 5.8 MB | 0.4 MB |
| GC.Collect during transition | 1 | 0 |

## Toolbox I reach for first

**Backend** — `Span<T>` / `Memory<T>`, `ArrayPool<byte>`, `ObjectPool<T>`, `System.IO.Pipelines`, `RecyclableMemoryStream`, source generators over reflection, Server GC tuning (`HeapHardLimit`, POH, large-object compaction).

**Unity** — Burst, Jobs, IL2CPP-friendly generics, struct enumerators, allocation-free LINQ alternatives, Addressables pre-warm, SRP Batcher discipline, atlasing.

**Wire format** — MessagePack over JSON, batching / coalescing on tick boundary, delta-encoding for repeated state, MTU- and Nagle-aware framing on mobile.

**Observability** — OpenTelemetry traces tagged with the same fixture id used in the profiler run, Grafana panels for p50/p95/p99 and GC pause, alerts on tail-latency regressions per release.

---

<sub>**Note on confidentiality.** Figures here illustrate methodology and order of magnitude from real engagements; raw traces and customer-specific datasets stay with the respective teams. Happy to walk through a sanitized or recorded dotTrace / dotMemory snapshot on a call.</sub>
