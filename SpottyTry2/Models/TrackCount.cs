﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class TrackCount:PageResponse
    {
        public PlaylistItems[] Items { get; set; }
    }
}