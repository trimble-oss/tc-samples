using System.Globalization;
using Trimble.Connect.Client.Models;

namespace TCBrowser.Maui.Converters
{
    public class IsFolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FolderItem folderItem)
            {
                // Use the same permissive logic as the ViewModel
                // Check if it has no file extension (folders typically don't have extensions)
                var hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(folderItem.Name));
                
                // Be more permissive: if it has no extension, treat it as a potential folder
                // This matches the ViewModel's logic: isFolder = hasChildren || hasNoExtension
                // Since we can't check for children in the converter, we use the permissive approach
                // Items without extensions are treated as folders (even if they have dots in the name)
                bool isFolder = hasNoExtension;
                
                return isFolder;
            }
            
            return false; // Default to not a folder
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

