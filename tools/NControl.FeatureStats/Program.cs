// NControl 功能统计工具(运行时权威数据)
// 用法: dotnet run --project tools/NControl.FeatureStats
using Microsoft.Extensions.Logging.Abstractions;
using NControl.Core;
using NControl.Modules.Optimization;
using NControl.Modules.Applications;
using NControl.Modules.Cleanup;
using NControl.Modules.Repair;
using NControl.Modules.Tools;

var catalog = new FunctionCatalog(NullLogger<FunctionCatalog>.Instance);
new OptimizationModuleRegistrar().RegisterFeatures(catalog);
new ApplicationsModuleRegistrar().RegisterFeatures(catalog);
new CleanupModuleRegistrar().RegisterFeatures(catalog);
new RepairModuleRegistrar().RegisterFeatures(catalog);
new ToolsModuleRegistrar().RegisterFeatures(catalog);

new OptimizationModuleRegistrar().RegisterPresets(catalog);
new ApplicationsModuleRegistrar().RegisterPresets(catalog);

var all = catalog.All.ToList();
Console.WriteLine($"功能总数: {all.Count}");
Console.WriteLine($"\n== 按分类 ==");
foreach (var g in all.GroupBy(f => f.Category).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key,-16} {g.Count()}");
Console.WriteLine($"\n== 按风险 ==");
foreach (var g in all.GroupBy(f => f.Risk).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key,-12} {g.Count()}");
Console.WriteLine($"\n== 按模块 ==");
foreach (var g in all.GroupBy(f => f.Module).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key,-12} {g.Count()}");
Console.WriteLine($"\n需管理员权限: {all.Count(f => f.RequiresAdmin)}");
Console.WriteLine($"需重启: Reboot {all.Count(f => f.Restart == RestartRequirement.Reboot)} / ExplorerRestart {all.Count(f => f.Restart == RestartRequirement.ExplorerRestart)}");
Console.WriteLine($"即时工具(IsTool): {all.Count(f => f.IsTool)}");

Console.WriteLine($"\n== 预设 ==");
foreach (var p in catalog.Presets)
{
    var ids = p.FeatureIds;
    var dup = ids.Count - ids.Distinct().Count();
    Console.WriteLine($"  {p.Id,-24} {p.Name,-12} 引用 {ids.Count} 项(去重 {ids.Distinct().Count()},重复 {dup})");
}
