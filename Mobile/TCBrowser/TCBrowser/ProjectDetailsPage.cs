namespace Examples.Mobile
{
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of project members.
    /// </summary>
    public class ProjectDetailsPage : ContentPage
    {
        private IProjectClient client;

        public ProjectDetailsPage(IProjectClient client, Project entity)
        {
            this.client = client;

            this.Title = "General";

            this.Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 10,

                    Children =
                    {
                        new Image ()
                        {
                            Source = ImageSource.FromStream(() => new ThumbnailImageSource(client).GetStream(entity.ThumbnailUrl).Result),
                            Aspect = Aspect.AspectFit,
                        },

                        new Label
                        {
                            Text = entity.Name,
                        },

                        new Label
                        {
                            Text = string.Format("Uri: {0}", client.ServiceUri.AbsoluteUri),
                        },

                        new Label
                        {
                            Text = string.Format("Location: {0}", entity.Location),
                        },

                        new Label
                        {
                            Text = string.Format("Size: {0}", entity.Size),
                        },

                        new Label
                        {
                            Text = string.Format("Created by '{0}' on {1}", entity.CreatedBy.Email, entity.CreatedOn.GetValueOrDefault()),
                        },

                        new Label
                        {
                            Text = string.Format("Modified by '{0}' on {1}", entity.ModifiedBy.Email, entity.ModifiedOn.GetValueOrDefault()),
                        },

                        new Label
                        {
                            Text = string.Format("Last visited on {0}", entity.LastVisited.GetValueOrDefault()),
                        },

                    }
                },
            };
        }
    }
}
