using CompanyIntel.Api;
using CompanyIntel.Api.Data;
using CompanyIntel.Api.Ingestion;
using CompanyIntel.Api.Models;
using CompanyIntel.Api.Rag;
using CompanyIntel.Api.Suggestions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddQdrantClient("qdrant");

builder.AddOllamaApiClient("ollama-all-minilm").AddEmbeddingGenerator();
builder.AddOllamaApiClient("ollama-llama3-1").AddChatClient();

const string CollectionName = "documents";

builder.Services.AddSingleton<VectorStoreCollection<Guid, DocumentRecord>>(sp =>
{
    var embeddingGenerator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    var vectorStore = new QdrantVectorStore(
        sp.GetRequiredService<Qdrant.Client.QdrantClient>(),
        ownsClient: false,
        new QdrantVectorStoreOptions { EmbeddingGenerator = embeddingGenerator }
    );
    return vectorStore.GetCollection<Guid, DocumentRecord>(CollectionName);
});

builder.AddSqliteDbContext<IngestionDbContext>("ingestion-db");

builder.Services.AddTransient<IngestionService>();
builder.Services.AddSingleton<SuggestionGenerator>();
builder.Services.AddHostedService<SuggestionGeneratorService>();

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IngestionDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS ChatSuggestions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Text TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        )
        """
    );
}

app.MapDefaultEndpoints();

// RAG agent
using var chatClient = new EnsureMessageIdChatClient(
    app.Services.GetRequiredService<IChatClient>()
);
var collection = app.Services.GetRequiredService<VectorStoreCollection<Guid, DocumentRecord>>();

var ragOptions = new TextSearchProviderOptions
{
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
    RecentMessageMemoryLimit = 4,
    RecentMessageRolesIncluded = [ChatRole.User],
    ContextPrompt = "## Additional Context",
    CitationsPrompt = """
        Cite sources using their SourceName with bold text: **SourceDocName**.
        Do NOT use markdown links for citations (no [text](url) format).
        If multiple sources support a claim, cite all of them.
        At the end of your response, include a "Sources" section listing each unique source ONCE (no duplicates).
        Every source MUST include the page number in the format: **FileName (Page N)**.
        """,
    // Other options:
    // ContextFormatter = results => ...,          — full control over chunk rendering (overrides ContextPrompt/CitationsPrompt)
    // SearchInputMessageFilter = msg => ...,      — filter which messages build the search query
    // StorageInputMessageFilter = msg => ...,     — filter which messages are stored in recent memory
    // StateKey = "custom-key",                    — override when using multiple TextSearchProvider instances
};

const string SystemInstructions = """
    You are a document intelligence assistant that answers questions ONLY from retrieved documents.

    ## Rules
    - ONLY use information from the "Additional Context" section provided in the conversation.
    - If no Additional Context is present, or it does not contain relevant information, reply EXACTLY:
      "I don't have enough information in the documents to answer that."
    - NEVER use your own knowledge, training data, or make assumptions beyond the provided context.
    - Do NOT fabricate sources or references.

    ## Response format
    - Use markdown formatting.
    - Be concise and well-structured.
    - Group related information under headings when appropriate.
    """;

var searchFunc = VectorSearchAdapter.Create(collection, top: 5);

var chatAgent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        Name = "agentic_chat",
        ChatOptions = new ChatOptions { Instructions = SystemInstructions },
        AIContextProviders = [new TextSearchProvider(searchFunc, ragOptions)],
    }
);

app.MapAGUI("/", chatAgent);

// REST chat endpoint (for programmatic access and evaluation testing)
app.MapPost(
    "/api/chat",
    async (ChatRequest req, CancellationToken ct) =>
    {
        await collection.EnsureCollectionExistsAsync(ct);

        var searchResults = await searchFunc(req.Message, ct);
        var context = string.Join("\n\n", searchResults.Select(r => $"[{r.SourceName}]: {r.Text}"));

        var messages = new List<ChatMessage> { new(ChatRole.User, req.Message) };

        if (!string.IsNullOrEmpty(context))
        {
            messages.Insert(
                0,
                new ChatMessage(
                    ChatRole.System,
                    SystemInstructions + "\n\n## Additional Context\n" + context
                )
            );
        }

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

        return Results.Ok(
            new
            {
                answer = response.Text,
                context = searchResults.Select(r => new { r.SourceName, r.Text }),
            }
        );
    }
);

// REST endpoints for ingestion
app.MapPost(
        "/api/ingest",
        async (IFormFile file, IngestionService ingestion, CancellationToken ct) =>
        {
            if (
                !string.Equals(
                    Path.GetExtension(file.FileName),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return Results.BadRequest(new { error = "Only PDF files are supported." });
            }

            await using var stream = file.OpenReadStream();
            var result = await ingestion.IngestPdfAsync(
                stream,
                file.FileName,
                file.FileName,
                file.Length,
                ct
            );
            if (result.Status == "completed")
            {
                var suggestions = app.Services.GetRequiredService<SuggestionGenerator>();
                _ = suggestions.GenerateAsync(CancellationToken.None);
            }

            return Results.Ok(
                new
                {
                    fileName = result.FileName,
                    chunks = result.ChunkCount,
                    pages = result.PageCount,
                    fileSizeBytes = result.FileSizeBytes,
                    status = result.Status,
                }
            );
        }
    )
    .DisableAntiforgery();

app.MapGet(
    "/api/documents",
    async (IngestionDbContext db, CancellationToken ct) =>
    {
        var names = await db
            .IngestionRecords.Select(r => r.FileName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
        return Results.Ok(names);
    }
);

app.MapDelete(
    "/api/documents/{source}",
    async (
        string source,
        VectorStoreCollection<Guid, DocumentRecord> col,
        IngestionDbContext db,
        CancellationToken ct
    ) =>
    {
        var idsToDelete = new List<Guid>();
        await foreach (var result in col.SearchAsync(source, top: 500, cancellationToken: ct))
        {
            if (result.Record.FileName == source)
            {
                idsToDelete.Add(result.Record.Id);
            }
        }

        if (idsToDelete.Count == 0)
        {
            return Results.NotFound(new { error = $"No document found with name '{source}'." });
        }

        await col.DeleteAsync(idsToDelete, ct);
        await db.IngestionRecords.Where(r => r.FileName == source).ExecuteDeleteAsync(ct);

        return Results.Ok(new { deleted = idsToDelete.Count });
    }
);

app.MapGet(
    "/api/documents/history",
    async (IngestionDbContext db, CancellationToken ct) =>
    {
        var records = await db
            .IngestionRecords.OrderByDescending(r => r.IngestedAt)
            .ToListAsync(ct);
        return Results.Ok(records);
    }
);

app.MapGet(
    "/api/documents/stats",
    async (IngestionDbContext db, CancellationToken ct) =>
    {
        var records = await db.IngestionRecords.ToListAsync(ct);
        return Results.Ok(
            new
            {
                totalDocuments = records.Count,
                totalChunks = records.Sum(r => r.ChunkCount),
                totalPages = records.Sum(r => r.PageCount),
                totalSizeBytes = records.Sum(r => r.FileSizeBytes),
            }
        );
    }
);

app.MapGet(
    "/api/suggestions",
    async (IngestionDbContext db, CancellationToken ct) =>
    {
        var suggestions = await db
            .ChatSuggestions.OrderByDescending(s => s.CreatedAt)
            .Select(s => new { title = s.Text, message = s.Text })
            .ToListAsync(ct);
        return Results.Ok(suggestions);
    }
);

await app.RunAsync();

record ChatRequest(string Message);
