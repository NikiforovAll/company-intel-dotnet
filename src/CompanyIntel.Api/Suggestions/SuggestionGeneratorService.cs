namespace CompanyIntel.Api.Suggestions;

public sealed class SuggestionGeneratorService(
    SuggestionGenerator generator,
    ILogger<SuggestionGeneratorService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        try
        {
            await generator.GenerateAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to generate initial suggestions");
        }
    }
}
