using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace WB.I18N.Wpf.Sample;

/// <summary>在实际加载的 WPF 窗口上验证资源读取与动态绑定。</summary>
internal static class VerificationRunner
{
    /// <summary>验证生成入口、语言切换、资源键绑定、格式化参数和资源回退。</summary>
    /// <param name="window">已经加载并显示的示例窗口。</param>
    public static async Task RunAsync(MainWindow window)
    {
        SampleViewModel viewModel = (SampleViewModel)window.DataContext;
        string threadCulture = Thread.CurrentThread.CurrentCulture.Name;
        string threadUiCulture = Thread.CurrentThread.CurrentUICulture.Name;
        await FlushBindingsAsync(window);

        string chineseSummary = string.Format(
            CultureInfo.GetCultureInfo("zh"),
            "设备 {0}，金额 {1:C2}",
            viewModel.ProductCode,
            viewModel.Amount
        );

        Verify("当前中文语言标签", ReadText(window, "LanguageTagText"), "zh");
        Verify("中文 GetString", I18N.GetString(I18N.Greeting), "你好，世界！");
        Verify("生成的资源键", I18N.Greeting, "Greeting");
        Verify("空键使用备用文案", I18N.GetString(null, "空键备用"), "空键备用");
        Verify("空白键使用备用文案", I18N.GetString("   ", "空键备用"), "空键备用");
        Verify("键名首尾空白按原样保留", I18N.GetString(" Greeting "), " Greeting ");
        Verify("带空白的缺失键使用备用文案", I18N.GetString(" Greeting ", "备用文案"), "备用文案");
        Verify("资源键区分大小写", I18N.GetString("greeting"), "greeting");
        Verify("中文 Format", I18N.Format(I18N.ItemCount, 3), "数量：3");
        Verify("中文静态 XAML", ReadText(window, "GreetingText"), "你好，世界！");
        Verify("中文生成键 XAML", ReadText(window, "GeneratedKeyText"), "你好，世界！");
        VerifyBindingKind(window, "GreetingText", expectMultiBinding: false);
        VerifyBindingKind(window, "LiteralFallbackText", expectMultiBinding: false);
        VerifyBindingKind(window, "DynamicText", expectMultiBinding: true);
        VerifyBindingKind(window, "CountText", expectMultiBinding: true);
        VerifyBindingKind(window, "MultiText", expectMultiBinding: true);
        Verify("中文动态资源键", ReadText(window, "DynamicText"), "你好，世界！");
        Verify("固定备用文案", ReadText(window, "LiteralFallbackText"), "固定备用文案");
        Verify("缺失键显示键名", ReadText(window, "MissingKeyText"), "NotDefinedWithoutFallback");
        Verify("中文默认资源", ReadText(window, "ParentFallbackText"), "中文默认资源");
        Verify("中文中性资源", ReadText(window, "NeutralFallbackText"), "仅默认资源");
        Verify("中文格式参数", ReadText(window, "CountText"), "数量：3");
        Verify("中文多参数 XAML", ReadText(window, "MultiText"), chineseSummary);
        Verify("C# GetString 展示", ReadText(window, "CodeGetStringText"), "你好，世界！");
        Verify("C# GetString 备用文案展示", ReadText(window, "CodeGetFallbackText"), "C# 固定备用文案");
        Verify("C# Format 展示", ReadText(window, "CodeFormatText"), chineseSummary);
        Verify("C# 缺失键显示键名", I18N.GetString("NotDefinedWithoutFallback"), "NotDefinedWithoutFallback");

        // 当前窗口保持打开，语言事件应驱动已有控件重新取值。
        I18N.Current.LanguageTag = "en-US";
        window.RefreshCodeExamples();
        await FlushBindingsAsync(window);
        Verify("切换不修改线程文化", Thread.CurrentThread.CurrentCulture.Name, threadCulture);
        Verify("切换不修改线程界面文化", Thread.CurrentThread.CurrentUICulture.Name, threadUiCulture);
        I18N.Current.LanguageTag = "not_a_culture!";
        Verify("无效语言标签被忽略", I18N.Current.LanguageTag, "en-US");
        I18N.Current.LanguageTag = " EN-us ";
        Verify("语言标签大小写规范化", I18N.Current.LanguageTag, "en-US");
        string englishSummary = string.Format(
            CultureInfo.GetCultureInfo("en-US"),
            "Device {0}, amount {1:C2}",
            viewModel.ProductCode,
            viewModel.Amount
        );

        Verify("当前英文地区标签", ReadText(window, "LanguageTagText"), "en-US");
        Verify("父级 en 资源", I18N.GetString(I18N.Greeting), "Hello, world!");
        Verify("英文静态 XAML", ReadText(window, "GreetingText"), "Hello, world!");
        Verify("英文生成键 XAML", ReadText(window, "GeneratedKeyText"), "Hello, world!");
        Verify("英文动态资源键", ReadText(window, "DynamicText"), "Hello, world!");
        Verify("英文固定备用文案", ReadText(window, "LiteralFallbackText"), "固定备用文案");
        Verify("英文缺失键显示键名", ReadText(window, "MissingKeyText"), "NotDefinedWithoutFallback");
        Verify("en-US 父级资源回退", ReadText(window, "ParentFallbackText"), "English parent resource");
        Verify("英文格式参数", ReadText(window, "CountText"), "Items: 3");
        Verify("英文多参数 XAML", ReadText(window, "MultiText"), englishSummary);
        Verify("中性资源回退", I18N.GetString("NeutralOnly"), "仅默认资源");
        Verify("中性资源 XAML 回退", ReadText(window, "NeutralFallbackText"), "仅默认资源");
        Verify("英文 C# GetString 展示", ReadText(window, "CodeGetStringText"), "Hello, world!");
        Verify("英文 C# Format 展示", ReadText(window, "CodeFormatText"), englishSummary);

        viewModel.ResourceKey = "Secondary";
        viewModel.Count = 5;
        viewModel.ProductCode = "B-02";
        viewModel.Amount = 2345.6m;
        window.RefreshCodeExamples();
        await FlushBindingsAsync(window);
        string updatedSummary = string.Format(
            CultureInfo.GetCultureInfo("en-US"),
            "Device {0}, amount {1:C2}",
            viewModel.ProductCode,
            viewModel.Amount
        );

        Verify("绑定键变化", ReadText(window, "DynamicText"), "Welcome to WPF");
        Verify("参数绑定变化", ReadText(window, "CountText"), "Items: 5");
        Verify("多个参数绑定变化", ReadText(window, "MultiText"), updatedSummary);
        Verify("C# 格式化参数变化", ReadText(window, "CodeFormatText"), updatedSummary);
        Verify("英文 Format", I18N.Format("ItemCount", 5), "Items: 5");

        viewModel.ResourceKey = "MissingResource";
        viewModel.FallbackText = "Fallback from view model";
        await FlushBindingsAsync(window);
        Verify("缺失键备用文案", ReadText(window, "DynamicText"), "Fallback from view model");
        viewModel.FallbackText = "Updated fallback";
        await FlushBindingsAsync(window);
        Verify("绑定备用文案变化", ReadText(window, "DynamicText"), "Updated fallback");
        Verify("C# 缺失键回退", I18N.GetString("MissingResource", "Fallback from code"), "Fallback from code");

        I18N.Current.LanguageTag = "zh";
        viewModel.ResourceKey = "Greeting";
        window.RefreshCodeExamples();
        await FlushBindingsAsync(window);
        Verify("中文切回无需重建", ReadText(window, "GreetingText"), "你好，世界！");
        Verify("中文生成键切回", ReadText(window, "GeneratedKeyText"), "你好，世界！");
        Verify("动态键切回", ReadText(window, "DynamicText"), "你好，世界！");
        Verify("语言标签切回", ReadText(window, "LanguageTagText"), "zh");
    }

    /// <summary>等待已排队的 WPF 绑定更新在 Dispatcher 上完成。</summary>
    /// <param name="window">拥有绑定目标的窗口。</param>
    private static async Task FlushBindingsAsync(Window window)
    {
        await window.Dispatcher.InvokeAsync(
            () =>
            {
            },
            DispatcherPriority.ApplicationIdle
        );
    }

    /// <summary>读取由 XAML 创建的控件文本，确保验证经过 WPF 标记扩展。</summary>
    /// <param name="window">包含目标控件的窗口。</param>
    /// <param name="name">XAML 控件名称。</param>
    /// <returns>控件当前文本。</returns>
    private static string ReadText(Window window, string name)
    {
        TextBlock control = (TextBlock)window.FindName(name);
        return control.Text;
    }

    /// <summary>确认固定键采用单绑定，动态键或格式化参数采用多重绑定。</summary>
    /// <param name="window">包含目标控件的窗口。</param>
    /// <param name="name">待检查的文本控件名称。</param>
    /// <param name="expectMultiBinding">是否应为多重绑定。</param>
    private static void VerifyBindingKind(Window window, string name, bool expectMultiBinding)
    {
        TextBlock control = (TextBlock)window.FindName(name);
        BindingBase? binding = BindingOperations.GetBindingBase(control, TextBlock.TextProperty);
        bool matches = expectMultiBinding ? binding is MultiBinding : binding is Binding;
        if (!matches)
        {
            throw new InvalidOperationException($"{name} 的绑定类型不符合优化后的预期。");
        }
    }

    /// <summary>比较实际文本与预期文本，并在失败时终止自检。</summary>
    /// <param name="scenario">当前验证场景。</param>
    /// <param name="actual">实际文本。</param>
    /// <param name="expected">预期文本。</param>
    private static void Verify(string scenario, string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{scenario}：预期“{expected}”，实际“{actual}”。"
            );
        }

        Console.WriteLine($"通过：{scenario}");
    }
}
