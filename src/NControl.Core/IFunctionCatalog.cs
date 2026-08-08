namespace NControl.Core;

/// <summary>
/// 功能目录:所有功能先进入统一目录,再由页面展示(产品文档 §3.2 规则 1)。
/// 页面不得直接拥有功能。
/// </summary>
public interface IFunctionCatalog
{
    IReadOnlyList<FunctionItem> All { get; }
    IReadOnlyList<Preset> Presets { get; }
    IReadOnlyList<string> Categories { get; }

    void Register(FunctionItem item);
    void RegisterPreset(Preset preset);

    FunctionItem? Find(string id);
    IReadOnlyList<FunctionItem> ByModule(ModuleKind module);
    IReadOnlyList<FunctionItem> ByCategory(string category);
    IReadOnlyList<FunctionItem> Search(string keyword);
}
