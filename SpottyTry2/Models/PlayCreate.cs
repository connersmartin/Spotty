using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SelectListItem = System.Web.WebPages.Html.SelectListItem;

namespace SpottyTry2.Models
{
    public class PlayCreate
    {
        public string UserId { get; set; }
        public List<string> Genres { get; set; }
        public string Genre { get; set; }

        public int Bpm { get; set; }
        public string Name { get; set; }
    }
}