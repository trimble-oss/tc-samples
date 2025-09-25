using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Net;
//using Newtonsoft.Json;
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
        //    ViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();;
        Debug.WriteLine($"[OnAppearing] ViewModel hash: {ViewModel.GetHashCode()}");
        BindingContext = ViewModel;
        ViewModel.SelectedTab = "Files";
        FilesRadioButton.IsChecked = true;
    }

    //protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    //{
    //    base.OnNavigatedTo(args);

    //    if (args.TryGetQueryParameter("projectJson", out string projectJson))
    //    {
    //        var decoded = WebUtility.UrlDecode(projectJson);
    //        SelectedProject = JsonSerializer.Deserialize<ProjectMetaData>(decoded);

    //        if (SelectedProject != null)
    //        {
    //            Debug.WriteLine($"Loaded project: {SelectedProject.Name}");
    //            await ViewModel.LoadProjectDataByIdAsync(SelectedProject);
    //        }
    //        else
    //        {
    //            Debug.WriteLine("⚠️ Failed to deserialize project");
    //        }
    //    }
    //    else
    //    {
    //        Debug.WriteLine("⚠️ Missing projectJson parameter");
    //    }
    //}

    //public async void LoadProjectData(ProjectMetaData metadata)
    //{
    //    if (metadata == null)
    //    {
    //        Debug.WriteLine("[ProjectDetailsView] Received project is null!");
    //        return;
    //    }


    //     await ViewModel.LoadProjectDataByIdAsync(metadata);

    //    //  await _projectVm.LoadProjectDataByIdAsync(projectId, regionName);
    //}

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
        Debug.WriteLine($"[OnAppearing] ViewModel hash: {ViewModel.GetHashCode()}");
    }
}

