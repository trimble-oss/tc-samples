using Microsoft.Maui.Controls;

namespace SignIn.Maui.SerialPkce
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LoginView), typeof(LoginView));
            Routing.RegisterRoute(nameof(ProjectsView), typeof(ProjectsView));
        }
    }
}
