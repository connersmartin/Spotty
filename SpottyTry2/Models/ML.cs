using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Data.DataView;
using Microsoft.ML.Data;
using Microsoft.ML;
using Microsoft.ML.Transforms;

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
            /*public float Danceability { get; set; }
            public float Energy { get; set; }
            public float Instrumentalness { get; set; }*/
            //[ColumnName("Label")]
            public string Label { get; set; }
            public float Liked { get; set; }
        }

        public class TrackPredict
        {
            public float Acousticness { get; set; }
            public float Liked { get; set; }
        }

        public static List<AdvTrack> Predict(List<AdvTrack> goodTracks, List<AdvTrack> recTracks)
        {
            List<TrackDetail> td = new List<TrackDetail>();
            foreach (var g in goodTracks)
            {
                td.Add(new TrackDetail()
                {
                    Acousticness = g.Acousticness
                    /*Danceability = g.Danceability,
                    Energy = g.Energy,
                    Instrumentalness = g.Instrumentalness,*/
                });
            }

            IEnumerable<TrackDetail> trackDetails = td;
            try
            {

                MLContext mlContext = new MLContext();

                IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(trackDetails);

                /*var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
                    .Append(mlContext.Transforms.Concatenate("Acousticness", "Danceability", "Energy", "Instrumentalness"))
                    .AppendCacheCheckpoint(mlContext)
                    .Append(mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(
                        labelColumnName: "Label", featureColumnName: "Stats"))
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));*/

                 var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("CategoricalOneHot", "Liked")
                     /*.Append(mlContext.Transforms.Categorical.OneHotEncoding("Acousticness", "Acousticness"))
                     .Append(mlContext.Transforms.Categorical.OneHotEncoding("Danceability", "Danceability"))
                     .Append(mlContext.Transforms.Categorical.OneHotEncoding("Energy", "Energy"))
                     .Append(mlContext.Transforms.Categorical.OneHotEncoding("Instrumentalness", "Instrumentalness"))*/
                     ;

                var model = pipeline.Fit(trainingDataView);

                //var s = trainingDataView.Preview();

                foreach (var track in recTracks)
                {
                    var prediction = mlContext.Model.CreatePredictionEngine<TrackDetail, TrackPredict>(model).Predict(
                        new TrackDetail()
                        {
                            Acousticness = track.Acousticness
                            /*,
                            Danceability = track.Danceability,
                            Energy = track.Energy,
                            Instrumentalness = track.Instrumentalness*/
                        });
                    if (prediction.Liked > 0)
                    {
                        track.Valence = 1;
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