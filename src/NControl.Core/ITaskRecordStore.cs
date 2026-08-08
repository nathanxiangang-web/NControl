namespace NControl.Core;

/// <summary>任务记录存储(第一代使用 SQLite,优先用于任务记录、历史和后续扩展)。</summary>
public interface ITaskRecordStore
{
    Task SaveAsync(TaskRecord record);
    Task<IReadOnlyList<TaskRecord>> GetRecentAsync(int limit);
    Task<IReadOnlyList<TaskRecord>> GetAllAsync();
}
