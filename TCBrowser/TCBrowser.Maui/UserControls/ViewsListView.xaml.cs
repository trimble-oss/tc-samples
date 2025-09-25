using System.Diagnostics;
using TCBrowser.Maui.ViewModels;

namespace TCBrowser.Maui.UserControls;

public partial class ViewsListView : ContentView
{
	public ViewsListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"[ViewsListView] BindingContext = {BindingContext?.GetType().Name}");
        
        if (BindingContext is ProjectVm vm)
        {
            Debug.WriteLine($"[ViewsListView] ViewModel hash: {vm.GetHashCode()}");
            Debug.WriteLine($"[ViewsListView] ViewsList count: {vm.ViewsList?.Count}");
        }
    }
}