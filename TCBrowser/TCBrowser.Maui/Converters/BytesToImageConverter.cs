using System.Globalization;


namespace TCBrowser.Maui.Converters
{
    public partial class BytesToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) 
                return null;

            var bytes = (byte[])value;

            var streamSource = ImageSource.FromStream(() => new MemoryStream(bytes));

            return streamSource;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
