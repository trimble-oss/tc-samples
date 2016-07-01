namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of projects.
    /// </summary>
    public class ProjectListPage : ContentPage
    {
        private readonly ObservableCollection<ProjectVM> items = new ObservableCollection<ProjectVM>();
        private readonly ITrimbleConnectClient client;

        public ProjectListPage(ITrimbleConnectClient client)
        {
            this.client = client;

            this.Title = "Projects";

            this.RefreshAsync();

            var listView = new ListView
            {
                ItemsSource = this.items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new ImageCell();
                    cell.SetBinding<ProjectVM>(TextCell.TextProperty, _ => _.Name);
                    cell.SetBinding<ProjectVM>(TextCell.DetailProperty, _ => _.Description);
                    cell.SetBinding<ProjectVM>(ImageCell.ImageSourceProperty, _ => _.ImageSource);

                    cell.Tapped += async delegate
                    {
                        var project = (ProjectVM)cell.BindingContext;
                        await this.Navigation.PushAsync(new ProjectPage(await this.client.GetProjectClientAsync(project.Entity), project.Entity));
                    };

                    return cell;
                }),
                IsPullToRefreshEnabled = true,
            };

            listView.RefreshCommand = new Command(async () =>
            {
                try
                {
                    await this.RefreshAsync();
                }
                finally
                {
                    listView.IsRefreshing = false;
                }
            });
            this.Content = listView;
        }

        private async Task RefreshAsync()
        {
            if (this.IsBusy)
            {
                return;
            }

            this.IsBusy = true;

            try
            {
                var projects = await this.client.GetProjectsAsync(100).ConfigureAwait(false);

                this.items.Clear();

                while(true)
                {
                    ////Device.BeginInvokeOnMainThread(() => {
                        foreach (var project in projects)
                        {
                            this.items.Add(new ProjectVM(this.client, project));
                        }
                    ////});

                    if (projects.HasMore)
                    {
                        projects = await projects.GetNextPageAsync().ConfigureAwait(false);
                    }
					else
					{
						break;
					}
				}
            }
            catch (Exception e)
            {
                await this.DisplayAlert("ProjectList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        class ProjectVM
        {
            public ProjectVM(ITrimbleConnectClient client, Project entity)
            {
                this.Entity = entity;
                this.Name = entity.Name;
                this.Description = entity.Description;

                if (this.Entity.ThumbnailUrl != null && this.Entity.ThumbnailUrl.StartsWith("http"))
                {
                    this.ImageSource = ImageSource.FromStream(() => new ThumbnailImageSource(client.GetProjectClientAsync(this.Entity).Result).GetStream(this.Entity.ThumbnailUrl).Result);
                }
            }

            public Project Entity { get; private set; }
            public string Name { get; private set; }
            public string Description { get; private set; }
            public ImageSource ImageSource { get; private set; }
        }
    }
}
