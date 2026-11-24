# WB.I18N.Wpf

面向 .NET 8 WPF 的 `.resx` 多语言工具库。资源声明、生成的 `I18N.GetString` / `I18N.Format` / `I18N.Current.LanguageTag` 与 `WB.I18N.Avalonia` 2.2.0 保持相近的调用方式；界面绑定只使用 WPF 的 `MarkupExtension`、`Binding` 和 `MultiBinding`。

## 资源声明

在应用项目的 `Resources` 文件夹加入默认资源 `Language.resx`，按需加入 `Language.en.resx`、`Language.fr-CA.resx` 等语言文件。示例资源的完整清单名是 `MyApp.Resources.Language`。在应用中声明一个顶层静态分部类：

```csharp
using WB.I18N.Wpf;

namespace MyApp.Localization;

/// <summary>提供应用统一的多语言入口。</summary>
[I18NResource("MyApp.Resources.Language")]
public static partial class I18N
{
}
```

资源声明有三种互斥写法。Demo 使用上面的字符串资源名；如果已有强类型资源类，或者资源放在独立程序集，可以把特性替换为以下相应写法：

```csharp
// 强类型资源类的完整名称与默认 .resx 清单名一致。
[I18NResource(typeof(MyApp.Resources.Language))]
```

```csharp
// 资源类型可为目标程序集中的任意锚点类型，资源名需填写真实清单名。
[I18NResource(typeof(SharedTexts.ResourceAnchor), "SharedTexts.Resources.Language")]
```

每个程序集最多声明一个带特性的顶层 `static partial` 类，三种写法不能同时放入一个 Demo 进程。生成器会补齐调用入口，并在程序集初始化时注册资源；应用自身的声明优先于引用库的声明。

直接引用本仓库项目时，应用除了引用 `src/WB.I18N.Wpf`，还需把 `src/WB.I18N.Generator` 作为 Analyzer 引用。`samples/WB.I18N.Wpf.Sample` 已给出项目文件写法。安装构建出的 NuGet 包时，生成器随包自动加载，无需单独引用生成器项目。

## 自动生成资源键

从 1.0.3 起，把默认 `.resx` 文件交给源生成器的 `AdditionalFiles`，生成器会在同一个 `I18N` 分部类中生成资源键常量。每次资源文件变化后重新构建即可更新常量，无需手写 Key 类：

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Language.resx" />
</ItemGroup>
```

```csharp
string title = I18N.GetString(I18N.Greeting);
string message = I18N.Format(I18N.Welcome, "Alice");
```

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:MyApp.Localization">
    <TextBlock Text="{I18N Key={x:Static local:I18N.Greeting}}" />
</Window>
```

资源键必须是合法且不与 `I18N` 入口成员冲突的 C# 标识符；不合法的键仍可用原始字符串调用 `GetString` 或 XAML 的 `Key`。若资源声明在独立程序集，`AdditionalFiles` 应放在声明 `I18N` 的项目中；其他 WPF 项目可通过 `x:Static` 引用该程序集公开的 `I18N` 类。

## C# 调用

```csharp
I18N.Current.LanguageTag = "en-US";
string title = I18N.GetString("Title");
string fallback = I18N.GetString("NotDefined", "备用文案");
string message = I18N.Format("Welcome", "Alice");
```

`LanguageTag` 默认是 `zh`；可设置 .NET 识别的语言或地区标签，空值和无效值会被忽略。切换只改变本库的语言状态，不修改线程的 `CurrentCulture` 或 `CurrentUICulture`。`Format` 按当前选择的语言格式化数字与日期。资源按当前地区、上级语言、默认 `.resx` 回退；仍找不到键时使用 `Fallback`，没有备用文案时显示键名。空键返回备用文案或空字符串。自 1.0.2 起，非空资源键按原样查询，首尾空格也属于键名；例如 `Capacity` 与 `Capacity ` 可分别使用。

## WPF XAML 调用

库已映射到 WPF 默认 XAML 命名空间。窗口使用标准 `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` 时，不用另行声明 `xmlns:i18n`，直接写 `{I18N ...}` 或 `<I18N>`。

固定键与备用文案：

```xml
<TextBlock Text="{I18N Key=Title}" />
<TextBlock Text="{I18N Key=Missing, Fallback='备用文案'}" />
```

`Key` 和 `Fallback` 都可以绑定到 DataContext。属性变化时，现有控件会重新查找文案：

```xml
<TextBlock Text="{I18N Key={Binding ResourceKey}, Fallback={Binding FallbackText}}" />
```

格式化参数使用 WPF `Binding`。参数本身变化及语言切换都会重新格式化：

```xml
<TextBlock>
  <TextBlock.Text>
    <I18N Key="Welcome">
      <I18N.Arguments>
        <Binding Path="UserName" />
      </I18N.Arguments>
    </I18N>
  </TextBlock.Text>
</TextBlock>
```

在 ViewModel 属性变化时发出 `INotifyPropertyChanged.PropertyChanged`；设置 `I18N.Current.LanguageTag` 后，已有 XAML 绑定立即刷新，无需重启窗口。这个标记扩展用于支持 WPF 绑定的依赖属性，例如 `TextBlock.Text`、`Button.Content`。

## 性能实现（1.0.1 起）

固定资源键且无格式参数时，标记扩展只创建一个 WPF `Binding`；绑定资源键、备用文案或格式参数时才使用 `MultiBinding`，且只添加实际会变化的子绑定。资源查找按当前语言和实际访问的键进行按需缓存，语言切换或资源重新注册时会更换缓存，不预先加载全部 `.resx` 键。缓存会随着当前语言中已查询的不同键数占用内存；外部调用方式与 1.0.0 相同。

## 示例与验证

打开 `WB.I18N.Wpf.sln`，运行 `samples/WB.I18N.Wpf.Sample`。Demo 左列展示实际写法，右列展示运行结果；顶部按钮可切换 `zh` / `en-US`、循环资源键、更新格式参数和备用文案。它包含以下全部主要调用方式：

示例项目中的 `samples/WB.I18N.Wpf.Sample/README.md` 给出每种调用的完整代码。

| 用法 | Demo 中的写法或结果 |
| --- | --- |
| 当前语言 | `I18N.Current.LanguageTag`；点击语言按钮时已有界面立即刷新 |
| 固定资源键 | `{I18N Key=Greeting}` |
| 固定备用文案 | `{I18N Key=MissingLiteral, Fallback='固定备用文案'}` |
| 绑定资源键与备用文案 | `{I18N Key={Binding ResourceKey}, Fallback={Binding FallbackText}}`；两个属性可分别变化 |
| 缺键无备用文案 | `{I18N Key=NotDefinedWithoutFallback}`；直接显示键名 |
| 父级语言回退 | 选 `en-US`，`ParentOnly` 从 `Language.en.resx` 读取 |
| 默认资源回退 | 选 `en-US`，`NeutralOnly` 从默认 `Language.resx` 读取 |
| 单参数格式化 | `<I18N Key="ItemCount">` 的 `Arguments` 包含一个 `Binding` |
| 多参数格式化 | `<I18N Key="ProductSummary">` 的 `Arguments` 包含产品编号与金额两个 `Binding`，金额按所选语言格式化 |
| C# 直接调用 | `I18N.GetString(key)`、`I18N.GetString(key, fallback)`、`I18N.Format(key, args)` 的结果均显示在窗口中 |

资源声明的三种互斥形式见上面的“资源声明”一节；本 Demo 选用字符串资源名。以下命令会构建并执行窗口自检，覆盖中英文即时切换、资源键绑定、参数变化与各级回退：

```powershell
dotnet build .\WB.I18N.Wpf.sln -c Release
dotnet run --project .\samples\WB.I18N.Wpf.Sample\WB.I18N.Wpf.Sample.csproj -c Release -- --verify
```

打包命令：

```powershell
dotnet pack .\src\WB.I18N.Wpf\WB.I18N.Wpf.csproj -c Release
```
