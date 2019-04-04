using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class Seed
    {
        public int InitialPoolSize { get; set; }
        public int AfterFilteringSize { get; set; }
        public int AfterRelinkingSize { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Href { get; set; }
    }
}