using BrickForge.Api.Endpoints;
using BrickForge.Api.Persistence;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;

var builder = WebApplication.CreateBuilder(args);

// ── Options ──────────────────────────────────────────────────────────────────
builder.Services.Configure<GenerationOptions>(
    builder.Configuration.GetSection("Generation"));
builder.Services.Configure<BrickForge.Core.Options.OllamaOptions>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<BrickForge.Core.Options.ExportOptions>(
    builder.Configuration.GetSection("Export"));

// ── Domain / Repositories ────────────────────────────────────────────────────
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();

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
builder.Services.AddHealthChecks();

var app = builder.Build();

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
