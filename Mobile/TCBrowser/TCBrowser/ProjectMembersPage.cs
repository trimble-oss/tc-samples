namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of project members.
    /// </summary>
    public class ProjectMembersPage : ContentPage
    {
        private ObservableCollection<PersonVM> items = new ObservableCollection<PersonVM>();
        private IProjectClient client;

        public ProjectMembersPage(IProjectClient client)
        {
            this.client = client;

            this.Title = "Members";

            this.RefreshAsync();

            this.Content = new ListView
            {
                ItemsSource = items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new TextCell();
                    cell.SetBinding<PersonVM>(TextCell.TextProperty, _ => _.FullName);
                    cell.SetBinding<PersonVM>(TextCell.DetailProperty, _ => _.Details);
                    return cell;
                }),
                IsPullToRefreshEnabled = true,
                RefreshCommand = new Command(async () =>
                {
                    try
                    {
                        await this.RefreshAsync();
                    }
                    finally
                    {
                        ((ListView)this.Content).IsRefreshing = false;
                    }
                }),
            };
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
                var members = await this.client.Members.GetAllAsync();

                items.Clear();
                foreach (var member in members)
                {
                    items.Add(new PersonVM(member));
                }
            }
            catch (Exception e)
            {
                await this.DisplayAlert("MemberList", e.Message, "OK");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        class PersonVM
        {
            public PersonVM(Person person)
            {
                this.FullName = person.LastName + " " + person.FirstName;
                this.Details = person.Email + " (" + person.Role + ")";
            }

            public string FullName { get; private set; }
            public string Details { get; private set; }
        }
    }
}
