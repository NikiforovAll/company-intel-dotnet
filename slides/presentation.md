---
marp: true
title: AI Building Blocks in .NET
author: Oleksii Nikiforov
size: 16:9
theme: copilot
pagination: true
footer: ""
---

<!-- _class: lead -->

![bg fit](./img/bg-title.png)

# **AI Building Blocks in .NET**
## RAG Document Intelligence with Microsoft.Extensions.AI & Aspire

---

<!-- _class: hero -->

![bg left:35% brightness:1.00](./img/oleksii.png)

## Oleksii Nikiforov

- Lead Software Engineer at EPAM Systems
- AI Engineering Coach
- +10 years in software development
- Open Source and Blogging

<br/>

> <i class="fa-brands fa-github"></i> [nikiforovall](https://github.com/nikiforovall)
<i class="fa-brands fa-linkedin"></i> [Oleksii Nikiforov](https://www.linkedin.com/in/nikiforov-oleksii/)
<i class="fa fa-window-maximize"></i> [nikiforovall.blog](https://nikiforovall.blog/)

---

![bg fit](./img/bg-slide-alt2.png)

# Problem Statement

- AI integrations are **tightly coupled** to specific providers
- Switching OpenAI → Ollama → Azure = **rewriting code**
- RAG apps need: embeddings + vector store + LLM + evaluation
- How do you build **provider-agnostic** AI apps in .NET?

---

![bg fit](./img/bg-section.png)

# .NET AI Building Blocks

## The core abstractions for provider-agnostic AI

---

![bg fit](./img/bg-slide-alt3.png)

# The Four Building Blocks

| Block | Package | Purpose |
|-------|---------|---------|
| **MEAI** | Microsoft.Extensions.AI | `IChatClient`, `IEmbeddingGenerator` |
| **VectorData** | Microsoft.Extensions.VectorData | `VectorStoreCollection`, search |
| **Agent Framework** | Microsoft.Agents.AI | `IAIAgent`, AG-UI protocol |
| **MCP** | Model Context Protocol | Tool & resource discovery |

<br/>

<div class="key">

**Key:** Like `ILogger` or `HttpClient` — abstractions that let you swap implementations without changing app code

</div>

---

![bg fit](./img/bg-slide-alt1.png)

# IChatClient — Universal Chat Abstraction

```ts
// Register with any provider — one line change to swap
builder.AddOllamaApiClient("ollama-llama3-1").AddChatClient();

// Consume via DI — completely provider-agnostic
IChatClient chatClient = sp.GetRequiredService<IChatClient>();
var response = await chatClient.GetResponseAsync(messages);
```

<br/>

<div class="tip">

**Same interface** works with OpenAI, Azure OpenAI, Ollama, Anthropic — swap the registration, keep the code

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# IEmbeddingGenerator — Vector Embeddings

```ts
// Register embedding provider
builder.AddOllamaApiClient("ollama-all-minilm").AddEmbeddingGenerator();

// Provider-agnostic embedding generation
IEmbeddingGenerator<string, Embedding<float>> embedder = ...;
var embeddings = await embedder.GenerateAsync(["some text"]);
// → Embedding<float>[384]
```

<br/>

<div class="key">

**Key:** Same abstraction for OpenAI `text-embedding-3-small`, Ollama `all-minilm`, Azure, etc.

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# DelegatingChatClient — Middleware Pattern

```csharp
sealed class LoggingChatClient(IChatClient inner, ILogger logger)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Chat request: {Count} messages",
            messages.Count());
        var response = await base.GetResponseAsync(messages, options, ct);
        logger.LogInformation("Chat response: {Tokens} tokens",
            response.Usage?.TotalTokenCount);
        return response;
    }
}
```

---

![bg fit](./img/bg-slide-alt1.png)

# DelegatingChatClient — Composable Pipeline

```ts
// Built-in middleware: logging, telemetry, caching

builder.Services.AddChatClient(chatClient)
    .UseOpenTelemetry()
    .UseLogging()
    .UseFunctionInvocation();
```

<br/>

<div class="tip">

**Like ASP.NET middleware** but for AI calls — logging, caching, retry, telemetry as composable behaviors

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# VectorStore — Attribute-Driven Data Model

```csharp
public sealed class DocumentRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreData]
    public string Text { get; set; } = "";

    [VectorStoreData(IsIndexed = true)]
    public string Source { get; set; } = "";

    [VectorStoreVector(Dimensions: 384)]
    public string Embedding => Text;  // auto-embedded!
}
```

<div class="key">

**Key:** `[VectorStoreVector]` pointing to a `string` property = automatic embedding generation at upsert time

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# VectorStore — Provider Registration

```ts
builder.Services.AddSingleton<
    VectorStoreCollection<Guid, DocumentRecord>>(sp =>
{
    var embedder = sp.GetRequiredService<
        IEmbeddingGenerator<string, Embedding<float>>>();
    var vectorStore = new QdrantVectorStore(
        sp.GetRequiredService<QdrantClient>(),
        new QdrantVectorStoreOptions {
            EmbeddingGenerator = embedder });
    return vectorStore
        .GetCollection<Guid, DocumentRecord>("documents");
});
```

<br/>

<div class="tip">

**Swap** `QdrantVectorStore` for `InMemoryVectorStore`, `RedisVectorStore`, `AzureAISearchVectorStore` — same interface

</div>

---

![bg fit](./img/bg-section.png)

# Building a RAG Application

## PDF → Chunks → Vectors → Grounded Answers

---

![bg fit](./img/bg-slide-alt2.png)

# Solution Architecture

```
Phase 1: INGEST                       Phase 2: QUERY
───────────────────────────           ────────────────────────
Upload PDF document                   "What does the report say about X?"
  → PdfPig extracts text                → Vector search (Qdrant)
  → Recursive chunking (128 tokens)     → Retrieve top-5 chunks
  → Embed (all-minilm, 384-dim)         → LLM generates grounded answer
  → Upsert to Qdrant                    → Citations from stored knowledge
```

<br/>

<div class="key">

**All AI operations go through building block abstractions** — `IChatClient`, `IEmbeddingGenerator`, `VectorStoreCollection`

</div>

---

![bg fit](./img/bg-slide-alt2.png)

# PDF Extraction → Chunking

```ts
// PdfPig — pure .NET, no native deps
var extraction = PdfTextExtractor.Extract(pdfStream);
// → Text with "## Page N" markers, page count

// Recursive chunking with overlap
var chunks = TextChunker.Chunk(extraction.Text);
// Target: 128 tokens | Max: 200 | Overlap: 25
// Separators: paragraphs → lines → sentences → words
```

<br/>

<div class="tip">

**Small chunks** (128 tokens) improve retrieval precision — overlap ensures context continuity

</div>

---

![bg fit](./img/bg-slide-alt1.png)

# Ingestion Pipeline

```ts
var records = chunks.Select((chunk, i) =>
    new DocumentRecord
    {
        Text = chunk,
        FileName = fileName,
        ChunkIndex = i,
        Source = source,
    });

await collection.UpsertAsync(records, ct);
// Embedding generated automatically via VectorStoreVector!
```

<br/>

<div class="key">

**Zero embedding code** — `VectorStoreCollection` generates embeddings at upsert using `IEmbeddingGenerator`

</div>

---

# Document Ingestion

![center](./img/ingestion.png)

---

![bg fit](./img/bg-slide-alt2.png)

# Vector Search Adapter

```ts
public static Func<string, CancellationToken,
    Task<IEnumerable<TextSearchResult>>>
    Create(VectorStoreCollection<Guid, DocumentRecord> collection,
           int top = 5) =>
    async (query, ct) =>
    {
        var results = new List<TextSearchResult>();
        await foreach (var result in
            collection.SearchAsync(query, top: top, ct))
        {
            results.Add(new TextSearchResult
            {
                Text = result.Record.Text,
                SourceName = FormatSourceName(...),
            });
        }
        return results;
    };
```

---

![bg fit](./img/bg-slide-alt3.png)

# Agentic RAG — TextSearchProvider

```ts
var searchFunc = VectorSearchAdapter.Create(collection, top: 5);

var chatAgent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        Name = "agentic_chat",
        ChatOptions = new ChatOptions {
            Instructions = SystemInstructions },
        AIContextProviders = [
            new TextSearchProvider(searchFunc, ragOptions)
        ],
    });

app.MapAGUI("/", chatAgent);
```

<br/>

<div class="key">

**TextSearchProvider** auto-injects relevant context before each LLM call — retrieval is transparent to the agent

</div>

---

# Chat — Q&A with Citations

![center](./img/chat.png)

---

![bg fit](./img/bg-section.png)

# .NET Aspire **Orchestration**

## One command to start everything

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# AppHost — Full Stack Orchestration

```csharp
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var llama = ollama.AddModel("llama3.1");
var embedModel = ollama.AddModel("all-minilm");

var qdrant = builder.AddQdrant("qdrant")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.CompanyIntel_Api>("api")
    .WithReference(llama).WithReference(embedModel)
    .WithReference(qdrant).WithReference(sqlite);

builder.AddJavaScriptApp("ui", "../CompanyIntel.UI", "dev")
    .WithEnvironment("AGENT_URL", api.GetEndpoint("http"));
```

<div class="tip">

`dotnet aspire run` — starts Ollama + model pulls + Qdrant + SQLite + API + Next.js UI

</div>

---

![bg fit](./img/bg-section.png)

# RAG Evaluation

## Measuring quality with LLM-as-Judge

---

<style scoped>
section {
  font-size: 28px;
}
</style>
![bg fit](./img/bg-slide-alt3.png)

# Why Evaluate RAG?

- RAG quality is **invisible** without measurement
- Retrieval failures → hallucinated or irrelevant answers
- Need to catch **regressions** before users do
- Four dimensions of quality:

| Dimension | Question |
|-----------|----------|
| **Relevance** | Does the answer address the question? |
| **Coherence** | Is it well-structured and logical? |
| **Groundedness** | Is every claim backed by context? |
| **Retrieval** | Did we find the right chunks? |

---

<style scoped>
section {
  font-size: 30px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# Microsoft.Extensions.AI.Evaluation

```ts
IEvaluator[] evaluators = [
    new RelevanceEvaluator(),
    new CoherenceEvaluator(),
    new GroundednessEvaluator(),
    new RetrievalEvaluator(),
];
```

<br/>

- **Scoring:** 1–5 scale via **LLM-as-Judge**
- Judge LLM reads (question, answer, context) and scores each dimension
- Returns `NumericMetric` with value, rating, and reasoning
- Part of `Microsoft.Extensions.AI.Evaluation.Quality` package

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# Relevance (1–5)

## Does the response address the user's question?

| Score | Meaning |
|-------|---------|
| **5** | Directly and completely answers the question |
| **3** | Partially relevant, missing key aspects |
| **1** | Completely off-topic or irrelevant |

<br/>

- Judge reads **(question, answer)** — no context needed
- Pure question-answer alignment check
- Catches: wrong topic, incomplete answers, tangential responses

---


<style scoped>
section {
  font-size: 28px;
}
</style>


![bg fit](./img/bg-slide-alt2.png)

# Coherence (1–5)

## Is the answer well-organized and readable?

| Score | Meaning |
|-------|---------|
| **5** | Clear structure, logical flow, easy to follow |
| **3** | Somewhat organized but with inconsistencies |
| **1** | Disjointed, contradictory, hard to follow |

<br/>

- Evaluates the **answer in isolation** — no context needed
- Catches: repetitive text, broken formatting, contradictions
- Important for user trust and readability

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# Groundedness (1–5)

## Is every claim supported by the retrieved context?

| Score | Meaning |
|-------|---------|
| **5** | All claims directly supported by provided context |
| **3** | Some claims lack support or are extrapolated |
| **1** | Answer fabricates information not in context |

<br/>

<div class="warning">

**Most critical metric for RAG** — catches hallucinations. Requires `GroundednessEvaluatorContext`

</div>

---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt2.png)

# Retrieval (1–5)

## Are the retrieved chunks useful for answering?

| Score | Meaning |
|-------|---------|
| **5** | All chunks highly relevant to the question |
| **3** | Mix of relevant and irrelevant chunks |
| **1** | Retrieved chunks are useless for this question |

<br/>

- Evaluates the **retrieval pipeline**, not the LLM answer
- Requires `RetrievalEvaluatorContext` with chunk texts
- Low score → fix chunking, embedding model, or search params

---

![bg fit](./img/bg-slide-alt3.png)

# Testing with Aspire

```csharp
public class AspireAppFixture : IAsyncLifetime
{
    public HttpClient ApiClient { get; private set; }
    public Uri OllamaEndpoint { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CompanyIntel_AppHost>();
        _app = await appHost.BuildAsync();

        await _app.StartAsync();
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api");
        
        ApiClient = _app.CreateHttpClient("api", "http");
    }
}
```

---

![bg fit](./img/bg-slide-alt2.png)

# Evaluation Test

```csharp
// 1. Call RAG endpoint
var result = await client.PostAsJsonAsync("/api/chat",
    new { message = question });

// 2. Configure LLM judge
IChatClient judge = new OllamaApiClient(ollamaEndpoint, "llama3.1");
var chatConfig = new ChatConfiguration(judge);

// 3. Provide grounding context
var context = new EvaluationContext[] {
    new GroundednessEvaluatorContext(groundingContext),
    new RetrievalEvaluatorContext(retrievedChunks),
};

// 4. Score each dimension
foreach (var evaluator in evaluators) {
    var evalResult = await evaluator.EvaluateAsync(
        messages, modelResponse, chatConfig, context, ct);
    var metric = evalResult.Get<NumericMetric>(metricName);
    Assert.True(metric.Value >= 3);
}
```

---

![bg fit](./img/bg-slide-alt1.png)

# End-to-End Evaluation Flow

```
dotnet test

  → Aspire starts Ollama + Qdrant + API (ephemeral)
  → Upload test PDF (contoso-corp.pdf)
  → For each eval scenario:
      → POST /api/chat with question
      → Get (answer, context chunks)
      → Judge LLM scores 4 metrics (1–5)
      → Assert all metrics ≥ 3

  → Aspire tears down everything
```


---

<style scoped>
section {
  font-size: 28px;
}
</style>

![bg fit](./img/bg-slide-alt.png)

# Key Takeaways

1. **MEAI abstractions** — `IChatClient`, `IEmbeddingGenerator`. Write once, swap providers

2. **VectorData attributes** — `[VectorStoreKey/Data/Vector]` + automatic embedding = minimal boilerplate

3. **DelegatingChatClient** — middleware for AI calls. Logging, retry, telemetry as pipeline behaviors

4. **Aspire for testing** — spin up full RAG stack per test run. Ephemeral containers = clean, reproducible eval

5. **LLM-as-Judge** — measure Relevance, Coherence, Groundedness, Retrieval before shipping

---

![bg fit](./img/bg-slide-alt2.png)

# Resources

- [.NET AI essentials — the core building blocks](https://devblogs.microsoft.com/dotnet/dotnet-ai-essentials-the-core-building-blocks-explained/)
- [Vector Data in .NET — building blocks part 2](https://devblogs.microsoft.com/dotnet/vector-data-in-dotnet-building-blocks-for-ai-part-2/)
- [GitHub: company-intel-dotnet](https://github.com/NikiforovAll/company-intel-dotnet)
- [Microsoft.Extensions.AI docs](https://learn.microsoft.com/dotnet/ai/)

---

![bg fit](./img/bg-title.png)

## **Thank You**
### Questions?
