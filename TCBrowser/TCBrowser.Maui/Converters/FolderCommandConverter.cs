using System.Globalization;
using System.Windows.Input;
using Trimble.Connect.Client.Models;

namespace TCBrowser.Maui.Converters
{
    public class FolderCommandConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This is for single value binding - not used in this case
            return null;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            var folderItem = values[0] as FolderItem;
            var command = values[1] as ICommand;

            if (folderItem == null || command == null)
                return null;

            // Check if it's a folder
            var hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(folderItem.Name));
            var commonFileExtensions = new[] { ".txt", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar", ".mp4", ".avi", ".mp3", ".wav" };
            bool hasCommonFileExtension = commonFileExtensions.Any(ext => folderItem.Name.ToLower().EndsWith(ext));
            bool isFolder = hasNoExtension && !hasCommonFileExtension;

            // Return the command only if it's a folder, otherwise return null
            return isFolder ? command : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

