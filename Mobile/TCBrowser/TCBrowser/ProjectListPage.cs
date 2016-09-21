namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Trimble.Identity;
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
                        await this.Navigation.PushAsync(new ProjectPage(await AppState.Instance.Client.GetProjectClientAsync(project.Entity), project.Entity));
                    };

                    return cell;
                }),
                IsPullToRefreshEnabled = true,
            };

            listView.RefreshCommand = new Command(
                async () =>
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

            MessagingCenter.Subscribe<AppState>(this, AppState.UserChangedMessageId, async (sender) =>
            {
                this.items.Clear();
                await this.RefreshAsync();
            });
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!AppState.Instance.IsSignedIn)
            {
                if (!AppState.Instance.IsInProgress)
                {
                    AppState.Instance.SignInAsync();
                }
            }
            else if (!this.items.Any())
            {
                await this.RefreshAsync();
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
                var projects = await AppState.Instance.Client.GetProjectsAsync(100);

                this.items.Clear();

                while (true)
                {
                    foreach (var project in projects)
                    {
                        this.items.Add(new ProjectVM(AppState.Instance.Client, project));
                    }

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
            catch (AuthenticationException e)
            {
                await AppState.Instance.SignInAsync();
            }
            catch (Exception e)
            {
                var exception = e as InvalidServiceOperationException;
                if (exception != null && (exception.StatusCode == (int) HttpStatusCode.Unauthorized || (exception.StatusCode == (int)HttpStatusCode.BadRequest && exception.ErrorCode == ResponseErrorCode.InvalidSession)))
                {
                    this.items.Clear();
                    AppState.Instance.SignInAsync();
                }
                else
                {
                    await this.DisplayAlert("ProjectList", e.Message, "OK");
                }
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
