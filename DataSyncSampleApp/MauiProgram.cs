using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;

namespace DataSyncSampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SqlCipherBootstrap.EnsureInitialized();
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
