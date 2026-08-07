using NControl.Core;

namespace NControl.Modules.Repair;

/// <summary>
/// 修复模块:针对故障恢复系统、更新、商店和网络能力(产品文档 §3.3)。
/// 按问题组织,而不是只展示 DISM、SFC 等命令名称。
/// </summary>
public sealed class RepairModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "修复模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        // ---------- 系统映像与文件 ----------
        // 合并项:连续执行 DISM 扫描健康 → 修复映像 → SFC 扫描(常规修复顺序,耗时较长)
        catalog.Register(F("repair.system-integrity", "系统映像与文件修复", "系统映像与文件",
            "连续执行:① DISM 扫描映像健康 → ② DISM 修复映像 → ③ SFC 扫描系统文件。适用于系统文件损坏、更新失败、蓝屏等场景,耗时较长(建议预留 20-30 分钟)。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "DISM.exe /Online /Cleanup-Image /ScanHealth; DISM.exe /Online /Cleanup-Image /RestoreHealth; sfc.exe /scannow", 5400));

        // ---------- 更新与商店 ----------
        catalog.Register(F("repair.update-reset", "重置 Windows 更新组件", "更新与商店",
            "停止更新相关服务,重命名更新缓存目录后重新启动服务;修复更新卡住、下载失败等问题。",
            RiskLevel.Caution, true, RestartRequirement.Reboot,
            "Stop-Service wuauserv,bits,cryptsvc -Force -ErrorAction SilentlyContinue; Rename-Item 'C:\\Windows\\SoftwareDistribution' 'SoftwareDistribution.old' -Force -ErrorAction SilentlyContinue; Rename-Item 'C:\\Windows\\System32\\catroot2' 'catroot2.old' -Force -ErrorAction SilentlyContinue; Start-Service wuauserv,bits,cryptsvc -ErrorAction SilentlyContinue"));
        catalog.Register(F("repair.store-reset", "重置 Microsoft Store 缓存", "更新与商店",
            "运行 wsreset 重置商店缓存;修复商店无法打开或下载失败的问题。",
            RiskLevel.Caution, false, RestartRequirement.None,
            "Start-Process -FilePath 'wsreset.exe' -Wait"));
        catalog.Register(F("repair.store-reregister", "重新注册 Microsoft Store", "更新与商店",
            "重新注册商店应用组件;修复商店无法启动或反复闪退的问题。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Get-AppxPackage -AllUsers Microsoft.WindowsStore | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -ErrorAction SilentlyContinue }"));

        // ---------- 网络 ----------
        catalog.Register(F("repair.network-dns-flush", "清理 DNS 缓存", "网络",
            "刷新本地 DNS 缓存;适用于网页解析错误、域名不生效等场景。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "ipconfig /flushdns"));
        catalog.Register(F("repair.network-winsock", "重置 Winsock 协议栈", "网络",
            "重置 Winsock 目录;适用于无法上网、网络连接异常等场景,可能需要重启。",
            RiskLevel.Caution, true, RestartRequirement.Reboot,
            "netsh winsock reset"));
        catalog.Register(F("repair.network-tcpip", "重置 TCP/IP 协议栈", "网络",
            "重置 IP 协议栈配置;适用于 IP 配置损坏导致的网络故障,可能需要重启。",
            RiskLevel.Caution, true, RestartRequirement.Reboot,
            "netsh int ip reset"));
        catalog.Register(F("repair.network-ip-renew", "重新获取 IP 地址", "网络",
            "释放并重新获取 IP 地址;执行期间网络会短暂中断。",
            RiskLevel.Caution, false, RestartRequirement.None,
            "ipconfig /release; ipconfig /renew"));
    }

    public void RegisterPresets(IFunctionCatalog catalog)
    {
        // 修复模块第一代以单项/多步任务执行,不提供批量预设。
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string command, int timeoutSeconds = 180) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Repair,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = "Windows 系统内置工具",
        Command = command,
        TimeoutSeconds = timeoutSeconds
    };
}
