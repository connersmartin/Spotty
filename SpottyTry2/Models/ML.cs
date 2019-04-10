using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Data.DataView;
using Microsoft.ML.Data;
using Microsoft.ML;

namespace SpottyTry2.Models
{
    public class Ml
    {
        public class TrackDetail
        {
            /*public bool Advanced { get; set; }
            public string Id { get; set; }
            public string Analysis_url { get; set; }
            public string Track_href { get; set; }
            public string Type { get; set; }
            public int? Key { get; set; }
            public float? Liveness { get; set; }
            public float? Loudness { get; set; }
            public int? Mode { get; set; }
            public float? Speechiness { get; set; }
            public float? Tempo { get; set; }
            public int? Time_signature { get; set; }
            public int? Duration_ms { get; set; }
            public string Uri { get; set; }*/
            public float Acousticness { get; set; }
            public float Danceability { get; set; }
            public float Energy { get; set; }
            public float Instrumentalness { get; set; }
            public float Valence { get; set; }
            public float Label { get; set; }
        }

        public class TrackPredict
        {
            public float PredictedLabel { get; set; }
        }

        public static List<AdvTrack> Predict(List<AdvTrack> goodTracks, List<AdvTrack> recTracks)
        {
            List<TrackDetail> td = new List<TrackDetail>();
            foreach (var g in goodTracks)
            {
                td.Add(new TrackDetail()
                {
                    Acousticness = g.Acousticness,
                    Danceability = g.Danceability,
                    Energy = g.Energy,
                    Instrumentalness = g.Instrumentalness,
                    Label = 1
                });
            }
            try
            {

                MLContext mlContext = new MLContext();

                IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(td);

                var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
                    .Append(mlContext.Transforms.Concatenate("Prediction","Acousticness", "Danceability", "Energy", "Instrumentalness",
                        "Valence"))
                    .AppendCacheCheckpoint(mlContext)
                    .Append(mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(
                        labelColumnName: "Label", featureColumnName: "Prediction"))
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                var model = pipeline.Fit(trainingDataView);
                foreach (var track in recTracks)
                {
                    var prediction = mlContext.Model.CreatePredictionEngine<TrackDetail, TrackPredict>(model).Predict(
                        new TrackDetail()
                        {
                            Acousticness = track.Acousticness,
                            Danceability = track.Danceability,
                            Energy = track.Energy,
                            Instrumentalness = track.Instrumentalness
                        });
                    if (prediction.PredictedLabel < .5)
                    {
                        recTracks.Remove(track);
                    }
                    
                }

                return recTracks;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}