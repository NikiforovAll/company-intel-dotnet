# Company Intelligence (.NET)

RAG-based document intelligence assistant. Upload PDFs, ask questions offline with grounded, cited answers. .NET reimplementation of [company_intel](https://github.com/NikiforovAll/company_intel).

[![Deploy presentation to Pages](https://github.com/NikiforovAll/company-intel-dotnet/actions/workflows/marp-pages.yml/badge.svg)](https://nikiforovall.blog/company-intel-dotnet/rag-dotnet-ai-building-blocks)

## How it works

```
Phase 1: INGEST (upload)              Phase 2: QUERY (chat)
───────────────────────────           ────────────────────────
Upload PDF document                   "What does the report say about X?"
  → PdfPig extracts text                → Vector search (Qdrant)
  → Recursive chunking (128 tokens)     → Retrieve relevant chunks
  → Embed (all-minilm, 384-dim)         → LLM generates grounded answer
  → Upsert to Qdrant                    → Citations from stored knowledge
```

## Example

|         Document ingestion         | Chat — Q&A with citations |
| :--------------------------------: | :-----------------------: |
| ![ingestion](assets/ingestion.png) | ![chat](assets/chat.png)  |

## Tech stack

| Layer           | Choice                               |
| --------------- | ------------------------------------ |
| LLM             | Llama 3.1 via Ollama (local-only)    |
| Embeddings      | all-minilm (384-dim)                 |
| Vector store    | Qdrant                               |
| Agent framework | Microsoft.Agents.AI + AG-UI protocol |
| AI abstractions | Microsoft.Extensions.AI              |
| Frontend        | CopilotKit + Next.js 15              |
| Orchestration   | .NET Aspire                          |
| PDF extraction  | PdfPig                               |
| Observability   | OpenTelemetry → Aspire dashboard     |

## Quick start

```bash
# Prerequisites: .NET 10 SDK, Node.js 20+

# Start everything
dotnet aspire run
```

This starts Ollama (+ model pulls), Qdrant, SQLite, API, and Next.js UI via .NET Aspire. Open the Aspire dashboard link from terminal output.

## Project structure

```
src/CompanyIntel.AppHost/          → .NET Aspire orchestrator
src/CompanyIntel.Api/              → ASP.NET Core backend (RAG agent, ingestion)
src/CompanyIntel.UI/               → Next.js 15 frontend (CopilotKit + AG-UI)
src/CompanyIntel.ServiceDefaults/  → Shared OpenTelemetry, health checks, resilience
tests/                             → Integration & RAG evaluation tests
slides/                            → Marp presentation (GitHub Pages)
```

## Presentation

[View slides](https://nikiforovall.blog/company-intel-dotnet/rag-dotnet-ai-building-blocks)
