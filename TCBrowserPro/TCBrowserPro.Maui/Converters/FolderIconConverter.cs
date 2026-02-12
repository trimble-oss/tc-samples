using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TCBrowserPro.Maui.ViewModels;
using Trimble.Connect.Client.Models;

namespace TCBrowserPro.Maui.Converters
{
    public class FolderIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FolderItem folderItem)
            {
                // Get the ViewModel from the parameter (passed from XAML)
                ProjectVm viewModel = parameter as ProjectVm;
                
                // Check if it has children (most reliable method for folders with non-zero sizes)
                bool hasChildren = false;
                if (viewModel != null)
                {
                    try
                    {
                        // Use reflection to access the private _allFiles property
                        var allFilesProperty = typeof(ProjectVm).GetProperty("_allFiles", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        if (allFilesProperty != null)
                        {
                            var allFiles = allFilesProperty.GetValue(viewModel) as System.Collections.ObjectModel.ObservableCollection<FolderItem>;
                            if (allFiles != null)
                            {
                                hasChildren = allFiles.Any(f => f.ParentIdentifier == folderItem.Identifier);
                            }
                        }
                    }
                    catch
                    {
                        // If reflection fails, fall back to other checks
                    }
                }
                
                // Use the same detection logic as IsFolderItem() in ProjectsListViewModel
                // Check 1: VersionIdentifier (some folders don't have it, but some do!)
                bool hasNoVersionId = string.IsNullOrEmpty(folderItem.VersionIdentifier);
                
                // Check 2: Size (folders typically have Size = 0 or null, but can have non-zero if they contain files)
                bool hasZeroSize = folderItem.Size == null || folderItem.Size == 0;
                
                // Check 3: File extension (folders typically don't have extensions)
                bool hasNoExtension = string.IsNullOrEmpty(Path.GetExtension(folderItem.Name));
                
                // Decision logic: 
                // 1. If it has children, it's definitely a folder (most reliable)
                // 2. Size=0 AND no extension = folder (even if it has VersionIdentifier)
                // 3. If no VersionIdentifier, it's definitely a folder (traditional case)
                // 4. If no extension, it's likely a folder (be permissive)
                bool isFolder = hasChildren || (hasZeroSize && hasNoExtension);
                
                if (hasNoVersionId)
                {
                    isFolder = true;
                }
                else if (hasNoExtension && !hasChildren)
                {
                    // If no extension and we couldn't check for children, be permissive
                    // Most items without extensions are folders
                    isFolder = true;
                }
                
                if (isFolder)
                {
                    return "📂"; // Distinct folder icon
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
