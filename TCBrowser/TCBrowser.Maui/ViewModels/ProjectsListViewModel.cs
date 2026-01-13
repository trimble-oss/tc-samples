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
using System.IO;
using System.Linq;
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

        public ProjectsListViewModel(IShellViewModel shellViewModel, CurrentProjectService currentProjectService, ProjectVm _projectVm)
        {
            _syncLock = new object();
            Projects = new List<ProjectMetaData>();
            FilteredProjects = new ObservableCollection<ProjectMetaData>();
            this.shellViewModel = shellViewModel;
            this.projectVm = _projectVm;

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
                    // Error is silently handled as thumbnail loading failure shouldn't block project display
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
        
        // Store all files and folders from the initial fetch
        private ObservableCollection<FolderItem> _allFiles { get; set; } = new();
        
        // Display filtered files based on current folder
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

        // Navigation state
        private string _currentFolderPath = "";
        public string CurrentFolderPath
        {
            get => _currentFolderPath;
            set
            {
                _currentFolderPath = value;
                OnPropertyChanged(nameof(CurrentFolderPath));
                OnPropertyChanged(nameof(CanNavigateBack));
                OnPropertyChanged(nameof(BreadcrumbPath));
            }
        }

        public bool CanNavigateBack => !string.IsNullOrEmpty(CurrentFolderPath);
        
        public string BreadcrumbPath
        {
            get
            {
                if (string.IsNullOrEmpty(CurrentFolderPath))
                {
                    // Show project name at root, or empty string if no project
                    return SelectedProject?.Name ?? "";
                }
                return CurrentFolderPath;
            }
        }

        private readonly CurrentProjectService _currentProjectService;
        private ObservableCollection<Trimble.Connect.Client.Models.Todo> _todosList = new ObservableCollection<Trimble.Connect.Client.Models.Todo>();
        public ObservableCollection<Trimble.Connect.Client.Models.Todo> TodosList 
        { 
            get => _todosList;
            set
            {
                _todosList = value;
                OnPropertyChanged(nameof(TodosList));
            }
        }

        private ObservableCollection<Trimble.Connect.Client.Models.View> _viewsList = new ObservableCollection<Trimble.Connect.Client.Models.View>();
        public ObservableCollection<Trimble.Connect.Client.Models.View> ViewsList 
        { 
            get => _viewsList;
            set
            {
                _viewsList = value;
                OnPropertyChanged(nameof(ViewsList));
            }
        }

        public ICommand SelectFilesCommand { get; }
        public ICommand SelectTodosCommand { get; }
        public ICommand SelectViewsCommand { get; }
        public ICommand FolderTappedCommand { get; }
        public ICommand FileTappedCommand { get; }
        public ICommand TodoTappedCommand { get; }
        public ICommand ViewTappedCommand { get; }
        public ICommand NavigateBackCommand { get; }

        private string _selectedTab = "Files";

        public string SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    // Execute specific logic based on the selected tab
                    switch (value)
                    {
                        case "Files":
                            // Logic for Files tab
                            break;
                        case "Todos":
                            // Logic for Todos tab
                            break;
                        case "Views":
                            // Logic for Views tab
                            break;
                    }
                }
            }
        }

        //public IRelayCommand<string> ChangeTabCommand { get; }

        private ProjectMetaData _selectedProject;

        private readonly IShellViewModel _shellViewModel;

        public ProjectMetaData SelectedProject
        {
            get => _currentProjectService.SelectedProject;
            set
            {
                if (_currentProjectService.SelectedProject != value)
                {
                    _currentProjectService.SelectedProject = value;
                    OnPropertyChanged(nameof(SelectedProject)); // Notify UI of change
                    OnPropertyChanged(nameof(BreadcrumbPath)); // Update breadcrumb when project changes

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
        private readonly ConfigService _configService;

        public ProjectVm(IShellViewModel shellViewModel, CurrentProjectService currentProjectService, ConfigService configService)
        {
            this._shellViewModel = shellViewModel;
            this._currentProjectService = currentProjectService ?? throw new ArgumentNullException(nameof(currentProjectService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            // _selectedProject = this.SelectedProject;
            // LoadProjectDataByIdAsync(_selectedProject);
            SelectFilesCommand = new Command(OnSelectFiles);
            SelectTodosCommand = new Command(OnSelectTodos);
            SelectViewsCommand = new Command(OnSelectViews);
            FolderTappedCommand = new Command<FolderItem>(OnFolderTapped);
            FileTappedCommand = new Command<FolderItem>(OnFileTapped);
            TodoTappedCommand = new Command<Trimble.Connect.Client.Models.Todo>(OnTodoTapped);
            ViewTappedCommand = new Command<Trimble.Connect.Client.Models.View>(OnViewTapped);
            NavigateBackCommand = new Command(OnNavigateBack, () => CanNavigateBack);
            if (currentProjectService.SelectedProject != null)
            {
                _ = LoadProjectDataByIdAsync(currentProjectService.SelectedProject);
            }
        }

        private void OnSelectFiles() => SelectedTab = "Files";
        private void OnSelectTodos() => SelectedTab = "Todos";
        private void OnSelectViews() => SelectedTab = "Views";

        /// <summary>
        /// Determines if an item is a folder using the SDK's reliable detection method.
        /// Uses VersionIdentifier: folders don't have it, files do.
        /// </summary>
        private bool IsFolderItem(FolderItem item)
        {
            if (item == null)
            {
                return false;
            }
            
            // Check 1: VersionIdentifier (most reliable - folders don't have it, files do)
            bool hasNoVersionId = string.IsNullOrEmpty(item.VersionIdentifier);
            
            // Check 2: Size (folders typically have Size = 0 or null)
            bool hasZeroSize = item.Size == null || item.Size == 0;
            
            // Check 3: File extension (folders typically don't have extensions)
            bool hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(item.Name));
            
            // Decision logic: Size=0 AND no extension = folder (even if it has VersionIdentifier)
            bool isFolder = hasZeroSize && hasNoExtension;
            
            // If no VersionIdentifier, it's definitely a folder (most reliable check)
            if (hasNoVersionId)
            {
                isFolder = true;
            }
            
            return isFolder;
        }

        private void OnFolderTapped(FolderItem folder)
        {
            if (folder == null) return;

            // Use the reliable folder detection method
            bool isFolder = IsFolderItem(folder);

            // Only navigate if it's actually a folder
            // If it's a file, it should have been handled by OnFileTapped
            if (!isFolder)
            {
                // Treat as file - open in browser
                OnFileTapped(folder);
                return;
            }

            // Navigate into the folder
            var newPath = string.IsNullOrEmpty(CurrentFolderPath) 
                ? folder.Name 
                : $"{CurrentFolderPath}/{folder.Name}";
            
            CurrentFolderPath = newPath;
            FilterFilesByCurrentFolder();
            
            // Update the back command's CanExecute state
            ((Command)NavigateBackCommand).ChangeCanExecute();
        }

        private async void OnFileTapped(FolderItem file)
        {
            if (file == null || SelectedProject == null) return;

            // Use the reliable folder detection method
            bool isFolder = IsFolderItem(file);

            if (isFolder)
            {
                // Folders should use the folder navigation, not open in browser
                return;
            }

            try
            {
                // Construct URL for Trimble Connect file viewer using environment config
                var envConfig = _configService.Config;
                var webViewerUri = envConfig.WebViewerUri.TrimEnd('/');
                var fileId = file.Identifier;
                var versionId = file.VersionIdentifier ?? fileId; // Use VersionIdentifier if available, fallback to fileId
                var url = $"{webViewerUri}/projects/{SelectedProject.Identifier}/viewer/2D?id={fileId}&version={versionId}&type=revisions&etag={fileId}";
                
                await Launcher.OpenAsync(new Uri(url));
            }
                catch (Exception ex)
                {
                    // Error is silently handled - user will see that the URL didn't open
                }
        }

        private async void OnTodoTapped(Trimble.Connect.Client.Models.Todo todo)
        {
            if (todo == null || SelectedProject == null) return;

            try
            {
                // Construct URL for Trimble Connect todo using environment config
                var envConfig = _configService.Config;
                var webViewerUri = envConfig.WebViewerUri.TrimEnd('/');
                var url = $"{webViewerUri}/projects/{SelectedProject.Identifier}/todo";
                
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                // Error is silently handled - user will see that the URL didn't open
            }
        }

        private async void OnViewTapped(Trimble.Connect.Client.Models.View view)
        {
            if (view == null || SelectedProject == null) return;

            try
            {
                // Construct URL for Trimble Connect 3D viewer using environment config
                var envConfig = _configService.Config;
                var webViewerUri = envConfig.WebViewerUri.TrimEnd('/');
                var webAppUri = envConfig.WebAppUri.TrimEnd('/');
                var projectId = SelectedProject.Identifier;
                var url = $"{webViewerUri}/projects/{projectId}/viewer/3d/?=&origin={webAppUri}";
                
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                // Error is silently handled - user will see that the URL didn't open
            }
        }

        private void OnNavigateBack()
        {
            if (!CanNavigateBack) return;

            // Navigate to parent folder
            var pathParts = CurrentFolderPath.Split('/');
            if (pathParts.Length > 1)
            {
                CurrentFolderPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
            }
            else
            {
                CurrentFolderPath = "";
            }

            FilterFilesByCurrentFolder();
            ((Command)NavigateBackCommand).ChangeCanExecute();
        }

        private void FilterFilesByCurrentFolder()
        {
            FilesList.Clear();

            if (string.IsNullOrEmpty(CurrentFolderPath))
            {
                // Show root level items - items that have no parent or whose parent is not in the collection
                var allParentIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                var rootItems = _allFiles.Where(f => 
                    string.IsNullOrEmpty(f.ParentIdentifier) || 
                    !allParentIds.Contains(f.ParentIdentifier)).ToList();
                
                // Sort by name for better UX
                rootItems = rootItems.OrderBy(f => f.Name).ToList();
                
                foreach (var item in rootItems)
                {
                    FilesList.Add(item);
                }
            }
            else
            {
                // Find the current folder and show its children
                var currentFolder = FindFolderByPath(CurrentFolderPath);
                if (currentFolder != null)
                {
                    var children = _allFiles.Where(f => f.ParentIdentifier == currentFolder.Identifier).ToList();
                    
                    // Sort by name for better UX
                    children = children.OrderBy(f => f.Name).ToList();
                    
                    foreach (var item in children)
                    {
                        FilesList.Add(item);
                    }
                }
                else
                {
                    // Instead of resetting to root, try to go back one level
                    var pathParts = CurrentFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (pathParts.Length > 1)
                    {
                        // Go back one level
                        CurrentFolderPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
                        // Recursively call to filter with the new path
                        FilterFilesByCurrentFolder();
                    }
                    else
                    {
                        // Only reset to root if we're already at the first level
                        CurrentFolderPath = "";
                        FilterFilesByCurrentFolder();
                    }
                }
            }
        }

        private FolderItem FindFolderByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
                
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0)
                return null;
                
            FolderItem currentFolder = null;

            foreach (var part in pathParts)
            {
                if (currentFolder == null)
                {
                    // Looking for root level folder - find folders with no parent or parent not in collection
                    var allParentIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                    var candidates = _allFiles.Where(f => 
                        f.Name == part && 
                        IsFolderItem(f) && // Use consistent folder detection
                        (string.IsNullOrEmpty(f.ParentIdentifier) || !allParentIds.Contains(f.ParentIdentifier))).ToList();
                    
                    if (candidates.Count == 0)
                    {
                        return null;
                    }
                    
                    currentFolder = candidates.First();
                }
                else
                {
                    // Looking for child folder - must match name, be a folder, and have currentFolder as parent
                    var candidates = _allFiles.Where(f => 
                        f.Name == part && 
                        IsFolderItem(f) && // Use consistent folder detection
                        f.ParentIdentifier == currentFolder.Identifier).ToList();
                    
                    if (candidates.Count == 0)
                    {
                        return null;
                    }
                    
                    currentFolder = candidates.First();
                }
            }

            return currentFolder;
        }

        private string GetRootFolderIdentifier()
        {
            // This might need to be adjusted based on how Trimble Connect structures the root folder
            // You may need to inspect the actual data to determine the root folder identifier
            return null; // or return the actual root folder identifier if known
        }

        //private void OnProjectSelected(ProjectMetaData selectedProject)
        //{
        //    // Fire and forget, non-blocking
        //    _ = LoadProjectDataAsync(selectedProject);
        //}

         public async Task LoadProjectDataByIdAsync(ProjectMetaData selectedProject)
            {
            IsLoaded = true;
            FilesList.Clear();
            TodosList.Clear();
            ViewsList.Clear();

            if (selectedProject == null)
            {
                IsLoaded = false;
                return; // Exit early if no project to load
            }
            try
                {
                   if (_shellViewModel == null)
                   {
                    return; // Or throw an exception
                   }

                   var shellVm = _shellViewModel as ShellViewModel;
                   if (shellVm == null)
                   {
                    return; // Or throw an exception
                   }

                   if (shellVm.TrimbleConnectClient == null)
                   {
                    return; // Or throw an exception
                   }

                   var project = new Project
                    {
                        Name = selectedProject.Name,
                        Identifier = selectedProject.Identifier,
                        Location = selectedProject.RegionName
                    };

                    var projectClient = (await (_shellViewModel as ShellViewModel).TrimbleConnectClient.GetProjectClientAsync(project).ConfigureAwait(false));

                if (projectClient == null)
                {
                    return; // Stop execution if we can't get a client
                }

                if (projectClient.Files == null)
                {
                    // No file operations possible
                }
                var filesTask = projectClient.Files.GetSnapshot().ConfigureAwait(false);

                if (projectClient.Todos == null)
                {
                    // No todo operations possible
                }

                var todosTask = projectClient.Todos.GetAllAsync().ConfigureAwait(false);

                if (projectClient.Views == null)
                {
                    // No view operations possible
                }
                var viewsTask = projectClient.Views.GetAllAsync().ConfigureAwait(false);

                    var filesData = await filesTask;
                    var todosData = await todosTask;
                    var viewsData = await viewsTask;

                     await MainThread.InvokeOnMainThreadAsync(async () =>
                     {
                         // Clear both collections
                         _allFiles.Clear();
                         FilesList.Clear();
                         
                         // Store all files in _allFiles
                         foreach (var file in filesData)
                         {
                             _allFiles.Add(file);
                         }
                         
                         // Reset to root folder and filter
                         CurrentFolderPath = "";
                         FilterFilesByCurrentFolder();

                        foreach (var todo in todosData)
                        {
                            TodosList.Add(todo);
                        }
                        OnPropertyChanged(nameof(TodosList)); // Force UI update

                        foreach (var view in viewsData)
                        {
                            ViewsList.Add(view);
                        }
                        OnPropertyChanged(nameof(ViewsList)); // Force UI update
                    });
                }
                catch (Exception ex)
                {
                    // Error is silently handled - project data loading failure
                }
            }
        
    }
}

