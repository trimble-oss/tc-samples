using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using SignIn.Maui.SerialPkce.Models;
using Trimble.Identity.OAuth.AuthCode;

namespace SignIn.Maui.SerialPkce.ViewModels
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
            
            IsLoading = false;
            ShowLogin = true;
            ShowLaunchBrowser = false;
            ShowLaunchingBrowser = false;
            IsLogOutPage = false;
            ShowLongDescription = false;
            
            this.authCodeCredentialsProvider = loginContext;
            this.shellViewModel = shellViewModel;
            authCodeCredentialsProvider.OnTokenRefreshed += AuthCodeCredentialsProvider_OnTokenRefreshed;
            
            Console.WriteLine($"[LoginViewModel] Initial state - IsLoading: {IsLoading}, ShowLogin: {ShowLogin}");
        }

        private void AuthCodeCredentialsProvider_OnTokenRefreshed(string refreshToken, long timeInTicks)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SignInSample.SerialPkce", "config.json");
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
                Console.WriteLine($"Error saving refresh token and code verifier: {ex.Message}");
            }
        }

        public void DoSilentLogin()
        {
            try
            {
                Console.WriteLine("[DoSilentLogin] Starting silent login attempt");
                var refreshToken = string.Empty;
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SignInSample.SerialPkce", "config.json");

                if (File.Exists(path))
                {
                    try
                    {
                        Console.WriteLine($"[DoSilentLogin] Reading config from: {path}");
                        using (var fileStream = File.OpenText(path))
                        {
                            using (var reader = new JsonTextReader(fileStream))
                            {
                                var tokenInfo = JsonSerializer.CreateDefault(new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local }).Deserialize<RefreshTokenInfo>(reader);
                                refreshToken = tokenInfo?.RefreshToken;
                                Console.WriteLine($"[DoSilentLogin] Token loaded: {!string.IsNullOrEmpty(refreshToken)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DoSilentLogin] Error reading refresh token file: {ex.Message}");
                        Console.WriteLine($"[DoSilentLogin] Stack trace: {ex.StackTrace}");
                        
                        try
                        {
                            Console.WriteLine($"[DoSilentLogin] Deleting corrupted config file");
                            File.Delete(path);
                        }
                        catch
                        {
                        }
                        
                        refreshToken = string.Empty;
                    }
                }
                else
                {
                    Console.WriteLine($"[DoSilentLogin] No config file found at: {path}");
                }

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine("[DoSilentLogin] Attempting silent login with saved token");
                    IsLoading = true;
                    ShowLogin = false;
                    IsLogOutPage = false;

                    Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine("[DoSilentLogin] Calling WithRefreshToken");
                            authCodeCredentialsProvider.WithRefreshToken(refreshToken);
                            
                            Console.WriteLine("[DoSilentLogin] Calling RefreshTokenAsync");
                            var refreshTask = authCodeCredentialsProvider.RefreshTokenAsync();
                            var timeoutTask = Task.Delay(15000);
                            var completedTask = await Task.WhenAny(refreshTask, timeoutTask).ConfigureAwait(false);

                            if (completedTask == timeoutTask)
                            {
                                Console.WriteLine("[DoSilentLogin] Refresh timed out after 15 seconds");
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    IsLoading = false;
                                    ShowLogin = true;
                                    ShowLaunchBrowser = false;
                                    ShowLaunchingBrowser = false;
                                });
                                return;
                            }

                            var accessToken = await refreshTask.ConfigureAwait(false);
                            Console.WriteLine($"[DoSilentLogin] Refresh completed. Token received: {!string.IsNullOrEmpty(accessToken)}");

                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                Console.WriteLine("[DoSilentLogin] Populating regions");
                                var projectListViewModel = Application.Current.Handler.MauiContext.Services.GetService<IProjectsListViewModel>();
                                await projectListViewModel.PopulateRegions().ConfigureAwait(false);
                                
                                Console.WriteLine("[DoSilentLogin] Navigating to ProjectsView");
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
                                });
                            }
                            else
                            {
                                Console.WriteLine("[DoSilentLogin] No access token received, showing login");
                                await MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    IsLoading = false;
                                    ShowLogin = true;
                                    ShowLaunchBrowser = false;
                                    ShowLaunchingBrowser = false;
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DoSilentLogin] Exception during silent login: {ex.Message}");
                            Console.WriteLine($"[DoSilentLogin] Exception type: {ex.GetType().Name}");
                            Console.WriteLine($"[DoSilentLogin] Stack trace: {ex.StackTrace}");
                            
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                IsLoading = false;
                                ShowLogin = true;
                                ShowLaunchBrowser = false;
                                ShowLaunchingBrowser = false;
                            });
                        }
                    });
                }
                else
                {
                    Console.WriteLine("[DoSilentLogin] No refresh token available, showing login screen");
                    IsLoading = false;
                    ShowLogin = true;
                    ShowLaunchBrowser = false;
                    ShowLaunchingBrowser = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DoSilentLogin] CRITICAL: Outer exception caught: {ex.Message}");
                Console.WriteLine($"[DoSilentLogin] Stack trace: {ex.StackTrace}");
                
                // Ensure UI is always in a valid state
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    ShowLogin = true;
                    ShowLaunchBrowser = false;
                    ShowLaunchingBrowser = false;
                });
            }
        }

        [RelayCommand]
        private void SignIn()
        {
            Console.WriteLine("SignIn button clicked - starting authentication flow");
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
                await MainThread.InvokeOnMainThreadAsync(Reset);
            });

            Task.Run(async () =>
            {
                try
                {
#if ANDROID
                    var activity = await Platform.WaitForActivityAsync();
                    authCodeCredentialsProvider.WithActivity(activity);
#endif
                    var accessToken = string.Empty;

                    Console.WriteLine("Calling AcquireTokenAsync...");

                    // Run AcquireTokenAsync off UI thread - it blocks on browser redirect; blocking UI thread causes WinUI pointer handler crashes
                    accessToken = await authCodeCredentialsProvider.AcquireTokenAsync().ConfigureAwait(false);
                    Console.WriteLine($"AcquireTokenAsync completed. Token received: {!string.IsNullOrEmpty(accessToken)}");

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
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
                            Console.WriteLine("Navigating to ProjectsView...");
                            await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
                        });
                    }
                    else
                    {
                        Console.WriteLine("No access token received");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            IsLoading = false;
                            ShowLogin = true;
                            ShowLaunchBrowser = false;
                            ShowLaunchingBrowser = false;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignIn error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsLoading = false;
                        ShowLogin = true;
                        ShowLongDescription = false;
                        ShowLaunchBrowser = false;
                        ShowLaunchingBrowser = false;
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
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Serial PKCE: Logout clears the access token, refresh token, and code verifier
                    var result = await authCodeCredentialsProvider.Logout().ConfigureAwait(false);
                    IsLogOutPage = false;
                    IsLoading = false;
                    ShowLogin = true;
                    ShowLongDescription = false;
                    ShowLaunchBrowser = false;
                    ShowLaunchingBrowser = false;

                    // Remove saved config so DoSilentLogin won't try refresh (would fail without code verifier)
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SignInSample.SerialPkce", "config.json");
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing config on logout: {ex.Message}");
                    }
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
