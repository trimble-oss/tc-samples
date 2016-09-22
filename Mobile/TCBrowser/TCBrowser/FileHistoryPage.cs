namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of file versions.
    /// </summary>
    public class FileHistoryPage : ContentPage
    {
        private ObservableCollection<NodeVM> items = new ObservableCollection<NodeVM>();
        private IProjectClient client;
        private readonly FolderItem item;

        public FileHistoryPage(IProjectClient client, FolderItem item)
        {
            this.client = client;
            this.item = item;

            this.Title = "History";

            this.RefreshAsync();

            var listView = new ListView
            {
                ItemsSource = items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new ImageCell();
                    cell.SetBinding<NodeVM>(TextCell.TextProperty, _ => _.Name);
                    cell.SetBinding<NodeVM>(TextCell.DetailProperty, _ => _.Details);
                    cell.SetBinding<NodeVM>(ImageCell.ImageSourceProperty, _ => _.ImageSource);
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
            if (IsBusy)
            {
                return;
            }

            this.IsBusy = true;

            try
            {
                var versions = this.item.Type == "FOLDER" 
                    ? await this.client.Files.GetFolderVersionsAsync(this.item.Identifier)
                    : await this.client.Files.GetFileVersionsAsync(this.item.Identifier);

                items.Clear();
                foreach (var version in versions)
                {
                    items.Add(new NodeVM(this.client, version));
                }
            }
            catch (Exception e)
            {
                await this.DisplayAlert("FileVersions", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        class NodeVM : INotifyPropertyChanged
        {
            public NodeVM(IProjectClient client, FolderItem item)
            {
                this.Entity = item;
                this.Name = string.Format("{0}", item.Name);
                this.Details = string.Format("{0}", item.Size);
                this.LoadImageAsync(client);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public FolderItem Entity { get; private set; }

            public string Name { get; private set; }
            public string Details { get; private set; }
            public ImageSource ImageSource { get; private set; }

            private async void LoadImageAsync(IProjectClient client)
            {
                if (string.IsNullOrEmpty(this.Entity.ParentVersionIdentifier))
                {
                    return;
                }

                try
                {
                    var info = await client.Files.GetFileInfoAsync(this.Entity.Identifier).ConfigureAwait(false);
                    if (info.ThumbnailUrl != null && info.ThumbnailUrl.StartsWith("http"))
                    {
                        ImageSource = ImageSource.FromStream(() => new ThumbnailImageSource(client).GetStream(info.ThumbnailUrl).Result);
                        NotifyPropertyChanged("ImageSource");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            private void NotifyPropertyChanged(string info)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
