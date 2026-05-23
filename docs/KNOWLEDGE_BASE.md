# Knowledge base (GIDROKS)

## Purpose

Facts about this repository’s **documentation, project write-ups, and benchmarks**, aimed at contributors or future-you. Product/engineering narrative for hiring lives in the root [README.md](../README.md).

## Project docs

| Resource | Contents |
|----------|----------|
| [projects/README.md](projects/README.md) | Index for project write-ups and the reserved fourth-project slot |
| [projects/database-analyzer.md](projects/database-analyzer.md) | SQL Server/PostgreSQL + 1C observability and diagnostic platform |
| [projects/tender-finder.md](projects/tender-finder.md) | Belarus tender aggregator with Streamlit UI, FastAPI, SQLite cache, and Telegram delivery |
| [projects/webconto-kb.md](projects/webconto-kb.md) | Knowledge-base portal used as the source of truth for an AI consultant |
| [projects/webconto-ai-consultant.md](projects/webconto-ai-consultant.md) | Placeholder for the fourth project |

**Inputs:** Portfolio briefs and screenshots supplied by the project owner. Screenshots are published with permission; sensitive repository and customer data are not disclosed. Private repository URLs are intentionally not published in this public repo.

**Outputs:** Short project cards in the root [README.md](../README.md), full write-ups under [projects/](projects/), and screenshots under [images/webconto/](images/webconto/).

**Config / env:** The project docs are static Markdown. No runtime configuration is required.

**Where to look for errors:** Broken relative links, missing screenshots, or stale metric counts in the Markdown files above.

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

[images/](images/) holds static images referenced from docs.

| Path | Contents |
|------|----------|
| [images/](images/) | Historical codec benchmark PNGs referenced from [perf/README.md](perf/README.md) |
| [images/webconto/database-analyzer/](images/webconto/database-analyzer/) | Analyzer UI, Grafana, and Telegram screenshots |
| [images/webconto/tender-finder/](images/webconto/tender-finder/) | Streamlit tender-monitoring screenshots |
| [images/webconto/webconto-kb/](images/webconto/webconto-kb/) | Knowledge-base portal screenshots |
| [images/webconto/ai-consultant/](images/webconto/ai-consultant/) | Reserved for the fourth Webconto project |
