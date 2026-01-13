using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using TCBrowser.Maui.Models;
using TCBrowser.Maui.Services;
using TCBrowser.Maui.UserControls;
using TCBrowser.Maui.ViewModels;
using Trimble.Connect.Client.Models;

namespace TCBrowser.Maui;

public partial class ProjectDetailsView : ContentPage
{
    private readonly CurrentProjectService _currentProjectService;
    public ProjectVm ViewModel { get; set; }

    private readonly ShellViewModel _shellViewModel;
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

