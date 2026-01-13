using System.Globalization;
using System.IO;
using Trimble.Connect.Client.Models;

namespace TCBrowser.Maui.Converters
{
    public class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FolderItem folderItem)
            {
                // Use the same detection logic as IsFolderItem() in ProjectsListViewModel
                // Check 1: VersionIdentifier (most reliable - folders don't have it, files do)
                bool hasNoVersionId = string.IsNullOrEmpty(folderItem.VersionIdentifier);
                
                // Check 2: Size (folders typically have Size = 0 or null)
                bool hasZeroSize = folderItem.Size == null || folderItem.Size == 0;
                
                // Check 3: File extension (folders typically don't have extensions)
                bool hasNoExtension = string.IsNullOrEmpty(Path.GetExtension(folderItem.Name));
                
                // Decision logic: Size=0 AND no extension = folder (even if it has VersionIdentifier)
                bool isFolder = hasZeroSize && hasNoExtension;
                
                // If no VersionIdentifier, it's definitely a folder (most reliable check)
                if (hasNoVersionId)
                {
                    isFolder = true;
                }
                
                if (isFolder)
                {
                    return "📁"; // Folder icon
                }
                else
                {
                    return ""; // No icon for files
                }
            }
            
            return ""; // Default to no icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
