using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SpottyTry2.Models
{
    public class Track:BaseSpotify
    {
        public Artist[] Artists { get; set; }
        public string[] Available_Markets { get; set; }
        public int Disc_Number { get; set; }
        public int Duration_Ms { get; set; }
        public bool Explicit { get; set; }
        public bool Is_Playable { get; set; }
        public Track Linked_From { get; set; }
        public Dictionary<string,string> Restrictions { get; set; }
        public string Preview_Url { get; set; }
        public int Track_Number { get; set; }
        public bool Is_Local { get; set; }
    }
}