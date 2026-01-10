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

        public ProjectsListViewModel(IShellViewModel shellViewModel, CurrentProjectService currentProjectService, ProjectVm _projectVm)
        {
            _syncLock = new object();
            Projects = new List<ProjectMetaData>();
            FilteredProjects = new ObservableCollection<ProjectMetaData>();
            this.shellViewModel = shellViewModel;
            Console.WriteLine("At Line 115, ShellViewModel is", this.shellViewModel);
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
        public ICommand NavigateBackCommand { get; }
        public ICommand DebugFilesCommand { get; }

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
                            // Logic for Files tab (if any specific logic is needed, otherwise just the visibility change is enough)
                            Debug.WriteLine("Files tab selected.");
                            break;
                        case "Todos":
                            // Logic for Todos tab
                            Debug.WriteLine("Todos tab selected.");
                            break;
                        case "Views":
                            // Logic for Views tab
                            Debug.WriteLine("Views tab selected.");
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
        public ProjectVm(IShellViewModel shellViewModel, CurrentProjectService currentProjectService)
        {
            this._shellViewModel = shellViewModel;
            this._currentProjectService = currentProjectService ?? throw new ArgumentNullException(nameof(currentProjectService));
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            // _selectedProject = this.SelectedProject;
            // LoadProjectDataByIdAsync(_selectedProject);
            SelectFilesCommand = new Command(OnSelectFiles);
            SelectTodosCommand = new Command(OnSelectTodos);
            SelectViewsCommand = new Command(OnSelectViews);
            FolderTappedCommand = new Command<FolderItem>(OnFolderTapped);
            NavigateBackCommand = new Command(OnNavigateBack, () => CanNavigateBack);
            DebugFilesCommand = new Command(OnDebugFiles);
            if (currentProjectService.SelectedProject != null)
            {
                _ = LoadProjectDataByIdAsync(currentProjectService.SelectedProject);
            }
            FilesList.CollectionChanged += (sender, e) =>
            {
                Debug.WriteLine($"[FilesList CollectionChanged] Action: {e.Action}, New items: {e.NewItems?.Count}, Old items: {e.OldItems?.Count}, Total: {FilesList.Count}");
            };
            
            TodosList.CollectionChanged += (sender, e) =>
            {
                Debug.WriteLine($"[TodosList CollectionChanged] Action: {e.Action}, New items: {e.NewItems?.Count}, Old items: {e.OldItems?.Count}, Total: {TodosList.Count}");
            };
            
            ViewsList.CollectionChanged += (sender, e) =>
            {
                Debug.WriteLine($"[ViewsList CollectionChanged] Action: {e.Action}, New items: {e.NewItems?.Count}, Old items: {e.OldItems?.Count}, Total: {ViewsList.Count}");
            };
        }

        private void OnSelectFiles() => SelectedTab = "Files";
        private void OnSelectTodos() => SelectedTab = "Todos";
        private void OnSelectViews() => SelectedTab = "Views";

        private void OnDebugFiles()
        {
            Debug.WriteLine($"=== DEBUG FILES INFO ===");
            Debug.WriteLine($"Total files in _allFiles: {_allFiles.Count}");
            Debug.WriteLine($"Files in FilesList: {FilesList.Count}");
            Debug.WriteLine($"Current folder path: '{CurrentFolderPath}'");
            Debug.WriteLine($"=== ALL FILES ===");
            foreach (var file in _allFiles.Take(10)) // Show first 10 files
            {
                Debug.WriteLine($"File: {file.Name} (ID: {file.Identifier}, Parent: {file.ParentIdentifier})");
            }
            if (_allFiles.Count > 10)
            {
                Debug.WriteLine($"... and {_allFiles.Count - 10} more files");
            }
            Debug.WriteLine($"=== CURRENT DISPLAYED FILES ===");
            foreach (var file in FilesList)
            {
                Debug.WriteLine($"Displayed: {file.Name} (ID: {file.Identifier}, Parent: {file.ParentIdentifier})");
            }
            Debug.WriteLine($"=== END DEBUG ===");
        }

        private void OnFolderTapped(FolderItem folder)
        {
            if (folder == null) return;

            Debug.WriteLine($"Item tapped: {folder.Name} (ID: {folder.Identifier}, Parent: {folder.ParentIdentifier})");

            // Check if the item is a folder by checking if it has children
            // This is the most reliable way to detect folders
            bool hasChildren = _allFiles.Any(f => f.ParentIdentifier == folder.Identifier);
            
            // Also check if it has no file extension (folders typically don't have extensions)
            bool hasNoExtension = string.IsNullOrEmpty(System.IO.Path.GetExtension(folder.Name));
            
            // Be more permissive: if it has children, it's definitely a folder
            // Also treat items without extensions as potential folders (even if they have dots in the name)
            // This allows navigation into items like "0.1.1.0 test build" if they have children
            bool isFolder = hasChildren || hasNoExtension;

            Debug.WriteLine($"  HasChildren: {hasChildren}, HasNoExtension: {hasNoExtension}, IsFolder: {isFolder}");

            // Always try to navigate - if it's not a folder, we'll just show an empty list
            // This is more user-friendly than blocking navigation
            var newPath = string.IsNullOrEmpty(CurrentFolderPath) 
                ? folder.Name 
                : $"{CurrentFolderPath}/{folder.Name}";
            
            Debug.WriteLine($"Navigating to: {newPath}");
            CurrentFolderPath = newPath;
            FilterFilesByCurrentFolder();
            
            // Update the back command's CanExecute state
            ((Command)NavigateBackCommand).ChangeCanExecute();
            
            // If we navigated but found no children, log it for debugging
            if (isFolder && FilesList.Count == 0)
            {
                Debug.WriteLine($"⚠️ Folder '{folder.Name}' appears to be empty or has no accessible children");
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
            Debug.WriteLine($"Filtering files for path: '{CurrentFolderPath}'");
            Debug.WriteLine($"Total files available: {_allFiles.Count}");

            if (string.IsNullOrEmpty(CurrentFolderPath))
            {
                // Show root level items - items that have no parent or whose parent is not in the collection
                var allParentIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                var rootItems = _allFiles.Where(f => 
                    string.IsNullOrEmpty(f.ParentIdentifier) || 
                    !allParentIds.Contains(f.ParentIdentifier)).ToList();
                
                Debug.WriteLine($"Found {rootItems.Count} root level items");
                
                // Sort by name for better UX
                rootItems = rootItems.OrderBy(f => f.Name).ToList();
                
                foreach (var item in rootItems)
                {
                    FilesList.Add(item);
                    Debug.WriteLine($"Added root item: {item.Name} (ID: {item.Identifier}, Parent: {item.ParentIdentifier ?? "null"})");
                }
            }
            else
            {
                // Find the current folder and show its children
                var currentFolder = FindFolderByPath(CurrentFolderPath);
                if (currentFolder != null)
                {
                    var children = _allFiles.Where(f => f.ParentIdentifier == currentFolder.Identifier).ToList();
                    Debug.WriteLine($"Found {children.Count} children for folder '{currentFolder.Name}' (ID: {currentFolder.Identifier})");
                    
                    // Sort by name for better UX
                    children = children.OrderBy(f => f.Name).ToList();
                    
                    foreach (var item in children)
                    {
                        FilesList.Add(item);
                        Debug.WriteLine($"Added child item: {item.Name} (ID: {item.Identifier}, Parent: {item.ParentIdentifier})");
                    }
                }
                else
                {
                    Debug.WriteLine($"⚠️ Could not find folder for path: {CurrentFolderPath}");
                    // Try to recover by resetting to root
                    CurrentFolderPath = "";
                    FilterFilesByCurrentFolder();
                }
            }
            
            Debug.WriteLine($"FilesList now contains {FilesList.Count} items");
        }

        private FolderItem FindFolderByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
                
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            FolderItem currentFolder = null;

            foreach (var part in pathParts)
            {
                if (currentFolder == null)
                {
                    // Looking for root level folder - find items with no parent or parent not in collection
                    var allParentIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                    currentFolder = _allFiles.FirstOrDefault(f => 
                        f.Name == part && 
                        (string.IsNullOrEmpty(f.ParentIdentifier) || !allParentIds.Contains(f.ParentIdentifier)));
                }
                else
                {
                    // Looking for child folder - must match name and have currentFolder as parent
                    currentFolder = _allFiles.FirstOrDefault(f => 
                        f.Name == part && 
                        f.ParentIdentifier == currentFolder.Identifier);
                }

                if (currentFolder == null)
                {
                    Debug.WriteLine($"⚠️ Could not find folder part '{part}' in path '{path}'");
                    break;
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
                Debug.WriteLine("Warning: LoadProjectDataByIdAsync called with null selectedProject. Cannot load data.");
                IsLoaded = false;
                return; // Exit early if no project to load
            }
            try
                {
                   if (_shellViewModel == null)
                   {
                    Debug.WriteLine("Error: _shellViewModel is null in LoadProjectDataByIdAsync.");
                    return; // Or throw an exception
                   }

                   var shellVm = _shellViewModel as ShellViewModel;
                   if (shellVm == null)
                   {
                    Debug.WriteLine("Error: _shellViewModel cannot be cast to ShellViewModel.");
                    return; // Or throw an exception
                   }

                   if (shellVm.TrimbleConnectClient == null)
                   {
                    Debug.WriteLine("Error: TrimbleConnectClient is null in ShellViewModel.");
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
                    Debug.WriteLine("Error: projectClient is null after GetProjectClientAsync. Check project data and TrimbleConnectClient setup.");
                    return; // Stop execution if we can't get a client
                }

                if (projectClient.Files == null)
                {
                    Debug.WriteLine("Warning: projectClient.Files is null. No file operations possible.");
                    // You might choose to return here or just proceed knowing files won't load
                }
                var filesTask = projectClient.Files.GetSnapshot().ConfigureAwait(false);

                if (projectClient.Todos == null)
                {
                    Debug.WriteLine("Warning: projectClient.Files is null. No file operations possible.");
                    // You might choose to return here or just proceed knowing files won't load
                }

                var todosTask = projectClient.Todos.GetAllAsync().ConfigureAwait(false);


                if (projectClient.Views == null)
                {
                    Debug.WriteLine("Warning: projectClient.Files is null. No file operations possible.");
                    // You might choose to return here or just proceed knowing files won't load
                }
                var viewsTask = projectClient.Views.GetAllAsync().ConfigureAwait(false);

                    //var psetdata = await projectClient.Pset.PSetClient().ConfigureAwait(false);

                    var filesData = await filesTask;
                    Console.WriteLine($"Files fetched: {filesData?.Count()}");

                    var todosData = await todosTask;
                    var viewsData = await viewsTask;

                     await MainThread.InvokeOnMainThreadAsync(async () =>
                     {
                         Debug.WriteLine($"[LoadProjectData] About to add {filesData?.Count()} files");
                         
                         // Clear both collections
                         _allFiles.Clear();
                         FilesList.Clear();
                         
                         // Store all files in _allFiles
                         foreach (var file in filesData)
                         {
                             _allFiles.Add(file);
                             Debug.WriteLine($"Loaded file/folder: {file.Name} (ID: {file.Identifier}, Parent: {file.ParentIdentifier})");
                         }
                         
                         Debug.WriteLine($"Total files/folders loaded: {_allFiles.Count}");
                         
                         // Reset to root folder and filter
                         CurrentFolderPath = "";
                         FilterFilesByCurrentFolder();
                         
                         Debug.WriteLine($"[LoadProjectData] FilesList now has {FilesList.Count} items");

                        Debug.WriteLine($"[LoadProjectData] About to add {todosData?.Count()} todos");
                        foreach (var todo in todosData)
                        {
                            Debug.WriteLine($"[LoadProjectData] Todo type: {todo?.GetType().Name}, Value: {todo}");
                            TodosList.Add(todo);
                            Debug.WriteLine($"Here's the Todo Format: {todo.Title}");
                        }
                        Debug.WriteLine($"[LoadProjectData] TodosList now has {TodosList.Count} items");
                        OnPropertyChanged(nameof(TodosList)); // Force UI update

                        Debug.WriteLine($"[LoadProjectData] About to add {viewsData?.Count()} views");
                        foreach (var view in viewsData)
                        {
                            Debug.WriteLine($"[LoadProjectData] View type: {view?.GetType().Name}, Value: {view}");
                            ViewsList.Add(view);
                        }
                        Debug.WriteLine($"[LoadProjectData] ViewsList now has {ViewsList.Count} items");
                        OnPropertyChanged(nameof(ViewsList)); // Force UI update

                        Debug.WriteLine($"[LoadProjectData] Current SelectedTab: {SelectedTab}");
                        Debug.WriteLine($"[LoadProjectData] Data loading completed. Forcing property notifications.");

                        // Add test data if collections are empty to verify UI binding
                        //if (TodosList.Count == 0)
                        //{
                        //    Debug.WriteLine("[LoadProjectData] TodosList is empty, adding test data");
                        //    TodosList.Add(new { Title = "Test Todo 1", Description = "This is a test todo item" });
                        //    TodosList.Add(new { Title = "Test Todo 2", Description = "Another test todo item" });
                        //}

                        //if (ViewsList.Count == 0)
                        //{
                        //    Debug.WriteLine("[LoadProjectData] ViewsList is empty, adding test data");
                        //    ViewsList.Add(new { Name = "Test View 1", Description = "This is a test view item" });
                        //    ViewsList.Add(new { Name = "Test View 2", Description = "Another test view item" });
                        //}

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
