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
    public ProjectMetaData SelectedProject { get; set; }

    private readonly CurrentProjectService _currentProjectService;

    private string _projectJson { get; set; }
    public ProjectVm ViewModel { get; set; }

    public IAsyncRelayCommand<string> LoadProjectDataCommand { get; }

    private readonly ShellViewModel _shellViewModel;
    public ProjectDetailsView(ProjectVm projectVm, CurrentProjectService currentProjectService)
	{
        InitializeComponent();
        ViewModel = projectVm;
        _currentProjectService = currentProjectService;
        //    ViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();;
        Debug.WriteLine($"[OnAppearing] ViewModel hash: {ViewModel.GetHashCode()}");
        BindingContext = ViewModel;
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

    public async void LoadProjectData(ProjectMetaData metadata)
    {

        if (metadata == null)
        {
            Debug.WriteLine("[ProjectDetailsView] Received project is null!");
            return;
        }

         
         await ViewModel.LoadProjectDataByIdAsync(metadata);

        //  await _projectVm.LoadProjectDataByIdAsync(projectId, regionName);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine($"[OnAppearing] ViewModel hash: {ViewModel.GetHashCode()}");
        await ViewModel.LoadProjectDataByIdAsync(SelectedProject);
    }
}

