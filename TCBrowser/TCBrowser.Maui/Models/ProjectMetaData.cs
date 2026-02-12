

using CommunityToolkit.Mvvm.ComponentModel;

namespace TCBrowser.Maui.Models
{
    public partial class ProjectMetaData : ObservableObject
    {
        public string RootId { get; set; }
        
        public string Identifier { get; set; }
        public string Size { get; set; }
        public string RegionName { get; set; }

        [ObservableProperty]
        private byte[] _thumbnailSource;

        [ObservableProperty]
        private string _name;
    }
}
