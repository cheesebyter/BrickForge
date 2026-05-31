using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.Api.Endpoints;
using BrickForge.Api.Health;
using BrickForge.Api.Persistence;
using BrickForge.Api.Services;
using BrickForge.Api.Workers;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Repair;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Catalog;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;
using BrickForge.Core.Pipelines;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── Options ──────────────────────────────────────────────────────────────────
builder.Services.Configure<GenerationOptions>(
    builder.Configuration.GetSection("Generation"));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<ExportOptions>(
    builder.Configuration.GetSection("Export"));
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage"));
builder.Services.Configure<ExternalAiOptions>(
    builder.Configuration.GetSection("ExternalAi"));

// ── Ollama client ─────────────────────────────────────────────────────────────
var ollamaOptsValue = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new OllamaOptions();
if (ollamaOptsValue.MockMode)
{
    builder.Services.AddSingleton<IOllamaClient, MockOllamaClient>();
}
else
{
    builder.Services.AddHttpClient("ollama", client =>
    {
        client.BaseAddress = new Uri(ollamaOptsValue.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(ollamaOptsValue.TimeoutSeconds);
    });

    builder.Services.AddSingleton<IOllamaClient>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient("ollama");
        var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
        return new OllamaClient(httpClient, opts);
    });
}

// ── Prompt analyzer ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<IPromptAnalyzer>(sp =>
{
    var client = sp.GetRequiredService<IOllamaClient>();
    var ollamaOpts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    var genOpts = sp.GetRequiredService<IOptions<GenerationOptions>>().Value;
    return new PromptAnalysisService(client, ollamaOpts, genOpts);
});

// ── Data registries (graceful degradation on missing files) ───────────────────
SupportedPartsRegistry partsRegistry;
TemplateRegistry templateRegistry;
try
{
    var partsDir = Path.Combine(AppContext.BaseDirectory, "data", "parts");
    var partsJson = File.ReadAllText(Path.Combine(partsDir, "supported-parts.json"));
    var colorsJson = File.ReadAllText(Path.Combine(partsDir, "supported-colors.json"));
    partsRegistry = SupportedPartsRegistry.FromJson(partsJson, colorsJson);
    templateRegistry = TemplateRegistry.FromDirectory(partsDir);
}
catch (Exception ex)
{
    Console.WriteLine($"[warning] Data files not found at startup: {ex.Message}");
    partsRegistry = new SupportedPartsRegistry([], []);
    templateRegistry = new TemplateRegistry([]);
}

builder.Services.AddSingleton(partsRegistry);
builder.Services.AddSingleton(templateRegistry);
builder.Services.AddSingleton<SmallMachineGenerator>();
builder.Services.AddSingleton<TemplateBasedGenerator>();
builder.Services.AddSingleton<BrickGraphValidator>();
builder.Services.AddSingleton<BrickGraphRepairAgent>();

// ── Job queue and pipeline ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IJobQueue, DefaultJobQueue>();
builder.Services.AddSingleton<IGenerationPipelineService, GenerationPipelineService>();
builder.Services.AddHostedService<GenerationJobWorker>();

// ── Persistence ───────────────────────────────────────────────────────────────
builder.Services.AddSingleton<SqliteJobRepository>(sp =>
{
    var storageOpts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    return new SqliteJobRepository(storageOpts.ConnectionString);
});
builder.Services.AddSingleton<IJobRepository>(sp => sp.GetRequiredService<SqliteJobRepository>());
builder.Services.AddSingleton<ICatalogRepository>(sp => sp.GetRequiredService<SqliteJobRepository>());

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "BrickForge API",
        Version = "v1",
        Description = "Local-first AI brick model generation API."
    });
});

// ── CORS (localhost only) ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("ollama");

var app = builder.Build();

// ── BF-MVP1-023: Serve static SPA UI from wwwroot/ ────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();

// ── BF-MVP1-021: Seed catalog tables at startup ───────────────────────────────
var catalogRepo = app.Services.GetRequiredService<ICatalogRepository>();
var tplReg = app.Services.GetRequiredService<TemplateRegistry>();
var partsReg = app.Services.GetRequiredService<SupportedPartsRegistry>();

var templateEntries = tplReg.TemplateIds
    .Select(id => new TemplateDefinitionEntry(id, id, "1.0", null));
await catalogRepo.SeedTemplatesAsync(templateEntries);

var partEntries = partsReg.SupportedPartNumbers
    .Select(pn => new PartDefinitionEntry(pn, pn, pn, null, true));
await catalogRepo.SeedPartsAsync(partEntries);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BrickForge API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();

app.MapHealthChecks("/health");

app.MapGenerationJobEndpoints();

app.Run();

// Required for WebApplicationFactory<Program> in tests.
public partial class Program { }

