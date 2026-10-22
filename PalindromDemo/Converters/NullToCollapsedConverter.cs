using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PalindromDemo.Converters;

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value == null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}