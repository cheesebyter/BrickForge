using System.Data;
using BrickForge.Core.Jobs;
using Microsoft.Data.Sqlite;

namespace BrickForge.Api.Persistence;

/// <summary>
/// SQLite-backed implementation of <see cref="IJobRepository"/>.
/// Uses a single open connection kept alive for the lifetime of the singleton.
/// A SemaphoreSlim serialises all write operations for thread safety.
/// </summary>
public sealed class SqliteJobRepository : IJobRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SqliteJobRepository(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS GenerationJobs (
                Id              TEXT NOT NULL PRIMARY KEY,
                CreatedAt       TEXT NOT NULL,
                Prompt          TEXT NOT NULL,
                Status          TEXT NOT NULL,
                TemplateName    TEXT,
                TargetParts     INTEGER,
                ActualParts     INTEGER,
                OutputPath      TEXT,
                ValidationScore REAL,
                ErrorMessage    TEXT
            );

            CREATE TABLE IF NOT EXISTS GeneratedFiles (
                Id        TEXT NOT NULL PRIMARY KEY,
                JobId     TEXT NOT NULL,
                FileType  TEXT NOT NULL,
                FilePath  TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<GenerationJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return GetByIdCore(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<GenerationJob> CreateAsync(GenerationJob job, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            InsertJob(job);
            foreach (var file in job.Files)
                InsertFile(file);
            return job;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(GenerationJob job, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            UpdateJobCore(job);
            DeleteFilesForJob(job.Id);
            foreach (var file in job.Files)
                InsertFile(file);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<GenerationJob>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return ListCore();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _connection.Dispose();
    }

    // ── Core helpers (called while lock is held) ──────────────────────────────

    private GenerationJob? GetByIdCore(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM GenerationJobs WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var job = MapJob(reader);
        reader.Close();

        AttachFiles(job);
        return job;
    }

    private IReadOnlyList<GenerationJob> ListCore()
    {
        var jobs = new List<GenerationJob>();

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM GenerationJobs ORDER BY CreatedAt DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                jobs.Add(MapJob(reader));
        }

        foreach (var job in jobs)
            AttachFiles(job);

        return jobs.AsReadOnly();
    }

    private void InsertJob(GenerationJob job)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO GenerationJobs (Id, CreatedAt, Prompt, Status, TemplateName, TargetParts, ActualParts, OutputPath, ValidationScore, ErrorMessage)
            VALUES (@id, @createdAt, @prompt, @status, @templateName, @targetParts, @actualParts, @outputPath, @validationScore, @errorMessage)
            """;
        BindJobParams(cmd, job);
        cmd.ExecuteNonQuery();
    }

    private void UpdateJobCore(GenerationJob job)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE GenerationJobs
            SET Status          = @status,
                TemplateName    = @templateName,
                TargetParts     = @targetParts,
                ActualParts     = @actualParts,
                OutputPath      = @outputPath,
                ValidationScore = @validationScore,
                ErrorMessage    = @errorMessage
            WHERE Id = @id
            """;
        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@status", job.Status.ToString());
        cmd.Parameters.AddWithValue("@templateName", (object?)job.TemplateName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@targetParts", (object?)job.TargetParts ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actualParts", (object?)job.ActualParts ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@outputPath", (object?)job.OutputPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@validationScore", (object?)job.ValidationScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@errorMessage", (object?)job.ErrorMessage ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private void DeleteFilesForJob(string jobId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM GeneratedFiles WHERE JobId = @jobId";
        cmd.Parameters.AddWithValue("@jobId", jobId);
        cmd.ExecuteNonQuery();
    }

    private void InsertFile(GeneratedFile file)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO GeneratedFiles (Id, JobId, FileType, FilePath, CreatedAt)
            VALUES (@id, @jobId, @fileType, @filePath, @createdAt)
            """;
        cmd.Parameters.AddWithValue("@id", file.Id);
        cmd.Parameters.AddWithValue("@jobId", file.JobId);
        cmd.Parameters.AddWithValue("@fileType", file.FileType);
        cmd.Parameters.AddWithValue("@filePath", file.FilePath);
        cmd.Parameters.AddWithValue("@createdAt", file.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private void AttachFiles(GenerationJob job)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM GeneratedFiles WHERE JobId = @jobId";
        cmd.Parameters.AddWithValue("@jobId", job.Id);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            job.Files.Add(new GeneratedFile
            {
                Id = reader.GetString("Id"),
                JobId = reader.GetString("JobId"),
                FileType = reader.GetString("FileType"),
                FilePath = reader.GetString("FilePath"),
                CreatedAt = DateTimeOffset.Parse(reader.GetString("CreatedAt"))
            });
        }
    }

    private static void BindJobParams(SqliteCommand cmd, GenerationJob job)
    {
        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@createdAt", job.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@prompt", job.Prompt);
        cmd.Parameters.AddWithValue("@status", job.Status.ToString());
        cmd.Parameters.AddWithValue("@templateName", (object?)job.TemplateName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@targetParts", (object?)job.TargetParts ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actualParts", (object?)job.ActualParts ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@outputPath", (object?)job.OutputPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@validationScore", (object?)job.ValidationScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@errorMessage", (object?)job.ErrorMessage ?? DBNull.Value);
    }

    private static GenerationJob MapJob(SqliteDataReader reader)
    {
        return new GenerationJob
        {
            Id = reader.GetString("Id"),
            CreatedAt = DateTimeOffset.Parse(reader.GetString("CreatedAt")),
            Prompt = reader.GetString("Prompt"),
            Status = Enum.Parse<JobStatus>(reader.GetString("Status")),
            TemplateName = reader.IsDBNull("TemplateName") ? null : reader.GetString("TemplateName"),
            TargetParts = reader.IsDBNull("TargetParts") ? null : reader.GetInt32("TargetParts"),
            ActualParts = reader.IsDBNull("ActualParts") ? null : reader.GetInt32("ActualParts"),
            OutputPath = reader.IsDBNull("OutputPath") ? null : reader.GetString("OutputPath"),
            ValidationScore = reader.IsDBNull("ValidationScore") ? null : reader.GetDouble("ValidationScore"),
            ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage")
        };
    }
}
