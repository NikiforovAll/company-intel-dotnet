# CompanyIntel.AppHost.Tests

Aspire integration tests for the CompanyIntel API. Tests boot the full distributed application (Ollama, Qdrant, SQLite, API) using `DistributedApplicationTestingBuilder`.

## Test Categories

### Health Tests (`ApiHealthTests.cs`)

Basic smoke test - verifies the API starts and `/health` returns 200.

### RAG Evaluation Tests (`RagEvaluationTests.cs`)

End-to-end evaluation of the RAG pipeline quality using [Microsoft.Extensions.AI.Evaluation](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries).

**Flow:**
1. Boots full Aspire stack via `AspireAppFixture`
2. Ingests a test PDF (`TestData/contoso-corp.pdf`) with known facts
3. Sends questions via `POST /api/chat`
4. Evaluates responses with 4 quality evaluators (Ollama llama3.1 as LLM judge):
   - **Relevance** - how relevant the response is to the query
   - **Coherence** - logical and orderly presentation
   - **Groundedness** - alignment with retrieved context
   - **Retrieval** - quality of retrieved chunks

**Assertions:** Relaxed score threshold (>= 3 out of 5) since local LLM judges score harsher than GPT-4o. Scores on a 1-5 scale: 1=Poor, 2=Below Average, 3=Average, 4=Good, 5=Exceptional.

**Example output:**
```
Question: Who is the CEO of Contoso Corp?
Answer: Jane Smith serves as the Chief Executive Officer (CEO) of Contoso Corp.
Context chunks: 4
  Relevance: 4 (Rating: Good, Failed: False)
  Coherence: 4 (Rating: Good, Failed: False)
  Groundedness: 4 (Rating: Good, Failed: False)
  Retrieval: 5 (Rating: Exceptional, Failed: False)
```

## Running

```bash
# All tests (with artifacts logging)
bash scripts/run-tests.sh

# Just eval tests
bash scripts/run-tests.sh tests/CompanyIntel.AppHost.Tests RagEvaluationTests

# Just health test
bash scripts/run-tests.sh tests/CompanyIntel.AppHost.Tests ApiHealthTests

# Direct (no artifacts)
dotnet test tests/CompanyIntel.AppHost.Tests --logger "console;verbosity=detailed"
```

## Prerequisites

- Docker (for Ollama and Qdrant containers)
- Ollama models: `llama3.1`, `all-minilm`

## Timing

| Test | Approx Duration |
|------|----------------|
| Health check | ~20s |
| RAG eval (per scenario) | ~2-4 min |
| Full suite | ~8-12 min |

Most time is spent on Aspire app startup and LLM inference.

## Key Files

| File | Purpose |
|------|---------|
| `AspireAppFixture.cs` | Shared xUnit fixture - boots Aspire stack, exposes API HttpClient + Ollama endpoint |
| `RagEvaluationTests.cs` | RAG quality evaluation using M.E.AI.Evaluation |
| `ApiHealthTests.cs` | Basic health endpoint smoke test |
| `TestData/contoso-corp.pdf` | 3-page test document about fictional Contoso Corp |
