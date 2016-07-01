namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of todos in the project.
    /// </summary>
    public class ProjectToDosPage : ContentPage
    {
        private readonly ObservableCollection<Todo> items = new ObservableCollection<Todo>();
        private readonly IProjectClient client;

        public ProjectToDosPage(IProjectClient client)
        {
            this.client = client;

            this.Title = "ToDos";

            this.RefreshAsync();

            var listView = new ListView
            {
                ItemsSource = items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new TextCell();
                    cell.SetBinding<Todo>(TextCell.TextProperty, _ => _.Label);
                    cell.SetBinding<Todo>(TextCell.DetailProperty, _ => _.Description);
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
                var entities = await this.client.Todos.GetAllAsync();

                this.items.Clear();
                foreach (var entity in entities)
                {
                    items.Add(entity);
                }
            }
            catch (Exception e)
            {
                await this.DisplayAlert("ToDoList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }
}
