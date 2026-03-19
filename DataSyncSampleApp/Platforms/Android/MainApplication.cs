using Android.App;
using Android.Runtime;

namespace DataSyncSampleApp;

[Application]
public class MainApplication : MauiApplication
{
    static MainApplication()
    {
        // Earliest safe hook: before instance ctor / MAUI pipeline.
        SqlCipherBootstrap.EnsureInitialized();
    }

    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        SqlCipherBootstrap.EnsureInitialized();
        base.OnCreate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
