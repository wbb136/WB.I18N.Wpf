namespace WB.I18N.Wpf;

/// <summary>声明应用语言资源的位置，支持当前程序集和独立资源程序集。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class I18NResourceAttribute : Attribute
{
    private readonly Type? _resourceAssemblyAnchor;
    private readonly string _baseName;

    /// <summary>声明特性所在程序集中的语言资源。</summary>
    /// <param name="baseName">资源的完整清单名称，省略扩展名和语言后缀。</param>
    public I18NResourceAttribute(string baseName)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new ArgumentException("资源名称不能为空或空白。", nameof(baseName));
        }
        _baseName = baseName;
    }

    /// <summary>通过类型的程序集和完整名称定位语言资源。</summary>
    /// <param name="resourceType">完整名称与中性资源名称一致的类型。</param>
    public I18NResourceAttribute(Type resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);
        _baseName = resourceType.FullName
            ?? throw new ArgumentException("资源类型必须具有完整名称。", nameof(resourceType));
        _resourceAssemblyAnchor = resourceType;
    }

    /// <summary>通过目标程序集中的类型声明语言资源的位置。</summary>
    /// <param name="resourceAssemblyAnchor">资源所在程序集中的任意类型。</param>
    /// <param name="baseName">资源的完整清单名称，省略扩展名和语言后缀。</param>
    public I18NResourceAttribute(Type resourceAssemblyAnchor, string baseName)
        : this(baseName)
    {
        ArgumentNullException.ThrowIfNull(resourceAssemblyAnchor);
        _resourceAssemblyAnchor = resourceAssemblyAnchor;
    }

    /// <summary>获取用于定位独立资源程序集的类型。</summary>
    internal Type? ResourceAssemblyAnchor
    {
        get
        {
            return _resourceAssemblyAnchor;
        }
    }

    /// <summary>获取传递给资源管理器的中性资源名称。</summary>
    public string BaseName
    {
        get
        {
            return _baseName;
        }
    }
}
