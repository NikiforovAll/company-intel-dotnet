using CompanyIntel.Api.Data;
using CompanyIntel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace CompanyIntel.Api.Suggestions;

public sealed class SuggestionGenerator(
    IServiceScopeFactory scopeFactory,
    IChatClient chatClient,
    VectorStoreCollection<Guid, DocumentRecord> collection,
    ILogger<SuggestionGenerator> logger
)
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task GenerateAsync(CancellationToken ct = default)
    {
        if (!await _semaphore.WaitAsync(0, ct))
        {
            logger.LogInformation("Suggestion generation already in progress, skipping");
            return;
        }

        try
        {
            await GenerateCoreAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task GenerateCoreAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IngestionDbContext>();

        var docNames = await db
            .IngestionRecords.Where(r => r.Status == "completed")
            .Select(r => r.FileName)
            .Distinct()
            .ToListAsync(ct);

        // Fallback: discover doc names from vector store when SQLite metadata is missing
        if (docNames.Count == 0)
        {
            var names = new HashSet<string>();
            await foreach (
                var result in collection.SearchAsync("", top: 100, cancellationToken: ct)
            )
            {
                names.Add(result.Record.FileName);
            }
            docNames = [.. names];
        }

        if (docNames.Count == 0)
        {
            logger.LogInformation("No ingested documents found, skipping suggestion generation");
            return;
        }

        logger.LogInformation("Generating suggestions from {Count} documents", docNames.Count);

        const int maxSnippets = 15;
        var chunksByDoc = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var docName in docNames)
        {
            await foreach (
                var result in collection.SearchAsync(docName, top: 20, cancellationToken: ct)
            )
            {
                if (
                    !string.Equals(
                        result.Record.FileName,
                        docName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    continue;

                var text = result.Record.Text;
                if (text.Length > 500)
                    text = text[..500];

                if (!chunksByDoc.TryGetValue(docName, out var list))
                    chunksByDoc[docName] = list = [];
                list.Add($"[{docName}]: {text}");
            }
        }

        // Round-robin snippets across documents for even coverage
        var snippets = new List<string>();
        var queues = chunksByDoc.Values.Select(c => new Queue<string>(c)).ToList();
        while (snippets.Count < maxSnippets && queues.Count > 0)
        {
            for (var i = queues.Count - 1; i >= 0; i--)
            {
                if (queues[i].Count == 0)
                {
                    queues.RemoveAt(i);
                    continue;
                }
                snippets.Add(queues[i].Dequeue());
                if (snippets.Count >= maxSnippets)
                    break;
            }
        }

        var prompt = $"""
            You are generating starter questions for a document Q&A system.
            Below are snippets from ingested documents. Generate exactly 8 questions that:
            - Are DIRECTLY and FULLY answerable from the provided snippets
            - Are short (under 80 chars) and conversational
            - Cover different topics across the documents
            - Ask about facts, summaries, or comparisons that the content clearly supports
            - Do NOT ask about details, specifics, or data points not present in the snippets

            Documents: {string.Join(", ", docNames)}

            Content snippets:
            {string.Join("\n", snippets)}

            Return ONLY the 8 questions, one per line, no numbering, no quotes, no extra text.
            """;

        // Use a generous timeout — local LLM inference can be slow
        using var llmCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        llmCts.CancelAfter(TimeSpan.FromMinutes(3));

        var response = await chatClient.GetResponseAsync(prompt, cancellationToken: llmCts.Token);
        var text2 = response.Text ?? "";

        var questions = text2
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(q => q.Length > 5 && q.Length < 120 && q.EndsWith('?'))
            .Take(8)
            .ToList();

        if (questions.Count == 0)
        {
            logger.LogWarning("LLM returned no valid questions");
            return;
        }

        var now = DateTime.UtcNow;
        var suggestions = questions
            .Select(q => new ChatSuggestion { Text = q, CreatedAt = now })
            .ToList();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM ChatSuggestions", ct);
        db.ChatSuggestions.AddRange(suggestions);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Generated {Count} chat suggestions", suggestions.Count);
    }
}
