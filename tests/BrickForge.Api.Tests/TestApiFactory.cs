using BrickForge.Core.Pipelines;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrickForge.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for endpoint tests.
/// Replaces IJobQueue with a no-op so jobs stay Queued throughout tests.
/// Development appsettings provide MockMode=true and SQLite :memory:.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Replace with no-op queue: jobs enqueued but never dequeued
            var queueDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IJobQueue));
            if (queueDesc != null) services.Remove(queueDesc);
            services.AddSingleton<IJobQueue, NoOpJobQueue>();
        });
    }
}

/// <summary>
/// Job queue that accepts enqueue calls but never delivers jobs.
/// Used to prevent background processing during endpoint tests.
/// </summary>
internal sealed class NoOpJobQueue : IJobQueue
{
    public void Enqueue(string jobId) { }

    public Task<string> DequeueAsync(CancellationToken cancellationToken = default)
        => Task.Delay(Timeout.Infinite, cancellationToken)
               .ContinueWith<string>(_ => string.Empty, cancellationToken,
                   TaskContinuationOptions.None, TaskScheduler.Default);
}
