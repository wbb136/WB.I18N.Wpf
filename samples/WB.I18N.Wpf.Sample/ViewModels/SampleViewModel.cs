using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WB.I18N.Wpf.Sample;

/// <summary>提供示例中可变化的资源键、备用文案和单参数、多参数格式化数据。</summary>
public sealed class SampleViewModel : INotifyPropertyChanged
{
    #region 展示数据

    private string _resourceKey = I18N.Greeting;
    private string _fallbackText = "备用文案 A";
    private int _count = 3;
    private string _productCode = "A-01";
    private decimal _amount = 1234.5m;

    /// <summary>获取或设置当前显示的资源键。</summary>
    public string ResourceKey
    {
        get
        {
            return _resourceKey;
        }
        set
        {
            SetProperty(ref _resourceKey, value);
        }
    }

    /// <summary>获取或设置缺少资源时显示的备用文案。</summary>
    public string FallbackText
    {
        get
        {
            return _fallbackText;
        }
        set
        {
            SetProperty(ref _fallbackText, value);
        }
    }

    /// <summary>获取或设置资源模板中的数量参数。</summary>
    public int Count
    {
        get
        {
            return _count;
        }
        set
        {
            SetProperty(ref _count, value);
        }
    }

    /// <summary>获取或设置多参数模板中的产品编号。</summary>
    public string ProductCode
    {
        get
        {
            return _productCode;
        }
        set
        {
            SetProperty(ref _productCode, value);
        }
    }

    /// <summary>获取或设置多参数模板中的金额。</summary>
    public decimal Amount
    {
        get
        {
            return _amount;
        }
        set
        {
            SetProperty(ref _amount, value);
        }
    }

    #endregion

    #region 示例操作

    /// <summary>在已有资源键和缺失资源键之间循环，以演示备用文案。</summary>
    public void AdvanceResourceKey()
    {
        if (ResourceKey == I18N.Greeting)
        {
            ResourceKey = I18N.Secondary;
        }
        else if (ResourceKey == I18N.Secondary)
        {
            ResourceKey = "MissingResource";
        }
        else
        {
            ResourceKey = I18N.Greeting;
        }
    }

    /// <summary>同时更新单参数和多参数模板使用的数据。</summary>
    public void AdvanceArguments()
    {
        Count++;
        Amount += 100m;
        ProductCode = ProductCode == "A-01" ? "B-02" : "A-01";
    }

    /// <summary>切换备用文案，以展示绑定的 Fallback 可以独立刷新。</summary>
    public void AdvanceFallbackText()
    {
        FallbackText = FallbackText == "备用文案 A" ? "备用文案 B" : "备用文案 A";
    }

    #endregion

    #region 属性通知

    /// <summary>属性发生变化时通知 WPF 绑定重新读取值。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>更新属性并只在值变化时发出通知。</summary>
    /// <typeparam name="T">属性值的类型。</typeparam>
    /// <param name="field">属性的后备字段。</param>
    /// <param name="value">要写入的新值。</param>
    /// <param name="propertyName">由编译器提供的属性名。</param>
    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
