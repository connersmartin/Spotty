using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class AdvTrackCalc
    {
        float? TotDance {get; set;}
        float? TotEnergy {get; set;}
        int? TotKey {get; set;}
        int? TotMode {get; set;}
        float? TotLoudness {get; set;}
        float? TotSpeechiness {get; set;}
        float? TotAcousticness {get; set;}
        float? TotInstrumentalness {get; set;}
        float? TotLiveness {get; set;}
        float? TotValence {get; set;}
        float? TotTempo {get; set;}
        int? TotDuration {get; set;}
        float? AvgDance { get { return this.AvgDance; } set { AvgDance = TotDance / Total; }}
        float? AvgEnergy { get; set; }
        int? AvgKey { get; set; }
        int? AvgMode { get; set; }
        float? AvgLoudness { get; set; }
        float? AvgSpeechiness { get; set; }
        float? AvgAcousticness { get; set; }
        float? AvgInstrumentalness { get; set; }
        float? AvgLiveness { get; set; }
        float? AvgValence { get; set; }
        float? AvgTempo { get; set; }
        int? AvgDuration { get; set; }
        float? StdDance { get; set; }
        float? StdEnergy { get; set; }
        int? StdKey { get; set; }
        int? StdMode { get; set; }
        float? StdLoudness { get; set; }
        float? StdSpeechiness { get; set; }
        float? StdAcousticness { get; set; }
        float? StdInstrumentalness { get; set; }
        float? StdLiveness { get; set; }
        float? StdValence { get; set; }
        float? StdTempo { get; set; }
        int? StdDuration { get; set; }
        int? Total { get; set; }

    }
}