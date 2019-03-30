using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class PlaylistItems
    {
        public DateTime AddedAt { get; set; }
        public User AddedBy { get; set; }
        public bool Is_Local { get; set; }
        public string PrimaryColor { get; set; }
        public Track Track { get; set; }
        public KeyValuePair<string, string> VideoThumbnail { get; set; }

    }
}