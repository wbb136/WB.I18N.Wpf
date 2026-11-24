using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WB.I18N.Generator;

/// <summary>根据调用方的资源声明生成本地化入口和程序集资源标记。</summary>
[Generator(LanguageNames.CSharp)]
public sealed class I18NResourceGenerator : IIncrementalGenerator
{
    private const string AttributeName = "WB.I18N.Wpf.I18NResourceAttribute";
    private const string Category = "Localization";

    private static readonly DiagnosticDescriptor InvalidDeclaration = new(
        "WBI18N001",
        "无效的 I18N 类声明",
        "I18NResource 必须标记在非文件局部、非泛型、非嵌套的 static partial class 上",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor MultipleDeclarations = new(
        "WBI18N002",
        "重复的 I18N 资源声明",
        "一个程序集只能声明一个带 I18NResource 特性的 I18N 类",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor ConflictingRegistration = new(
        "WBI18N003",
        "I18N 资源声明不一致",
        "I18N 类与程序集上的 I18NResource 声明指向不同资源，请统一声明",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor InvalidRegistration = new(
        "WBI18N004",
        "无效的 I18N 资源位置",
        "I18NResource 必须提供非空的资源名称或有效的资源类型",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor InvalidResourceKey = new(
        "WBI18N005",
        "无法生成资源键常量",
        "资源键“{0}”不是有效的 C# 标识符；仍可通过 I18N.GetString(\"{0}\") 按原样读取",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor ConflictingResourceKey = new(
        "WBI18N006",
        "资源键常量名称冲突",
        "资源键“{0}”与 I18N 的现有成员或其他资源键冲突",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor InvalidResourceFile = new(
        "WBI18N007",
        "无法读取资源键文件",
        "资源文件“{0}”无法解析：{1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor AmbiguousResourceFile = new(
        "WBI18N008",
        "重复的默认资源文件",
        "找到多个名为“{0}”的 AdditionalFiles，无法确定 I18N 的资源键来源",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>注册类级资源特性和程序集级资源特性的增量读取规则。</summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var declarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) =>
            {
                return node is TypeDeclarationSyntax;
            },
            static (attributeContext, _) =>
            {
                return attributeContext;
            }
        );

        var assemblyRegistration = context.CompilationProvider.Select(static (compilation, _) =>
        {
            return compilation.Assembly.GetAttributes().FirstOrDefault(static attribute =>
            {
                return attribute.AttributeClass?.ToDisplayString() == AttributeName;
            });
        });

        var resourceFiles = context.AdditionalTextsProvider
            .Where(static file =>
            {
                return file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (file, cancellationToken) =>
            {
                return (file.Path, Content: file.GetText(cancellationToken)?.ToString());
            })
            .Collect();

        context.RegisterSourceOutput(
            declarations.Collect().Combine(assemblyRegistration).Combine(resourceFiles),
            static (productionContext, input) =>
            {
                Generate(productionContext, input.Left.Left, input.Left.Right, input.Right);
            }
        );
    }

    /// <summary>检查声明并生成调用入口，保留兼容的现有程序集标记。</summary>
    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<GeneratorAttributeSyntaxContext> declarations,
        AttributeData? assemblyRegistration,
        ImmutableArray<(string Path, string? Content)> resourceFiles
    )
    {
        if (declarations.IsDefaultOrEmpty)
        {
            return;
        }

        if (declarations.Length > 1)
        {
            foreach (GeneratorAttributeSyntaxContext declaration in declarations)
            {
                context.ReportDiagnostic(Diagnostic.Create(MultipleDeclarations, declaration.TargetNode.GetLocation()));
            }

            return;
        }

        GeneratorAttributeSyntaxContext resource = declarations[0];
        Location location = resource.TargetNode.GetLocation();
        if (resource.TargetNode is not ClassDeclarationSyntax classDeclaration ||
            resource.TargetSymbol is not INamedTypeSymbol { IsStatic: true, Arity: 0, ContainingType: null } target ||
            classDeclaration.Modifiers.Any(static modifier =>
            {
                return modifier.ValueText == "file";
            }) ||
            !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidDeclaration, location));
            return;
        }

        string? classArguments = GetRegistrationArguments(resource.Attributes[0]);
        if (classArguments is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidRegistration, location));
            return;
        }

        if (assemblyRegistration is not null && GetRegistrationArguments(assemblyRegistration) != classArguments)
        {
            context.ReportDiagnostic(Diagnostic.Create(ConflictingRegistration, location));
            return;
        }

        string registration = assemblyRegistration is null
            ? $"[assembly: global::WB.I18N.Wpf.I18NResourceAttribute({classArguments})]"
            : string.Empty;
        string keyMembers = GetKeyMembers(context, location, target, resource.Attributes[0], resourceFiles);
        context.AddSource("I18N.g.cs", SourceText.From(CreateSource(target, registration, keyMembers), Encoding.UTF8));
    }

    /// <summary>从已登记的默认 .resx 生成同一 I18N 类中的资源键常量。</summary>
    private static string GetKeyMembers(
        SourceProductionContext context,
        Location location,
        INamedTypeSymbol target,
        AttributeData registration,
        ImmutableArray<(string Path, string? Content)> resourceFiles
    )
    {
        string fileName = GetResourceFileName(registration);
        (string Path, string? Content)[] matches = resourceFiles
            .Where(file =>
            {
                return string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();
        if (matches.Length == 0)
        {
            // 未把 .resx 设为 AdditionalFiles 的现有项目仍生成原有查值入口。
            return string.Empty;
        }

        if (matches.Length > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(AmbiguousResourceFile, location, fileName));
            return string.Empty;
        }

        (string path, string? content) = matches[0];
        XDocument document;
        try
        {
            document = XDocument.Parse(content ?? string.Empty);
        }
        catch (XmlException exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidResourceFile, location, path, exception.Message));
            return string.Empty;
        }

        HashSet<string> memberNames = new(StringComparer.Ordinal)
        {
            "RegisterI18NResources",
            "Current",
            "GetString",
            "Format"
        };
        foreach (ISymbol member in target.GetMembers())
        {
            memberNames.Add(member.Name);
        }

        StringBuilder members = new();
        foreach (XElement entry in document.Root?.Elements().Where(element =>
            {
                return element.Name.LocalName == "data";
            })
            ?? Enumerable.Empty<XElement>())
        {
            string? key = entry.Attribute("name")?.Value;
            if (key is null)
            {
                continue;
            }

            if (!SyntaxFacts.IsValidIdentifier(key))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidResourceKey, location, key));
                continue;
            }

            if (!memberNames.Add(key))
            {
                context.ReportDiagnostic(Diagnostic.Create(ConflictingResourceKey, location, key));
                continue;
            }

            members.Append("    /// <summary>资源键 ").Append(key).AppendLine("。</summary>");
            members.Append("    public const string ").Append(key).Append(" = ")
                .Append(SymbolDisplay.FormatLiteral(key, quote: true)).AppendLine(";");
            members.AppendLine();
        }

        return members.ToString();
    }

    /// <summary>根据资源声明确定应读取的默认 .resx 文件名。</summary>
    private static string GetResourceFileName(AttributeData registration)
    {
        ImmutableArray<TypedConstant> arguments = registration.ConstructorArguments;
        if (arguments.Length == 1 && arguments[0].Value is ITypeSymbol resourceType)
        {
            return resourceType.Name + ".resx";
        }

        string baseName = arguments.Length == 2
            ? (string)arguments[1].Value!
            : (string)arguments[0].Value!;
        int lastDot = baseName.LastIndexOf('.');
        return baseName.Substring(lastDot + 1) + ".resx";
    }

    /// <summary>把经编译器解析的特性参数还原为有效的程序集声明参数。</summary>
    private static string? GetRegistrationArguments(AttributeData attribute)
    {
        ImmutableArray<TypedConstant> arguments = attribute.ConstructorArguments;
        if (arguments.Length == 1 && arguments[0].Value is string localBaseName &&
            !string.IsNullOrWhiteSpace(localBaseName))
        {
            return SymbolDisplay.FormatLiteral(localBaseName, quote: true);
        }

        if (arguments.Length == 1 && arguments[0].Value is ITypeSymbol resourceType &&
            resourceType.TypeKind != TypeKind.Error)
        {
            string typeName = resourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"typeof({typeName})";
        }

        if (arguments.Length == 2 && arguments[0].Value is ITypeSymbol anchor &&
            anchor.TypeKind != TypeKind.Error && arguments[1].Value is string externalBaseName &&
            !string.IsNullOrWhiteSpace(externalBaseName))
        {
            string typeName = anchor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string baseName = SymbolDisplay.FormatLiteral(externalBaseName, quote: true);
            return $"typeof({typeName}), {baseName}";
        }

        return null;
    }

    /// <summary>生成和调用方类型匹配的分部声明，并转发到唯一的本地化入口。</summary>
    private static string CreateSource(INamedTypeSymbol target, string registration, string keyMembers)
    {
        string namespaceDeclaration = target.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : "namespace " + target.ContainingNamespace.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
                    SymbolDisplayGlobalNamespaceStyle.Omitted
                )
            ) + ";";
        string name = target.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        string accessibility = target.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";

        return $$"""
            // <auto-generated/>
            #nullable enable

            {{registration}}

            {{namespaceDeclaration}}

            {{accessibility}} static partial class {{name}}
            {
                /// <summary>在程序集初始化时注册该程序集声明的语言资源。</summary>
                [global::System.Runtime.CompilerServices.ModuleInitializerAttribute]
                internal static void RegisterI18NResources()
                {
                    global::WB.I18N.Wpf.I18NExtension.RegisterResourceAssembly(typeof({{name}}).Assembly);
                }

                /// <summary>获取当前应用共享的语言状态。</summary>
                public static global::WB.I18N.Wpf.I18NExtension Current
                {
                    get
                    {
                        return global::WB.I18N.Wpf.I18NExtension.Current;
                    }
                }

                /// <summary>查找当前语言的资源文案。</summary>
                public static string GetString(string? resourceKey, string? fallback = null)
                {
                    return global::WB.I18N.Wpf.I18NExtension.GetString(resourceKey, fallback);
                }

                /// <summary>查找并格式化当前语言的资源模板。</summary>
                public static string Format(string resourceKey, params object?[]? arguments)
                {
                    return global::WB.I18N.Wpf.I18NExtension.Format(resourceKey, arguments);
                }

            {{keyMembers}}
            }
            """;
    }
}
