using System.Net;
using Aspire.Hosting.Testing;

namespace CompanyIntel.AppHost.Tests;

public class ApiHealthTests
{
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task ApiHealthEndpointReturnsOk()
    {
        using var cts = new CancellationTokenSource(s_defaultTimeout);

        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.CompanyIntel_AppHost>(
                ["Ephemeral=true"],
                (appOptions, _) => appOptions.DisableDashboard = true,
                cts.Token
            );

        await using var app = await appHost.BuildAsync(cts.Token);
        await app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cts.Token);

        using var httpClient = app.CreateHttpClient("api", "http");
        var response = await httpClient.GetAsync("/health", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
