// Create file: Converters/Converters.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.Versioning;
using Windows.UI;

namespace DJAI.Converters
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue
                ? new SolidColorBrush(Color.FromArgb(255, 227, 242, 253)) // Light blue for user
                : new SolidColorBrush(Color.FromArgb(255, 241, 241, 241)); // Light gray for AI
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class BoolToSenderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue ? "You" : "AI";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class BoolNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool boolValue && boolValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
