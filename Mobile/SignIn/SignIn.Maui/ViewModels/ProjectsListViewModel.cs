using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;
using System.Globalization;

using System.Reflection;

using Microsoft.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SignIn.Maui;
using SignIn.Maui.Models;
using Region = Trimble.Connect.Client.Models.Region;
using Trimble.Connect.Client.Models;
using Trimble.Connect.Client;
using Newtonsoft.Json;

using System.Net.Http.Headers;
using System.Net;

namespace SignIn.Maui.ViewModels
{
    public partial class ProjectsListViewModel : ObservableObject, IProjectsListViewModel
    {
        #region Members

        private readonly object _syncLock;
        private bool _disposedValue;
        private string _selectedRegionName;

        [ObservableProperty]
        private Dictionary<string, Region> regionInfos;

        [ObservableProperty]
        private bool _areProjectsLoading;

        private readonly IShellViewModel shellViewModel;

        #endregion Members

        #region Properties

        public List<ProjectMetaData> Projects { get; set; }
        public ObservableCollection<ProjectMetaData> FilteredProjects { get; set; }

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

        #endregion Properties

        #region Constructor

        public ProjectsListViewModel(IShellViewModel shellViewModel) 
        {
            _syncLock = new object();

            Projects = new List<ProjectMetaData>();
            FilteredProjects = new ObservableCollection<ProjectMetaData>();
            this.shellViewModel = shellViewModel;


            //Set up Region names dictionary.
            regionInfos = new Dictionary<string, Region>();
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
                regionInfos?.Add(region.Location, region);
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

            await (shellViewModel as ShellViewModel).TrimbleConnectClient.GetProjectsAsync(parameters, 25, received: RecieveProjects, podFilter: 
                region => region.Location == SelectedRegionName).ReceiveAllAsync(projects.AddRange).ConfigureAwait(false);

            AreProjectsLoading = false;
        }

        private void RecieveProjects(IEnumerable<Project> projects)
        {
            foreach(var project in projects)
            {
                var projData = new ProjectMetaData { Identifier = project.Identifier, Name = project.Name };
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
            var imageSource = ImageSource.FromResource("SignIn.Maui.Resources.Images.no_projects_image.png", assembly);

            Stream stream = await((StreamImageSource)imageSource).Stream(CancellationToken.None).ConfigureAwait(false);
            byte[] bytesAvailable = new byte[stream.Length];
            stream.Read(bytesAvailable, 0, bytesAvailable.Length);

            return bytesAvailable;
        }

        private async Task DoRefreshProjectsAsync()
        {
            await PopulateProjectsInRegion();
            LastRefreshedTime = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        }
    }
}
