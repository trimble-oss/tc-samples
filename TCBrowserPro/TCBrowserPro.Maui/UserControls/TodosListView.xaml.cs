using System.Diagnostics;
using TCBrowserPro.Maui.ViewModels;

namespace TCBrowserPro.Maui.UserControls;

public partial class TodosListView : ContentView
{
	public TodosListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
    }
}