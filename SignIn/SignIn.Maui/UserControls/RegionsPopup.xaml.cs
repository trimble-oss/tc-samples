using CommunityToolkit.Maui.Views;

namespace SignIn.Maui.UserControls;

public partial class RegionsPopup : Popup
{
	public RegionsPopup()
	{
		InitializeComponent();
	}

    void RegionMenuItems_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        var projectsListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
        projectsListViewModel.SelectedRegionName = (string)e.SelectedItem;
        Task.Run(projectsListViewModel.PopulateProjectsInRegion);
        this.Close();
    }
}
