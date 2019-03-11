using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class BaseSpotify
    {
        //You can GET the href you receive to get the necessary information
        public string Href { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
        public Dictionary<string, string> External_Urls { get; set; } //external url object   

    }
}