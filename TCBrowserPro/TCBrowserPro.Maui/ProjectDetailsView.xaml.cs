using CommunityToolkit.Mvvm.Input;
using TCBrowserPro.Maui.Services;
using TCBrowserPro.Maui.ViewModels;

namespace TCBrowserPro.Maui;

public partial class ProjectDetailsView : ContentPage
{
    private readonly CurrentProjectService _currentProjectService;
    public ProjectVm ViewModel { get; set; }

    public ProjectDetailsView(ProjectVm projectVm, CurrentProjectService currentProjectService)
	{
        InitializeComponent();
        ViewModel = projectVm;
        _currentProjectService = currentProjectService;
        BindingContext = ViewModel;
        ViewModel.SelectedTab = "Files";
        FilesRadioButton.IsChecked = true;
    }

    private void OnFilesTabChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && ViewModel != null)
            ViewModel.SelectedTab = "Files";
    }

    private void OnTodosTabChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && ViewModel != null)
            ViewModel.SelectedTab = "Todos";
    }

    private void OnViewsTabChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && ViewModel != null)
            ViewModel.SelectedTab = "Views";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
    }
}

