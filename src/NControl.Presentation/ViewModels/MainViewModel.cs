using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>
/// 应用壳层:导航、顶部搜索、底部执行栏与执行弹窗的调度。
/// 页面通过 DataTemplate 呈现,统一从这里发起执行。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private static readonly string[] SelectionPages = { "optimize", "settings", "apps", "cleanup" };

    private static readonly Dictionary<string, (string Title, string Desc)> Meta = new()
    {
        ["home"] = ("首页", "集中管理 Windows 的优化、精简、修复与常用工具"),
        ["optimize"] = ("一键优化", "选择预设方案或自定义优化项目"),
        ["settings"] = ("系统设置", "统一管理资源管理器、任务栏、隐私、更新与性能设置"),
        ["apps"] = ("应用管理", "管理预装应用、Windows 组件和常用软件"),
        ["cleanup"] = ("清理维护", "清理缓存、临时文件和常见系统垃圾"),
        ["repair"] = ("系统修复", "运行 Windows 自带修复工具解决常见问题"),
        ["tools"] = ("实用工具", "面向日常运维的 Windows 工具集合"),
        ["records"] = ("任务记录", "查看历史执行任务与结果"),
        ["appsettings"] = ("设置", "应用数据与行为"),
        ["about"] = ("关于", "版本、技术基线与功能来源台账")
    };

    private readonly IServiceProvider _services;
    private readonly IFunctionCatalog _catalog;
    private readonly Dictionary<string, object?> _pages = new();
    private string _currentKey = "home";

    public MainViewModel(
        IServiceProvider services,
        IFunctionCatalog catalog,
        SelectionService selection,
        NavigationService navigation,
        ExecutionDialogViewModel execDialog)
    {
        _services = services;
        _catalog = catalog;
        Selection = selection;
        ExecDialog = execDialog;
        navigation.Main = this;

        NavItems.Add(new NavItemViewModel(this, "home", "首页", "\uE80F"));
        NavItems.Add(new NavItemViewModel(this, "optimize", "一键优化", "\uE945"));
        NavItems.Add(new NavItemViewModel(this, "settings", "系统设置", "\uE713"));
        NavItems.Add(new NavItemViewModel(this, "apps", "应用管理", "\uE71D"));
        NavItems.Add(new NavItemViewModel(this, "cleanup", "清理维护", "\uE74D"));
        NavItems.Add(new NavItemViewModel(this, "repair", "系统修复", "\uE90F"));
        NavItems.Add(new NavItemViewModel(this, "tools", "实用工具", "\uE950"));

        BottomNavItems.Add(new NavItemViewModel(this, "records", "任务记录", "\uE81C"));
        BottomNavItems.Add(new NavItemViewModel(this, "appsettings", "设置", "\uE713"));
        BottomNavItems.Add(new NavItemViewModel(this, "about", "关于", "\uE946"));

        Selection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SelectionService.Count) or nameof(SelectionService.HasHighRisk))
                UpdateBottomBar();
        };
        Selection.Selected.CollectionChanged += (_, _) => UpdateBottomBar();

        ExecDialog.TaskCompleted += record =>
        {
            (GetPage("home") as IRefreshable)?.Refresh();
            (GetPage("records") as IRefreshable)?.Refresh();
        };

        Navigate("home", null);
    }

    public ObservableCollection<NavItemViewModel> NavItems { get; } = new();
    public ObservableCollection<NavItemViewModel> BottomNavItems { get; } = new();
    public SelectionService Selection { get; }
    public ExecutionDialogViewModel ExecDialog { get; }

    [ObservableProperty]
    private object? currentPage;

    [ObservableProperty]
    private string pageTitle = "";

    [ObservableProperty]
    private string pageDesc = "";

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private bool searchOpen;

    [ObservableProperty]
    private ObservableCollection<SearchItemViewModel> searchResults = new();

    [ObservableProperty]
    private bool bottomBarVisible;

    [ObservableProperty]
    private string selectedLabelText = "已选择 0 项";

    public void Navigate(string key, object? param)
    {
        if (!Meta.TryGetValue(key, out var meta)) return;

        _currentKey = key;
        PageTitle = meta.Title;
        PageDesc = meta.Desc;

        foreach (var nav in NavItems) nav.IsActive = nav.Key == key;
        foreach (var nav in BottomNavItems) nav.IsActive = nav.Key == key;

        var page = GetPage(key);
        if (param is string category && page is SystemSettingsViewModel settings)
            settings.ApplyCategory(category);

        CurrentPage = page;
        UpdateBottomBar();
    }

    private object? GetPage(string key)
    {
        if (_pages.TryGetValue(key, out var cached)) return cached;
        var page = key switch
        {
            "home" => _services.GetService(typeof(HomeViewModel)),
            "optimize" => _services.GetService(typeof(OptimizeViewModel)),
            "settings" => _services.GetService(typeof(SystemSettingsViewModel)),
            "apps" => _services.GetService(typeof(AppsViewModel)),
            "cleanup" => _services.GetService(typeof(CleanupViewModel)),
            "repair" => _services.GetService(typeof(RepairViewModel)),
            "tools" => _services.GetService(typeof(ToolsViewModel)),
            "records" => _services.GetService(typeof(RecordsViewModel)),
            "appsettings" => _services.GetService(typeof(AppSettingsViewModel)),
            "about" => _services.GetService(typeof(AboutViewModel)),
            _ => null
        };
        if (page is not null) _pages[key] = page;
        return page;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchOpen = false;
            SearchResults.Clear();
            return;
        }
        var results = _catalog.Search(value).Select(f => new SearchItemViewModel(f)).ToArray();
        SearchResults = new ObservableCollection<SearchItemViewModel>(results);
        SearchOpen = results.Length > 0;
    }

    [RelayCommand]
    private void ChooseSearchResult(SearchItemViewModel item)
    {
        Navigate(item.PageKey, item.Item.Module == ModuleKind.Optimization ? item.Item.Category : null);
        SearchText = "";
        SearchOpen = false;
    }

    [RelayCommand]
    private void CloseSearch()
    {
        SearchText = "";
        SearchOpen = false;
    }

    [RelayCommand]
    private void ClearSelection() => Selection.Clear();

    [RelayCommand]
    private void NavigateToRecords() => Navigate("records", null);

    [RelayCommand]
    private async Task ExecuteSelectionAsync()
    {
        if (Selection.Count == 0) return;
        await ExecDialog.RunAsync(
            Selection.Selected.ToArray(),
            $"批量执行 {Selection.Count} 项",
            showConfirm: true);
    }

    public async Task RunSingleAsync(FunctionItem item)
        => await ExecDialog.RunAsync(new[] { item }, item.Name, showConfirm: false);

    private void UpdateBottomBar()
    {
        BottomBarVisible = Selection.Count > 0 && SelectionPages.Contains(_currentKey);
        SelectedLabelText = $"已选择 {Selection.Count} 项";
    }
}
