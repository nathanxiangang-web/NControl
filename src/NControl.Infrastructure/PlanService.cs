using System.Text.Json;
using System.Text.Encodings.Web;
using NControl.Core;

namespace NControl.Infrastructure;

/// <summary>
/// 配置方案服务默认实现:JSON 文件存储于 %LocalAppData%\NControl\plans\(第二代 §5-§6)。
/// 导入流程:解析 → Schema 检查 → FunctionId 匹配 → 兼容性检查 → 风险检查(§6)。
/// </summary>
public sealed class PlanService : IPlanService
{
    private readonly string _plansDir;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public PlanService(AppPaths paths)
    {
        _plansDir = Path.Combine(paths.DataFolder, "plans");
        Directory.CreateDirectory(_plansDir);
    }

    public void Save(PlanConfig config)
    {
        var filePath = Path.Combine(_plansDir, $"{SanitizeFileName(config.Name)}.json");
        config.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.WriteAllText(filePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public IReadOnlyList<PlanConfig> GetAll()
    {
        if (!Directory.Exists(_plansDir)) return Array.Empty<PlanConfig>();
        return Directory.GetFiles(_plansDir, "*.json")
            .Select(f =>
            {
                try
                {
                    return JsonSerializer.Deserialize<PlanConfig>(File.ReadAllText(f), JsonOptions);
                }
                catch
                {
                    return null;
                }
            })
            .Where(c => c is not null)
            .Cast<PlanConfig>()
            .ToList();
    }

    public void Delete(string name)
    {
        var filePath = Path.Combine(_plansDir, $"{SanitizeFileName(name)}.json");
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    public void Export(PlanConfig config, string filePath)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public PlanImportResult Import(string filePath, IFunctionCatalog catalog, CompatibilityEngine compat)
    {
        var result = new PlanImportResult();

        // 1. 解析
        PlanConfig? config;
        try
        {
            var text = File.ReadAllText(filePath);
            config = JsonSerializer.Deserialize<PlanConfig>(text, JsonOptions);
        }
        catch (Exception ex)
        {
            result.ParseOk = false;
            result.ParseError = $"配置格式非法:{ex.Message}";
            return result;
        }
        if (config is null || string.IsNullOrEmpty(config.Name))
        {
            result.ParseOk = false;
            result.ParseError = "配置缺少名称字段";
            return result;
        }
        result.ParseOk = true;

        // 2. Schema 检查
        if (config.SchemaVersion > 1)
            result.ParseError = $"配置格式版本({config.SchemaVersion})高于当前支持版本(1),部分内容可能无法解析";
        else if (config.SchemaVersion < 1)
            result.SchemaHint = $"配置格式版本较旧({config.SchemaVersion}),已尝试兼容解析";

        // 3. FunctionId 匹配(自动去重)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueIds = new List<string>();
        foreach (var id in config.Functions ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!seen.Add(id))
            {
                result.DuplicateCount++;
                continue;
            }
            uniqueIds.Add(id);
        }

        // 4. 兼容性 + 风险检查
        foreach (var id in uniqueIds)
        {
            var item = catalog.Find(id);
            if (item is null)
            {
                result.UnknownIds.Add(id);
                continue;
            }

            var compatResult = compat.Evaluate(item);
            var imported = new ImportedFunction
            {
                FunctionId = id,
                Name = item.Name,
                Risk = item.Risk,
                CompatStatus = compatResult.Status,
                CompatReason = compatResult.Reason
            };

            if (compatResult.Status == CompatibilityStatus.Unsupported)
            {
                result.Unsupported.Add(imported);
            }
            else if (item.Risk == RiskLevel.HighRisk)
            {
                result.HighRisk.Add(imported);
            }
            else
            {
                result.Ready.Add(imported);
            }
        }

        result.Config = config;
        return result;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "未命名方案" : safe;
    }
}
