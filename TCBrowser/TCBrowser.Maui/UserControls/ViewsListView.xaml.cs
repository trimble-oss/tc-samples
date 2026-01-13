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
    }
}