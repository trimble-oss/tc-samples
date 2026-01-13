using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;
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
using TCBrowserPro.Maui;
using TCBrowserPro.Maui.Models;
using TCBrowserPro.Maui.Services;
using Trimble.Connect.Client;
using Trimble.Connect.Client.Models;
using Region = Trimble.Connect.Client.Models.Region;

namespace TCBrowserPro.Maui.ViewModels
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
            var imageSource = ImageSource.FromResource("TCBrowserPro.Maui.Resources.Images.no_projects_image.png", assembly);

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
        
        public string BreadcrumbPath => string.IsNullOrEmpty(CurrentFolderPath) ? "Root" : $"Root/{CurrentFolderPath}";

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

        // Store the current projectClient for CRUD operations
        private IProjectClient _projectClient;

        // File CRUD Commands
        public ICommand UploadFileCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand DownloadFileCommand { get; }
        public ICommand RenameFileCommand { get; }
        public ICommand CreateFolderCommand { get; }

        // Todo CRUD Commands
        public ICommand CreateTodoCommand { get; }
        public ICommand UpdateTodoCommand { get; }
        public ICommand DeleteTodoCommand { get; }
        public ICommand TodoTappedCommand { get; }

        // View CRUD Commands
        public ICommand CreateViewCommand { get; }
        public ICommand UpdateViewCommand { get; }
        public ICommand DeleteViewCommand { get; }
        public ICommand ViewTappedCommand { get; }

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
            _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            // _selectedProject = this.SelectedProject;
            // LoadProjectDataByIdAsync(_selectedProject);
            SelectFilesCommand = new Command(OnSelectFiles);
            SelectTodosCommand = new Command(OnSelectTodos);
            SelectViewsCommand = new Command(OnSelectViews);
            FolderTappedCommand = new Command<FolderItem>(OnFolderTapped);
            NavigateBackCommand = new Command(OnNavigateBack, () => CanNavigateBack);

            // File CRUD Commands
            UploadFileCommand = new Command(async () => await OnUploadFile());
            DeleteFileCommand = new Command<FolderItem>(async (file) => await OnDeleteFile(file));
            DownloadFileCommand = new Command<FolderItem>(async (file) => await OnDownloadFile(file));
            RenameFileCommand = new Command<FolderItem>(async (file) => await OnRenameFile(file));
            CreateFolderCommand = new Command(async () => await OnCreateFolder());

            // Todo CRUD Commands
            CreateTodoCommand = new Command(async () => await OnCreateTodo());
            UpdateTodoCommand = new Command<Trimble.Connect.Client.Models.Todo>(async (todo) => await OnUpdateTodo(todo));
            DeleteTodoCommand = new Command<Trimble.Connect.Client.Models.Todo>(async (todo) => await OnDeleteTodo(todo));
            TodoTappedCommand = new Command<Trimble.Connect.Client.Models.Todo>(async (todo) => await OnTodoTapped(todo));

            // View CRUD Commands
            CreateViewCommand = new Command(async () => await OnCreateView());
            UpdateViewCommand = new Command<Trimble.Connect.Client.Models.View>(async (view) => await OnUpdateView(view));
            DeleteViewCommand = new Command<Trimble.Connect.Client.Models.View>(async (view) => await OnDeleteView(view));
            ViewTappedCommand = new Command<Trimble.Connect.Client.Models.View>(async (view) => await OnViewTapped(view));

            if (currentProjectService.SelectedProject != null)
            {
                _ = LoadProjectDataByIdAsync(currentProjectService.SelectedProject);
            }
        }

        private void OnSelectFiles() => SelectedTab = "Files";
        private void OnSelectTodos() => SelectedTab = "Todos";
        private void OnSelectViews() => SelectedTab = "Views";

        /// <summary>
        /// Determines if a FolderItem is actually a folder (not a file)
        /// Uses multiple heuristics since VersionIdentifier alone isn't reliable
        /// </summary>
        private bool IsFolderItem(FolderItem item)
        {
            if (item == null)
            {
                return false;
            }
            
            // Check 0: If it has children, it's definitely a folder (most reliable for folders with non-zero sizes)
            bool hasChildren = _allFiles.Any(f => f.ParentIdentifier == item.Identifier);
            
            // Check 1: VersionIdentifier (some folders don't have it, but some do!)
            bool hasNoVersionId = string.IsNullOrEmpty(item.VersionIdentifier);
            
            // Check 2: Size (folders typically have Size = 0 or null, but can have non-zero if they contain files)
            bool hasZeroSize = item.Size == null || item.Size == 0;
            
            // Check 3: File extension (folders typically don't have extensions)
            bool hasNoExtension = string.IsNullOrEmpty(Path.GetExtension(item.Name));
            
            // Decision logic:
            // 1. If it has children, it's definitely a folder (most reliable)
            // 2. Size=0 AND no extension = folder
            // 3. If no VersionIdentifier, it's definitely a folder (traditional case)
            // 4. If no extension, it's likely a folder (be permissive)
            bool isFolder = hasChildren || (hasZeroSize && hasNoExtension);
            
            if (hasNoVersionId)
            {
                isFolder = true;
            }
            else if (hasNoExtension && !hasChildren)
            {
                // If no extension and no children found, be permissive
                // Most items without extensions are folders
                isFolder = true;
            }
            
            return isFolder;
        }

        private async void OnFolderTapped(FolderItem item)
        {
            if (item == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Item is null", "OK");
                return;
            }
            
            // Use the helper method for consistent folder detection
            bool isFolder = IsFolderItem(item);

            if (isFolder)
            {
                try
                {
                    // Navigate into the folder
                    var newPath = string.IsNullOrEmpty(CurrentFolderPath) 
                        ? item.Name 
                        : $"{CurrentFolderPath}/{item.Name}";
                    
                    CurrentFolderPath = newPath;
                    
                    // Always try to load folder contents from server when navigating into a folder
                    // GetSnapshot() might not include all nested items
                    if (_projectClient?.Files != null)
                    {
                        try
                        {
                            var folderItems = await _projectClient.Files.GetFolderItemsAsync(item.Identifier);
                            if (folderItems != null)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    foreach (var folderItem in folderItems)
                                    {
                                        var existing = _allFiles.FirstOrDefault(f => f.Identifier == folderItem.Identifier);
                                        if (existing != null)
                                        {
                                            // Update existing item (in case it changed)
                                            var index = _allFiles.IndexOf(existing);
                                            _allFiles[index] = folderItem;
                                        }
                                        else
                                        {
                                            // Add new item
                                            _allFiles.Add(folderItem);
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception loadEx)
                        {
                            // Error loading folder contents - continue anyway
                        }
                    }
                    
                    // Filter to show contents of the new folder (after loading)
                    FilterFilesByCurrentFolder();
                    
                    // Update the back command's CanExecute state
                    ((Command)NavigateBackCommand).ChangeCanExecute();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to navigate into folder: {ex.Message}", "OK");
                }
            }
            else
            {
                // It's a file - open it in browser
                await OpenFileInBrowser(item);
            }
        }

        private async Task OpenFileInBrowser(FolderItem file)
        {
            try
            {
                if (_projectClient == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project loaded", "OK");
                    return;
                }

                // Safety check: Don't try to open folders as files
                // Use the same helper method for consistent detection
                bool isFolder = IsFolderItem(file);
                if (isFolder)
                {
                    await Application.Current.MainPage.DisplayAlert("Info", $"'{file.Name}' is a folder. Please tap it to navigate into it.", "OK");
                    return;
                }

                // Download the file and get a stream
                // The Trimble Connect SDK doesn't have GetDownloadUrlAsync, so we'll download and open
                
                // Triple-check: Ensure we have a valid VersionIdentifier before attempting download
                // This should have been caught earlier, but adding extra safety
                if (string.IsNullOrWhiteSpace(file.VersionIdentifier))
                {
                    await Application.Current.MainPage.DisplayAlert("Info", $"'{file.Name}' is a folder. Please tap it to navigate into it.", "OK");
                    return;
                }
                
                var stream = await _projectClient.Files.DownloadAsync(
                    file.Identifier,
                    file.VersionIdentifier,
                    null,  // format
                    null,  // progress
                    CancellationToken.None);

                // Save to temp file and open
                var tempPath = Path.Combine(FileSystem.CacheDirectory, file.Name);
                using (var fileStream = File.Create(tempPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // Open the file using the default application
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(tempPath)
                });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Could not open file: {ex.Message}", "OK");
            }
        }

        private void OnNavigateBack()
        {
            if (!CanNavigateBack) return;

            // Navigate to parent folder
            if (string.IsNullOrEmpty(CurrentFolderPath))
            {
                return; // Already at root
            }

            var pathParts = CurrentFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length > 1)
            {
                CurrentFolderPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
            }
            else
            {
                CurrentFolderPath = ""; // Go back to root
            }

            FilterFilesByCurrentFolder();
            ((Command)NavigateBackCommand).ChangeCanExecute();
        }

        private async Task RefreshFilesListAsync()
        {
            try
            {
                if (_projectClient == null || _projectClient.Files == null)
                {
                    return;
                }

                var filesResult = await _projectClient.Files.GetSnapshot();
                var filesData = filesResult ?? Enumerable.Empty<FolderItem>();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Clear and reload _allFiles
                    _allFiles.Clear();
                    foreach (var file in filesData)
                    {
                        _allFiles.Add(file);
                    }
                    
                    // Re-apply the current folder filter
                    FilterFilesByCurrentFolder();
                });
            }
            catch (Exception ex)
            {
                // Error refreshing files list
            }
        }

        private void FilterFilesByCurrentFolder()
        {
            FilesList.Clear();

            if (string.IsNullOrEmpty(CurrentFolderPath))
            {
                // At root level - show ONLY items that are actually at root
                // Root items are those whose ParentIdentifier is either:
                // 1. null/empty (true root items)
                // 2. Points to a folder that doesn't exist in _allFiles (the root folder itself, which isn't in the snapshot)
                // BUT: We must exclude items whose parent EXISTS in _allFiles (those are nested items!)
                
                var allFolderIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                var rootItems = _allFiles.Where(f => 
                {
                    // If no parent, it's definitely at root
                    if (string.IsNullOrEmpty(f.ParentIdentifier))
                    {
                        return true;
                    }
                    
                    // If parent ID exists in _allFiles, this item is nested inside another folder
                    // DO NOT show it at root!
                    if (allFolderIds.Contains(f.ParentIdentifier))
                    {
                        return false;
                    }
                    
                    // Parent doesn't exist in _allFiles - this means parent is the root folder itself
                    // (which isn't returned in the snapshot)
                    return true;
                }).ToList();
                
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
                    
                    // Get all direct children (files and subfolders) of the current folder
                    var children = _allFiles.Where(f => f.ParentIdentifier == currentFolder.Identifier)
                        .OrderBy(f => IsFolderItem(f) ? 0 : 1) // Folders first, then files
                        .ThenBy(f => f.Name) // Then alphabetically
                        .ToList();
                    
                    foreach (var item in children)
                    {
                        FilesList.Add(item);
                    }
                }
            }
            OnPropertyChanged(nameof(FilesList));
        }

        private FolderItem FindFolderByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null; // Root level
            }

            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0)
            {
                return null; // Root level
            }

            FolderItem currentFolder = null;

            foreach (var part in pathParts)
            {
                if (currentFolder == null)
                {
                    // Looking for root level folder - find folder with this name that is at root
                    // Root folders are those whose parent doesn't exist in _allFiles or is null
                    var allFolderIds = _allFiles.Select(f => f.Identifier).ToHashSet();
                    currentFolder = _allFiles.FirstOrDefault(f => 
                        f.Name == part && 
                        IsFolderItem(f) && // Use consistent folder detection
                        (string.IsNullOrEmpty(f.ParentIdentifier) || !allFolderIds.Contains(f.ParentIdentifier)));
                }
                else
                {
                    // Looking for child folder - find folder with this name whose parent is currentFolder
                    currentFolder = _allFiles.FirstOrDefault(f => 
                        f.Name == part && 
                        IsFolderItem(f) && // Use consistent folder detection
                        f.ParentIdentifier == currentFolder.Identifier);
                }

                if (currentFolder == null)
                {
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

        private async Task<string> GetCurrentFolderIdentifierAsync()
        {
            // If we're in a subfolder, get that folder's identifier
            if (!string.IsNullOrEmpty(CurrentFolderPath))
            {
                var currentFolder = FindFolderByPath(CurrentFolderPath);
                if (currentFolder != null)
                {
                    return currentFolder.Identifier;
                }
                else
                {
                    // Don't silently fall back to root - this is a bug!
                    // But we need to return something, so return null and let the caller handle it
                    return null;
                }
            }

            // Otherwise, we're at root level
            // Try to get the root folder identifier from existing files
            if (_allFiles.Count > 0)
            {
                // Strategy 1: Find a root-level item and get its parent identifier
                var rootLevelItem = _allFiles.FirstOrDefault(f => !string.IsNullOrEmpty(f.ParentIdentifier));
                if (rootLevelItem != null)
                {
                    return rootLevelItem.ParentIdentifier;
                }

                // Strategy 2: Find the root folder itself (a folder with no parent)
                var rootFolder = _allFiles.FirstOrDefault(f => 
                    IsFolderItem(f) && // It's a folder (use consistent detection)
                    string.IsNullOrEmpty(f.ParentIdentifier)); // It has no parent (root folder)
                
                if (rootFolder != null)
                {
                    return rootFolder.Identifier;
                }

                // Strategy 3: If all items have null ParentIdentifier, they're all at root
                // In this case, we need to query for the root folder or use the project's root
                // Try to get RootId from the selected project
                if (SelectedProject != null && !string.IsNullOrEmpty(SelectedProject.RootId))
                {
                    return SelectedProject.RootId;
                }

                // Strategy 4: Try to query the root folder from the API
                if (_projectClient != null)
                {
                    try
                    {
                        // Try to get root folder items - if this works, we can infer the root folder ID
                        // Or try getting folder items with null (root)
                        var rootItems = await _projectClient.Files.GetFolderItemsAsync(null);
                        if (rootItems != null && rootItems.Any())
                        {
                            // All these items are at root, so their ParentIdentifier should be the root folder ID
                            var firstItem = rootItems.First();
                            if (!string.IsNullOrEmpty(firstItem.ParentIdentifier))
                            {
                                return firstItem.ParentIdentifier;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Could not query root folder from API
                    }
                }
            }
            else
            {
                // No files loaded yet, try to get RootId from the selected project
                if (SelectedProject != null && !string.IsNullOrEmpty(SelectedProject.RootId))
                {
                    return SelectedProject.RootId;
                }

                // Try to query the API for root folder
                if (_projectClient != null)
                {
                    try
                    {
                        var rootItems = await _projectClient.Files.GetFolderItemsAsync(null);
                        if (rootItems != null && rootItems.Any())
                        {
                            var firstItem = rootItems.First();
                            if (!string.IsNullOrEmpty(firstItem.ParentIdentifier))
                            {
                                return firstItem.ParentIdentifier;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Could not query root folder from API
                    }
                }
            }

            // Final fallback: For completely empty projects, try using the project identifier
            // Some Trimble Connect implementations use the project ID as the root folder reference
            if (SelectedProject != null && !string.IsNullOrEmpty(SelectedProject.Identifier))
            {
                // Note: This might not work, but it's worth trying before giving up
                return SelectedProject.Identifier;
            }

            // If all strategies fail, return null
            // The calling code will try null first, and if that fails, show a helpful error
            return null;
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

                   // Validate project data before creating Project object
                   if (string.IsNullOrEmpty(selectedProject.Identifier))
                   {
                       await MainThread.InvokeOnMainThreadAsync(async () =>
                       {
                           await Application.Current.MainPage.DisplayAlert("Error", 
                               "Invalid project: Project identifier is missing.", "OK");
                       });
                       IsLoaded = false;
                       return;
                   }

                   if (string.IsNullOrEmpty(selectedProject.RegionName))
                   {
                       await MainThread.InvokeOnMainThreadAsync(async () =>
                       {
                           await Application.Current.MainPage.DisplayAlert("Error", 
                               "Invalid project: Project region is missing.", "OK");
                       });
                       IsLoaded = false;
                       return;
                   }

                   var project = new Project
                    {
                        Name = selectedProject.Name ?? "Unknown",
                        Identifier = selectedProject.Identifier,
                        Location = selectedProject.RegionName
                    };

                    IProjectClient projectClient = null;
                    try
                    {
                        projectClient = await (_shellViewModel as ShellViewModel).TrimbleConnectClient.GetProjectClientAsync(project).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            string errorMessage = "Failed to access project. ";
                            
                            if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
                            {
                                errorMessage += "The project may not exist or may have been deleted.";
                            }
                            else if (ex.Message.Contains("403") || ex.Message.Contains("access") || ex.Message.Contains("permission"))
                            {
                                errorMessage += "You may not have access to this project. Please check your permissions.";
                            }
                            else if (ex.Message.Contains("USER_NOT_IN_PROJECT"))
                            {
                                errorMessage += "You are not a member of this project. Please contact the project administrator.";
                            }
                            else
                            {
                                errorMessage += $"Error: {ex.Message}";
                            }
                            
                            await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
                        });
                        IsLoaded = false;
                        return;
                    }

                // Store the project client for CRUD operations
                _projectClient = projectClient;

                if (projectClient == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", 
                            "Failed to initialize project client. The project may not be accessible or may not exist.", "OK");
                    });
                    IsLoaded = false;
                    return; // Stop execution if we can't get a client
                }

                if (projectClient.Files == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", 
                            "Project files service is not available. The project may not support file operations.", "OK");
                    });
                    IsLoaded = false;
                    return;
                }
                
                Task<IQueryResult<FolderItem>> filesTask = null;
                try
                {
                    filesTask = projectClient.Files.GetSnapshot();
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", 
                            $"Failed to load project files: {ex.Message}", "OK");
                    });
                    IsLoaded = false;
                    return;
                }

                Task<IQueryResult<Todo>> todosTask = null;
                if (projectClient.Todos != null)
                {
                    try
                    {
                        todosTask = projectClient.Todos.GetAllAsync();
                    }
                    catch (Exception ex)
                    {
                        // Error initializing todos
                    }
                }

                Task<IQueryResult<Trimble.Connect.Client.Models.View>> viewsTask = null;
                if (projectClient.Views != null)
                {
                    try
                    {
                        viewsTask = projectClient.Views.GetAllAsync();
                    }
                    catch (Exception ex)
                    {
                        // Error initializing views
                    }
                }

                    //var psetdata = await projectClient.Pset.PSetClient().ConfigureAwait(false);

                    IEnumerable<FolderItem> filesData = null;
                    if (filesTask != null)
                    {
                        try
                        {
                            var filesResult = await filesTask;
                            // IQueryResult<T> implements IEnumerable<T>, so we can use it directly
                            filesData = filesResult ?? Enumerable.Empty<FolderItem>();
                        }
                        catch (Exception ex)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Application.Current.MainPage.DisplayAlert("Error", 
                                    $"Failed to load files: {ex.Message}", "OK");
                            });
                        }
                    }

                    IEnumerable<Todo> todosData = null;
                    if (todosTask != null)
                    {
                        try
                        {
                            var todosResult = await todosTask;
                            // IQueryResult<T> implements IEnumerable<T>, so we can use it directly
                            todosData = todosResult ?? Enumerable.Empty<Todo>();
                        }
                        catch (Exception ex)
                        {
                            // Error loading todos
                        }
                    }

                    IEnumerable<Trimble.Connect.Client.Models.View> viewsData = null;
                    if (viewsTask != null)
                    {
                        try
                        {
                            var viewsResult = await viewsTask;
                            // IQueryResult<T> implements IEnumerable<T>, so we can use it directly
                            viewsData = viewsResult ?? Enumerable.Empty<Trimble.Connect.Client.Models.View>();
                        }
                        catch (Exception ex)
                        {
                            // Error loading views
                        }
                    }

                     await MainThread.InvokeOnMainThreadAsync(async () =>
                     {
                         // Clear both collections
                         _allFiles.Clear();
                         FilesList.Clear();
                         
                         // Store all files in _allFiles
                         if (filesData != null)
                         {
                             foreach (var file in filesData)
                             {
                                 _allFiles.Add(file);
                             }
                         }
                         
                         // Reset to root folder and filter
                         CurrentFolderPath = "";
                         FilterFilesByCurrentFolder();

                        if (todosData != null)
                        {
                        foreach (var todo in todosData)
                        {
                            TodosList.Add(todo);
                            }
                        }
                        OnPropertyChanged(nameof(TodosList)); // Force UI update

                        if (viewsData != null)
                        {
                            foreach (Trimble.Connect.Client.Models.View view in viewsData)
                        {
                            ViewsList.Add(view);
                            }
                        }
                        OnPropertyChanged(nameof(ViewsList)); // Force UI update

                        // await Shell.Current.GoToAsync($"ProjectDetailsView?projectId={selectedProject.Identifier}").ConfigureAwait(false);
                    });
                }
                catch (Exception ex)
                {
                    // Error loading project data
                }
            }

        #region File CRUD Operations

        private async Task OnUploadFile()
        {
            try
            {
                if (_projectClient == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project loaded", "OK");
                    return;
                }

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a file to upload"
                });

                if (result != null)
                {
                    var fileName = await Application.Current.MainPage.DisplayPromptAsync(
                        "File Name",
                        "Enter file name:",
                        initialValue: result.FileName);

                    if (string.IsNullOrWhiteSpace(fileName))
                        return;

                    var stream = await result.OpenReadAsync();

                    // Get current folder identifier
                    string parentFolderId = await GetCurrentFolderIdentifierAsync();

                    if (string.IsNullOrEmpty(parentFolderId))
                    {
                        // If we couldn't determine the root folder ID, try multiple approaches
                        
                        // Try 1: null (some SDK versions accept this)
                        try
                        {
                            var rootFile = await _projectClient.Files.UploadAsync(
                                null,
                        stream,
                        fileName,
                        "application/octet-stream",
                                null,
                                CancellationToken.None);
                            
                            // Refresh the entire file list from the server
                            await RefreshFilesListAsync();
                            await Application.Current.MainPage.DisplayAlert("Success", $"File '{fileName}' uploaded successfully", "OK");
                            return;
                        }
                        catch (Exception ex1)
                        {
                            // Try 2: Empty string
                            try
                            {
                                var rootFile = await _projectClient.Files.UploadAsync(
                                    string.Empty,
                                    stream,
                                    fileName,
                                    "application/octet-stream",
                                    null,
                                    CancellationToken.None);
                                
                                // Refresh the entire file list from the server
                                await RefreshFilesListAsync();
                                await Application.Current.MainPage.DisplayAlert("Success", $"File '{fileName}' uploaded successfully", "OK");
                                return;
                            }
                            catch (Exception ex2)
                            {
                                // Try 3: Project identifier
                                if (SelectedProject != null && !string.IsNullOrEmpty(SelectedProject.Identifier))
                                {
                                    try
                                    {
                                        var rootFile = await _projectClient.Files.UploadAsync(
                                            SelectedProject.Identifier,
                                            stream,
                                            fileName,
                                            "application/octet-stream",
                                            null,
                        CancellationToken.None);

                    // Refresh the entire file list from the server
                    await RefreshFilesListAsync();
                    await Application.Current.MainPage.DisplayAlert("Success", $"File '{fileName}' uploaded successfully", "OK");
                    return;
                                    }
                                    catch (Exception ex3)
                                    {
                                        // Attempt 3 failed
                                    }
                                }
                                
                                // All attempts failed
                                await Application.Current.MainPage.DisplayAlert("Error", 
                                    $"Cannot upload file to root folder. The project appears to be empty and the root folder ID cannot be determined.\n\n" +
                                    $"Please try:\n" +
                                    $"1. Creating a folder first through the Trimble Connect web interface\n" +
                                    $"2. Or ensure the project has at least one file or folder\n\n" +
                                    $"Error: {ex2.Message}", "OK");
                                return;
                            }
                        }
                    }

                    // Upload file (without progress tracking to avoid type conflicts)
                    var uploadedFile = await _projectClient.Files.UploadAsync(
                        parentFolderId,
                        stream,
                        fileName,
                        "application/octet-stream",
                        null,  // progress
                        CancellationToken.None);

                    // Add the newly uploaded file to _allFiles immediately so it appears in the UI
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (!_allFiles.Any(f => f.Identifier == uploadedFile.Identifier))
                        {
                            _allFiles.Add(uploadedFile);
                        }
                    });

                    // If we're inside a folder, reload that folder's contents to see the new item
                    if (!string.IsNullOrEmpty(CurrentFolderPath) && parentFolderId != null)
                    {
                        try
                        {
                            var folderItems = await _projectClient.Files.GetFolderItemsAsync(parentFolderId);
                            if (folderItems != null)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    // Update or add items from this folder
                                    foreach (var folderItem in folderItems)
                                    {
                                        var existing = _allFiles.FirstOrDefault(f => f.Identifier == folderItem.Identifier);
                                        if (existing != null)
                                        {
                                            // Update existing item
                                            var index = _allFiles.IndexOf(existing);
                                            _allFiles[index] = folderItem;
                                        }
                                        else
                                        {
                                            // Add new item
                                            _allFiles.Add(folderItem);
                                        }
                                    }
                                    
                                    // Re-apply the current folder filter to show the new item
                                    FilterFilesByCurrentFolder();
                                });
                            }
                        }
                        catch (Exception loadEx)
                        {
                            // Fall back to full refresh
                            await RefreshFilesListAsync();
                        }
                    }
                    else
                    {
                        // At root level, do a full refresh
                        await RefreshFilesListAsync();
                    }

                    await Application.Current.MainPage.DisplayAlert("Success", $"File '{fileName}' uploaded successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to upload file: {ex.Message}", "OK");
            }
        }

        private async Task OnDeleteFile(FolderItem file)
        {
            // Check if it's a folder or file (declare outside try for use in catch)
            bool isFolder = false;
            string itemType = "item";
            
            try
            {
                if (file == null || _projectClient == null)
                    return;

                // Check if it's a folder or file
                isFolder = IsFolderItem(file);
                itemType = isFolder ? "folder" : "file";
                
                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete",
                    $"Are you sure you want to delete the {itemType} '{file.Name}'?{(isFolder ? " This will also delete all contents inside the folder." : "")}",
                    "Yes",
                    "No");

                if (!confirm)
                    return;

                // For files, use VersionIdentifier; for folders, we might need to use Identifier
                // The Trimble Connect SDK DeleteFileAsync might work for both, but let's handle both cases
                if (isFolder)
                {
                    // Folders might not have VersionIdentifier, or it might be the same as Identifier
                    // Try using VersionIdentifier if available, otherwise use Identifier
                    string deleteId = !string.IsNullOrEmpty(file.VersionIdentifier) ? file.VersionIdentifier : file.Identifier;
                    await _projectClient.Files.DeleteFileAsync(deleteId);
                }
                else
                {
                    // For files, use VersionIdentifier
                    if (string.IsNullOrEmpty(file.VersionIdentifier))
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", $"Cannot delete file '{file.Name}': File version identifier is missing.", "OK");
                        return;
                    }
                    await _projectClient.Files.DeleteFileAsync(file.VersionIdentifier);
                }

                // Remove from collections and refresh
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Remove from _allFiles
                    var itemToRemove = _allFiles.FirstOrDefault(f => f.Identifier == file.Identifier);
                    if (itemToRemove != null)
                    {
                        _allFiles.Remove(itemToRemove);
                    }
                    
                    // Remove from FilesList
                    var itemInList = FilesList.FirstOrDefault(f => f.Identifier == file.Identifier);
                    if (itemInList != null)
                    {
                        FilesList.Remove(itemInList);
                    }
                    
                    OnPropertyChanged(nameof(FilesList));
                });

                // If we're inside a folder, reload its contents to reflect the deletion
                if (!string.IsNullOrEmpty(CurrentFolderPath))
                {
                    var currentFolder = FindFolderByPath(CurrentFolderPath);
                    if (currentFolder != null && _projectClient?.Files != null)
                    {
                        try
                        {
                            var folderItems = await _projectClient.Files.GetFolderItemsAsync(currentFolder.Identifier);
                            if (folderItems != null)
                            {
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    // Update _allFiles with current folder contents
                                    var itemsToRemove = _allFiles.Where(f => f.ParentIdentifier == currentFolder.Identifier && 
                                        !folderItems.Any(fi => fi.Identifier == f.Identifier)).ToList();
                                    foreach (var item in itemsToRemove)
                                    {
                                        _allFiles.Remove(item);
                                    }
                                    
                                    foreach (var folderItem in folderItems)
                                    {
                                        var existing = _allFiles.FirstOrDefault(f => f.Identifier == folderItem.Identifier);
                                        if (existing != null)
                                        {
                                            var index = _allFiles.IndexOf(existing);
                                            _allFiles[index] = folderItem;
                                        }
                                        else
                                        {
                                            _allFiles.Add(folderItem);
                                        }
                                    }
                                    
                                    // Re-apply filter
                                    FilterFilesByCurrentFolder();
                                });
                            }
                        }
                        catch (Exception refreshEx)
                        {
                            // Fall back to full refresh
                            await RefreshFilesListAsync();
                        }
                    }
                }
                else
                {
                    // At root level, do a full refresh
                    await RefreshFilesListAsync();
                }

                await Application.Current.MainPage.DisplayAlert("Success", $"'{file.Name}' deleted successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete {itemType}: {ex.Message}", "OK");
            }
        }

        private async Task OnDownloadFile(FolderItem file)
        {
            try
            {
                if (file == null || _projectClient == null)
                    return;

                // Check if it's a file (not a folder) by checking for file extension
                if (string.IsNullOrEmpty(Path.GetExtension(file.Name)))
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "Cannot download folders", "OK");
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Info", $"Downloading '{file.Name}'...", "OK");

                // Download file (without progress tracking to avoid type conflicts)
                var stream = await _projectClient.Files.DownloadAsync(
                    file.Identifier,
                    file.VersionIdentifier,
                    null,  // format
                    null,  // progress
                    CancellationToken.None);

                // Save to app data directory
                var localPath = Path.Combine(FileSystem.AppDataDirectory, file.Name);
                using (var fileStream = File.Create(localPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Application.Current.MainPage.DisplayAlert("Success", $"File downloaded to: {localPath}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to download file: {ex.Message}", "OK");
            }
        }

        private async Task OnRenameFile(FolderItem file)
        {
            try
            {
                if (file == null || _projectClient == null)
                    return;

                var newName = await Application.Current.MainPage.DisplayPromptAsync(
                    "Rename File",
                    "Enter new name:",
                    initialValue: file.Name);

                if (string.IsNullOrWhiteSpace(newName) || newName == file.Name)
                    return;

                // Update file with new name (keeping same parent)
                var updatedFile = await _projectClient.Files.UpdateFileAsync(
                    file.Identifier,
                    file.VersionIdentifier,
                    newName,
                    null);  // null = don't move

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var index = _allFiles.IndexOf(file);
                    if (index >= 0)
                    {
                        _allFiles[index] = updatedFile;
                    }
                    FilterFilesByCurrentFolder();
                    OnPropertyChanged(nameof(FilesList));
                });

                await Application.Current.MainPage.DisplayAlert("Success", $"File renamed to '{newName}'", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to rename file: {ex.Message}", "OK");
            }
        }

        private async Task OnCreateFolder()
        {
            try
            {
                if (_projectClient == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project loaded", "OK");
                    return;
                }

                var folderName = await Application.Current.MainPage.DisplayPromptAsync(
                    "New Folder",
                    "Enter folder name:");

                if (string.IsNullOrWhiteSpace(folderName))
                    return;

                // Get current folder identifier
                string parentFolderId = await GetCurrentFolderIdentifierAsync();
                
                if (string.IsNullOrEmpty(parentFolderId))
                {
                    // If we couldn't determine the root folder ID, try multiple approaches
                    
                    // Try 1: null (some SDK versions accept this)
                    try
                    {
                        var rootFolder = await _projectClient.Files.CreateFolderAsync(null, folderName);
                        // Refresh the entire file list from the server
                        await RefreshFilesListAsync();
                        await Application.Current.MainPage.DisplayAlert("Success", $"Folder '{folderName}' created successfully", "OK");
                        return;
                    }
                    catch (Exception ex1)
                    {
                        
                        // Try 2: Empty string
                        try
                        {
                            var rootFolder = await _projectClient.Files.CreateFolderAsync(string.Empty, folderName);
                            // Refresh the entire file list from the server
                            await RefreshFilesListAsync();
                            await Application.Current.MainPage.DisplayAlert("Success", $"Folder '{folderName}' created successfully", "OK");
                            return;
                        }
                        catch (Exception ex2)
                        {
                            // Try 3: Project identifier
                            if (SelectedProject != null && !string.IsNullOrEmpty(SelectedProject.Identifier))
                            {
                                try
                                {
                                    var rootFolder = await _projectClient.Files.CreateFolderAsync(SelectedProject.Identifier, folderName);
                            // Refresh the entire file list from the server
                            await RefreshFilesListAsync();
                            await Application.Current.MainPage.DisplayAlert("Success", $"Folder '{folderName}' created successfully", "OK");
                            return;
                                }
                                catch (Exception ex3)
                                {
                                    // Attempt 3 failed
                                }
                            }
                            
                            // All attempts failed
                            await Application.Current.MainPage.DisplayAlert("Error", 
                                $"Cannot create folder at root. The project appears to be empty and the root folder ID cannot be determined.\n\n" +
                                $"Please try:\n" +
                                $"1. Creating a folder first through the Trimble Connect web interface\n" +
                                $"2. Or ensure the project has at least one file or folder\n\n" +
                                $"Error: {ex2.Message}", "OK");
                            return;
                        }
                    }
                }

                var newFolder = await _projectClient.Files.CreateFolderAsync(parentFolderId, folderName);

                // Add the newly created folder to _allFiles immediately so it appears in the UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!_allFiles.Any(f => f.Identifier == newFolder.Identifier))
                    {
                        _allFiles.Add(newFolder);
                    }
                });

                // If we're inside a folder, reload that folder's contents to see the new item
                if (!string.IsNullOrEmpty(CurrentFolderPath) && parentFolderId != null)
                {
                    try
                    {
                        var folderItems = await _projectClient.Files.GetFolderItemsAsync(parentFolderId);
                        if (folderItems != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                // Update or add items from this folder
                                foreach (var folderItem in folderItems)
                                {
                                    var existing = _allFiles.FirstOrDefault(f => f.Identifier == folderItem.Identifier);
                                    if (existing != null)
                                    {
                                        // Update existing item
                                        var index = _allFiles.IndexOf(existing);
                                        _allFiles[index] = folderItem;
                                    }
                                    else
                                    {
                                        // Add new item
                                        _allFiles.Add(folderItem);
                                    }
                                }
                                
                                // Re-apply the current folder filter to show the new item
                                FilterFilesByCurrentFolder();
                            });
                        }
                    }
                    catch (Exception loadEx)
                    {
                        // Fall back to full refresh
                        await RefreshFilesListAsync();
                    }
                }
                else
                {
                    // At root level, do a full refresh
                    await RefreshFilesListAsync();
                }

                await Application.Current.MainPage.DisplayAlert("Success", $"Folder '{folderName}' created successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create folder: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Todo CRUD Operations

        private async Task OnCreateTodo()
        {
            try
            {
                if (_projectClient == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project loaded", "OK");
                    return;
                }

                // Show the Create Todo popup
                var popup = new TCBrowserPro.Views.CreateTodoPopup(_projectClient);
                var result = await Application.Current.MainPage.ShowPopupAsync(popup);

                // If a todo was created, add it to the list
                if (result is Trimble.Connect.Client.Models.Todo createdTodo)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TodosList.Add(createdTodo);
                        OnPropertyChanged(nameof(TodosList));
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open create todo popup: {ex.Message}", "OK");
            }
        }

        private async Task OnUpdateTodo(Trimble.Connect.Client.Models.Todo todo)
        {
            try
            {
                if (todo == null || _projectClient == null)
                    return;

                // Show the Edit Todo popup
                var popup = new TCBrowserPro.Views.EditTodoPopup(_projectClient, todo);
                var result = await Application.Current.MainPage.ShowPopupAsync(popup);

                // If the todo was updated, refresh it in the list
                if (result is Trimble.Connect.Client.Models.Todo updatedTodo)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var index = TodosList.IndexOf(todo);
                        if (index >= 0)
                        {
                            TodosList[index] = updatedTodo;
                            OnPropertyChanged(nameof(TodosList));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open edit todo popup: {ex.Message}", "OK");
            }
        }

        private async Task OnDeleteTodo(Trimble.Connect.Client.Models.Todo todo)
        {
            try
            {
                if (todo == null || _projectClient == null)
                    return;

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete",
                    $"Are you sure you want to delete todo '{todo.Title}'?",
                    "Yes",
                    "No");

                if (!confirm)
                    return;

                await _projectClient.Todos.DeleteAsync(todo.Identifier);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TodosList.Remove(todo);
                    OnPropertyChanged(nameof(TodosList));
                });

                await Application.Current.MainPage.DisplayAlert("Success", "Todo deleted successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete todo: {ex.Message}", "OK");
            }
        }

        private async Task OnTodoTapped(Trimble.Connect.Client.Models.Todo todo)
        {
            if (todo == null)
                return;

            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"Todo: {todo.Title}",
                "Cancel",
                null,
                "Edit",
                "Delete",
                "View Details",
                "Change Status");

            switch (action)
            {
                case "Edit":
                    await OnUpdateTodo(todo);
                    break;
                case "Delete":
                    await OnDeleteTodo(todo);
                    break;
                case "View Details":
                    await ShowTodoDetails(todo);
                    break;
                case "Change Status":
                    await ChangeTodoStatus(todo);
                    break;
            }
        }

        private async Task ShowTodoDetails(Trimble.Connect.Client.Models.Todo todo)
        {
            var details = $"Label: {todo.Label}\n" +
                          $"Title: {todo.Title}\n" +
                          $"Description: {todo.Description}\n" +
                          $"Status: {todo.Status}\n" +
                          $"Priority: {todo.Priority}\n" +
                          $"Completion: {todo.CompletionPercentage}%\n" +
                          $"Created: {todo.CreatedOn:g}\n" +
                          $"Due: {todo.DueDate:g}";

            await Application.Current.MainPage.DisplayAlert("Todo Details", details, "OK");
        }

        private async Task ChangeTodoStatus(Trimble.Connect.Client.Models.Todo todo)
        {
            try
            {
                var status = await Application.Current.MainPage.DisplayActionSheet(
                    "Select Status",
                    "Cancel",
                    null,
                    "Open",
                    "InProgress",
                    "Closed",
                    "Blocked");

                if (status == "Cancel" || status == null)
                    return;

                var currentTodo = await _projectClient.Todos.GetAsync(todo.Identifier);
                currentTodo.Status = status;

                if (status == "Closed")
                    currentTodo.CompletionPercentage = 100;

                var updatedTodo = await _projectClient.Todos.UpdateAsync(currentTodo);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var index = TodosList.IndexOf(todo);
                    if (index >= 0)
                    {
                        TodosList[index] = updatedTodo;
                        OnPropertyChanged(nameof(TodosList));
                    }
                });

                await Application.Current.MainPage.DisplayAlert("Success", $"Status changed to {status}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to change status: {ex.Message}", "OK");
            }
        }

        #endregion

        #region View CRUD Operations

        private async Task OnCreateView()
        {
            try
            {
                if (_projectClient == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project loaded", "OK");
                    return;
                }

                var name = await Application.Current.MainPage.DisplayPromptAsync("New View", "Enter view name:");
                if (string.IsNullOrWhiteSpace(name))
                    return;

                var description = await Application.Current.MainPage.DisplayPromptAsync("New View", "Enter view description (optional):");

                var newView = new Trimble.Connect.Client.Models.View
                {
                    Name = name,
                    Description = description ?? string.Empty
                };

                var createdView = await _projectClient.Views.CreateAsync(newView);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ViewsList.Add(createdView);
                    OnPropertyChanged(nameof(ViewsList));
                });

                await Application.Current.MainPage.DisplayAlert("Success", $"View '{name}' created successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create view: {ex.Message}", "OK");
            }
        }

        private async Task OnUpdateView(Trimble.Connect.Client.Models.View view)
        {
            try
            {
                if (view == null || _projectClient == null)
                    return;

                var newName = await Application.Current.MainPage.DisplayPromptAsync(
                    "Update View",
                    "Enter new name:",
                    initialValue: view.Name);

                if (string.IsNullOrWhiteSpace(newName))
                    return;

                // Get the latest version
                var currentView = await _projectClient.Views.GetAsync(view.Identifier);
                currentView.Name = newName;

                var updatedView = await _projectClient.Views.UpdateAsync(currentView);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var index = ViewsList.IndexOf(view);
                    if (index >= 0)
                    {
                        ViewsList[index] = updatedView;
                        OnPropertyChanged(nameof(ViewsList));
                    }
                });

                await Application.Current.MainPage.DisplayAlert("Success", "View updated successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update view: {ex.Message}", "OK");
            }
        }

        private async Task OnDeleteView(Trimble.Connect.Client.Models.View view)
        {
            try
            {
                if (view == null || _projectClient == null)
                    return;

                var confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete",
                    $"Are you sure you want to delete view '{view.Name}'?",
                    "Yes",
                    "No");

                if (!confirm)
                    return;

                await _projectClient.Views.DeleteAsync(view.Identifier);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ViewsList.Remove(view);
                    OnPropertyChanged(nameof(ViewsList));
                });

                await Application.Current.MainPage.DisplayAlert("Success", "View deleted successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete view: {ex.Message}", "OK");
            }
        }

        private async Task OnViewTapped(Trimble.Connect.Client.Models.View view)
        {
            if (view == null)
                return;

            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"View: {view.Name}",
                "Cancel",
                null,
                "View",
                "Edit",
                "Delete",
                "View Details");

            switch (action)
            {
                case "View":
                    await OpenViewInBrowser(view);
                    break;
                case "Edit":
                    await OnUpdateView(view);
                    break;
                case "Delete":
                    await OnDeleteView(view);
                    break;
                case "View Details":
                    await ShowViewDetails(view);
                    break;
            }
        }

        private async Task ShowViewDetails(Trimble.Connect.Client.Models.View view)
        {
            var details = $"Name: {view.Name}\n" +
                          $"Description: {view.Description}\n" +
                          $"Created: {view.CreatedOn:g}";

            await Application.Current.MainPage.DisplayAlert("View Details", details, "OK");
        }

        private async Task OpenViewInBrowser(Trimble.Connect.Client.Models.View view)
        {
            try
            {
                if (SelectedProject == null || string.IsNullOrEmpty(SelectedProject.Identifier))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No project selected", "OK");
                    return;
                }

                // Construct the URL to view the project in web browser using environment config
                var projectId = SelectedProject.Identifier;
                var envConfig = _configService.Config;
                var webViewerUri = envConfig.WebViewerUri.TrimEnd('/');
                var webAppUri = envConfig.WebAppUri.TrimEnd('/');
                var url = $"{webViewerUri}/projects/{projectId}/viewer/3d/?=&origin={webAppUri}";

                // Open the URL in the default browser
                await Launcher.Default.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open view in browser: {ex.Message}", "OK");
            }
        }

        #endregion
        
    }
}

