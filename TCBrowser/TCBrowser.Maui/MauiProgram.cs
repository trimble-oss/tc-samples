using Microsoft.Extensions.Logging;
using TCBrowser.Maui.ViewModels;
using CommunityToolkit.Maui;
using Trimble.Identity.OAuth.AuthCode;

namespace TCBrowser.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IAuthCodeCredentialsProvider, AuthCodeCredentialsProvider>();
            builder.Services.AddSingleton<IShellViewModel, ShellViewModel>();
            builder.Services.AddSingleton<ILoginViewModel, LoginViewModel>();
            builder.Services.AddSingleton<IProjectsListViewModel, ProjectsListViewModel>();
            builder.Services.AddSingleton<LoginView>();
            builder.Services.AddSingleton<ProjectsView>();
            builder.Services.AddSingleton<ProjectDetailsView>();
            builder.Services.AddTransient<UserControls.ToolBar>();
            builder.Services.AddSingleton<ProjectVm>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
