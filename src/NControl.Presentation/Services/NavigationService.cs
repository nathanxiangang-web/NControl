using NControl.Core;
using NControl.Presentation.ViewModels;

namespace NControl.Presentation.Services;

/// <summary>
/// 页面导航服务:避免页面 ViewModel 直接依赖 MainViewModel 造成构造期循环依赖。
/// MainViewModel 构造完成后注册自身;页面通过本服务发起导航与单任务执行。
/// </summary>
public sealed class NavigationService
{
    public MainViewModel? Main { get; set; }

    public void Navigate(string key, object? param) => Main?.Navigate(key, param);

    public Task RunSingleAsync(FunctionItem item)
        => Main is null ? Task.CompletedTask : Main.RunSingleAsync(item);
}
