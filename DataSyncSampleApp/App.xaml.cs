namespace DataSyncSampleApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new SignInPage());
        
        // Request storage permissions on startup (Android only)
#if ANDROID
        RequestStoragePermissions();
#endif
    }
    
#if ANDROID
    private async void RequestStoragePermissions()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }
            
            var readStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (readStatus != PermissionStatus.Granted)
            {
                readStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request failed: {ex.Message}");
        }
    }
#endif
}
