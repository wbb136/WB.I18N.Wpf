using System.Windows;

namespace WB.I18N.Wpf.Sample;

/// <summary>启动示例窗口，并在指定参数下运行 WPF 界面自检。</summary>
public partial class App : Application
{
    /// <summary>初始化资源入口并显示示例窗口。</summary>
    /// <param name="e">应用启动参数。</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        I18N.Current.LanguageTag = "zh";
        MainWindow window = new MainWindow();
        MainWindow = window;

        if (Array.IndexOf(e.Args, "--verify") >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            window.Loaded += OnVerificationWindowLoaded;
        }

        window.Show();
    }

    /// <summary>等待真实 WPF 视图加载完成后启动绑定与语言切换验证。</summary>
    /// <param name="sender">已加载的窗口。</param>
    /// <param name="e">加载事件参数。</param>
    private void OnVerificationWindowLoaded(object sender, RoutedEventArgs e)
    {
        MainWindow window = (MainWindow)sender;
        window.Loaded -= OnVerificationWindowLoaded;
        _ = RunVerificationAsync(window);
    }

    /// <summary>执行自检并通过进程退出码报告结果。</summary>
    /// <param name="window">已加载的示例窗口。</param>
    private async Task RunVerificationAsync(MainWindow window)
    {
        int exitCode;

        try
        {
            await VerificationRunner.RunAsync(window);
            Console.WriteLine("WPF 本地化自检通过。");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"WPF 本地化自检失败：{exception}");
            exitCode = 1;
        }

        Environment.ExitCode = exitCode;
        Shutdown(exitCode);
    }
}
