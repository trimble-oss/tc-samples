using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using SignIn.Maui.Models;
using Trimble.Identity.OAuth.AuthCode;

namespace SignIn.Maui.ViewModels
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

        public event Action Signout = delegate { };
        public event Action SignOut;

        private readonly IAuthCodeCredentialsProvider authCodeCredentialsProvider;

        private readonly IShellViewModel shellViewModel;

        public LoginViewModel(IAuthCodeCredentialsProvider loginContext, IShellViewModel shellViewModel)
        {
            ShowLogin = true;
            this.authCodeCredentialsProvider = loginContext;
            this.shellViewModel = shellViewModel;
            this.authCodeCredentialsProvider.OnTokenRefreshed += AuthCodeCredentialsProvider_OnTokenRefreshed;
        }

        private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SignInSample", "config.json");
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
                Console.WriteLine($"Error saving refresh token: {ex.Message}");
            }
        }

        public void DoSilentLogin()
        {
            var refreshToken = string.Empty;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SignInSample", "config.json");

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
                    Console.WriteLine($"Error reading refresh token file for silent login: {ex.Message}");
                    refreshToken = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                IsLoading = true;
                ShowLogin = false;
                IsLogOutPage = false;

                //authCodeCredentialsProvider.OnTokenRefreshed += (token, expiry) =>
                //{
                //    // Store the new refresh token (you can save it in the same way as above)
                //    var refreshTokenInfo = new RefreshTokenInfo(token, expiry, true);
                //    File.WriteAllText(path, JsonConvert.SerializeObject(refreshTokenInfo));
                //};

                Task.Run(async () =>
                {
                    try
                    {
                        authCodeCredentialsProvider.WithRefreshToken(refreshToken);
                        var accessToken = await authCodeCredentialsProvider.RefreshTokenAsync().ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            var projectListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
                            await projectListViewModel.PopulateRegions().ConfigureAwait(false);
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                                    await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
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
                //var viewController = Platform.GetCurrentUIViewController();
                //authCodeCredentialsProvider.WithViewController(viewController);
#endif
            Task.Run(async () =>
            {
                await Task.Delay(6000);
            }).ContinueWith(cw => Reset());

            Task.Run(async () =>
            {
#if ANDROID
                //var activity = await Platform.WaitForActivityAsync();
                //authCodeCredentialsProvider.WithActivity(activity);
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

