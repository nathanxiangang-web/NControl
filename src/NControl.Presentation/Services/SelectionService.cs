using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NControl.Core;

namespace NControl.Presentation.Services;

/// <summary>
/// 共享选择服务:保存用户在各页面勾选的功能项(产品文档 §5.2“待应用”状态)。
/// 页面切换不丢失选择;底部执行栏根据它显示已选择数量。
/// </summary>
public sealed class SelectionService : ObservableObject
{
    public ObservableCollection<FunctionItem> Selected { get; } = new();

    public int Count => Selected.Count;

    public bool HasHighRisk => Selected.Any(f => f.Risk == RiskLevel.HighRisk);

    public bool IsSelected(FunctionItem item) => Selected.Contains(item);

    public void Toggle(FunctionItem item)
    {
        if (!Selected.Remove(item)) Selected.Add(item);
        Raise();
    }

    public void Add(FunctionItem item)
    {
        if (!Selected.Contains(item)) Selected.Add(item);
        Raise();
    }

    public void AddRange(IEnumerable<FunctionItem> items)
    {
        foreach (var item in items) Add(item);
        Raise();
    }

    public void Remove(FunctionItem item)
    {
        if (Selected.Remove(item)) Raise();
    }

    public void Clear()
    {
        Selected.Clear();
        Raise();
    }

    private void Raise()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HasHighRisk));
    }
}
