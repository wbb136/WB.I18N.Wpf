using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Data;
using System.Windows.Markup;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "WB.I18N.Wpf")]

namespace WB.I18N.Wpf;

/// <summary>提供统一的界面语言和资源入口，支持 C# 查值和 WPF XAML 动态绑定。</summary>
[ContentProperty(nameof(Arguments))]
[XamlSetMarkupExtension(nameof(ReceiveMarkupExtension))]
public sealed class I18NExtension : MarkupExtension, INotifyPropertyChanged
{
    private static readonly object ResourceLock = new();
    private static readonly I18NExtension SharedInstance = new();
    private static Assembly? _declarationAssembly;
    private static I18NResourceState _resourceState = new(CultureInfo.GetCultureInfo("zh"), null);
    private static event PropertyChangedEventHandler? LanguageChanged;

    private object? _key;
    private object? _fallback;
    private readonly Collection<BindingBase> _arguments = new();

    /// <summary>创建可在 WPF XAML 中使用的语言资源标记扩展。</summary>
    public I18NExtension()
    {
    }

    /// <summary>获取应用共用的语言绑定源。</summary>
    public static I18NExtension Current
    {
        get
        {
            return SharedInstance;
        }
    }

    /// <summary>获取或设置 .NET 支持的界面语言或地区标签，例如 fr 或 fr-CA；无效值会被忽略。</summary>
    public string LanguageTag
    {
        get
        {
            return Volatile.Read(ref _resourceState).Culture.Name;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(value.Trim());
            }
            catch (CultureNotFoundException)
            {
                return;
            }

            lock (ResourceLock)
            {
                I18NResourceState previous = _resourceState;
                if (string.Equals(culture.Name, previous.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // 替换整份快照，令并发中的读取继续使用原资源与语言组合。
                Volatile.Write(ref _resourceState, new I18NResourceState(culture, previous.Resources));
            }

            // 仅更新本库资源所用的区域性，不修改调用线程的全局文化设置。
            LanguageChanged?.Invoke(Current, new PropertyChangedEventArgs(nameof(LanguageTag)));
        }
    }

    /// <summary>界面语言或资源注册变化时通知 WPF 绑定重新读取文案。</summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            LanguageChanged += value;
        }
        remove
        {
            LanguageChanged -= value;
        }
    }

    /// <summary>获取或设置资源键，可以是固定字符串或 WPF 绑定。</summary>
    public object? Key
    {
        get
        {
            return _key;
        }
        set
        {
            _key = value;
        }
    }

    /// <summary>获取或设置缺少资源时使用的备用文案，可以是固定字符串或 WPF 绑定。</summary>
    public object? Fallback
    {
        get
        {
            return _fallback;
        }
        set
        {
            _fallback = value;
        }
    }

    /// <summary>获取按占位符顺序传入的 WPF 参数绑定。</summary>
    public Collection<BindingBase> Arguments
    {
        get
        {
            return _arguments;
        }
    }

    /// <summary>获取当前资源语言，以便绑定转换器使用同一语言格式化参数。</summary>
    internal static CultureInfo ResourceCulture
    {
        get
        {
            return Volatile.Read(ref _resourceState).Culture;
        }
    }

    /// <summary>按原样资源键读取当前语言的资源，缺少译文时回退到上级语言、默认资源或备用文案。</summary>
    /// <param name="resourceKey">资源键。</param>
    /// <param name="fallback">资源不存在时使用的备用文案。</param>
    /// <returns>当前语言的文案、备用文案或资源键。</returns>
    public static string GetString(string? resourceKey, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return fallback ?? string.Empty;
        }

        string key = resourceKey;
        I18NResourceState state = GetResourceState();
        return state.GetString(key, state.Culture) ?? fallback ?? key;
    }

    /// <summary>读取当前语言的资源模板，并按当前资源语言格式化参数。</summary>
    /// <param name="resourceKey">资源模板的键。</param>
    /// <param name="arguments">按占位符顺序传入的值。</param>
    /// <returns>格式化后的文案。</returns>
    public static string Format(string resourceKey, params object?[]? arguments)
    {
        I18NResourceState state = GetResourceState();
        string template = GetString(resourceKey, null, state);
        if (arguments is { Length: > 0 })
        {
            return string.Format(state.Culture, template, arguments);
        }

        return template;
    }

    /// <summary>供源生成器注册调用方声明的资源位置。</summary>
    /// <param name="declarationAssembly">包含资源声明的程序集。</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterResourceAssembly(Assembly declarationAssembly)
    {
        ArgumentNullException.ThrowIfNull(declarationAssembly);

        // 启动应用的声明优先于它所引用的语言类库声明。
        Assembly? applicationAssembly = Assembly.GetEntryAssembly();
        I18NResourceAttribute? applicationResource =
            applicationAssembly?.GetCustomAttribute<I18NResourceAttribute>();
        if (applicationAssembly is not null && applicationResource is not null)
        {
            declarationAssembly = applicationAssembly;
        }

        lock (ResourceLock)
        {
            if (_declarationAssembly == declarationAssembly)
            {
                return;
            }

            if (_declarationAssembly is not null && declarationAssembly != applicationAssembly)
            {
                throw new InvalidOperationException(
                    "发现多个语言资源声明，请在启动项目的 I18N 部分类上指定资源。"
                );
            }

            I18NResourceAttribute resource =
                applicationResource
                ?? declarationAssembly.GetCustomAttribute<I18NResourceAttribute>()
                ?? throw new InvalidOperationException(
                    "请在 I18N 部分类上使用 I18NResource 特性声明语言资源。"
                );

            ResourceManager manager = new(
                resource.BaseName,
                resource.ResourceAssemblyAnchor?.Assembly ?? declarationAssembly
            );
            _declarationAssembly = declarationAssembly;
            I18NResourceState previous = _resourceState;
            Volatile.Write(ref _resourceState, new I18NResourceState(previous.Culture, manager));
        }

        // 资源初始化之前已经求值的绑定，需要在资源就绪后重新读取。
        LanguageChanged?.Invoke(Current, new PropertyChangedEventArgs(nameof(LanguageTag)));
    }

    /// <summary>向 WPF 提供随语言、资源键、备用文案和参数变化的绑定。</summary>
    /// <param name="serviceProvider">WPF XAML 提供的目标属性上下文。</param>
    /// <returns>用于目标属性的 WPF 绑定值。</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        Binding languageBinding = new(nameof(LanguageTag))
        {
            Source = Current,
            Mode = BindingMode.OneWay
        };

        // 固定键无需监听键和备用文案，仅保留一个语言绑定。
        if (Key is not BindingBase && Fallback is not BindingBase && Arguments.Count == 0)
        {
            languageBinding.Converter = I18NValueConverter.Instance;
            languageBinding.ConverterParameter = Fallback is null
                ? Key
                : new I18NBindingOptions(Key, Fallback);
            return languageBinding.ProvideValue(serviceProvider);
        }

        I18NBindingOptions options = new(Key, Fallback);
        MultiBinding binding = new()
        {
            Mode = BindingMode.OneWay,
            Converter = I18NMultiValueConverter.Instance,
            ConverterParameter = options
        };

        binding.Bindings.Add(languageBinding);
        if (Key is BindingBase keyBinding)
        {
            binding.Bindings.Add(keyBinding);
        }

        if (Fallback is BindingBase fallbackBinding)
        {
            binding.Bindings.Add(fallbackBinding);
        }

        // 参数绑定直接加入多重绑定，参数值变化时会自动重新格式化。
        foreach (BindingBase argument in Arguments)
        {
            binding.Bindings.Add(argument);
        }

        return binding.ProvideValue(serviceProvider);
    }

    /// <summary>在 WPF 求值嵌套标记扩展前保留资源键与备用文案的绑定对象。</summary>
    /// <param name="targetObject">正在创建的语言资源标记扩展。</param>
    /// <param name="eventArgs">嵌套标记扩展及其目标属性。</param>
    public static void ReceiveMarkupExtension(
        object targetObject,
        XamlSetMarkupExtensionEventArgs eventArgs
    )
    {
        if (targetObject is not I18NExtension extension ||
            eventArgs.MarkupExtension is not BindingBase binding)
        {
            return;
        }

        // Binding 只能直接设置在依赖属性上；这里先保存声明，稍后加入 MultiBinding。
        if (eventArgs.Member.Name == nameof(Key))
        {
            extension.Key = binding;
            eventArgs.Handled = true;
        }
        else if (eventArgs.Member.Name == nameof(Fallback))
        {
            extension.Fallback = binding;
            eventArgs.Handled = true;
        }
    }

    /// <summary>按指定语言和原样资源键读取资源，资源不存在时使用备用文案或资源键。</summary>
    /// <param name="resourceKey">资源键。</param>
    /// <param name="fallback">备用文案。</param>
    /// <param name="culture">本次查找使用的资源语言。</param>
    /// <returns>资源文案、备用文案或资源键。</returns>
    internal static string GetString(string? resourceKey, string? fallback, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return fallback ?? string.Empty;
        }

        string key = resourceKey;
        I18NResourceState state = GetResourceState();
        return state.GetString(key, culture) ?? fallback ?? key;
    }

    /// <summary>使用已捕获的语言状态与原样资源键查值，保证格式化模板和参数使用同一语言。</summary>
    /// <param name="resourceKey">资源键。</param>
    /// <param name="fallback">缺键时的备用文案。</param>
    /// <param name="state">本次格式化捕获的语言状态。</param>
    /// <returns>资源文案、备用文案或资源键。</returns>
    private static string GetString(string? resourceKey, string? fallback, I18NResourceState state)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return fallback ?? string.Empty;
        }

        string key = resourceKey;
        return state.GetString(key, state.Culture) ?? fallback ?? key;
    }

    /// <summary>获取已注册资源的当前快照，同时兼容启动程序集上的旧式资源声明。</summary>
    /// <returns>当前资源与语言状态。</returns>
    private static I18NResourceState GetResourceState()
    {
        I18NResourceState state = Volatile.Read(ref _resourceState);
        if (state.Resources is not null)
        {
            return state;
        }

        Assembly? applicationAssembly = Assembly.GetEntryAssembly();
        if (applicationAssembly?.IsDefined(typeof(I18NResourceAttribute), inherit: false) == true)
        {
            RegisterResourceAssembly(applicationAssembly);
        }

        return Volatile.Read(ref _resourceState);
    }

}
