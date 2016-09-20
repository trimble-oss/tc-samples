using System.Linq;

namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
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

        public ProjectListPage()
        {
            this.Title = "Projects";

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
                        await this.Navigation.PushAsync(new ProjectPage(await AppState.Client.GetProjectClientAsync(project.Entity), project.Entity));
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

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (AppState.CurrentUser == null)
            {
                try
                {
                    await AppState.SignInSilentlyAsync();
                    ((App.Current.MainPage as MainPage).Master as SettingsPage).Refresh();
                    await this.RefreshAsync();
                }
                catch (Exception e)
                {
                    App.Current.MainPage = new LoginPage();
                }
            }
            else
            {
                if (!this.items.Any())
                {
                    await this.RefreshAsync();
                }
            }
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
                var projects = await AppState.Client.GetProjectsAsync(100);

                this.items.Clear();

                while(true)
                {
                    ////Device.BeginInvokeOnMainThread(() => {
                        foreach (var project in projects)
                        {
                            this.items.Add(new ProjectVM(AppState.Client, project));
                        }
                    ////});

                    if (projects.HasMore)
                    {
                        projects = await projects.GetNextPageAsync();
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
