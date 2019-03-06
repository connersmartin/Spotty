using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class User: BaseSpotify
    {
        public string Display_Name { get; set; }
        public KeyValuePair<string, int> Followers { get; set; }
        public Image[] Images { get; set; }
        
    }
}
