using System.Text.Json;
using System.Text.RegularExpressions;

namespace NControl.Core;

/// <summary>
/// 清理扫描结果(第二代 §9):一个清理项的扫描统计。
/// </summary>
public sealed class CleanupScanItem
{
    public required string FunctionId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }

    /// <summary>文件/条目数量(能可靠统计时)。</summary>
    public long ItemCount { get; set; }

    /// <summary>占用空间字节数。</summary>
    public long SizeBytes { get; set; }

    /// <summary>扫描是否成功。</summary>
    public bool Ok { get; set; }

    /// <summary>错误/说明(无权限/路径不存在等)。</summary>
    public string? Note { get; set; }

    /// <summary>格式化大小文本。</summary>
    public string SizeText => FormatSize(SizeBytes);

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1L << 30) return $"{bytes / (double)(1L << 30):F2} GB";
        if (bytes >= 1L << 20) return $"{bytes / (double)(1L << 20):F1} MB";
        if (bytes >= 1L << 10) return $"{bytes / (double)(1L << 10):F0} KB";
        return $"{bytes} B";
    }
}

/// <summary>
/// 清理扫描服务(第二代 §9):对清理功能项计算可清理文件数与空间大小。
/// 原则:扫描数据必须真实;不为显示数字做过度扫描(§9)。
/// </summary>
public interface ICleanupScanner
{
    /// <summary>扫描单个清理项。返回 null 表示无法扫描(命令不支持统计)。</summary>
    Task<CleanupScanItem?> ScanAsync(FunctionItem item, CancellationToken ct = default);

    /// <summary>批量扫描。返回可扫描项结果。</summary>
    Task<IReadOnlyList<CleanupScanItem>> ScanManyAsync(IEnumerable<FunctionItem> items, IProgress<string>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// 清理扫描默认实现:从清理命令提取路径,统计文件数与大小。
/// 提取规则:Get-ChildItem -Path 'X' [-Recurse] → 统计 X 下条目。
/// </summary>
public sealed class CleanupScanner : ICleanupScanner
{
    private readonly IExecutionProvider _provider;

    // Get-ChildItem -Path 'X' 或 -Path "X" 或 -Path $env:TEMP
    private static readonly Regex PathPattern = new(
        @"Get-ChildItem\s+-Path\s+(?:['""]([^'""]+)['""]|(\$env:\w+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CleanupScanner(IEnumerable<IExecutionProvider> providers)
    {
        _provider = providers.FirstOrDefault(p => p.CanHandle(ExecutionKind.PowerShell))
            ?? throw new InvalidOperationException("缺少 PowerShell 执行提供程序");
    }

    public async Task<CleanupScanItem?> ScanAsync(FunctionItem item, CancellationToken ct = default)
    {
        var m = PathPattern.Match(item.Command ?? "");
        if (!m.Success) return null; // 无法提取路径 → 不扫描

        var path = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        var isEnvVar = path.StartsWith("$env:", StringComparison.OrdinalIgnoreCase);
        var recurse = item.Command?.Contains("-Recurse", StringComparison.OrdinalIgnoreCase) == true;

        var body = recurse
            ? "$p={0}; if (Test-Path $p) {{ $items = Get-ChildItem -Path $p -Recurse -Force -ErrorAction SilentlyContinue; $count = @($items).Count; $size = ($items | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum; @{{Count=$count;Size=[long]$size}} | ConvertTo-Json -Compress }} else {{ '{{\"Count\":-1,\"Size\":0}}' }}"
            : "$p={0}; if (Test-Path $p) {{ $items = Get-ChildItem -Path $p -Force -ErrorAction SilentlyContinue; $count = @($items).Count; $size = ($items | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum; @{{Count=$count;Size=[long]$size}} | ConvertTo-Json -Compress }} else {{ '{{\"Count\":-1,\"Size\":0}}' }}";
        var pathArg = isEnvVar ? path : "'" + path + "'";
        var measureCmd = string.Format(body, pathArg);

        var probe = new FunctionItem
        {
            Id = item.Id + ".scan",
            Name = "扫描:" + item.Name,
            Category = item.Category,
            Module = ModuleKind.Cleanup,
            Description = "",
            Risk = RiskLevel.Safe,
            RequiresAdmin = false,
            Restart = RestartRequirement.None,
            Source = "自研 · 清理扫描",
            Kind = ExecutionKind.PowerShell,
            Command = measureCmd,
            TimeoutSeconds = 60
        };

        try
        {
            var result = await _provider.ExecuteAsync(probe, null, ct);
            var output = (result.Output ?? "").Trim();
            var json = ExtractJson(output);
            if (json is null)
                return new CleanupScanItem { FunctionId = item.Id, Name = item.Name, Category = item.Category, Ok = false, Note = "扫描无输出" };

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            long count = root.GetProperty("Count").GetInt64();
            long size = root.GetProperty("Size").GetInt64();

            if (count == -1)
                return new CleanupScanItem { FunctionId = item.Id, Name = item.Name, Category = item.Category, Ok = true, ItemCount = 0, SizeBytes = 0, Note = "路径不存在" };

            return new CleanupScanItem
            {
                FunctionId = item.Id,
                Name = item.Name,
                Category = item.Category,
                Ok = true,
                ItemCount = count,
                SizeBytes = size
            };
        }
        catch (Exception ex)
        {
            return new CleanupScanItem { FunctionId = item.Id, Name = item.Name, Category = item.Category, Ok = false, Note = ex.Message };
        }
    }

    public async Task<IReadOnlyList<CleanupScanItem>> ScanManyAsync(IEnumerable<FunctionItem> items, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var results = new List<CleanupScanItem>();
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"正在扫描:{item.Name}…");
            var r = await ScanAsync(item, ct);
            if (r is not null) results.Add(r);
        }
        return results;
    }

    private static string? ExtractJson(string output)
    {
        var idx = output.IndexOf('{');
        if (idx < 0) return null;
        return output[idx..];
    }
}
