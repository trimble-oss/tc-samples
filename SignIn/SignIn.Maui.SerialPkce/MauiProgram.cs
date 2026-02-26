using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using SignIn.Maui.SerialPkce.ViewModels;
using CommunityToolkit.Maui;
using Trimble.Identity.OAuth.AuthCode;

namespace SignIn.Maui.SerialPkce
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

            // Register AuthCodeCredentialsProvider with Serial PKCE enabled
            builder.Services.AddSingleton<IAuthCodeCredentialsProvider, AuthCodeCredentialsProvider>();
            builder.Services.AddSingleton<IShellViewModel, ShellViewModel>();
            builder.Services.AddSingleton<ILoginViewModel, LoginViewModel>();
            builder.Services.AddSingleton<IProjectsListViewModel, ProjectsListViewModel>();
            builder.Services.AddSingleton<LoginView>();
            builder.Services.AddSingleton<ProjectsView>();
            builder.Services.AddTransient<UserControls.ToolBar>();


#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
