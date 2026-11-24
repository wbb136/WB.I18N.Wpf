using System.Windows.Data;

namespace WB.I18N.Wpf;

/// <summary>保存本地化绑定的固定文案和动态值位置，避免为固定值再创建 WPF 子绑定。</summary>
internal sealed class I18NBindingOptions
{
    private readonly string? _fixedKey;
    private readonly string? _fixedFallback;
    private readonly bool _keyIsBound;
    private readonly bool _fallbackIsBound;

    /// <summary>根据标记扩展中的资源键和备用文案确定需要监听的动态值。</summary>
    /// <param name="key">固定资源键或 WPF 绑定。</param>
    /// <param name="fallback">固定备用文案或 WPF 绑定。</param>
    public I18NBindingOptions(object? key, object? fallback)
    {
        _keyIsBound = key is BindingBase;
        _fallbackIsBound = fallback is BindingBase;
        _fixedKey = key as string;
        _fixedFallback = fallback as string;
    }

    /// <summary>获取固定资源键；资源键来自绑定时为空。</summary>
    public string? FixedKey
    {
        get
        {
            return _fixedKey;
        }
    }

    /// <summary>获取固定备用文案；备用文案来自绑定时为空。</summary>
    public string? FixedFallback
    {
        get
        {
            return _fixedFallback;
        }
    }

    /// <summary>获取资源键是否来自 WPF 绑定。</summary>
    public bool KeyIsBound
    {
        get
        {
            return _keyIsBound;
        }
    }

    /// <summary>获取备用文案是否来自 WPF 绑定。</summary>
    public bool FallbackIsBound
    {
        get
        {
            return _fallbackIsBound;
        }
    }
}
