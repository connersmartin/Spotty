﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class TrackCount
    {
        public string Href { get; set; }

        public PlaylistItems[] Items { get; set; }
        public int Limit { get; set; }
        public string Next { get; set; }
        public int Offset { get; set; }
        public int? Previous { get; set; }
        public int Total { get; set; }

    }
}