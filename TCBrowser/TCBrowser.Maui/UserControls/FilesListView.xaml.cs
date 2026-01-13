using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TCBrowser.Maui.Converters;
using TCBrowser.Maui.ViewModels;
using Trimble.Connect.Client.Models;

namespace TCBrowser.Maui.UserControls;

public partial class FilesListView : ContentView
{
	private readonly IsFolderConverter _isFolderConverter = new IsFolderConverter();

	public FilesListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    { 
        base.OnBindingContextChanged();
    }

    private void Frame_Loaded(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is FolderItem folderItem && BindingContext is ProjectVm vm)
        {
            // Clear any existing tap gestures to avoid duplicates
            var existingTapGestures = frame.GestureRecognizers.OfType<TapGestureRecognizer>().ToList();
            foreach (var gesture in existingTapGestures)
            {
                frame.GestureRecognizers.Remove(gesture);
            }
            
            // Use the same folder detection logic as ViewModel (VersionIdentifier-based)
            // Check 1: VersionIdentifier (most reliable - folders don't have it, files do)
            bool hasNoVersionId = string.IsNullOrEmpty(folderItem.VersionIdentifier);
            
            // Check 2: Size (folders typically have Size = 0 or null)
            bool hasZeroSize = folderItem.Size == null || folderItem.Size == 0;
            
            // Check 3: File extension (folders typically don't have extensions)
            bool hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(folderItem.Name));
            
            // Decision logic: Size=0 AND no extension = folder (even if it has VersionIdentifier)
            bool isFolder = hasZeroSize && hasNoExtension;
            
            // If no VersionIdentifier, it's definitely a folder (most reliable check)
            if (hasNoVersionId)
            {
                isFolder = true;
            }
            
            if (isFolder)
            {
                // Add tap gesture recognizer for folders (navigate into folder)
                var tapGesture = new TapGestureRecognizer
                {
                    Command = vm.FolderTappedCommand,
                    CommandParameter = folderItem
                };
                frame.GestureRecognizers.Add(tapGesture);
            }
            else
            {
                // Add tap gesture recognizer for files (open in browser)
                var tapGesture = new TapGestureRecognizer
                {
                    Command = vm.FileTappedCommand,
                    CommandParameter = folderItem
                };
                frame.GestureRecognizers.Add(tapGesture);
            }
        }
    }
}