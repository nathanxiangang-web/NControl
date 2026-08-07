using System.Text.Json;
using System.Text.Json.Serialization;

namespace NControl.Core;

/// <summary>
/// 用户配置方案(第二代 §5):只引用已登记 FunctionId,不携带任何代码/脚本/路径。
/// 配置具备未来社区可分享性(预留作者/标签/场景字段,当前可空)。
/// </summary>
public sealed class PlanConfig
{
    /// <summary>配置格式版本(独立于软件版本,§12.2)。</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>方案名称。</summary>
    public required string Name { get; set; }

    /// <summary>方案说明。</summary>
    public string Description { get; set; } = "";

    /// <summary>创建工具标识。</summary>
    public string CreatedWith { get; set; } = "NControl";

    /// <summary>引用的功能项 Id 列表(长期契约,§12.1)。</summary>
    public List<string> Functions { get; set; } = new();

    // ---- 未来社区字段(当前可空,§7) ----
    public string? Tags { get; set; }
    public string? Scenario { get; set; }
    public string? WindowsInfo { get; set; }
    public string? Author { get; set; }
    public string? CommunityId { get; set; }
    public string? CreatedVersion { get; set; }
    public string? UpdatedAt { get; set; }

    /// <summary>配置来源类型(区分官方/推荐/用户,§5.3)。</summary>
    [JsonIgnore]
    public PlanSource Source { get; set; } = PlanSource.User;
}

/// <summary>配置来源类型。</summary>
public enum PlanSource
{
    /// <summary>官方方案(内置预设)。</summary>
    Official,

    /// <summary>推荐方案(内置,来自文档/社区推荐)。</summary>
    Recommended,

    /// <summary>用户自建方案。</summary>
    User
}

/// <summary>
/// 配置导入分析结果(第二代 §6):解析 + 匹配 + 兼容性 + 风险检查。
/// </summary>
public sealed class PlanImportResult
{
    /// <summary>解析是否成功。</summary>
    public bool ParseOk { get; set; }

    /// <summary>解析错误信息(ParseOk=false 时)。</summary>
    public string? ParseError { get; set; }

    /// <summary>解析出的配置(Schema 检查通过后)。</summary>
    public PlanConfig? Config { get; set; }

    /// <summary>可正常执行的项。</summary>
    public List<ImportedFunction> Ready { get; set; } = new();

    /// <summary>未知 FunctionId(不存在)。</summary>
    public List<string> UnknownIds { get; set; } = new();

    /// <summary>不兼容项(存在但禁止执行)。</summary>
    public List<ImportedFunction> Unsupported { get; set; } = new();

    /// <summary>高风险项(不默认选择,需重新确认)。</summary>
    public List<ImportedFunction> HighRisk { get; set; } = new();

    /// <summary>重复 FunctionId(已去重)。</summary>
    public int DuplicateCount { get; set; }

    /// <summary>配置 schema 版本过旧提示。</summary>
    public string? SchemaHint { get; set; }
}

/// <summary>导入清单中的一项功能。</summary>
public sealed class ImportedFunction
{
    public required string FunctionId { get; init; }
    public required string Name { get; init; }
    public RiskLevel Risk { get; init; }
    public CompatibilityStatus CompatStatus { get; init; }
    public string? CompatReason { get; init; }
}

/// <summary>
/// 配置方案服务:用户方案保存/读取/删除 + 导入/导出(第二代 §5-§6)。
/// 配置只描述"执行哪些已登记功能",不携带任意代码(§5.2)。
/// </summary>
public interface IPlanService
{
    /// <summary>保存方案(新建或更新)。</summary>
    void Save(PlanConfig config);

    /// <summary>读取全部用户方案。</summary>
    IReadOnlyList<PlanConfig> GetAll();

    /// <summary>删除方案。</summary>
    void Delete(string name);

    /// <summary>导出方案到 JSON 文件。</summary>
    void Export(PlanConfig config, string filePath);

    /// <summary>从文件导入方案(解析 + 匹配 + 兼容性 + 风险检查)。</summary>
    PlanImportResult Import(string filePath, IFunctionCatalog catalog, CompatibilityEngine compat);
}
