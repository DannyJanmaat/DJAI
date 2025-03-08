using DJAI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace DJAI.Helpers
{
    public partial class RoleToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230)),
                    MessageRole.Assistant => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 240, 250)),
                    MessageRole.System => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 220, 220)),
                    _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240))
                };
            }
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public partial class BooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}