# Repository guide

This repository is primarily a **GitHub profile / portfolio** surface: the root [README.md](README.md) is the public bio, project list, and a shortened performance narrative.

## Layout

| Path | Role |
|------|------|
| [README.md](README.md) | Profile headline, contacts, tech stack, shipped titles, **Case 1** (LZ4 vs Brotli) summary + link to full perf notes |
| [docs/perf/README.md](docs/perf/README.md) | Full performance write-up: methodology, Cases 1–4, toolbox, confidentiality note, screenshot appendix |
| [docs/perf/codec-bench/](docs/perf/codec-bench/) | BenchmarkDotNet project used to reproduce the codec table |
| [docs/images/](docs/images/) | Static images referenced from the perf docs |

## Commands

From the repo root, run the codec benchmarks:

```bash
dotnet run -c Release --project docs/perf/codec-bench -- --filter '*Codec*'
```

See [docs/perf/codec-bench/README.md](docs/perf/codec-bench/README.md) for parameters, corpus description, and expected output shape.

## Documentation index

Concise operational notes: [docs/KNOWLEDGE_BASE.md](docs/KNOWLEDGE_BASE.md).
