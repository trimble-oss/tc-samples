using System.Globalization;

namespace TCBrowser.Maui.Converters
{
    public class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Trimble.Connect.Client.Models.FolderItem folderItem)
            {
                // Check if it's a folder by looking at the file extension
                // Folders typically don't have extensions
                var hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(folderItem.Name));
                
                // Be more permissive - if no extension and no dots, likely a folder
                // Also check for common file extensions to be more accurate
                var commonFileExtensions = new[] { ".txt", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar", ".mp4", ".avi", ".mp3", ".wav" };
                bool hasCommonFileExtension = commonFileExtensions.Any(ext => folderItem.Name.ToLower().EndsWith(ext));
                
                bool isFolder = hasNoExtension && !hasCommonFileExtension;
                
                if (isFolder)
                {
                    return "📁"; // Folder icon
                }
                else
                {
                    return "📄"; // File icon
                }
            }
            
            return "📄"; // Default to file icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
