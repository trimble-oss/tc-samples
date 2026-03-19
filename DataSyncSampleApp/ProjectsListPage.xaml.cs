using System.Collections.ObjectModel;
using DataSyncSampleApp.Models;
using Trimble.Connect.Client;
using Trimble.Connect.Data.Sync;
using ClientProject = Trimble.Connect.Client.Models.Project;

namespace DataSyncSampleApp;

public partial class ProjectsListPage : ContentPage
{
    private readonly ObservableCollection<ClientProject> _projects = new();
    private bool _openingProject;

    public ProjectsListPage()
    {
        InitializeComponent();
        ProjectsList.ItemsSource = _projects;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProjectsAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadProjectsAsync();

    private async Task LoadProjectsAsync()
    {
        var client = AppSession.ConnectClient;
        if (client == null)
        {
            await DisplayAlert("Error", "Not signed in.", "OK");
            return;
        }

        Busy.IsVisible = Busy.IsRunning = true;
        RefreshButton.IsEnabled = false;
        try
        {
            _projects.Clear();
            var batch = new List<ClientProject>();
            await client.GetProjectsAsync(false, null, p =>
            {
                foreach (var x in p) batch.Add(x);
            }, null, default).ConfigureAwait(true);

            foreach (var p in batch.OrderBy(x => x.Name))
                _projects.Add(p);

            if (_projects.Count == 0)
                await DisplayAlert("Projects", "No projects found for this account.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Load failed", ex.Message, "OK");
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private async void OnProjectRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement ve || ve.BindingContext is not ClientProject p)
            return;
        await OpenProjectAsync(p).ConfigureAwait(true);
    }

    private async Task OpenProjectAsync(ClientProject p)
    {
        if (_openingProject)
            return;
        _openingProject = true;
        Busy.IsVisible = Busy.IsRunning = true;
        RefreshButton.IsEnabled = false;

        AppSession.SelectedProject = p;
        try
        {
            var connect = AppSession.ConnectClient
                ?? throw new InvalidOperationException("Not signed in.");

            // Extension method on ITrimbleConnectClient — must NOT use dynamic (runtime binder skips extensions).
            var projectClient = await connect.GetProjectClientAsync(p, default).ConfigureAwait(true);
            AppSession.SyncClient = await SyncClient.CreateAsync(projectClient, default).ConfigureAwait(true);

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Navigation.PushAsync(new ProjectOperationsPage()).ConfigureAwait(true));
        }
        catch (Exception ex)
        {
            AppSession.SyncClient = null;
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Open project", ex.Message, "OK").ConfigureAwait(true));
        }
        finally
        {
            _openingProject = false;
            Busy.IsVisible = Busy.IsRunning = false;
            RefreshButton.IsEnabled = true;
        }
    }
}
