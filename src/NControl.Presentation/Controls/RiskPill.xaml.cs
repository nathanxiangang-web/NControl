using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NControl.Core;

namespace NControl.Presentation.Controls;

/// <summary>
/// 风险标签:根据风险等级显示颜色与文案(产品文档 §2.4 统一视觉语义)。
/// 可通过 Text 覆盖文案;通过 BackgroundOverride/ForegroundOverride 覆盖配色(用于“按需保留”等业务文案)。
/// </summary>
public partial class RiskPill : UserControl
{
    public static readonly DependencyProperty RiskProperty = DependencyProperty.Register(
        nameof(Risk), typeof(RiskLevel), typeof(RiskPill), new PropertyMetadata(RiskLevel.Safe, OnChanged));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(RiskPill), new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty BackgroundOverrideProperty = DependencyProperty.Register(
        nameof(BackgroundOverride), typeof(Brush), typeof(RiskPill), new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty ForegroundOverrideProperty = DependencyProperty.Register(
        nameof(ForegroundOverride), typeof(Brush), typeof(RiskPill), new PropertyMetadata(null, OnChanged));

    public RiskPill() => InitializeComponent();

    public RiskLevel Risk
    {
        get => (RiskLevel)GetValue(RiskProperty);
        set => SetValue(RiskProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Brush? BackgroundOverride
    {
        get => (Brush?)GetValue(BackgroundOverrideProperty);
        set => SetValue(BackgroundOverrideProperty, value);
    }

    public Brush? ForegroundOverride
    {
        get => (Brush?)GetValue(ForegroundOverrideProperty);
        set => SetValue(ForegroundOverrideProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RiskPill)d).Apply();

    private void Apply()
    {
        var (text, bg, fg) = Risk switch
        {
            RiskLevel.Safe => ("安全", (Brush)FindResource("SuccessSoftBrush"), (Brush)FindResource("SuccessBrush")),
            RiskLevel.Recommended => ("推荐", (Brush)FindResource("SuccessSoftBrush"), (Brush)FindResource("SuccessBrush")),
            RiskLevel.Caution => ("谨慎", (Brush)FindResource("WarningSoftBrush"), (Brush)FindResource("WarningBrush")),
            RiskLevel.HighRisk => ("高风险", (Brush)FindResource("DangerSoftBrush"), (Brush)FindResource("DangerBrush")),
            _ => ("实验性", (Brush)FindResource("NeutralSoftBrush"), (Brush)FindResource("NeutralBrush"))
        };

        if (!string.IsNullOrEmpty(Text)) text = Text!;
        if (BackgroundOverride is not null) bg = BackgroundOverride;
        if (ForegroundOverride is not null) fg = ForegroundOverride;

        Root.Background = bg;
        Txt.Text = text;
        Txt.Foreground = fg;
    }
}
