
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using SignIn.Maui.UserControls;


namespace SignIn.Maui;

public partial class ProjectsView : ContentPage
{
    private bool _isInitialized;

	public ProjectsView()
	{
		InitializeComponent();
        var projectsListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
        BindingContext = projectsListViewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        var projectsListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
        if(projectsListViewModel.FilteredProjects.Count == 0)
        {
            MainThread.InvokeOnMainThreadAsync(projectsListViewModel.PopulateProjectsInRegion);
        }
    }

    [RelayCommand]
    private void DoSelectRegion()
    {
        Shell.Current.CurrentPage.ShowPopup(new RegionsPopup { Anchor = RegionButtonImage });
    }
}