using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Windows.Input;
using TCBrowser.Maui;
using TCBrowser.Maui.Models;
using TCBrowser.Maui.Services;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;
using Region = Trimble.Connect.Client.Models.Region;

namespace TCBrowser.Maui.ViewModels
{
    public partial class ProjectsListViewModel : ObservableObject, IProjectsListViewModel
    {
        #region Members
        private readonly CurrentProjectService _currentProjectService;
        private readonly object _syncLock;
        private bool _disposedValue;
        private string _selectedRegionName;

        [ObservableProperty]
        private Dictionary<string, Region> regionInfos;

        [ObservableProperty]
        private bool _areProjectsLoading;

        private readonly IShellViewModel shellViewModel;

        // private readonly ProjectVm projectVm;

        public readonly ProjectVm projectVm;

        private ProjectMetaData _selectedProject;

        #endregion Members

        #region Properties

        public List<ProjectMetaData> Projects { get; set; }
        public ObservableCollection<ProjectMetaData> FilteredProjects { get; set; }

        public IAsyncRelayCommand<ProjectMetaData> NavigateToDetailsCommand { get; }

        /// <summary>
        /// Selected Region Name
        /// Bound to the selected item of region combo box
        /// </summary>
        public string SelectedRegionName
        {
            get
            {
                return _selectedRegionName;
            }
            set
            {
                //Set the value only if there is a change
                if (_selectedRegionName != value)
                {
                    _selectedRegionName = value;
                    OnPropertyChanged(nameof(SelectedRegionName));
                }
            }
        }

       public ProjectMetaData SelectedProject
        {
            get => projectVm.SelectedProject;
            set
            {
                if (projectVm.SelectedProject != value)
                {
                    projectVm.SelectedProject = value;
                    Console.WriteLine($"Selected Project: {value?.Name}");
                    NavigateToDetailsCommand.Execute(value);
                }
            }
        }

        //public ProjectMetaData SelectedProject
        //{
        //    get => _selectedProject;
        //    set
        //    {
        //        if (SetProperty(ref _selectedProject, value) && value != null)
        //        {
        //            NavigateToDetailsCommand.Execute(value);
        //        }
        //    }
        //}

        #endregion Properties

        #region Constructor

        public ProjectsListViewModel(IShellViewModel shellViewModel, CurrentProjectService currentProjectService)
        {
            _syncLock = new object();
            Projects = new List<ProjectMetaData>();
            FilteredProjects = new ObservableCollection<ProjectMetaData>();
            this.shellViewModel = shellViewModel;
            Console.WriteLine("At Line 115, ShellViewModel is", this.shellViewModel);
            this.projectVm = new ProjectVm(shellViewModel);

            NavigateToDetailsCommand = new AsyncRelayCommand<ProjectMetaData>(NavigateToProjectDetails);


            //Set up Region names dictionary.
            regionInfos = new Dictionary<string, Region>();
            _currentProjectService = currentProjectService;
        }

        #endregion Constructor

        [ObservableProperty]
        private string lastRefreshedTime;


        public async Task PopulateRegions()
        {
            //Get the Regions from Service
            var regions = (await (shellViewModel as ShellViewModel).TrimbleConnectClient.ReadConfigurationAsync().ConfigureAwait(false)).ToList();
            foreach (var region in regions)
            {
                if (regionInfos != null && !regionInfos.ContainsKey(region.Location))
                {
                    regionInfos.Add(region.Location, region);
                }
            }

            SelectedRegionName = regions.FirstOrDefault(d => d.IsMaster).Location;
        }

        public async Task PopulateProjectsInRegion()
        {
            AreProjectsLoading = true;
            FilteredProjects.Clear();
            var projects = new List<Project>();

            //Get the Projects from Service

            var parameters = new Dictionary<string, string>
                {
                    { "fullyLoaded", "false" }
                };

            await (shellViewModel as ShellViewModel).TrimbleConnectClient.GetProjectsAsync(parameters, 25, received: ReceiveProjects, podFilter:
                region => region.Location == SelectedRegionName).ReceiveAllAsync(projects.AddRange).ConfigureAwait(false);

            AreProjectsLoading = false;
        }

        private void ReceiveProjects(IEnumerable<Project> projects)
        {
            foreach (var project in projects)
            {
                var projData = new ProjectMetaData { Identifier = project.Identifier, Name = project.Name, RegionName = project.Location };
                FilteredProjects.Add(projData);
                LoadThumbnailAsync(projData, project.ThumbnailUrl);
            }
        }

        [RelayCommand]
        private async void DoRefreshProjects()
        {
            await PopulateProjectsInRegion();
        }

        private void LoadThumbnailAsync(ProjectMetaData project, string thumbnailSource)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(thumbnailSource) || (!thumbnailSource.StartsWith("https://") && !thumbnailSource.StartsWith("http://")))
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            project.ThumbnailSource = await ImageSourceToByteArray().ConfigureAwait(false);
                        }).ConfigureAwait(false);
                        return;
                    }

                    if (string.Equals(thumbnailSource, "https://resources.connect.trimble.com/thumb/project.png") ||
                        string.Equals(thumbnailSource, "https://resources.stage.connect.trimble.com/thumb/project.png"))
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            project.ThumbnailSource = await ImageSourceToByteArray().ConfigureAwait(false);
                        }).ConfigureAwait(false);
                        return;
                    }

                    project.ThumbnailSource = await GetImageInBytesAsync(thumbnailSource).ConfigureAwait(false);
                }
                catch (Exception ex)
                {

                }
            });
        }

        private async Task<byte[]> GetImageInBytesAsync(string thumbnailSource)
        {
            byte[] imageInBytes = null;
            var stream = (await (shellViewModel as ShellViewModel).TrimbleConnectClient.DownloadThumbnailAsync(thumbnailSource).ConfigureAwait(false)).Item1;
            using (BinaryReader br = new BinaryReader(stream))
            {
                imageInBytes = br.ReadBytes((int)stream.Length);
            }
            return imageInBytes;
        }

        private async Task<byte[]> ImageSourceToByteArray()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] names = assembly.GetManifestResourceNames();
            var imageSource = ImageSource.FromResource("TCBrowser.Maui.Resources.Images.no_projects_image.png", assembly);

            Stream stream = await ((StreamImageSource)imageSource).Stream(CancellationToken.None).ConfigureAwait(false);
            byte[] bytesAvailable = new byte[stream.Length];
            stream.Read(bytesAvailable, 0, bytesAvailable.Length);

            return bytesAvailable;
        }

        private async Task DoRefreshProjectsAsync()
        {
            await PopulateProjectsInRegion();
            LastRefreshedTime = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        }

        private async Task NavigateToProjectDetails(ProjectMetaData projectToNavigateTo)
        {
            //  string route = $"//{nameof(ProjectDetailsView)}?projectId={projectToNavigateTo.Identifier}";
            // await Shell.Current.GoToAsync(route);

            if (projectToNavigateTo == null)
                return;
            //var detailsPage = new ProjectDetailsView(projectVm);
            //await Shell.Current.Navigation.PushAsync(detailsPage);
            _currentProjectService.SelectedProject = projectToNavigateTo;
            string route = $"ProjectDetailsView?projectId={projectToNavigateTo.Identifier}";
            await Shell.Current.GoToAsync(route);
        }
    }

    public partial class ProjectVm : ObservableObject, INotifyPropertyChanged
    {
        public bool IsLoaded { get; private set; }
        private ObservableCollection<FolderItem> _filesList { get; set; } = new();
        public ObservableCollection<FolderItem> FilesList
        {
            get => _filesList;
            set
            {
                _filesList = value;
                OnPropertyChanged(nameof(FilesList));
            }
        }

        public ObservableCollection<object> TodosList { get; private set; } = new ObservableCollection<object>();
        public ObservableCollection<object> ViewsList { get; private set; } = new ObservableCollection<object>();

        public ICommand SelectFilesCommand { get; }
        public ICommand SelectTodosCommand { get; }
        public ICommand SelectViewsCommand { get; }

        [ObservableProperty]
        private string selectedTab = "Files";

        //public IRelayCommand<string> ChangeTabCommand { get; }

        private ProjectMetaData _selectedProject;

        private readonly IShellViewModel _shellViewModel;

        public ProjectMetaData SelectedProject
        {
            get => _selectedProject;
            set
            {
                //if (_selectedProject != value)
                //{
                //    _selectedProject = value;
                //}
                if (_selectedProject != value)
                {
                    _selectedProject = value;
                    OnPropertyChanged(nameof(SelectedProject)); // Notify UI of change

                    if (value != null) // Ensure there's a project to load
                    {
                        // Call the method to load data for the new project
                        _ = LoadProjectDataByIdAsync(value);
                    }
                    else
                    {
                        // Optionally clear lists if project is deselected
                        FilesList.Clear();
                        TodosList.Clear();
                        ViewsList.Clear();
                    }
                }
            }
        }
        public ProjectVm(IShellViewModel shellViewModel)
        {
            this._shellViewModel = shellViewModel;
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            // _selectedProject = this.SelectedProject;
            // LoadProjectDataByIdAsync(_selectedProject);
            SelectFilesCommand = new Command(OnSelectFiles);
            SelectTodosCommand = new Command(OnSelectTodos);
            SelectViewsCommand = new Command(OnSelectViews);
            FilesList.CollectionChanged += (sender, e) =>
            {
                Debug.WriteLine($"[FilesList CollectionChanged] Action: {e.Action}, New items: {e.NewItems?.Count}, Old items: {e.OldItems?.Count}, Total: {FilesList.Count}");
            };
        }

        private void OnSelectFiles() => SelectedTab = "Files";
        private void OnSelectTodos() => SelectedTab = "Todos";
        private void OnSelectViews() => SelectedTab = "Views";

        //private void OnProjectSelected(ProjectMetaData selectedProject)
        //{
        //    // Fire and forget, non-blocking
        //    _ = LoadProjectDataAsync(selectedProject);
        //}

         public async Task LoadProjectDataByIdAsync(ProjectMetaData selectedProject)
            {
            IsLoaded = true;
            try
                {
                    var project = new Project
                    {
                        Name = selectedProject.Name,
                        Identifier = selectedProject.Identifier,
                        Location = selectedProject.RegionName
                    };

                    var projectClient = (await (_shellViewModel as ShellViewModel).TrimbleConnectClient.GetProjectClientAsync(project).ConfigureAwait(false));

                    

                    var todosTask = projectClient.Todos.GetAllAsync().ConfigureAwait(false);

                    var filesTask = projectClient.Files.GetSnapshot().ConfigureAwait(false);

                    var viewsTask = projectClient.Views.GetAllAsync().ConfigureAwait(false);

                    //var psetdata = await projectClient.Pset.PSetClient().ConfigureAwait(false);

                    var filesData = await filesTask;
                    Console.WriteLine($"Files fetched: {filesData?.Count()}");

                    var todosData = await todosTask;
                    var viewsData = await viewsTask;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        foreach (var file in filesData)
                        {
                            FilesList.Add(file);
                        }
                        Console.WriteLine(FilesList);
                        TodosList.Clear();
                        foreach (var todo in todosData)
                        {
                            TodosList.Add(todo);
                            Console.WriteLine(TodosList);
                        }

                        ViewsList.Clear();
                        foreach (var view in viewsData)
                        {
                            ViewsList.Add(view);
                        }

                       OnSelectFiles();

                        // await Shell.Current.GoToAsync($"ProjectDetailsView?projectId={selectedProject.Identifier}").ConfigureAwait(false);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading project data: {ex.Message}");
                }
            }
        
    }
}
