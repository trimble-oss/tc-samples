namespace Examples.Mobile
{
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The file page.
    /// </summary>
    public class FilePage : TabbedPage
    {
        private IProjectClient client;

        public FilePage(IProjectClient client, FolderItem item)
        {
            this.client = client;
            Children.Add(new FileDetailsPage(client, item));
            Children.Add(new FileHistoryPage(client, item));
        }
    }
}
