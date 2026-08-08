using Microsoft.Extensions.Logging;

namespace NControl.Core;

/// <summary>内存功能目录实现。单例注册,启动时由各模块登记。</summary>
public sealed class FunctionCatalog : IFunctionCatalog
{
    private readonly object _gate = new();
    private readonly List<FunctionItem> _items = new();
    private readonly List<Preset> _presets = new();
    private readonly ILogger<FunctionCatalog> _logger;

    public FunctionCatalog(ILogger<FunctionCatalog> logger) => _logger = logger;

    public IReadOnlyList<FunctionItem> All
    {
        get { lock (_gate) return _items.ToArray(); }
    }

    public IReadOnlyList<Preset> Presets
    {
        get { lock (_gate) return _presets.ToArray(); }
    }

    public IReadOnlyList<string> Categories
    {
        get { lock (_gate) return _items.Select(i => i.Category).Distinct().ToArray(); }
    }

    public void Register(FunctionItem item)
    {
        lock (_gate)
        {
            if (_items.Any(i => i.Id == item.Id))
            {
                _logger.LogWarning("功能项已存在,忽略重复注册: {Id}", item.Id);
                return;
            }
            _items.Add(item);
        }
    }

    public void RegisterPreset(Preset preset)
    {
        lock (_gate)
        {
            if (_presets.Any(p => p.Id == preset.Id)) return;
            _presets.Add(preset);
        }
    }

    public FunctionItem? Find(string id)
    {
        lock (_gate) return _items.FirstOrDefault(i => i.Id == id);
    }

    public IReadOnlyList<FunctionItem> ByModule(ModuleKind module)
    {
        lock (_gate) return _items.Where(i => i.Module == module).ToArray();
    }

    public IReadOnlyList<FunctionItem> ByCategory(string category)
    {
        lock (_gate) return _items.Where(i => i.Category == category).ToArray();
    }

    public IReadOnlyList<FunctionItem> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<FunctionItem>();
        var k = keyword.Trim();
        lock (_gate)
        {
            return _items
                .Where(i => i.Name.Contains(k, StringComparison.OrdinalIgnoreCase)
                            || i.Description.Contains(k, StringComparison.OrdinalIgnoreCase)
                            || i.Category.Contains(k, StringComparison.OrdinalIgnoreCase)
                            || (i.Extra?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(20)
                .ToArray();
        }
    }
}
