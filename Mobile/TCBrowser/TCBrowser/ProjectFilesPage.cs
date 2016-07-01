namespace Examples.Mobile
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading.Tasks;
	using Trimble.Connect.Client;
	using Trimble.Connect.Client.Models;
	using Xamarin.Forms;

	/// <summary>
	/// The list of files in the project.
	/// </summary>
	public class ProjectFilesPage : ContentPage
    {
        private readonly ObservableCollection<NodeVM> items = new ObservableCollection<NodeVM>();
        private readonly IProjectClient client;
        private readonly string rootFolderId;

        public ProjectFilesPage(IProjectClient client, string rootFolderId)
        {
            this.client = client;
            this.rootFolderId = rootFolderId;

            this.Title = "Files";

            this.RefreshAsync();

            var listView = new ListView
            {
                ItemsSource = this.items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new ImageCell();
                    cell.SetBinding<NodeVM>(TextCell.TextProperty, _ => _.Name);
                    cell.SetBinding<NodeVM>(TextCell.DetailProperty, _ => _.Details);
                    cell.SetBinding<NodeVM>(ImageCell.ImageSourceProperty, _ => _.ImageSource);

                    cell.Tapped += async delegate
                    {
                        var node = (NodeVM)cell.BindingContext;
                        await this.Navigation.PushAsync(new FilePage(client, node.Item));
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
                this.items.Clear();
                await this.AddNodes(0, null, this.rootFolderId);
            }
            catch (Exception e)
            {
                await this.DisplayAlert("FileList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        private async Task AddNodes(int level, NodeVM parent, string folderIdentifier)
        {
            try
            {
				var children = (await this.client.Files.GetFolderItemsAsync(folderIdentifier)).ToArray();
                var insertafter = parent;

				var folders = new List<NodeVM>();

                foreach (var item in children)
                {
                    var vm = new NodeVM(this.client, level, item);
					if (parent == null)
					{
						this.items.Add(vm);
					}
					else
					{
						var index = this.items.IndexOf(insertafter);
						this.items.Insert(index, vm);
						insertafter = vm;
					}

                    if (item.Type == EntityType.Folder)
                    {
						folders.Add(vm);
                    }
                }

				foreach (var folder in folders)
				{
					await this.AddNodes(level + 1, folder, folder.Item.Identifier);
				}
			}
            catch (Exception e)
            {
                await this.DisplayAlert("FileList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        class NodeVM : INotifyPropertyChanged
        {
            public NodeVM(IProjectClient client, int level, FolderItem item)
            {
                this.Item = item;
                this.Name = string.Format("{0}{1}", new string(' ', 4 * level), item.Name);
                this.Details = string.Format("{0}{1}", new string(' ', 4 * level), item.Size);
				if (this.Item.ThumbnailUrl != null && this.Item.ThumbnailUrl.StartsWith("http"))
				{
					this.ImageSource = ImageSource.FromStream(() => new ThumbnailImageSource(client).GetStream(this.Item.ThumbnailUrl).Result);
					this.NotifyPropertyChanged("ImageSource");
				}
			}

            public event PropertyChangedEventHandler PropertyChanged;

            public FolderItem Item { get; private set; }

            public string Name { get; private set; }
            public string Details { get; private set; }
            public ImageSource ImageSource { get; private set; }

            private void NotifyPropertyChanged(string info)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
