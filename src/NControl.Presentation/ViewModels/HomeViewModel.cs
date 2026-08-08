using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>首页:推荐入口、常用工具、最近执行(产品文档 §6.1)。不展示虚假健康评分。</summary>
public partial class HomeViewModel : ObservableObject, IRefreshable
{
    private readonly ITaskRecordStore _store;

    public HomeViewModel(ITaskRecordStore store, NavigationService nav)
    {
        _store = store;

        QuickCards.Add(new QuickCardViewModel(
            "推荐优化", "关闭广告与推荐,调整任务栏、资源管理器和常用系统设置。", "查看推荐方案 →", "\uE945",
            new RelayCommand(() => nav.Navigate("optimize", null))));
        QuickCards.Add(new QuickCardViewModel(
            "系统精简", "集中管理常见预装应用、可选组件和不需要的系统功能。", "自定义选择 →", "\uE71D",
            new RelayCommand(() => nav.Navigate("apps", null))));
        QuickCards.Add(new QuickCardViewModel(
            "清理空间", "清理系统临时文件、缓存、日志和常见无用内容。", "开始清理 →", "\uE74D",
            new RelayCommand(() => nav.Navigate("cleanup", null))));
        QuickCards.Add(new QuickCardViewModel(
            "系统修复", "运行 DISM、SFC、更新修复和常见网络修复工具。", "查看修复工具 →", "\uE90F",
            new RelayCommand(() => nav.Navigate("repair", null))));

        ToolCards.Add(new ToolCardViewModel("网络诊断", "Ping / DNS / 路由", "\uEA6A",
            new RelayCommand(() => nav.Navigate("tools", null))));
        ToolCards.Add(new ToolCardViewModel("更新管理", "暂停、恢复与策略", "\uE895",
            new RelayCommand(() => nav.Navigate("settings", "Windows 更新"))));
        ToolCards.Add(new ToolCardViewModel("启动项管理", "查看与禁用启动项", "\uE7B5",
            new RelayCommand(() => nav.Navigate("tools", null))));
        ToolCards.Add(new ToolCardViewModel("Windows 功能", "WSL / 沙箱 / Hyper-V", "\uE950",
            new RelayCommand(() => nav.Navigate("tools", null))));

        _ = LoadRecentAsync();
    }

    public ObservableCollection<QuickCardViewModel> QuickCards { get; } = new();
    public ObservableCollection<ToolCardViewModel> ToolCards { get; } = new();
    public ObservableCollection<ActivityRowViewModel> Recent { get; } = new();

    public void Refresh() => _ = LoadRecentAsync();

    private async Task LoadRecentAsync()
    {
        try
        {
            var records = await _store.GetRecentAsync(5);
            Recent.Clear();
            foreach (var r in records)
                Recent.Add(new ActivityRowViewModel(r));
        }
        catch
        {
            // 记录加载失败不影响首页;错误会体现在任务记录页
        }
    }
}
