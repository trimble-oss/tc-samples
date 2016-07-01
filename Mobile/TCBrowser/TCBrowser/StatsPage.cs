namespace Examples.Mobile
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;

#if __IOS__
    using Foundation;
    using UIKit;
#elif __ANDROID__
    using Android.Content;
#endif

    using Xamarin.Forms;
    using System.Collections.Generic;
    /// <summary>
    /// The network statistics page.
    /// </summary>
    public class StatsPage : ContentPage
    {
        private readonly ObservableCollection<SampleVM> items = new ObservableCollection<SampleVM>();
        private bool sortByName = true;

        public StatsPage()
        {
            this.Title = "NetStats";
            ToolbarItem sort;

            this.ToolbarItems.Add(
                new ToolbarItem
                {
                    Text = "Share",
                    Icon = "UIBarButtonAction.png",
                    Command = new Command(() => this.ShareNetStats())
                });

            this.ToolbarItems.Add(
                new ToolbarItem
                {
                    Text = "Clear",
                    Icon = "UIBarButtonTrash.png",
                    Command = new Command(() => this.ClearNetStats())
                });

            this.ToolbarItems.Add(
                sort = new ToolbarItem
                {
                    Text = "Sort",
                    Command = new Command(() => { sortByName = !sortByName; this.Refresh(); })
                });

            this.Content = new ListView
            {
                ItemsSource = this.items,

                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new TextCell();
                    cell.SetBinding<SampleVM>(TextCell.TextProperty, _ => _.Name);
                    cell.SetBinding<SampleVM>(TextCell.DetailProperty, _ => _.Details);
                    return cell;
                }),
            };

            this.Refresh();
        }

        private void Refresh()
        {
            items.Clear();

            if (this.sortByName)
            {
                var byName = PerfTracer.Samples.GroupBy(s => s.Name);
                foreach (var sample in byName.OrderBy(s => new Uri(s.Key).AbsolutePath))
                {
                    var item = new SampleVM
                    {
                        Name = new Uri(sample.Key).AbsolutePath,
                        Details = string.Format(
                            "{0}:[{1} - {2:0.##} - {3}]ms",
                            sample.Count(),
                            sample.Select(s => s.ElapsedMilliseconds).Min(),
                            sample.Select(s => s.ElapsedMilliseconds).Average(),
                            sample.Select(s => s.ElapsedMilliseconds).Max())
                    };

                    this.items.Add(item);
                }
            }
            else
            {
                foreach (var sample in PerfTracer.Samples.OrderBy(s => s.Time))
                {
                    var item = new SampleVM
                    {
                        Name = new Uri(sample.Name).AbsolutePath,
                        Details = string.Format("{0:H:mm:ss.fff} {1}ms", sample.Time.ToLocalTime(), sample.ElapsedMilliseconds)
                    };

                    this.items.Add(item);
                }
            }
        }

        private void ShareNetStats()
        {
            var builder = new StringBuilder();

            if (this.sortByName)
            {
                var byName = PerfTracer.Samples.GroupBy(s => s.Name);
                foreach (var sample in byName.OrderBy(s => s.Key))
                {
                    builder.AppendFormat(
                        "{0}, {1}, {2}, {3:0.##}, {4}",
                        sample.Key,
                        sample.Count(),
                        sample.Select(s => s.ElapsedMilliseconds).Min(),
                        sample.Select(s => s.ElapsedMilliseconds).Average(),
                        sample.Select(s => s.ElapsedMilliseconds).Max());
                    builder.AppendLine();
                }
            }
            else
            {
                foreach (var sample in PerfTracer.Samples.OrderBy(s => s.Time))
                {
                    builder.AppendFormat(
                        "{0}, {1}, {2}",
                        sample.Time,
                        sample.Name,
                        sample.ElapsedMilliseconds);
                    builder.AppendLine();
                }
            }

#if __IOS__
            var activityController = new UIActivityViewController(new NSObject[] { new NSString(builder.ToString()) }, null);
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(activityController, true, null);
#elif __ANDROID__
            var intent = new Intent(Intent.ActionSend);      
            intent.SetType("text/plain"); 
            intent.PutExtra(Intent.ExtraText, builder.ToString()); 
            Forms.Context.StartActivity(Intent.CreateChooser(intent, "Choose an App"));
#endif
        }

        private void ClearNetStats()
        {
            PerfTracer.Clear();
            this.Refresh();
        }

        class SampleVM
        {
            public string Name { get; set; }

            public string Details { get; set; }
        }
    }
}
