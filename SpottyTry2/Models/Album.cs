using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class Album: BaseSpotify
    {
        public string AlbumType { get; set; }
        public Artist[] Artists { get; set; }
        public string[] AvailableMarkets { get; set; }
        public Image[] Images { get; set; }
        public string Name { get; set; }
        public string ReleaseDate { get; set; }
        public string ReleaseDatePrecision { get; set; }
        public int TotalTracks { get; set; }

    }
}