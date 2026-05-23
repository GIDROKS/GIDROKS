# Webconto project write-ups

This folder holds deeper notes for the Webconto projects summarized in the root
[README.md](../../README.md). The root README keeps the recruiter-facing version
short; these pages keep the architecture, metrics, and screenshots.

<sub>Published with permission from the company owner. Metrics and screenshots are sanitized for portfolio use; source repositories remain private.</sub>

## Projects

| Project | Status | Short description |
|---------|--------|-------------------|
| [DatabaseAnalyzer](database-analyzer.md) | Ready | SQL Server/PostgreSQL observability and diagnostic platform for 1C environments |
| [TenderFinder](tender-finder.md) | Ready | Belarus tender aggregator with Streamlit UI, FastAPI, SQLite cache, and Telegram delivery |
| [WebConto KB](webconto-kb.md) | Ready | Internal knowledge-base portal for 1C consulting, built as the source of truth for an AI consultant |
| [Webconto AI consultant](webconto-ai-consultant.md) | Coming soon | LLM-facing assistant layer connected to WebConto KB |

## How this layer is organized

- Each project page describes the problem, shipped scope, stack, metrics, and screenshots.
- Screenshots live under `docs/images/webconto/{project-slug}/`.
- The private repositories are not linked from this public portfolio.
- The AI consultant has a stable slot now, so the fourth project can be added without reshuffling the README.
