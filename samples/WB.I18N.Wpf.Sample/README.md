# WPF I18N Demo：完整用法

运行本项目即可看到左侧写法、右侧实时结果。顶部四个按钮分别切换 `zh` / `en-US`、资源键、格式参数和绑定备用文案。XAML 使用 WPF 默认命名空间，**不需要**声明 `xmlns:i18n`。

## 1. 声明 `.resx` 入口

本 Demo 实际编译的入口在 `Localization/I18N.cs`，默认资源是 `Resources/Language.resx`，英文资源是 `Resources/Language.en.resx`：

```csharp
using WB.I18N.Wpf;

namespace WB.I18N.Wpf.Sample;

/// <summary>声明 Demo 的语言资源入口。</summary>
[I18NResource("WB.I18N.Wpf.Sample.Resources.Language")]
public static partial class I18N
{
}
```

以下是同一个特性的其他两种**替代**声明方式，使用时仅保留一种：

```csharp
// 已有强类型资源类，其完整类型名与默认 .resx 清单名一致。
[I18NResource(typeof(MyApp.Resources.Language))]
```

```csharp
// 资源在独立程序集，锚点类型可为资源程序集中的任意类型。
[I18NResource(typeof(SharedTexts.ResourceAnchor), "SharedTexts.Resources.Language")]
```

安装 NuGet 包会自动加载生成器。直接通过项目引用时，应用项目还需像本 Demo 的 `.csproj` 一样把 `WB.I18N.Generator` 引用为 Analyzer。

Demo 项目还用 `<AdditionalFiles Include="Resources\Language.resx" />` 将默认资源交给生成器；生成器在 `I18N` 分部类中生成 `I18N.Greeting`、`I18N.ProductSummary` 等 `const string` 资源键，增删 `.resx` 键后重新构建即可更新。

## 2. XAML 调用

根元素只需标准 WPF 命名空间：

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
</Window>
```

固定键、字面备用文案、没有备用文案时显示键名：

```xml
<TextBlock Text="{I18N Key=Greeting}" />
<TextBlock Text="{I18N Key=MissingLiteral, Fallback='固定备用文案'}" />
<TextBlock Text="{I18N Key=NotDefinedWithoutFallback}" />
```

生成的键也可直接用于 XAML。声明 `xmlns:local="clr-namespace:WB.I18N.Wpf.Sample"` 后：

```xml
<TextBlock Text="{I18N Key={x:Static local:I18N.Greeting}}" />
```

资源键和备用文案都可以绑定。修改 ViewModel 属性并发出 `PropertyChanged` 后，原有控件立即更新：

```xml
<TextBlock Text="{I18N Key={Binding ResourceKey}, Fallback={Binding FallbackText}}" />
```

单参数或多参数格式化使用 `<I18N>` 的 `Arguments`。绑定参数变化及语言切换都会重新格式化：

```xml
<TextBlock.Text>
    <I18N Key="ItemCount">
        <I18N.Arguments>
            <Binding Path="Count" />
        </I18N.Arguments>
    </I18N>
</TextBlock.Text>
```

```xml
<TextBlock.Text>
    <I18N Key="ProductSummary">
        <I18N.Arguments>
            <Binding Path="ProductCode" />
            <Binding Path="Amount" />
        </I18N.Arguments>
    </I18N>
</TextBlock.Text>
```

选择 `en-US` 时，`ParentOnly` 从上级 `Language.en.resx` 回退；`NeutralOnly` 在英文资源中缺失，会回退到默认 `Language.resx`。没有资源和备用文案时显示键名。

## 3. C# 调用与即时切换

```csharp
I18N.Current.LanguageTag = "en-US";
string text = I18N.GetString(I18N.Greeting);
string backup = I18N.GetString("MissingFromCode", "C# 固定备用文案");
string formatted = I18N.Format(I18N.ProductSummary, "A-01", 1234.5m);
```

`MainWindow.xaml.cs` 展示了如何在按钮点击时切换 `LanguageTag` 并刷新直接调用 C# 取得的结果。XAML 中的 I18N 绑定由库自动刷新。`SampleViewModel.cs` 展示绑定资源键、备用文案及参数如何通过 `INotifyPropertyChanged` 更新。

## 4. 运行验证

在解决方案目录执行：

```powershell
dotnet build .\WB.I18N.Wpf.sln -c Release
dotnet run --project .\samples\WB.I18N.Wpf.Sample\WB.I18N.Wpf.Sample.csproj -c Release
dotnet run --project .\samples\WB.I18N.Wpf.Sample\WB.I18N.Wpf.Sample.csproj -c Release -- --verify
```

`--verify` 会打开真实 WPF 窗口，检查中英文切换、生成键的 C# 与 `x:Static` 用法、固定键和绑定键、两种备用文案、单/多参数格式化，以及上级语言和默认资源回退；验证失败时返回非零退出码。
