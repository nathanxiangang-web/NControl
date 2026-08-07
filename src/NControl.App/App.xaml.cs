using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NControl.Core;
using NControl.Infrastructure;
using NControl.Modules.Applications;
using NControl.Modules.Cleanup;
using NControl.Modules.Optimization;
using NControl.Modules.Repair;
using NControl.Modules.Tools;
using NControl.Presentation.Services;
using NControl.Presentation.ViewModels;

namespace NControl.App;

/// <summary>
/// 应用入口:Host + DI + 日志 + 模块注册(产品文档 §0.3 技术基线)。
/// WPF 只负责界面;可执行功能统一经过执行中心。
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    internal static void Trace(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), "ncontrol-startup.log"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Trace("OnStartup begin");

        // 全局异常兜底:记录完整异常,避免闪退(产品文档 §5.4 错误与中断)
        DispatcherUnhandledException += (_, args) =>
        {
            Trace($"DispatcherUnhandledException: {args.Exception}");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Trace($"AppDomain UnhandledException: {args.ExceptionObject}");
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Trace($"UnobservedTaskException: {args.Exception}");
            args.SetObserved();
        };

        var builder = Host.CreateApplicationBuilder();
        Trace("builder created");
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            optional: true,
            reloadOnChange: false);
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        // 基础设施
        builder.Services.AddSingleton<AppPaths>();
        builder.Services.AddSingleton<IFunctionCatalog, FunctionCatalog>();
        builder.Services.AddSingleton<ITaskRecordStore, SqliteTaskRecordStore>();
        builder.Services.AddSingleton<IExecutionProvider, PowerShellExecutionProvider>();
        builder.Services.AddSingleton<IExecutionProvider, CommandExecutionProvider>();
        builder.Services.AddSingleton<IExecutionCenter, ExecutionCenter>();
builder.Services.AddSingleton<IEnvironmentProbe, WindowsEnvironmentProbe>();
builder.Services.AddSingleton<CompatibilityEngine>();
builder.Services.AddSingleton<IPlanService, PlanService>();
builder.Services.AddSingleton<RollbackService>();

        // 业务模块
        builder.Services.AddSingleton<IModuleRegistrar, OptimizationModuleRegistrar>();
        builder.Services.AddSingleton<IModuleRegistrar, ApplicationsModuleRegistrar>();
        builder.Services.AddSingleton<IModuleRegistrar, CleanupModuleRegistrar>();
        builder.Services.AddSingleton<IModuleRegistrar, RepairModuleRegistrar>();
        builder.Services.AddSingleton<IModuleRegistrar, ToolsModuleRegistrar>();

        // 表现层
        builder.Services.AddSingleton<SelectionService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<ExecutionDialogViewModel>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<OptimizeViewModel>();
        builder.Services.AddSingleton<SystemSettingsViewModel>();
        builder.Services.AddSingleton<AppsViewModel>();
        builder.Services.AddSingleton<CleanupViewModel>();
        builder.Services.AddSingleton<RepairViewModel>();
        builder.Services.AddSingleton<ToolsViewModel>();
        builder.Services.AddSingleton<RecordsViewModel>();
        builder.Services.AddSingleton<AppSettingsViewModel>();
        builder.Services.AddSingleton<AboutViewModel>();

        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        Trace("host built");
        _host.Start();
        Trace("host started");

        // 两阶段登记:先功能,再方案(方案可能跨模块引用功能 Id)
        var catalog = _host.Services.GetRequiredService<IFunctionCatalog>();
        var registrars = _host.Services.GetServices<IModuleRegistrar>().ToArray();
        foreach (var registrar in registrars)
            registrar.RegisterFeatures(catalog);
        foreach (var registrar in registrars)
            registrar.RegisterPresets(catalog);

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("NControl 启动完成:功能目录 {Count} 项,方案 {PresetCount} 个",
            catalog.All.Count, catalog.Presets.Count);
        Trace($"catalog registered: {catalog.All.Count} items, {catalog.Presets.Count} presets");

        var window = _host.Services.GetRequiredService<MainWindow>();
        Trace("window resolved");
        MainWindow = window;
        window.Show();
        Trace("window shown");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            _host?.Dispose();
        }
        catch
        {
            // 退出阶段不阻塞关闭
        }
        base.OnExit(e);
    }
}
