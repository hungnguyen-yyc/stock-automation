using System.Windows;
using System.Windows.Data;

namespace Stock.UI.Utilities;

public class BooleanToCollapsedVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(Visibility) || !(value is bool))
        {
            throw new InvalidOperationException("The target must be a boolean");
        }

        if ((bool)value)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}