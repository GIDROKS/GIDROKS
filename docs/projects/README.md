# Project write-ups

This folder holds deeper notes for the Webconto projects summarized in the root
[README.md](../../README.md). The root README keeps the recruiter-facing version
short; these pages keep the architecture, metrics, and screenshots.

<sub>Published with permission from the company owner. Screenshots are published with permission; sensitive repository and customer data are not disclosed. Source repositories remain private.</sub>

## Projects

| Project | Status | Short description |
|---------|--------|-------------------|
| [Database Analyzer](database-analyzer.md) | Ready | SQL Server/PostgreSQL observability and diagnostic platform for 1C environments |
| [Tender Finder](tender-finder.md) | Ready | Belarus tender aggregator with Streamlit UI, FastAPI, SQLite cache, and Telegram delivery |
| [Knowledge Base Portal](webconto-kb.md) | Ready | Internal knowledge-base portal for 1C consulting, built as the source of truth for an AI consultant |
| [AI Consultant](webconto-ai-consultant.md) | Ready | LLM-facing assistant: Telegram/web chat, Dify RAG, KB sync, usage telemetry, call analytics |

## How this layer is organized

- Each project page describes the problem, shipped scope, stack, metrics, and screenshots.
- Screenshots live under `docs/images/webconto/{project-slug}/`.
- The private repositories are not linked from this public portfolio.
- Screenshot paths follow `docs/images/webconto/{project-slug}/` (AI Consultant images can be added when ready).
