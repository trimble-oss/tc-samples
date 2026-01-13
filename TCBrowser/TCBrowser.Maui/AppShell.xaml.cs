namespace TCBrowser.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LoginView), typeof(LoginView));
            Routing.RegisterRoute(nameof(ProjectsView), typeof(ProjectsView));
            Routing.RegisterRoute(nameof(ProjectDetailsView), typeof(ProjectDetailsView));
        }
    }
}
