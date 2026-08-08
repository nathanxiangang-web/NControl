namespace NControl.Core;

/// <summary>
/// 模块登记器:各业务模块通过它把功能项与方案登记进统一目录。
/// 分两阶段:先登记功能,再登记方案(方案可能跨模块引用功能 Id)。
/// </summary>
public interface IModuleRegistrar
{
    string ModuleName { get; }
    void RegisterFeatures(IFunctionCatalog catalog);
    void RegisterPresets(IFunctionCatalog catalog);
}
