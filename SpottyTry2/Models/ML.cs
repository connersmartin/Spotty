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
            public float? Acousticness { get; set; }
            public float? Danceability { get; set; }
            public float? Energy { get; set; }
            public float? Instrumentalness { get; set; }
            public float? Valence { get; set; }
            public bool Liked { get; set; }
        }

        public class TrackPredict
        {
            public bool Prediction { get; set; }
        }

        public static List<AdvTrack> Predict(List<AdvTrack> goodTracks, List<AdvTrack> recTracks)
        {
            try
            {

                MLContext mlContext = new MLContext();

                IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(goodTracks);

                var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Liked")
                    .Append(mlContext.Transforms.Concatenate("Acousticness", "Danceability", "Energy", "Instrumentalness",
                        "Valence"))
                    .AppendCacheCheckpoint(mlContext)
                    .Append(mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(
                        labelColumnName: "Liked", featureColumnName: "Features"))
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("Prediction"));

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
                    if (!prediction.Prediction)
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