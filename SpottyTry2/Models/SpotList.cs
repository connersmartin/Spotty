using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SpottyTry2.Models
{
    public class SpotList : BaseSpotify
    {
        public bool Collaborative { get; set; }
        public string Description { get; set; }
        public Image[] Images { get; set; }
        public string Name { get; set; }
        public User Owner { get; set; } //user object
        public bool Public { get; set; }
        public TrackCount Tracks { get; set; } // array of tracks object
        public Dictionary<string, string> Followers { get; set; }
        public string PrimaryColor { get; set; }
        public string Snapshot_Id { get; set; }



    }
}