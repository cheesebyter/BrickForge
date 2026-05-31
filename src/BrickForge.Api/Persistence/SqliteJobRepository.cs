using System.Data;
using BrickForge.Core.Catalog;
using BrickForge.Core.Jobs;
using Microsoft.Data.Sqlite;

namespace BrickForge.Api.Persistence;

/// <summary>
/// SQLite-backed implementation of <see cref="IJobRepository"/> and <see cref="ICatalogRepository"/>.
/// Uses a single open connection kept alive for the lifetime of the singleton.
/// A SemaphoreSlim serialises all write operations for thread safety.
/// </summary>
public sealed class SqliteJobRepository : IJobRepository, ICatalogRepository, IDisposable
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
                Difficulty      TEXT,
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

            CREATE TABLE IF NOT EXISTS ModelTemplates (
                Id          TEXT NOT NULL PRIMARY KEY,
                Name        TEXT NOT NULL,
                Version     TEXT NOT NULL,
                Description TEXT
            );

            CREATE TABLE IF NOT EXISTS PartDefinitions (
                Id         TEXT NOT NULL PRIMARY KEY,
                PartNumber TEXT NOT NULL,
                Name       TEXT NOT NULL,
                Category   TEXT,
                Supported  INTEGER NOT NULL DEFAULT 1
            );
            """;
        cmd.ExecuteNonQuery();

        // Migration: add Difficulty to existing databases that pre-date this column.
        TryAddColumn("GenerationJobs", "Difficulty", "TEXT");
    }

    private void TryAddColumn(string table, string column, string type)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists – ignore.
        }
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
            INSERT INTO GenerationJobs (Id, CreatedAt, Prompt, Status, TemplateName, Difficulty, TargetParts, ActualParts, OutputPath, ValidationScore, ErrorMessage)
            VALUES (@id, @createdAt, @prompt, @status, @templateName, @difficulty, @targetParts, @actualParts, @outputPath, @validationScore, @errorMessage)
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
                Difficulty      = @difficulty,
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
        cmd.Parameters.AddWithValue("@difficulty", (object?)job.Difficulty ?? DBNull.Value);
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
        cmd.Parameters.AddWithValue("@difficulty", (object?)job.Difficulty ?? DBNull.Value);
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
            Difficulty = reader.IsDBNull("Difficulty") ? null : reader.GetString("Difficulty"),
            TargetParts = reader.IsDBNull("TargetParts") ? null : reader.GetInt32("TargetParts"),
            ActualParts = reader.IsDBNull("ActualParts") ? null : reader.GetInt32("ActualParts"),
            OutputPath = reader.IsDBNull("OutputPath") ? null : reader.GetString("OutputPath"),
            ValidationScore = reader.IsDBNull("ValidationScore") ? null : reader.GetDouble("ValidationScore"),
            ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage")
        };
    }

    // ── ICatalogRepository ────────────────────────────────────────────────────

    public async Task SeedTemplatesAsync(
        IEnumerable<TemplateDefinitionEntry> templates,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            foreach (var t in templates)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = """
                    INSERT OR REPLACE INTO ModelTemplates (Id, Name, Version, Description)
                    VALUES (@id, @name, @version, @description)
                    """;
                cmd.Parameters.AddWithValue("@id", t.Id);
                cmd.Parameters.AddWithValue("@name", t.Name);
                cmd.Parameters.AddWithValue("@version", t.Version);
                cmd.Parameters.AddWithValue("@description", (object?)t.Description ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SeedPartsAsync(
        IEnumerable<PartDefinitionEntry> parts,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            foreach (var p in parts)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = """
                    INSERT OR REPLACE INTO PartDefinitions (Id, PartNumber, Name, Category, Supported)
                    VALUES (@id, @partNumber, @name, @category, @supported)
                    """;
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.Parameters.AddWithValue("@partNumber", p.PartNumber);
                cmd.Parameters.AddWithValue("@name", p.Name);
                cmd.Parameters.AddWithValue("@category", (object?)p.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@supported", p.Supported ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<TemplateDefinitionEntry>> ListTemplatesAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var results = new List<TemplateDefinitionEntry>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Version, Description FROM ModelTemplates ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TemplateDefinitionEntry(
                    reader.GetString("Id"),
                    reader.GetString("Name"),
                    reader.GetString("Version"),
                    reader.IsDBNull("Description") ? null : reader.GetString("Description")));
            }
            return results.AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<PartDefinitionEntry>> ListPartsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var results = new List<PartDefinitionEntry>();
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, PartNumber, Name, Category, Supported FROM PartDefinitions ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new PartDefinitionEntry(
                    reader.GetString("Id"),
                    reader.GetString("PartNumber"),
                    reader.GetString("Name"),
                    reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                    reader.GetInt32("Supported") != 0));
            }
            return results.AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }
}
