using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class ListResponse
    {
        public string Href { get; set; }
        public SpotList[] Items { get; set; }
    }
}