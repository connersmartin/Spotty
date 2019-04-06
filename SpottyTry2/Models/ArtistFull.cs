using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class ArtistFull:Artist
    {
        public Dictionary<string,string> Followers { get; set; }
        public string[] Genres { get; set; }
        public Image[] Images { get; set; }
        public int Popularity { get; set; }

    }
}