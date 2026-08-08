using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NControl.Presentation;

/// <summary>bool -> Visibility(取反)。</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public static InverseBoolToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>bool -> Visibility。</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static BoolToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>bool -> bool(取反),用于 IsEnabled 等属性。</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public static InverseBoolConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}

/// <summary>
/// 对象是否为 null 的可见性转换:null -> Collapsed。
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public static NullToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// 整型与参数比较的可见性转换。
/// 参数 "0" -> 值等于 0 时 Visible;参数 "not0" -> 值不等于 0 时 Visible。
/// </summary>
public sealed class IntEqualsToVisibilityConverter : IValueConverter
{
    public static IntEqualsToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var intValue = value is int i ? i : -1;
        var param = parameter?.ToString() ?? "";
        if (param == "not0")
            return intValue != 0 ? Visibility.Visible : Visibility.Collapsed;
        return int.TryParse(param, out var expected) && intValue == expected
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>枚举名称与参数比较的可见性转换(如 Confirm/Running/Done)。</summary>
public sealed class PhaseEqualsToVisibilityConverter : IValueConverter
{
    public static PhaseEqualsToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>管理员权限要求 -> 文本。</summary>
public sealed class AdminToTextConverter : IValueConverter
{
    public static AdminToTextConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "需管理员权限" : "普通权限";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>进度百分比 + 轨道宽度 -> 填充宽度(多值绑定)。</summary>
public sealed class PercentToWidthMultiConverter : IMultiValueConverter
{
    public static PercentToWidthMultiConverter Instance { get; } = new();

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = values.Length > 0 && values[0] is double p ? p : 0.0;
        var width = values.Length > 1 && values[1] is double w ? w : 0.0;
        return Math.Max(0, Math.Min(width, width * percent / 100.0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
