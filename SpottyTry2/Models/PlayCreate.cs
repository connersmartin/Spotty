using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SpottyTry2.Models
{
    public class PlayCreate
    {
        public string UserId { get; set; }
        public List<SelectListItem> Genres { get; set; }
        public string Artist { get; set; }
        public string Genre { get; set; }
        public int Length { get; set; }
        public int Bpm { get; set; }
        public string Name { get; set; }
    }
}