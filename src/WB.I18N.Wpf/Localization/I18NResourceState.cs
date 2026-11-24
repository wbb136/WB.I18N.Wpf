using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace WB.I18N.Wpf;

/// <summary>保存一代资源和语言状态，并按需缓存该语言已经查询过的资源键。</summary>
internal sealed class I18NResourceState
{
    private static readonly object MissingResource = new();
    private readonly ConcurrentDictionary<string, object> _resolvedStrings = new(StringComparer.Ordinal);
    private readonly CultureInfo _culture;
    private readonly ResourceManager? _resources;

    /// <summary>创建资源快照；语言或资源程序集更换时会创建新的快照。</summary>
    /// <param name="culture">本快照使用的资源语言。</param>
    /// <param name="resources">当前注册的资源管理器。</param>
    public I18NResourceState(CultureInfo culture, ResourceManager? resources)
    {
        _culture = culture;
        _resources = resources;
    }

    /// <summary>获取本快照使用的资源语言。</summary>
    public CultureInfo Culture
    {
        get
        {
            return _culture;
        }
    }

    /// <summary>获取当前注册的资源管理器，未注册时为空。</summary>
    public ResourceManager? Resources
    {
        get
        {
            return _resources;
        }
    }

    /// <summary>按指定语言查值；相同语言命中缓存，其他语言保持原有直接查找语义。</summary>
    /// <param name="key">保留原始空白的资源键。</param>
    /// <param name="culture">本次查询使用的资源语言。</param>
    /// <returns>找到的资源文案；缺失时返回空值。</returns>
    public string? GetString(string key, CultureInfo culture)
    {
        if (_resources is null)
        {
            return null;
        }

        if (!ReferenceEquals(culture, _culture))
        {
            return _resources.GetString(key, culture);
        }

        if (_resolvedStrings.TryGetValue(key, out object? cached))
        {
            return ReferenceEquals(cached, MissingResource) ? null : (string)cached;
        }

        // 只缓存资源查找结果，备用文案在每次调用时单独计算。
        string? resolved = _resources.GetString(key, culture);
        cached = _resolvedStrings.GetOrAdd(key, resolved ?? MissingResource);
        return ReferenceEquals(cached, MissingResource) ? null : (string)cached;
    }
}
