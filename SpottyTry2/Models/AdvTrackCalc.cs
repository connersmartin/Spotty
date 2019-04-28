using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class AdvTrackCalc
    {
        public List<AdvTrack> Tracks { get; set; }
        public float? TotDance {get; set;}
        public float? TotEnergy {get; set;}
        public int? TotKey {get; set;}
        public int? TotMode {get; set;}
        public float? TotLoudness {get; set;}
        public float? TotSpeechiness {get; set;}
        public float? TotAcousticness {get; set;}
        public float? TotInstrumentalness {get; set;}
        public float? TotLiveness {get; set;}
        public float? TotValence {get; set;}
        public float? TotTempo {get; set;}
        public int? TotDuration {get; set;}
        public List<float?> Dance { get; set; }
        public List<float?> Energy { get; set; }
        public List<int?> Key { get; set; }
        public List<int?> Mode { get; set; }
        public List<float?> Loudness { get; set; }
        public List<float?> Speechiness { get; set; }
        public List<float?> Acousticness { get; set; }
        public List<float?> Instrumentalness { get; set; }
        public List<float?> Liveness { get; set; }
        public List<float?> Valence { get; set; }
        public List<float?> Tempo { get; set; }
        public List<int?> Duration { get; set; }
        public float? StdDance { get; set; }
        public float? StdEnergy { get; set; }
        public int? StdKey { get; set; }
        public int? StdMode { get; set; }
        public float? StdLoudness { get; set; }
        public float? StdSpeechiness { get; set; }
        public float? StdAcousticness { get; set; }
        public float? StdInstrumentalness { get; set; }
        public float? StdLiveness { get; set; }
        public float? StdValence { get; set; }
        public float? StdTempo { get; set; }
        public int? StdDuration { get; set; }
        public float? AvgDance { get; set; }
        public float? AvgEnergy { get; set; }
        public int? AvgKey { get; set; }
        public int? AvgMode { get; set; }
        public float? AvgLoudness { get; set; }
        public float? AvgSpeechiness { get; set; }
        public float? AvgAcousticness { get; set; }
        public float? AvgInstrumentalness { get; set; }
        public float? AvgLiveness { get; set; }
        public float? AvgValence { get; set; }
        public float? AvgTempo { get; set; }
        public int? AvgDuration { get; set; }
        public Dictionary<string,int> GenreCount { get; set; }
        public int? Total { get; set; }

        public AdvTrackCalc()
        {
            Dance = new List<float?>();
            Energy = new List<float?>();
            Key = new List<int?>();
            Mode = new List<int?>();
            Loudness = new List<float?>();
            Speechiness = new List<float?>();
            Acousticness = new List<float?>();
            Instrumentalness = new List<float?>();
            Liveness = new List<float?>();
            Valence = new List<float?>();
            Tempo = new List<float?>();
            Duration = new List<int?>();
            GenreCount = new Dictionary<string, int>();
            TotDance = 0;
            TotEnergy = 0;
            TotKey = 0;
            TotMode = 0;
            TotLoudness = 0;
            TotSpeechiness = 0;
            TotAcousticness = 0;
            TotInstrumentalness = 0;
            TotLiveness = 0;
            TotValence = 0;
            TotTempo = 0;
            TotDuration = 0;
        }

        public double GetStdDev(float? avg, List<float?> val)
        {
            double std=0;

            foreach (var v in val)
            {
                std += Math.Pow((double)v - (double)avg, 2);
            }

            return Math.Sqrt(std/val.Count);
        }
        public double GetStdDev(float? avg, List<int?> val)
        {
            double std = 0;

            foreach (var v in val)
            {
                std += Math.Pow((double)v - (double)avg, 2);
            }

            return Math.Sqrt(std/val.Count);
        }
    }
}