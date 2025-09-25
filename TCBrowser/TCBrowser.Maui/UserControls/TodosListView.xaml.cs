using System.Diagnostics;
using TCBrowser.Maui.ViewModels;

namespace TCBrowser.Maui.UserControls;

public partial class TodosListView : ContentView
{
	public TodosListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"[TodosListView] BindingContext = {BindingContext?.GetType().Name}");
        
        if (BindingContext is ProjectVm vm)
        {
            Debug.WriteLine($"[TodosListView] ViewModel hash: {vm.GetHashCode()}");
            Debug.WriteLine($"[TodosListView] TodosList count: {vm.TodosList?.Count}");
        }
    }
}