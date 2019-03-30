using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SpottyTry2.Models
{
    public class Track:BaseSpotify
    {
        
        public Album Album { get; set; }
        public Artist[] Artists { get; set; }
        public string[] Available_Markets { get; set; }
        public int Disc_Number { get; set; }
        public int Duration_Ms { get; set; }
        public bool Episode { get; set; }

        public bool Explicit { get; set; }
        public Dictionary<string, string> ExternalIds { get; set; }
        public bool Is_Local { get; set; }
        public string Name { get; set; }
        public  int Popularity { get; set; }
        public string PreviewUrl { get; set; }
        public bool Trck { get; set; }
        public int TrackNumber { get; set; }

        
        public string Preview_Url { get; set; }
        






    }
}