namespace NControl.Core;

/// <summary>任务记录存储(SQLite,用于任务历史、结果明细与批次回滚分析)。</summary>
public interface ITaskRecordStore
{
    Task SaveAsync(TaskRecord record);
    Task<IReadOnlyList<TaskRecord>> GetRecentAsync(int limit);
    Task<IReadOnlyList<TaskRecord>> GetAllAsync();
}
