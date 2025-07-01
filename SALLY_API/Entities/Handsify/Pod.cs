using DocumentFormat.OpenXml.Spreadsheet;
using System.Drawing;
using System.Collections.Generic;

namespace SALLY_API.Entities.Handsify
{
    public class Pod
    {
        //public string PodMapLocation { get; set; }
        // public List<HHStation> HHStations { get; set; } = new List<HHStation>();

        public Dictionary<int, HHStation> HHStations { get; set; } = new Dictionary<int, HHStation>();

        //public Pod(string podMapLocation, List<HHStation> hhStations)
        //{
        //   // PodMapLocation = podMapLocation;
        //    HHStations = hhStations;

        //}

        public Pod()
        {

        }
        public override string ToString()
        {
            string stations = "";

            foreach (HHStation station in HHStations.Values)
            {

                stations += "\n" + station.StationID;

            }

            return stations;

        }
    }
}
