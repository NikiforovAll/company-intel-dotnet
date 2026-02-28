using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace CompanyIntel.AppHost.Tests;

public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;

    public HttpClient ApiClient { get; private set; } = null!;
    public Uri OllamaEndpoint { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.CompanyIntel_AppHost>(
                ["Ephemeral=true"],
                (appOptions, _) => appOptions.DisableDashboard = true,
                cts.Token
            );

        _app = await appHost.BuildAsync(cts.Token);
        await _app.StartAsync(cts.Token);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", cts.Token);

        ApiClient = _app.CreateHttpClient("api", "http");
        ApiClient.Timeout = TimeSpan.FromMinutes(5);

        var ollamaEndpoint = _app.GetEndpoint("ollama", "http");
        OllamaEndpoint = new Uri(ollamaEndpoint.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        ApiClient.Dispose();
        await _app.DisposeAsync();
    }
}
