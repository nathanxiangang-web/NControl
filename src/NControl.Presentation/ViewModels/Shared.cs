using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;

namespace NControl.Presentation.ViewModels;

/// <summary>可刷新页面(任务完成后由壳层调用)。</summary>
public interface IRefreshable
{
    void Refresh();
}

/// <summary>左侧导航项。</summary>
public partial class NavItemViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    public NavItemViewModel(MainViewModel main, string key, string title, string glyph)
    {
        _main = main;
        Key = key;
        Title = title;
        Glyph = glyph;
    }

    public string Key { get; }
    public string Title { get; }
    public string Glyph { get; }

    [ObservableProperty]
    private bool isActive;

    [RelayCommand]
    private void Navigate() => _main.Navigate(Key, null);
}

/// <summary>顶部搜索命中项。</summary>
public partial class SearchItemViewModel : ObservableObject
{
    public SearchItemViewModel(FunctionItem item)
    {
        Item = item;
        PageKey = item.Module switch
        {
            ModuleKind.Optimization => "settings",
            ModuleKind.Applications => "apps",
            ModuleKind.Cleanup => "cleanup",
            ModuleKind.Repair => "repair",
            _ => "tools"
        };
        ModuleLabel = item.Module switch
        {
            ModuleKind.Optimization => "系统设置",
            ModuleKind.Applications => "应用管理",
            ModuleKind.Cleanup => "清理维护",
            ModuleKind.Repair => "系统修复",
            _ => "实用工具"
        };
    }

    public FunctionItem Item { get; }
    public string PageKey { get; }
    public string ModuleLabel { get; }
    public string Title => Item.Name;
    public string Subtitle => $"{ModuleLabel} · {Item.Category}";
}

/// <summary>分类标签(芯片)。</summary>
public partial class ChipItem : ObservableObject
{
    public ChipItem(string title, bool isActive)
    {
        Title = title;
        IsActive = isActive;
    }

    public string Title { get; }

    [ObservableProperty]
    private bool isActive;
}
