# WPF I18N Demo：完整用法

本目录是可运行的完整 WPF 应用。顶部按钮切换 `zh` / `en-US`、资源键、格式化参数和绑定备用文案；左列展示调用，右列展示实时结果。窗口使用 WPF 默认 XAML 命名空间，可直接写 `{I18N ...}`。Demo 可在 .NET 6、7、8、9 上运行。

## 项目文件

```text
WB.I18N.Wpf.Sample/
├── WB.I18N.Wpf.Sample.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Localization/
│   └── I18N.cs
├── ViewModels/
│   └── SampleViewModel.cs
├── Resources/
│   ├── Language.resx
│   └── Language.en.resx
└── Verification/
    └── VerificationRunner.cs
```

以上文件组成可直接运行的案例。`Language.resx` 是默认资源并提供自动生成的 Key；`Language.en.resx` 提供英文译文；`VerificationRunner.cs` 在真实窗口加载后执行自动检查。生成的 `I18N.Greeting` 等常量来自源生成器，不需要手写或保存 `LangKey` 类。

在仓库根目录执行：

```powershell
dotnet build .\WB.I18N.Wpf.sln -c Release
dotnet run --project .\samples\WB.I18N.Wpf.Sample\WB.I18N.Wpf.Sample.csproj -c Release -f net8.0-windows
```

完整解决方案需要 .NET 9 SDK；运行选定目标需要对应的 Windows Desktop 运行时。本仓库的 Demo 项目使用相对 `ProjectReference` 引用库和生成器，克隆仓库后可直接构建。

## 1. 声明 `.resx` 资源

实际运行的 `Localization/I18N.cs` 内容如下。项目程序集名是 `WB.I18N.Wpf.Sample`，所以 `Resources/Language.resx` 的完整清单名是 `WB.I18N.Wpf.Sample.Resources.Language`：

```csharp
using WB.I18N.Wpf;

namespace WB.I18N.Wpf.Sample;

/// <summary>声明示例资源，并由源生成器补全 C# 语言访问入口。</summary>
[I18NResource("WB.I18N.Wpf.Sample.Resources.Language")]
public static partial class I18N
{
}
```

项目文件通过 `<AdditionalFiles Include="Resources\Language.resx" />` 将默认资源交给生成器。每次添加或删除默认资源中的键后重新构建，`I18N` 类会得到对应的 `const string` 成员。`Language.en.resx` 作为普通资源参与运行时查找，不需要再次列入 `AdditionalFiles`。

如果已有强类型资源类，可以把整个 `I18N.cs` 换成下列内容；前提是 `MyApp.Resources.Language` 类型由项目实际生成：

```csharp
using WB.I18N.Wpf;

namespace MyApp;

/// <summary>通过强类型资源类声明应用语言资源。</summary>
[I18NResource(typeof(Resources.Language))]
public static partial class I18N
{
}
```

如果资源位于独立程序集，可用该程序集中的锚点类型和完整清单名。下面的 `ResourceAnchor.cs` 应放在 `SharedTexts` 资源程序集，且该程序集包含 `Resources/Language.resx`：

```csharp
namespace SharedTexts;

/// <summary>定位共享语言资源所在的程序集。</summary>
public sealed class ResourceAnchor
{
}
```

应用项目的 `I18N.cs` 改为：

```csharp
using WB.I18N.Wpf;

namespace MyApp;

/// <summary>通过共享资源程序集声明应用语言资源。</summary>
[I18NResource(typeof(SharedTexts.ResourceAnchor), "SharedTexts.Resources.Language")]
public static partial class I18N
{
}
```

这三种声明互斥，一个应用只保留一种。默认资源的 `AdditionalFiles` 应放在声明 `I18N` 的项目中。

## 2. XAML 全部用法

窗口根元素只需标准 WPF 命名空间，以及供生成键使用的 `local` 前缀：

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:WB.I18N.Wpf.Sample">
</Window>
```

固定资源键使用自动生成的常量。故意缺失的键没有生成常量，用原始字符串展示备用文案和键名回退：

```xml
<TextBlock Text="{I18N Key={x:Static local:I18N.Greeting}}" />
<TextBlock Text="{I18N Key=MissingLiteral, Fallback='固定备用文案'}" />
<TextBlock Text="{I18N Key=NotDefinedWithoutFallback}" />
```

资源键和备用文案都可以绑定。`SampleViewModel.ResourceKey`、`FallbackText` 改变并发出 `PropertyChanged` 后，已有控件会立即更新：

```xml
<TextBlock Text="{I18N Key={Binding ResourceKey}, Fallback={Binding FallbackText}}" />
```

格式化参数使用 `I18N.Arguments`。参数变化和语言切换都会触发重新格式化，金额还按所选语言区域设置显示：

```xml
<TextBlock.Text>
    <I18N Key="{x:Static local:I18N.ItemCount}">
        <I18N.Arguments>
            <Binding Path="Count" />
        </I18N.Arguments>
    </I18N>
</TextBlock.Text>
```

```xml
<TextBlock.Text>
    <I18N Key="{x:Static local:I18N.ProductSummary}">
        <I18N.Arguments>
            <Binding Path="ProductCode" />
            <Binding Path="Amount" />
        </I18N.Arguments>
    </I18N>
</TextBlock.Text>
```

样式的 `Setter` 也能使用同一个扩展；Demo 中的 `LocalizedGreetingStyle` 和 `StyledGreetingText` 是可运行实例。把 `Style` 放在 `Grid.Resources`，把 `TextBlock` 放在该网格中：

```xml
<Style x:Key="LocalizedGreetingStyle" TargetType="TextBlock">
    <Setter Property="Text"
            Value="{I18N Key={x:Static local:I18N.Greeting}}" />
</Style>
<TextBlock Style="{StaticResource LocalizedGreetingStyle}" />
```

选择 `en-US` 时，`ParentOnly` 从上级 `Language.en.resx` 读取，`NeutralOnly` 回退到默认 `Language.resx`。没有资源和备用文案时显示原始键名。

## 3. C# 调用与即时切换

`MainWindow.xaml.cs` 展示 C# 查值、格式化和按钮点击切换语言：

```csharp
I18N.Current.LanguageTag = "en-US";
string text = I18N.GetString(I18N.Greeting);
string backup = I18N.GetString("MissingFromCode", "C# 固定备用文案");
string formatted = I18N.Format(I18N.ProductSummary, "A-01", 1234.5m);
```

设置 `LanguageTag` 后，现有 XAML 绑定自动刷新。C# 直接取得的字符串是一次性结果；Demo 的 `RefreshCodeExamples` 在切换按钮中重新读取。`SampleViewModel.cs` 展示可绑定的键、备用文案和格式化数据如何发出 `INotifyPropertyChanged` 通知。

## 4. 复制为独立 WPF 项目

复制本目录中的 `App.xaml`、`App.xaml.cs`、`MainWindow.xaml`、`MainWindow.xaml.cs`、`Localization/I18N.cs`、`ViewModels/SampleViewModel.cs`、两个 `Resources/*.resx` 和 `Verification/VerificationRunner.cs`。保留相对文件夹结构。将这些源码中的 `WB.I18N.Wpf.Sample` 统一改成自己的项目命名空间，例如 `MyApp`；同时把 `I18NResource` 的完整清单名改成 `MyApp.Resources.Language`。`App.xaml` 与 `MainWindow.xaml` 的 `x:Class`、窗口的 `xmlns:local` 也要对应 `MyApp`。

独立项目不再使用本仓库的相对 `ProjectReference`。下列 `MyApp.csproj` 是可直接复制的完整包引用配置，示例选择 .NET 8；需要其他版本时把 `TargetFramework` 改为 `net6.0-windows`、`net7.0-windows` 或 `net9.0-windows`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>MyApp</RootNamespace>
    <AssemblyName>MyApp</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="WB.I18N.Wpf" Version="1.1.1" />
    <AdditionalFiles Include="Resources\Language.resx" />
  </ItemGroup>
</Project>
```

`1.1.1` 包当前由本仓库源码本地生成。执行 `dotnet pack .\src\WB.I18N.Wpf\WB.I18N.Wpf.csproj -c Release` 后，把 `WB.I18N.Wpf.1.1.1.nupkg` 放进独立项目的 `packages` 文件夹。项目旁的完整 `NuGet.Config` 为：

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

从本次交付的独立 Demo ZIP 解压运行时，`packages`、`NuGet.Config` 和所有源码已放好。将其中的 `WB.I18N.Wpf.Sample.csproj` 作为完整案例打开即可；复制到自己的应用时按上面步骤改命名空间和资源名。

## 5. 自动验证

在仓库根目录执行以下命令，逐个打开真实 WPF 窗口并检查中文、英文、生成键、固定和绑定资源键、备用文案、格式化参数、样式 `Setter`、上级语言回退与默认资源回退：

```powershell
dotnet build .\WB.I18N.Wpf.sln -c Release
$demo = '.\samples\WB.I18N.Wpf.Sample\WB.I18N.Wpf.Sample.csproj'
foreach ($framework in @('net6.0-windows', 'net7.0-windows', 'net8.0-windows', 'net9.0-windows'))
{
    dotnet run --project $demo -c Release -f $framework -- --verify
    if ($LASTEXITCODE -ne 0)
    {
        throw "$framework 的 WPF 自检失败。"
    }
}
```

`--verify` 失败时以非零退出码结束。独立 Demo ZIP 也可在解压目录执行 `dotnet restore`、`dotnet run -f net8.0-windows -- --verify`。