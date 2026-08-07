using NControl.Core;
using NControl.Presentation.ViewModels;

namespace NControl.Presentation.Services;

/// <summary>
/// 页面导航服务:避免页面 ViewModel 直接依赖 MainViewModel 造成构造期循环依赖。
/// MainViewModel 构造完成后注册自身;页面通过本服务发起导航与单任务执行。
/// </summary>
public sealed class NavigationService
{
    private readonly IEnumerable<IExecutionProvider>? _providers;

    public NavigationService() { }

    public NavigationService(IEnumerable<IExecutionProvider> providers)
    {
        _providers = providers;
    }

    public MainViewModel? Main { get; set; }

    public void Navigate(string key, object? param) => Main?.Navigate(key, param);

    public Task RunSingleAsync(FunctionItem item)
        => Main is null ? Task.CompletedTask : Main.RunSingleAsync(item);

    /// <summary>
    /// 控制台窗口执行:不经过执行弹窗,直接在独立控制台窗口运行命令并显示进度。
    /// 用于 DISM/SFC 等长耗时修复(UseConsoleWindow=true 的项)。
    /// </summary>
    public async Task RunConsoleAsync(FunctionItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Command)) return;
        var provider = _providers?.FirstOrDefault(p => p.CanHandle(item.Kind));
        if (provider is null) return;
        await provider.ExecuteAsync(item, null, CancellationToken.None);
    }
}
