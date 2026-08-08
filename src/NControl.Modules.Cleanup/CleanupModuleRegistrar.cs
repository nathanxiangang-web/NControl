using NControl.Core;

namespace NControl.Modules.Cleanup;

/// <summary>
/// 清理模块:可删除数据、缓存、临时文件和维护任务(产品文档 §3.3)。
/// 清理项按来源分组;明确区分“可以安全清理”和“清理后可能影响排错/恢复”。
/// </summary>
public sealed class CleanupModuleRegistrar : IModuleRegistrar
{
    public string ModuleName => "清理模块";

    public void RegisterFeatures(IFunctionCatalog catalog)
    {
        // ---------- 系统临时文件 ----------
        catalog.Register(F("cleanup.user-temp", "用户临时目录", "系统临时文件",
            "清理当前用户 Temp 目录中的无用文件(部分正在使用的文件会自动跳过)。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-ChildItem -Path $env:TEMP -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("cleanup.windows-temp", "系统临时目录", "系统临时文件",
            "清理 C:\\Windows\\Temp 中的系统临时文件。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Get-ChildItem -Path 'C:\\Windows\\Temp' -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("cleanup.prefetch", "预读取文件(Prefetch)", "系统临时文件",
            "清理系统预读取文件;系统会在需要时重新生成。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Get-ChildItem -Path 'C:\\Windows\\Prefetch' -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue"));

        // ---------- 更新残留 ----------
        catalog.Register(F("cleanup.update-cache", "Windows 更新缓存", "更新残留",
            "清理已下载但不再需要的更新文件(SoftwareDistribution\\Download)。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Stop-Service wuauserv -Force -ErrorAction SilentlyContinue; Get-ChildItem -Path 'C:\\Windows\\SoftwareDistribution\\Download' -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue; Start-Service wuauserv -ErrorAction SilentlyContinue"));

        // ---------- 应用缓存 ----------
        catalog.Register(F("cleanup.thumbnails", "缩略图缓存", "应用缓存",
            "删除资源管理器缩略图缓存,系统会在需要时重新生成。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-ChildItem -Path \"$env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\" -Filter 'thumbcache_*' -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("cleanup.d3d-shader-cache", "DirectX 着色器缓存", "应用缓存",
            "删除 DirectX 着色器缓存,部分游戏首次启动可能稍慢。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Get-ChildItem -Path \"$env:LOCALAPPDATA\\D3DSCache\" -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"));
        catalog.Register(F("cleanup.edge-cache", "Edge 浏览器缓存", "应用缓存",
            "清理 Edge 浏览器网页缓存;已打开的页面数据不受影响,浏览历史保留。",
            RiskLevel.Caution, false, RestartRequirement.None,
            "Get-ChildItem -Path \"$env:LOCALAPPDATA\\Microsoft\\Edge\\User Data\\Default\\Cache\" -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"));

        // ---------- 日志与报告 ----------
        catalog.Register(F("cleanup.wer-reports", "Windows 错误报告", "日志与报告",
            "删除历史错误报告与崩溃转储;清理后可能影响近期故障的排错依据。",
            RiskLevel.Caution, true, RestartRequirement.None,
            "Get-ChildItem -Path 'C:\\ProgramData\\Microsoft\\Windows\\WER' -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue"));

        // ---------- 回收站 ----------
        catalog.Register(F("cleanup.recycle-bin", "清空回收站", "回收站",
            "清空回收站中的全部内容,删除后无法从回收站恢复。",
            RiskLevel.Safe, false, RestartRequirement.None,
            "Clear-RecycleBin -Force -ErrorAction SilentlyContinue"));
    }

    public void RegisterPresets(IFunctionCatalog catalog)
    {
        // 清理模块不提供批量预设,由页面勾选并查看真实扫描结果后统一执行。
    }

    private static FunctionItem F(
        string id, string name, string category, string description, RiskLevel risk,
        bool admin, RestartRequirement restart, string command) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        Module = ModuleKind.Cleanup,
        Description = description,
        Risk = risk,
        RequiresAdmin = admin,
        Restart = restart,
        Source = "自研 · PowerShell 清理脚本",
        Command = command
    };
}
