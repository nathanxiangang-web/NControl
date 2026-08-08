using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NControl.Core;

namespace NControl.Presentation.ViewModels;

/// <summary>关于页:版本、技术基线与功能来源台账(产品文档 §8.3)。</summary>
public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel()
    {
        foreach (var record in SourceLedger.Records)
            Sources.Add(new SourceRowViewModel(record));
    }

    public string Version => typeof(AboutViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    public string ProductLine => "NControl · Windows 控制中心(第一代功能型测试版本)";

    public IReadOnlyList<string> Baseline { get; } = new[]
    {
        "开发语言:C#(.NET 10 LTS,目标 Windows 10/11 x64)",
        "桌面 UI:WPF + XAML,MVVM(CommunityToolkit.Mvvm)",
        "基础设施:Microsoft.Extensions.Hosting / DI / Logging",
        "本地配置:JSON;任务记录:SQLite",
        "功能执行:C# 原生能力 + PowerShell + Windows 系统命令",
        "产品结构:Core → Modules → Execution/Infrastructure → Integrations"
    };

    public IReadOnlyList<string> Gen1Scope { get; } = new[]
    {
        "第一代为功能型测试版本,不宣称覆盖所有电脑或保证性能提升。",
        "不展示虚构健康评分与性能百分比。",
        "高风险功能不进任何推荐方案;来源与许可证台账完整。"
    };

    public ObservableCollection<SourceRowViewModel> Sources { get; } = new();
}
