namespace Examples.Mobile
{
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The project page.
    /// </summary>
    public class ProjectPage : TabbedPage
    {
        private readonly IProjectClient client;
        private readonly Project entity;

        public ProjectPage(IProjectClient client, Project entity)
        {
            this.client = client;
            this.entity = entity;

            this.ToolbarItems.Add(
                new ToolbarItem
                {
                    Text = "Stats",
                    Command = new Command(() => this.Navigation.PushAsync(new StatsPage()))
                });

            this.Children.Add(new ProjectDetailsPage(client, entity));
            this.Children.Add(new ProjectMembersPage(client));
            this.Children.Add(new ProjectFilesPage(client, entity.RootFolderIdentifier));
            this.Children.Add(new ProjectToDosPage(client));
            this.Children.Add(new ProjectViewsPage(client));
        }
    }
}
