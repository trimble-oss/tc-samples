using TCBrowser.Maui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimble.Connect.Client.Common;
using Trimble.Connect.Client.Models;
using Region = Trimble.Connect.Client.Models.Region;

namespace TCBrowser.Maui
{
    public interface IProjectsListViewModel
    {
        /// <summary>
        /// Region Infos dictionary
        /// Key - Location string of region as returned by server,
        /// Value - Region Info of the region
        /// </summary>
        Dictionary<string, Region> RegionInfos { get; }

        ObservableCollection<ProjectMetaData> FilteredProjects { get; set; }

        string SelectedRegionName { get; set; }

        Task PopulateRegions();

        Task PopulateProjectsInRegion();
    }
}
