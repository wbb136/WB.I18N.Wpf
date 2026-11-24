using System.Globalization;
using System.Windows.Data;

namespace WB.I18N.Wpf;

/// <summary>固定资源键不带格式参数时，用单个 WPF 绑定随语言变化更新文案。</summary>
internal sealed class I18NValueConverter : IValueConverter
{
    private static readonly I18NValueConverter SharedInstance = new();

    /// <summary>创建固定键绑定共用的无状态转换器。</summary>
    private I18NValueConverter()
    {
    }

    /// <summary>获取固定键绑定共用的转换器。</summary>
    public static I18NValueConverter Instance
    {
        get
        {
            return SharedInstance;
        }
    }

    /// <summary>界面语言改变时重新读取固定资源键。</summary>
    /// <param name="value">当前语言标记，由绑定通知重新求值。</param>
    /// <param name="targetType">WPF 目标属性类型。</param>
    /// <param name="parameter">固定键，或包含固定备用文案的选项。</param>
    /// <param name="culture">WPF 绑定自身的区域性。</param>
    /// <returns>当前语言对应的资源文案。</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is I18NBindingOptions options)
        {
            return I18NExtension.GetString(
                options.FixedKey,
                options.FixedFallback,
                I18NExtension.ResourceCulture
            );
        }

        return I18NExtension.GetString(parameter as string, null, I18NExtension.ResourceCulture);
    }

    /// <summary>资源文案是只读结果，不支持反向写入。</summary>
    /// <param name="value">目标属性当前值。</param>
    /// <param name="targetType">源绑定目标类型。</param>
    /// <param name="parameter">固定键或绑定选项。</param>
    /// <param name="culture">WPF 绑定自身的区域性。</param>
    /// <returns>此方法始终抛出不支持异常。</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("语言资源绑定不支持反向写入。");
    }
}
