namespace Examples.Mobile
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Trimble.Connect.Client;
    using Trimble.Connect.Client.Models;
    using Xamarin.Forms;

    /// <summary>
    /// The list of file versions.
    /// </summary>
    public class FileDetailsPage : ContentPage
    {
        private IProjectClient client;
        private readonly FolderItem item;
        private ProgressBar progressBar;
        private Label stat;

        public FileDetailsPage(IProjectClient client, FolderItem item)
        {
            this.client = client;
            this.item = item;

            this.Title = "File Details";

            Entry format;

            this.Content = new StackLayout
            {
                Padding = 20,
                Spacing = 20,

                Children =
                {
                    new Image ()
                    {
                        Source = ImageSource.FromStream(() => new ThumbnailImageSource(client).GetStream(this.item.ThumbnailUrl).Result),
                        Aspect = Aspect.AspectFit,
                    },

                    new Label
                    {
                        Text = item.Name,
                        HorizontalOptions = LayoutOptions.Center,
                    },

                    new Label
                    {
                        Text = string.Format("Size={0}", item.Size),
                        HorizontalOptions = LayoutOptions.Center,
                    },

                    (format = new Entry
                    {
                        Placeholder = "format",
                        Keyboard = Keyboard.Email,
                    }),

                    new Button
                    {
                        Text = "Download",
                        Command = new Command(async () =>
                        {
                            await this.Download(format.Text);
                        })
                    },

                    (this.progressBar = new ProgressBar
                    {
                    }),

                    (this.stat = new Label
                    {
                        Text = string.Empty,
                        HorizontalOptions = LayoutOptions.Center,
                    }),
                }
            };
        }

        private async Task Download(string format)
        {
            this.progressBar.Progress = 0f;
            var watch = Stopwatch.StartNew();
            var total = 0L;
            
            using (var stream = await this.client.Files.DownloadAsync(
                this.item.Identifier, 
                this.item.VersionIdentifier, 
                string.IsNullOrEmpty(format) ? null : format, 
                (args) => this.progressBar.Progress = args.ProgressPercentage / 100f))
            {
                byte[] buffer = new byte[8 * 1024];
                for (;;)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    total += bytesRead;
                }
            }

            var elapsed = watch.ElapsedMilliseconds;
            this.stat.Text = string.Format("{0} bytes in {1} ms ({2} b/sec)", total, elapsed, total / (elapsed / 1000f));
        }
    }
}
