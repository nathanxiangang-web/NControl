namespace NControl.Core;

/// <summary>来源台账条目(产品文档 §8.3)。</summary>
public sealed record SourceRecord(
    string Project,
    string License,
    string Version,
    string Functions,
    string LocalChanges,
    string VerifiedOn);

/// <summary>
/// 开源功能来源台账:记录功能从哪里来、使用哪个版本、遵循什么许可证、何时验证。
/// 展示于“关于”页面。
/// </summary>
public static class SourceLedger
{
    public static IReadOnlyList<SourceRecord> Records { get; } = new[]
    {
        new SourceRecord(
            "自研 · Windows 注册表/系统命令适配",
            "N/A(本项目)",
            "第一代 v0.2",
            "任务栏/开始菜单、资源管理器、隐私与广告、更新、性能、游戏、清理、修复、工具等全部第一代功能",
            "按《NControl 第一代产品开发文档 v0.2》统一编写名称、说明、风险分级与执行入口;未引入外部页面结构",
            "Windows 10/11 x64(开发环境)"),
        new SourceRecord(
            "Win11Debloat(研究参考)",
            "MIT",
            "待接入时记录上游提交号",
            "广告推荐、预装应用清理等候选功能池",
            "仅研究实现思路;第一代不直接引入,不继承其页面结构与用户流程",
            "未验证(暂不接入)"),
        new SourceRecord(
            "ZyperWin++(研究参考)",
            "待核(接入前需核对许可证)",
            "待接入时记录上游提交号",
            "系统优化/清理候选功能池",
            "仅研究;需重新评估许可证、适配成本与功能价值",
            "未验证(暂不接入)"),
        new SourceRecord(
            "ZyperWin++(参考实现)",
            "未公开(免费软件,接入前需核对)",
            "4.2(2026-08-02 功能数据)",
            "外观/资源管理器、性能、系统设置、隐私、更新、Edge 等约 123 项注册表/服务配置",
            "仅参考注册表与系统命令操作;名称、说明、风险分级全部按 NControl 标准重写;不继承其 UI 与流程;经评估不引入 Defender 关闭等高破坏性默认项",
            "Windows 10/11 x64(待真机验证)"),
        new SourceRecord(
            "Windows 系统内置工具",
            "Windows 系统组件",
            "Windows 10/11",
            "DISM、SFC、netsh、ipconfig、powercfg、wsreset、Get-AppxPackage 等",
            "以系统自带命令直接调用,不引入第三方二进制",
            "Windows 10/11 x64")
    };
}
