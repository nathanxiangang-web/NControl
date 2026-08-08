using NControl.Core;

namespace NControl.Modules.Tools;

/// <summary>
/// 工具模块:查看、诊断或操作日常 Windows 管理对象(产品文档 §3.3)。
/// 查看类工具与修改类工具在视觉和确认方式上区分;即时工具以查看/诊断类为主。
/// </summary>
public sealed class ToolsModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "工具模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        // ---------- 网络 ----------
        catalog.Register(F("tools.ping", "网络连通性测试(Ping)", "网络",
            "向公共 DNS(223.5.5.5)发送 4 次 Ping,检查网络连通与延迟。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "ping -n 4 223.5.5.5", isTool: true));
        catalog.Register(F("tools.dns-lookup", "DNS 查询", "网络",
            "查询 www.microsoft.com 的 DNS 解析结果。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Resolve-DnsName www.microsoft.com | Format-Table -AutoSize", isTool: true));
        catalog.Register(F("tools.netstat-ports", "端口占用查看", "网络",
            "查看当前正在监听的端口及对应进程(前 40 条)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "netstat -ano | Select-Object -First 40", isTool: true));
        catalog.Register(F("tools.route-table", "路由表查看", "网络",
            "查看当前 IPv4 路由表。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "route print -4", isTool: true));

        // ---------- 系统 ----------
        catalog.Register(F("tools.system-info", "系统信息查看", "系统",
            "查看系统版本、硬件与补丁信息(耗时约 10-30 秒)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "systeminfo", isTool: true, timeoutSeconds: 240));
        catalog.Register(F("tools.startup-items", "启动项查看", "系统",
            "查看当前登录启动项(注册表与启动文件夹)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-CimInstance Win32_StartupCommand | Select-Object Name, Command, Location | Format-Table -AutoSize", isTool: true));
        catalog.Register(F("tools.services-status", "服务状态查看", "系统",
            "查看系统服务当前状态(运行中/已停止)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-Service | Sort-Object Status, Name | Select-Object Status, Name, DisplayName | Format-Table -AutoSize", isTool: true));
        catalog.Register(F("tools.scheduled-tasks", "计划任务查看", "系统",
            "查看已注册的计划任务(前 60 条)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-ScheduledTask | Select-Object TaskName, State | Select-Object -First 60 | Format-Table -AutoSize", isTool: true));
        catalog.Register(F("tools.env-vars", "环境变量查看", "系统",
            "查看用户级与系统级环境变量。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "'--- 用户环境变量 ---'; [Environment]::GetEnvironmentVariables('User') | Format-Table -AutoSize; '--- 系统环境变量 ---'; [Environment]::GetEnvironmentVariables('Machine') | Format-Table -AutoSize", isTool: true));
        catalog.Register(F("tools.optional-features", "Windows 可选功能列表", "系统",
            "查看已安装/未安装的可选功能(WSL、Hyper-V、沙盒等);需要管理员权限。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Get-WindowsOptionalFeature -Online | Select-Object FeatureName, State | Format-Table -AutoSize", isTool: true, timeoutSeconds: 300));
    }

    public void RegisterPresets(IFunctionCatalog catalog)
    {
        // 工具模块不提供预设。
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string command, bool isTool, int timeoutSeconds = 120) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Tools,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = "Windows 系统内置工具",
        Command = command,
        IsTool = isTool,
        TimeoutSeconds = timeoutSeconds
    };
}
