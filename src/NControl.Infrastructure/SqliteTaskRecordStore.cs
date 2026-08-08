using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NControl.Core;

namespace NControl.Infrastructure;

/// <summary>
/// SQLite 任务记录存储。任务记录持久化于 %LocalAppData%\NControl\ncontrol.db。
/// 单项明细以 JSON 列存储,保持简单可靠并兼容历史记录。
/// </summary>
public sealed class SqliteTaskRecordStore : ITaskRecordStore, IDisposable
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteTaskRecordStore> _logger;
    private readonly SqliteConnection _connection;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public SqliteTaskRecordStore(AppPaths paths, ILogger<SqliteTaskRecordStore> logger)
    {
        _dbPath = paths.DatabasePath;
        _logger = logger;
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS TaskRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                FinishedAt TEXT NULL,
                Result TEXT NOT NULL,
                SuccessCount INTEGER NOT NULL DEFAULT 0,
                FailedCount INTEGER NOT NULL DEFAULT 0,
                CancelledCount INTEGER NOT NULL DEFAULT 0,
                RequiresRestart INTEGER NOT NULL DEFAULT 0,
                ItemsJson TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveAsync(TaskRecord record)
    {
        var itemsJson = JsonSerializer.Serialize(record.Items, JsonOptions);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO TaskRecords (Name, StartedAt, FinishedAt, Result, SuccessCount, FailedCount, CancelledCount, RequiresRestart, ItemsJson)
            VALUES ($name, $started, $finished, $result, $ok, $fail, $cancel, $restart, $items);
            """;
        cmd.Parameters.AddWithValue("$name", record.Name);
        cmd.Parameters.AddWithValue("$started", record.StartedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$finished", record.FinishedAt?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$result", record.Result);
        cmd.Parameters.AddWithValue("$ok", record.SuccessCount);
        cmd.Parameters.AddWithValue("$fail", record.FailedCount);
        cmd.Parameters.AddWithValue("$cancel", record.CancelledCount);
        cmd.Parameters.AddWithValue("$restart", record.RequiresRestart ? 1 : 0);
        cmd.Parameters.AddWithValue("$items", itemsJson);
        await cmd.ExecuteNonQueryAsync();

        using var idCmd = _connection.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        record.Id = Convert.ToInt64((await idCmd.ExecuteScalarAsync())!);
    }

    public async Task<IReadOnlyList<TaskRecord>> GetRecentAsync(int limit)
    {
        var result = new List<TaskRecord>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Name, StartedAt, FinishedAt, Result, SuccessCount, FailedCount, CancelledCount, RequiresRestart, ItemsJson
            FROM TaskRecords ORDER BY Id DESC LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(ReadRecord(reader));
        return result;
    }

    public async Task<IReadOnlyList<TaskRecord>> GetAllAsync()
    {
        var result = new List<TaskRecord>();
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Name, StartedAt, FinishedAt, Result, SuccessCount, FailedCount, CancelledCount, RequiresRestart, ItemsJson
            FROM TaskRecords ORDER BY Id DESC;
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(ReadRecord(reader));
        return result;
    }

    private static TaskRecord ReadRecord(SqliteDataReader reader)
    {
        var itemsJson = reader.GetString(9);
        var items = JsonSerializer.Deserialize<List<TaskItemRecord>>(itemsJson, JsonOptions) ?? new List<TaskItemRecord>();
        return new TaskRecord
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            StartedAt = DateTime.Parse(reader.GetString(2)).ToLocalTime(),
            FinishedAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)).ToLocalTime(),
            Result = reader.GetString(4),
            SuccessCount = reader.GetInt32(5),
            FailedCount = reader.GetInt32(6),
            CancelledCount = reader.GetInt32(7),
            RequiresRestart = reader.GetInt32(8) == 1,
            Items = items
        };
    }

    public void Dispose() => _connection.Dispose();
}
