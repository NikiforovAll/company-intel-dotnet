var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder
    .AddOllama("ollama")
    .WithImageTag("latest")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var llama = ollama.AddModel("llama3.1");
var embedModel = ollama.AddModel("all-minilm");

var qdrant = builder
    .AddQdrant("qdrant", apiKey: builder.AddParameter("qdrant-apikey", "localdev"))
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent);

if (
    !string.Equals(builder.Configuration["UseVolumes"], "false", StringComparison.OrdinalIgnoreCase)
)
{
    qdrant.WithDataVolume();
}

builder
    .AddProject<Projects.CompanyIntel_Api>("api")
    .WithReference(llama)
    .WithReference(embedModel)
    .WithReference(qdrant);

await builder.Build().RunAsync();
