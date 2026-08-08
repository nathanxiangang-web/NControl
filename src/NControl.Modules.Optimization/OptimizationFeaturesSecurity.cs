using NControl.Core;

namespace NControl.Modules.Optimization;

/// <summary>
/// 安全设置:与《ZyperWin++ 当前功能统计表》(ZyperData.xml, 2026-08-02)逐项对齐。
/// 名称/说明/分类均采用文档原文;实现参考 ZyperWin++ 4.2 源码的注册表操作。
/// 安全设置整体按产品文档 §7.2 治理:全部标记 HighRisk,不进任何预设,需用户明确勾选。
/// </summary>
public static class OptimizationFeaturesSecurity
{
    public static void Register(IFunctionCatalog catalog)
    {
        // ===== 文档:安全设置(10 项)=====

        // 1、将用户账号控制（UAC）调整为从不通知
        catalog.Register(F("advanced.disable-uac-notifications", "将用户账号控制（UAC）调整为从不通知", "安全设置",
            "按需开启：免去提权弹窗，需防误操作", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'PromptOnSecureDesktop' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'ConsentPromptBehaviorUser' -Value 3 -Type DWord -Force"));

        // 2、用于内置管理员账户的管理审批模式
        catalog.Register(F("advanced.admin-filter-token", "用于内置管理员账户的管理审批模式", "安全设置",
            "按需开启：提升安全，但会增加确认弹窗", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'FilterAdministratorToken' -Value 1 -Type DWord -Force"));

        // 3、以管理审批模式运行所有管理员
        catalog.Register(F("advanced.admin-enable-lua", "以管理审批模式运行所有管理员", "安全设置",
            "按需开启：提升安全，但会增加提权确认", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableLUA' -Value 1 -Type DWord -Force"));

        // 4、仅提升安全路径下的UIAccess程序
        catalog.Register(F("advanced.secure-uia-paths", "仅提升安全路径下的UIAccess程序", "安全设置",
            "按需开启：提升安全，部分工具可能受限", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableSecureUIAPaths' -Value 1 -Type DWord -Force"));

        // 5、允许UIAccess程序在非安全桌面上提升
        catalog.Register(F("advanced.uia-nonsecure-desktop", "允许UIAccess程序在非安全桌面上提升", "安全设置",
            "按需开启：兼容辅助工具，安全性略降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableUIADesktopToggle' -Value 1 -Type DWord -Force"));

        // 6、关闭SmartScreen应用筛选器
        catalog.Register(F("advanced.disable-smartscreen", "关闭SmartScreen应用筛选器", "安全设置",
            "按需开启：减少误报拦截，下载需自行判断", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer' -Name 'SmartScreenEnabled' -Value 'off' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\Windows Defender\\SmartScreen' -Name 'EnableSmartScreen' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Policies\\Microsoft\\MicrosoftEdge\\PhishingFilter' -Name 'EnabledV9' -Value 0 -Type DWord -Force"));

        // 7、关闭打开程序的安全警告
        catalog.Register(F("advanced.disable-open-security-warning", "关闭打开程序的安全警告", "安全设置",
            "按需开启：打开程序不再警告，需自行辨别", RiskLevel.HighRisk, false, RestartRequirement.None,
            "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force; Set-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Associations' -Name 'ModRiskFileTypes' -Value '.bat;.exe;.reg;.vbs;.chm;.msi;.js;.cmd' -Type String -Force"));

        // 8、关闭防火墙
        catalog.Register(F("advanced.disable-firewall", "关闭防火墙", "安全设置",
            "按需开启：有第三方防护或纯内网时可关", RiskLevel.HighRisk, true, RestartRequirement.None,
            "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force; Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile' -Name 'EnableFirewall' -Value 0 -Type DWord -Force"));

        // 9、关闭内存完整
        catalog.Register(F("advanced.disable-memory-integrity", "关闭内存完整", "安全设置",
            "按需开启：兼容性和性能提高，防护下降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 0 -Type DWord -Force"));

        // 10、关闭虚拟化安全性
        catalog.Register(F("advanced.disable-vbs", "关闭虚拟化安全性", "安全设置",
            "按需开启：性能可能提高，隔离防护下降", RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard' -Name 'EnableVirtualizationBasedSecurity' -Value 0 -Type DWord -Force"));

        // ===== 扩展:彻底禁用 Windows 安全中心 / Defender =====
        // 按用户要求不提供恢复:调用 SuperUser 执行 ZyperWin++ 风格 DEFENDER.CMD Disable,
        // 处理安全服务/驱动、进程、计划任务、实时防护、SmartScreen 和 AMSI 相关项。
        // 运行前校验 SuperUser 固定 SHA-256;执行后验证核心服务 Start=4。
        catalog.Register(F("advanced.disable-security-center", "彻底禁用安全中心 / Defender（不可恢复）", "安全设置",
            "以 TrustedInstaller 权限直接禁用 Windows 安全中心与 Microsoft Defender，自动关闭篡改保护，并处理相关服务/驱动、进程、实时防护、计划任务、SmartScreen 和 AMSI 配置。\n\n⛔ 不可恢复：NControl 不提供恢复按钮，任务记录也无法回滚此项。\n⚠ 执行前：请先创建完整系统镜像/快照；不需要手动关闭“篡改保护”。\n⚠ 影响：系统将失去 Defender 实时防护、安全状态通知、SmartScreen 和部分 AMSI 检查；需重启后完全生效。",
            RiskLevel.HighRisk, true, RestartRequirement.Reboot,
            DisableSecurityCenterCommand,
            source: "ZyperWin++ 方案适配 · SuperUser + DEFENDER.CMD"));
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string command, string? restoreCommand = null,
        string source = "ZyperWin++ 适配 · 注册表/系统命令(2026-08-02 文档对齐)") => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Optimization,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = source,
        Command = command,
        RestoreCommand = restoreCommand
    };

    private const string DisableSecurityCenterCommand = """
        if ([string]::IsNullOrWhiteSpace($env:NCONTROL_APP_BASE)) { Write-Error 'NControl 程序目录未传递,已取消高权限操作。'; exit 2 };
        $root = Join-Path $env:NCONTROL_APP_BASE 'Tools\SecurityCenter';
        $helperName = if ([Environment]::Is64BitOperatingSystem) { 'SuperUser64.exe' } else { 'SuperUser32.exe' };
        $helper = Join-Path $root $helperName;
        $controller = Join-Path $root 'KILLSECURITYCENTER.CMD';
        $expectedHash = if ([Environment]::Is64BitOperatingSystem) { 'E7272643420F4C9EFB0CAB9C1F56A61709EE56E9A3ED1A6E5EF9A960AE988236' } else { 'ABBA43DEF7A9F4894AC8B5617932F2860D1C0A79F427ACCCF6667E671FF4A135' };
        if (-not (Test-Path -LiteralPath $helper) -or -not (Test-Path -LiteralPath $controller)) { Write-Error '安全中心控制组件缺失,请重新安装 NControl。'; exit 2 };
        if ((Get-FileHash -LiteralPath $helper -Algorithm SHA256).Hash -ne $expectedHash) { Write-Error 'SuperUser 校验失败,为防止执行被替换的高权限程序,已取消操作。'; exit 3 };
        $payloadHashes = @{
            'KILLSECURITYCENTER.CMD' = '679ED7BC9840A862DA18627270EB5D73E0C5BC5B50458F88A3DAEC74BB78D859';
            'DEFENDER.CMD' = '7E5D754BF8E401A2D07357210B742248779EC91D5D2E6E31F5FB54B29D652BD9';
            'WINDOWS DEFENDER CACHE MAINTENANCE.XML' = '61C05E126CF7DD4E860C9C126E92B8AC8A801959A552F92571A1A59E71F7C37E';
            'WINDOWS DEFENDER CLEANUP.XML' = '0A8ED5599765119DF68D92CBAA6CE9D185CD5B5C77E2CD9847AE081A39FF96D6';
            'WINDOWS DEFENDER SCHEDULED SCAN.XML' = '3BDBBC5916A8E8F98D2A3A6FB5CF7ADB7BAFD8F24A705BD789E84B8F5791EC68';
            'WINDOWS DEFENDER VERIFICATION.XML' = '64F713042816E7342CB36AFBA7322389B06C3299DAA58E9D6009FFBCC32195AC';
        };
        foreach ($entry in $payloadHashes.GetEnumerator()) {
            $payload = Join-Path $root $entry.Key;
            if (-not (Test-Path -LiteralPath $payload) -or (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne $entry.Value) { Write-Error ("安全中心脚本/模板缺失或已被替换:" + $entry.Key); exit 3 };
        };
        $controllerLiteral = $controller.Replace("'", "''");
        $innerCommand = "& '$controllerLiteral' 'Disable'; exit `$LASTEXITCODE";
        $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($innerCommand));
        $trustedInstallerCommand = "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $encodedCommand";
        & $helper /w /c $trustedInstallerCommand;
        $code = $LASTEXITCODE;
        if ($code -ne 0) { Write-Error "彻底禁用安全中心/Defender 失败,SuperUser 启动退出码 $code。"; exit $code };
        $targets = @('WinDefend','wscsvc','WdNisSvc','SecurityHealthService') | Where-Object { Test-Path -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Services\$_" };
        $notDisabled = $targets | Where-Object { (Get-ItemPropertyValue -LiteralPath "HKLM:\SYSTEM\CurrentControlSet\Services\$_" -Name Start -ErrorAction Stop) -ne 4 };
        if ($targets.Count -lt 2 -or $notDisabled.Count -gt 0) { Write-Error ('禁用后验证失败: ' + ($notDisabled -join ',')); exit 4 };
        $tamperPath = 'HKLM:\SOFTWARE\Microsoft\Windows Defender\Features';
        if ((Test-Path -LiteralPath $tamperPath) -and (Get-ItemPropertyValue -LiteralPath $tamperPath -Name TamperProtection -ErrorAction SilentlyContinue) -ne 4) { Write-Error '禁用后验证失败:TamperProtection 未关闭。'; exit 5 };
        Write-Output '安全中心/Defender 核心服务已设为禁用;请重启系统完成操作。';
        """;
}
