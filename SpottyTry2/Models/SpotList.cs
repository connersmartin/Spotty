using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class SpotList
    {
        public bool Collaborative { get; set; }
        public string Description { get; set; }
        public string External_Urls { get; set; } //external url object
        public  string Href { get; set; }
        public  string Id { get; set; }
        public Image[] Images { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; } //user object
        public bool Public { get; set; }
        public string Tracks { get; set; } // array of tracks object
        public string Type { get; set; }
        public string Uri { get; set; }

    }
}