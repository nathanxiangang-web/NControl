using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;

namespace NControl.Presentation.ViewModels;

/// <summary>任务记录页:读取 SQLite 中的历史任务(产品文档 §4.3、§6.1)。</summary>
public partial class RecordsViewModel : ObservableObject, IRefreshable
{
    private readonly ITaskRecordStore _store;

    public RecordsViewModel(ITaskRecordStore store)
    {
        _store = store;
        _ = LoadAsync();
    }

    public ObservableCollection<RecordCardViewModel> Records { get; } = new();

    [ObservableProperty]
    private string emptyText = "还没有任务记录。从首页或任意页面发起一次执行后,这里会显示结果。";

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    public void Refresh() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var records = await _store.GetAllAsync();
            Records.Clear();
            foreach (var r in records)
                Records.Add(new RecordCardViewModel(r));
            EmptyText = Records.Count == 0 ? "还没有任务记录。从首页或任意页面发起一次执行后,这里会显示结果。" : "";
        }
        catch (Exception ex)
        {
            EmptyText = $"任务记录读取失败:{ex.Message}";
        }
    }
}
