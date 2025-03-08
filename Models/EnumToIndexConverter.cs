using Microsoft.UI.Xaml.Data;

namespace DJAI.Models
{
    public partial class EnumToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is AIProvider provider ? (int)provider : (object)0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is int index ? (AIProvider)index : (object)AIProvider.Anthropic;
        }
    }
}