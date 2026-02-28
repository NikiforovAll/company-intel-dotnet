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

var isPersistent = !string.Equals(
    builder.Configuration["Ephemeral"],
    "true",
    StringComparison.OrdinalIgnoreCase
);

string? sqlitePath = null;
if (isPersistent)
{
    qdrant.WithDataVolume();
    sqlitePath = Path.Combine(builder.AppHostDirectory, "..", ".data");
    Directory.CreateDirectory(sqlitePath);
}

var sqlite = builder
    .AddSqlite(
        "ingestion-db",
        databasePath: sqlitePath,
        databaseFileName: isPersistent ? "ingestion.db" : null
    )
    .WithSqliteWeb();

var api = builder
    .AddProject<Projects.CompanyIntel_Api>("api")
    .WithReference(llama)
    .WithReference(embedModel)
    .WithReference(qdrant)
    .WithReference(sqlite);

builder
    .AddJavaScriptApp("ui", "../CompanyIntel.UI", "dev")
    .WithPnpm()
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithEnvironment("AGENT_URL", api.GetEndpoint("http"))
    .WithOtlpExporter();

await builder.Build().RunAsync();
