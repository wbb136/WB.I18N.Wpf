using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WB.I18N.Wpf;

/// <summary>将 WPF 多重绑定中的语言、资源键、备用文案和格式参数合成为本地化文本。</summary>
internal sealed class I18NMultiValueConverter : IMultiValueConverter
{
    private static readonly I18NMultiValueConverter SharedInstance = new();

    /// <summary>创建 WPF 多重绑定使用的本地化转换器。</summary>
    private I18NMultiValueConverter()
    {
    }

    /// <summary>获取所有本地化绑定共用的无状态转换器。</summary>
    public static I18NMultiValueConverter Instance
    {
        get
        {
            return SharedInstance;
        }
    }

    /// <summary>在语言、资源键、备用文案或格式参数变化时重新生成文本。</summary>
    /// <param name="values">依次包含语言、需要监听的资源键和备用文案，以及格式参数。</param>
    /// <param name="targetType">WPF 目标属性类型。</param>
    /// <param name="parameter">固定文案和动态绑定位置。</param>
    /// <param name="culture">WPF 绑定自身的区域性。</param>
    /// <returns>资源查找和参数格式化后的文本。</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        I18NBindingOptions options = (I18NBindingOptions)parameter;
        CultureInfo language = I18NExtension.ResourceCulture;
        int valueIndex = 1;
        string? key = options.FixedKey;
        string? fallback = options.FixedFallback;
        if (options.KeyIsBound)
        {
            key = values[valueIndex] as string;
            valueIndex++;
        }

        if (options.FallbackIsBound)
        {
            fallback = values[valueIndex] as string;
            valueIndex++;
        }

        string text = I18NExtension.GetString(key, fallback, language);
        if (values.Length == valueIndex)
        {
            return text;
        }

        object?[] arguments = new object?[values.Length - valueIndex];
        for (int index = 0; index < arguments.Length; index++)
        {
            object? value = values[index + valueIndex];
            if (value == DependencyProperty.UnsetValue || value == Binding.DoNothing)
            {
                value = null;
            }

            arguments[index] = value;
        }

        return string.Format(language, text, arguments);
    }

    /// <summary>本地化文本是只读结果，不支持反向写入资源键或参数。</summary>
    /// <param name="value">目标属性当前值。</param>
    /// <param name="targetTypes">源绑定的目标类型。</param>
    /// <param name="parameter">转换器参数。</param>
    /// <param name="culture">WPF 绑定自身的区域性。</param>
    /// <returns>此方法始终抛出不支持异常。</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("语言资源绑定不支持反向写入。");
    }
}
