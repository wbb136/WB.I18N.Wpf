using System.Windows;

namespace WB.I18N.Wpf.Sample;

/// <summary>展示无需重启即可切换语言、资源键和备用文案的示例窗口。</summary>
public partial class MainWindow : Window
{
    /// <summary>创建窗口并设置用于绑定资源键和格式参数的视图模型。</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new SampleViewModel();
        RefreshCodeExamples();
    }

    /// <summary>在中文和英文之间切换当前界面语言。</summary>
    /// <param name="sender">触发切换的按钮。</param>
    /// <param name="e">按钮点击事件参数。</param>
    private void OnSwitchLanguage(object sender, RoutedEventArgs e)
    {
        bool isChinese = I18N.Current.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        I18N.Current.LanguageTag = isChinese ? "en-US" : "zh";
        RefreshCodeExamples();
    }

    /// <summary>循环切换已有资源键与缺失资源键，演示绑定和备用文案。</summary>
    /// <param name="sender">触发切换的按钮。</param>
    /// <param name="e">按钮点击事件参数。</param>
    private void OnSwitchResourceKey(object sender, RoutedEventArgs e)
    {
        SampleViewModel viewModel = (SampleViewModel)DataContext;
        viewModel.AdvanceResourceKey();
    }

    /// <summary>更新两个格式化模板的绑定参数和 C# 格式化示例。</summary>
    /// <param name="sender">触发更新的按钮。</param>
    /// <param name="e">按钮点击事件参数。</param>
    private void OnUpdateArguments(object sender, RoutedEventArgs e)
    {
        SampleViewModel viewModel = (SampleViewModel)DataContext;
        viewModel.AdvanceArguments();
        RefreshCodeExamples();
    }

    /// <summary>单独更改缺失资源键对应的绑定备用文案。</summary>
    /// <param name="sender">触发更新的按钮。</param>
    /// <param name="e">按钮点击事件参数。</param>
    private void OnUpdateFallback(object sender, RoutedEventArgs e)
    {
        SampleViewModel viewModel = (SampleViewModel)DataContext;
        viewModel.AdvanceFallbackText();
    }

    /// <summary>重新计算 C# 直接调用的结果；XAML 绑定会由语言状态自行刷新。</summary>
    internal void RefreshCodeExamples()
    {
        SampleViewModel viewModel = (SampleViewModel)DataContext;
        LanguageTagText.Text = I18N.Current.LanguageTag;
        CodeGetStringText.Text = I18N.GetString(I18N.Greeting);
        CodeGetFallbackText.Text = I18N.GetString("MissingFromCode", "C# 固定备用文案");
        CodeFormatText.Text = I18N.Format(I18N.ProductSummary, viewModel.ProductCode, viewModel.Amount);
    }
}
