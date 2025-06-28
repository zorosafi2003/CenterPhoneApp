using Microsoft.Maui.Controls;
using System.Globalization;

namespace CentersBarCode.Converters;

public class BoolToFlyoutBehaviorConverter : IValueConverter
{
    public static readonly BoolToFlyoutBehaviorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showFlyout)
        {
            return showFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
        }
        return FlyoutBehavior.Disabled;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}