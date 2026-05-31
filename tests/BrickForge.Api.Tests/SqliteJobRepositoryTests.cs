using BrickForge.Api.Persistence;
using BrickForge.Core.Jobs;

namespace BrickForge.Api.Tests;

public sealed class SqliteJobRepositoryTests
{
    private static SqliteJobRepository CreateRepo() =>
        new("Data Source=:memory:");

    private static GenerationJob MakeJob(string prompt = "test-prompt") => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        CreatedAt = DateTimeOffset.UtcNow,
        Prompt = prompt
    };

    [Fact]
    public async Task CreateAsync_AndGetByIdAsync_ReturnsJob()
    {
        var repo = CreateRepo();
        var job = MakeJob("test-prompt");

        await repo.CreateAsync(job);

        var retrieved = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(job.Id, retrieved.Id);
        Assert.Equal("test-prompt", retrieved.Prompt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenJobDoesNotExist_ReturnsNull()
    {
        var repo = CreateRepo();

        var result = await repo.GetByIdAsync("nonexistent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStatus()
    {
        var repo = CreateRepo();
        var job = MakeJob("prompt");
        await repo.CreateAsync(job);

        job.Status = JobStatus.Completed;
        await repo.UpdateAsync(job);

        var updated = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(updated);
        Assert.Equal(JobStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllCreatedJobs()
    {
        var repo = CreateRepo();
        var j1 = MakeJob("p1");
        var j2 = MakeJob("p2");
        await repo.CreateAsync(j1);
        await repo.CreateAsync(j2);

        var all = await repo.ListAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithFilesAttached_ReturnsFilesOnJob()
    {
        var repo = CreateRepo();
        var job = MakeJob("prompt");
        job.Files.Add(new GeneratedFile
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = job.Id,
            FileType = "model.mpd",
            FilePath = "/data/outputs/job1/model.mpd",
            CreatedAt = DateTimeOffset.UtcNow
        });
        job.Files.Add(new GeneratedFile
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = job.Id,
            FileType = "parts.csv",
            FilePath = "/data/outputs/job1/parts.csv",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await repo.CreateAsync(job);

        var retrieved = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Files.Count);
        Assert.Contains(retrieved.Files, f => f.FileType == "model.mpd");
        Assert.Contains(retrieved.Files, f => f.FileType == "parts.csv");
    }

    [Fact]
    public async Task UpdateAsync_WithFiles_PersistsAndReplacesPreviousFiles()
    {
        var repo = CreateRepo();
        var job = MakeJob("prompt");
        await repo.CreateAsync(job);

        job.Files.Add(new GeneratedFile
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = job.Id,
            FileType = "model.mpd",
            FilePath = "/out/model.mpd",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await repo.UpdateAsync(job);

        job.Files.Add(new GeneratedFile
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = job.Id,
            FileType = "parts.csv",
            FilePath = "/out/parts.csv",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await repo.UpdateAsync(job);

        var updated = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Files.Count);
        Assert.Contains(updated.Files, f => f.FileType == "model.mpd");
        Assert.Contains(updated.Files, f => f.FileType == "parts.csv");
    }
}

