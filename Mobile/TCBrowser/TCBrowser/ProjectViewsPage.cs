namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Xamarin.Forms;

    /// <summary>
    /// The list of views in the project.
    /// </summary>
    public class ProjectViewsPage : ContentPage
    {
        private ObservableCollection<ViewVM> items = new ObservableCollection<ViewVM>();
        private IProjectClient client;

        public ProjectViewsPage(IProjectClient client)
        {
            this.client = client;

            this.Title = "Views";

            this.RefreshAsync();

            var listView = new ListView
            {
                ItemsSource = items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new ImageCell();
                    cell.SetBinding<ViewVM>(TextCell.TextProperty, _ => _.Name);
                    cell.SetBinding<ViewVM>(TextCell.DetailProperty, _ => _.Description);
                    cell.SetBinding<ViewVM>(ImageCell.ImageSourceProperty, _ => _.ImageSource);
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
                var entities = await this.client.Views.GetAllAsync();

                this.items.Clear();
                foreach (var entity in entities)
                {
                    items.Add(new ViewVM(this.client, entity));
                }
            }
            catch (Exception e)
            {
                await this.DisplayAlert("ViewList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        class ViewVM : INotifyPropertyChanged
        {
            public ViewVM(IProjectClient client, Trimble.Connect.Client.Models.View entity)
            {
                this.Entity = entity;
                this.Name = entity.Name;
                this.Description = entity.Description;

                this.LoadImageAsync(client);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public Trimble.Connect.Client.Models.View Entity { get; private set; }
            public string Name { get; private set; }
            public string Description { get; private set; }
            public ImageSource ImageSource { get; private set; }

            private async void LoadImageAsync(IProjectClient client)
            {
                if (this.Entity.ThumbnailUrl != null && this.Entity.ThumbnailUrl.StartsWith("http"))
                {
                    ImageSource = ImageSource.FromStream(() => new ThumbnailImageSource(client).GetStream(this.Entity.ThumbnailUrl).Result);
                    NotifyPropertyChanged("ImageSource");
                }
            }

            private void NotifyPropertyChanged(string info)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
