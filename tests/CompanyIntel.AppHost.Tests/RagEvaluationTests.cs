using System.Net.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OllamaSharp;

namespace CompanyIntel.AppHost.Tests;

public class RagEvaluationTests(AspireAppFixture fixture, ITestOutputHelper output)
    : IClassFixture<AspireAppFixture>,
        IAsyncLifetime
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromMinutes(5);
    private static bool s_dataIngested;

    public async ValueTask InitializeAsync()
    {
        if (s_dataIngested)
        {
            return;
        }

        using var cts = new CancellationTokenSource(s_timeout);
        var pdfPath = Path.Combine(AppContext.BaseDirectory, "TestData", "contoso-corp.pdf");

        using var content = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(pdfPath);
#pragma warning disable CA2000 // Dispose objects before losing scope

        content.Add(new StreamContent(fileStream), "file", "contoso-corp.pdf");
#pragma warning restore CA2000 // Dispose objects before losing scope

        var response = await fixture.ApiClient.PostAsync("/api/ingest", content, cts.Token);
        response.EnsureSuccessStatusCode();

        // Allow time for embeddings to be indexed
        await Task.Delay(3000, cts.Token);
        s_dataIngested = true;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(EvalScenarios))]
    public async Task EvaluateRagResponseQuality(string question)
    {
        using var cts = new CancellationTokenSource(s_timeout);

        // 1. Call the chat endpoint
        var chatResponse = await fixture.ApiClient.PostAsJsonAsync(
            "/api/chat",
            new { message = question },
            cts.Token
        );
        if (!chatResponse.IsSuccessStatusCode)
        {
            var errorBody = await chatResponse.Content.ReadAsStringAsync(cts.Token);
            Assert.Fail($"Chat returned {chatResponse.StatusCode}: {errorBody}");
        }

        var result = await chatResponse.Content.ReadFromJsonAsync<ChatApiResponse>(
            cancellationToken: cts.Token
        );
        Assert.NotNull(result);
        output.WriteLine($"Question: {question}");
        output.WriteLine($"Answer: {result.Answer}");
        output.WriteLine($"Context chunks: {result.Context.Count}");

        // 2. Build evaluation inputs
        IList<ChatMessage> messages = [new ChatMessage(ChatRole.User, question)];
        var modelResponse = new ChatResponse([new ChatMessage(ChatRole.Assistant, result.Answer)]);

        // 3. Create judge ChatConfiguration using Ollama llama3.1
        IChatClient judgeChatClient = new OllamaApiClient(fixture.OllamaEndpoint, "llama3.1");
        var chatConfig = new ChatConfiguration(judgeChatClient);

        // 4. Build grounding context for evaluators
        var groundingContext = string.Join(
            "\n\n",
            result.Context.Select(c => $"[{c.SourceName}]: {c.Text}")
        );

        var additionalContext = new EvaluationContext[]
        {
            new GroundednessEvaluatorContext(groundingContext),
            new RetrievalEvaluatorContext(result.Context.Select(c => c.Text).ToArray()),
        };

        // 5. Run evaluators
        IEvaluator[] evaluators =
        [
            new RelevanceEvaluator(),
            new CoherenceEvaluator(),
            new GroundednessEvaluator(),
            new RetrievalEvaluator(),
        ];

        foreach (var evaluator in evaluators)
        {
            var evalResult = await evaluator.EvaluateAsync(
                messages,
                modelResponse,
                chatConfig,
                additionalContext,
                cts.Token
            );

            foreach (var metricName in evaluator.EvaluationMetricNames)
            {
                var metric = evalResult.Get<NumericMetric>(metricName);
                output.WriteLine(
                    $"  {metricName}: {metric.Value} (Rating: {metric.Interpretation?.Rating}, Failed: {metric.Interpretation?.Failed})"
                );

                Assert.NotNull(metric.Value);
                // Relaxed threshold for local LLM judge (score 1-5, require >= 3)
                Assert.True(
                    metric.Value >= 3,
                    $"{metricName} score too low: {metric.Value} (min 3). Reason: {metric.Reason}"
                );
            }
        }
    }

    public static TheoryData<string> EvalScenarios =>
        new()
        {
            { "What products does Contoso Corp offer?" },
            { "Who is the CEO of Contoso Corp?" },
            { "What is Contoso Corp's annual revenue?" },
        };
}

file record ContextChunk(string SourceName, string Text);

file record ChatApiResponse(string Answer, List<ContextChunk> Context);
