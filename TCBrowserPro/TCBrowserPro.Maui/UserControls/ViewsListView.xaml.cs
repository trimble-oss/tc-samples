using System.Diagnostics;
using TCBrowserPro.Maui.ViewModels;

namespace TCBrowserPro.Maui.UserControls;

public partial class ViewsListView : ContentView
{
	public ViewsListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
    }
}