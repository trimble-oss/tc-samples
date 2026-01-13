using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using TCBrowserPro.Maui.Models;
using TCBrowserPro.Maui.Services;
using Trimble.Identity.OAuth.AuthCode;

namespace TCBrowserPro.Maui.ViewModels
{
    public partial class LoginViewModel : ObservableObject, ILoginViewModel
    {
        [ObservableProperty]
        private bool _isLoading;
        [ObservableProperty]
        private bool _isLogOutPage;
        [ObservableProperty]
        private bool _showLaunchBrowser;
        [ObservableProperty]
        private bool _showLaunchingBrowser;
        [ObservableProperty]
        private bool _showLongDescription;
        [ObservableProperty]
        private bool _showLogin;

        [ObservableProperty]
        private string _selectedEnvironment;

        public List<string> AvailableEnvironments { get; private set; }

        public event Action Signout = delegate { };
        public event Action SignOut;

        private readonly IAuthCodeCredentialsProvider authCodeCredentialsProvider;
        private readonly IShellViewModel shellViewModel;
        private readonly ConfigService _configService;

        public LoginViewModel(IAuthCodeCredentialsProvider loginContext, IShellViewModel shellViewModel, ConfigService configService)
        {
            ShowLogin = true;
            this.authCodeCredentialsProvider = loginContext;
            this.shellViewModel = shellViewModel;
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            this.authCodeCredentialsProvider.OnTokenRefreshed += AuthCodeCredentialsProvider_OnTokenRefreshed;
            
            // Load available environments and set selected
            AvailableEnvironments = _configService.AvailableEnvironments;
            SelectedEnvironment = _configService.GetSelectedEnvironmentName();
        }

        partial void OnSelectedEnvironmentChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && _configService != null)
            {
                try
                {
                    // Update the selected environment in ConfigService
                    _configService.SetSelectedEnvironment(value);
                    
                    // Reinitialize ShellViewModel with new environment
                    if (shellViewModel is ShellViewModel svm)
                    {
                        svm.Reinitialize();
                    }
                }
                catch (Exception ex)
                {
                    // Log error - environment change failed
                    // Failed to change environment
                }
            }
        }

        private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BrowserProSample", "config.json");
            var refreshTokenInfo = new RefreshTokenInfo(refreshToken, timeInTicks, true);
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }

                string json = JsonConvert.SerializeObject(refreshTokenInfo);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - refresh token saving failure shouldn't block the app
                // Error is silently handled as refresh token saving is non-critical
            }
        }

        public void DoSilentLogin()
        {
            var refreshToken = string.Empty;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BrowserProSample", "config.json");

            if (File.Exists(path))
            {
                try
                {
                    using (var fileStream = File.OpenText(path))
                    {
                        using (var reader = new JsonTextReader(fileStream))
                        {
                            refreshToken = JsonSerializer.CreateDefault(new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local }).Deserialize<RefreshTokenInfo>(reader)?.RefreshToken;
                        }
                    }
                }
                catch (Exception ex)
                {
                    refreshToken = string.Empty; // Invalidate if file is corrupt
                }
            }

                if (!string.IsNullOrEmpty(refreshToken))
                {
                   IsLoading = true;
                   ShowLogin = false;
                   IsLogOutPage = false;
                   ShowLongDescription = false;
                   ShowLaunchBrowser = false;
                   ShowLaunchingBrowser = false;

                Task.Run(async () =>
                {
                    try
                    {
                        authCodeCredentialsProvider.WithRefreshToken(refreshToken);
                        var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync(CancellationToken.None).ConfigureAwait(false);
                        //AuthCodeCredentialsProvider_OnTokenRefreshed(CancellationToken.None);

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            var projectListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
                            if (projectListViewModel != null)
                            {
                                await projectListViewModel.PopulateRegions().ConfigureAwait(false);
                            }
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                 await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
                                 IsLoading = false;
                            });
                        }
                        else
                        {
                            IsLoading = false;
                            ShowLogin = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsLoading = false;
                            ShowLogin = true;
                        });
                    }
                });
            }
        }

        [RelayCommand]
        private void SignIn()
        {
            IsLoading = true;
            ShowLogin = false;
            ShowLongDescription = true;
#if IOS
                var viewController = Platform.GetCurrentUIViewController();
                authCodeCredentialsProvider.WithViewController(viewController);
#endif
            Task.Run(async () =>
            {
                await Task.Delay(6000);
            }).ContinueWith(cw => Reset());

            Task.Run(async () =>
            {
#if ANDROID
                var activity = await Platform.WaitForActivityAsync();
                authCodeCredentialsProvider.WithActivity(activity);
#endif
                var accessToken = string.Empty;
                

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    accessToken = await authCodeCredentialsProvider.AcquireTokenAsync().ConfigureAwait(false);
                    await (this.shellViewModel as ShellViewModel).TrimbleConnectClient.InitializeTrimbleConnectUserAsync().ConfigureAwait(false);
                });
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    IsLoading = false;
                    ShowLogin = true;
                    var projectListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
                    await projectListViewModel.PopulateRegions().ConfigureAwait(false);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
                    });
                }
            });
        }

        private void Reset()
        {
            ShowLaunchBrowser = true;
            ShowLogin = false;
            ShowLaunchingBrowser = false;
        }

        public void DoLogOut()
        {
            IsLoading = true;
            ShowLogin = false;

            Task.Run(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async() =>
                {
                    var result = await authCodeCredentialsProvider.Logout().ConfigureAwait(false);
                    IsLogOutPage = false;
                    IsLoading = false;
                    ShowLogin = true;
                    ShowLongDescription = false;
                    ShowLaunchBrowser = false;
                    ShowLaunchingBrowser = false;
                });

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    IsLoading = false;
                    await Shell.Current.GoToAsync($"//{nameof(LoginView)}").ConfigureAwait(false);
                });
            });
        }

        [RelayCommand]
        private void DoLoginAgain()
        {
            authCodeCredentialsProvider.Cancel();
            ShowLaunchingBrowser = true;
            ShowLaunchBrowser = false;
            SignIn();
        }
    }
}

