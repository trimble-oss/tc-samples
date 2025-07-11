using System.Collections;
using System.Globalization;

namespace SignIn.Maui.Converters
{
    public class DictionaryToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IDictionary dictionary))
                return null;
         
            return dictionary.Keys.Cast<object>().ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
