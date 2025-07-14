using System.Globalization;

namespace TCBrowser.Maui.Converters
{
    public class ActualWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BindableObject element)
            {
                return element.GetValue(VisualElement.WidthProperty);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
