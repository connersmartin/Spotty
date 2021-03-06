﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Permissions;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using SpottyTry2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SpottyTry2.Controllers
{
    public class HomeController : Controller
    {
        //Actual page stuff
        #region Actions
        public ActionResult Index()
        {
            //now that we pass in the authorization we should be able to get things started
            return View();
        }

        //Gets current users playlists
        //stretch goal pagination
        public async Task<ActionResult> CurrentPlaylist()
        {

            var playlists = new List<SimplePlaylist>();
            string getString = "https://api.spotify.com/v1/me/playlists";

            var paramDict = new Dictionary<string, string>()
            {
                {"limit","50" }
            };

            var playlist = await SpotApi(HttpMethod.Get, getString,paramDict);
            //gets playlists
            var res = JsonConvert.DeserializeObject<ListResponse>(playlist.ToString());

            //then get individual playlists to a list using a get on each playlist
            foreach (var pList in res.Items)
            {
                var pLength = 0;
                try
                {
                    //get the playlist detail just for length
                    var getTracks = await GetPlaylistTracks(pList.Href);

                    foreach (var t in getTracks)
                    {
                        pLength += t.Track.Duration_Ms;
                    }

                    playlists.Add(new SimplePlaylist
                    {
                        Name = pList.Name,
                        Count = pList.Tracks.Total,
                        Length = pLength / 60000,
                        Id = pList.Id
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return PartialView("_CurrentPlaylists", playlists);
   
        }

        //Creates a new playlist and populates it
        public async Task<ActionResult> NewPlaylist(AdvTrack playCreate)
        {

            //TODO populate playlist
            //Like previous spotify app
            //Based on genre BPM, etc

            //https://api.spotify.com/v1/recommendations/available-genre-seeds
            //https://api.spotify.com/v1/recommendations
            //using artists/genre seeds
            //get tracks to populate the playlist with
            var tracks = await PopulatePlaylist(playCreate);
            
            //create playlist after getting tracks in case there is an error
            var playlist = await CreateNewPlaylist(playCreate);

            var p = await AddTracksToPlaylist(tracks, playlist.Id);

            return RedirectToAction("ViewPlaylist","Home", new { href = playlist.Id });
        }

       //Returns the Audio Feature info for a given Playlist/Album
       public async Task<ActionResult> ViewFeatures(string href)
        {
            var adv = await GetAdvAudioFeatures(href);

            return View("ViewFeatures", adv );
        }

        //Playlist creation from Audio Features
        public async Task<ActionResult> NewAdvPlaylist(AdvTrackCalc adv)
        {
            var g = adv.GenreList.ToArray();

            var user = await GetCurrentUser();
            var advTrack = new AdvTrack()
            {
                Acousticness = adv.AvgAcousticness,
                Danceability = adv.AvgDance,
                Energy = adv.AvgEnergy,
                Instrumentalness = adv.AvgInstrumentalness,
                Liveness = adv.AvgLiveness,
                Loudness = adv.AvgLoudness,
                Speechiness = adv.AvgSpeechiness,
                Tempo = adv.AvgTempo,
                Valence = adv.AvgValence,
                UserId=user.Id,
                GenreList = adv.GenreList,
                Genre = string.Join(",",g)
                
            };
            return PartialView("_AdvTrackFeatures", advTrack);
        }

        //Gets some info for normal playlist creation and caches it
        public async Task<ActionResult> PlayCreate()
        {
            //gets the id of the user
            const string genreCache = "Genre";

            List<SelectListItem> genre = CacheLayer.Get<List<SelectListItem>>(genreCache);
            var  user = await GetCurrentUser();
            
            //gets currently available genres
            if (genre == null)
            {
                genre = await GetCurrentGenres();
                CacheLayer.Add(genre, genreCache);
            }

            return PartialView("_AdvTrackFeatures", new AdvTrack() { UserId = user.Id.ToString(), Genres = genre });
        }

        //Reusable View playlist function
        public async Task<ActionResult> ViewPlaylist(string href)
        {

            var getPlaylist = await SpotApi(HttpMethod.Get, "https://api.spotify.com/v1/playlists/"+href);

            var m = JsonConvert.DeserializeObject<SpotList>(getPlaylist);

            return View("ViewPlaylist",m );
        }
        #endregion

        //Logic for the playlist creation
        #region Work
        /// <summary>
        /// GETS The Info needed for all the tracks in a given playlist
        /// </summary>
        /// <param name="href">The full href of the playlist</param>
        /// <returns>Array of PlaylistItems</returns>
        public async Task<PlaylistItems[]> GetPlaylistTracks(string href)
        {
            var getPlaylist = await SpotApi(HttpMethod.Get, href);

            var list = JsonConvert.DeserializeObject<SpotList>(getPlaylist);

            //get the track detail
            return list.Tracks.Items;
        }
        
        /// <summary>
        /// GETS the advanced audio features of tracks
        /// </summary>
        /// <param name="trackIds">Array of IDs from tracks</param>
        /// <returns>List of advanced track features</returns>
        public async Task<List<AdvTrack>> GetAudioFeatures(string[] trackIds)
        {
            var t = string.Join(",", trackIds);
            var res = await SpotApi(HttpMethod.Get, "https://api.spotify.com/v1/audio-features/",
                new Dictionary<string, string>()
                {
                    {"ids", t}
                });

            var conv = JsonConvert.DeserializeObject<Dictionary<string,AdvTrack[]>>(res).Values.FirstOrDefault();

        
            return conv.ToList() ;
        }
 
        /// <summary>
        /// POSTS a new playlist
        /// </summary>
        /// <param name="playCreate">Info from view to create the playlist</param>
        /// <returns>Playlist object</returns>
        public async Task<SpotList> CreateNewPlaylist(PlayCreate playCreate)
        {
            var paramDict = new Dictionary<string, string>()
            {
                { "name", playCreate.Name }
            };
            string postString = string.Format("https://api.spotify.com/v1/users/{0}/playlists", playCreate.UserId);
            var playlist = await SpotApi(HttpMethod.Post, postString, paramDict, true);
            //creates new playlist
            return JsonConvert.DeserializeObject<SpotList>(playlist);
        }

        /// <summary>
        /// GETS tracks recommended by spotify based on the supplied criteria
        /// </summary>
        /// <param name="playCreate">Playlist info</param>
        /// <returns>List of track objects</returns>
        public async Task<List<Track>> PopulatePlaylist(AdvTrack playCreate, int limit = 0)
        {
           
            //set a default length
            var totalTime = playCreate.Length <= 0 ? 3600000 : 60000 * playCreate.Length;
            var trackList = new List<Track>();
            var getString = "https://api.spotify.com/v1/recommendations";

            //need to replace selected genres with closest match.
            //LoadAndReplaceGenres(playCreate.Genre)

            var genreString = "https://api.spotify.com/v1/recommendations/available-genre-seeds";

            var response = await SpotApi(HttpMethod.Get, genreString);

            var j = JsonConvert.DeserializeObject<GenreList>(response).Genres.ToList<string>();
            var splitGenre = playCreate.Genre.Replace(" ","-").Split(',').ToList<string>();
            var genreList = "";
            foreach (var genre in j.ToList<string>())
            {
                foreach (var g in splitGenre.ToList<string>())
                {
                    if (g==genre)
                    {
                        genreList += g + ",";
                    }
                }
            }

            if (genreList == "")
            {
                foreach (var genre in j)
                {
                    foreach (var g in splitGenre)
                    {
                        if (g.Contains(genre) && !genreList.Contains(genre))
                        {
                            genreList += genre + ",";
                        }
                    }
                }
            }

            playCreate.Genre = genreList;

            var paramDict = await BuildRecParamDict(playCreate);
                        
            //get recommended tracks based on seeds
            var res = await SpotApi(HttpMethod.Get, getString, paramDict);

            var list = JsonConvert.DeserializeObject<SeedResult>(res).Tracks.ToList();
            if (limit == 0)
            {
                foreach (var l in list)
                {
                    if (totalTime>0)
                    {
                        trackList.Add(l);
                    }
                    totalTime -= l.Duration_Ms;
                }
            }
            else
            {
                foreach (var l in list)
                {
                    if (limit>0)
                    {
                        trackList.Add(l);
                    }
                    limit--;
                }
            }
            return trackList;
        }

        /// <summary>
        /// GETS a single artist based on search params
        /// </summary>
        /// <param name="artistSeed">Name of an artist (exact)</param>
        /// <returns>Artist object's id</returns>
        public async Task<string> GetSingleArtist(string artistSeed)
        {
            var artDict = new Dictionary<string, string>()
                {
                    { "q","\""+artistSeed+"\"" },
                    {"type","artist" },
                    {"limit","3" }
                };
            var artString = "https://api.spotify.com/v1/search";

            var response = await SpotApi(HttpMethod.Get, artString, artDict);

            var art = JsonConvert.DeserializeObject<Dictionary<string, ArtistResult>>(response);

            int pop = 0;
            var chosen = new ArtistFull();

            foreach (var a in art.Values.FirstOrDefault().Items)
            {
                if (a.Popularity > pop)
                {
                    pop = a.Popularity;
                    chosen = a;
                }
            }

            return chosen.Id;
        }
        /// <summary>
        /// Gets a full artist profile based on their id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ArtistFull> GetArtistFull(string id)
        {
            var getString = "https://api.spotify.com/v1/artists/"+id;
            var response = await SpotApi(HttpMethod.Get, getString);
            var art = JsonConvert.DeserializeObject<ArtistFull>(response);

            return art;
        }

        /// <summary>
        /// POSTS tracks to a given playlist
        /// </summary>
        /// <param name="tracks">Track objects</param>
        /// <param name="id">Playlist ID</param>
        private async Task<string> AddTracksToPlaylist(List<Track> tracks, string id)
        {
            var postString = string.Format("https://api.spotify.com/v1/playlists/{0}/tracks", id);
            //add the tracks
            foreach (var t in tracks)
            {
                var paramDict = new Dictionary<string, string>()
                {
                    {"uris",t.Uri }
                };
                var url = postString + "?" + BuildParamString(paramDict);

                var response = await SpotApi(HttpMethod.Post, url);
            }

            return postString;
        }

        /// <summary>
        /// GETS the current available genre list (cached)
        /// </summary>
        /// <returns>List of SelectListItems to populate drop own menu</returns>
        public async Task<List<SelectListItem>> GetCurrentGenres()
        {
            var getString = "https://api.spotify.com/v1/recommendations/available-genre-seeds";

            var res = await SpotApi(HttpMethod.Get, getString);

            var j = JsonConvert.DeserializeObject<GenreList>(res);

            var g = new List<SelectListItem>(){
                new SelectListItem(){Text="Select a genre", Value="",Selected=true }
            };

            foreach (var sl in j.Genres)
            {
                g.Add(new SelectListItem() { Text = sl, Value = sl });
            }

            return g;
        }

        /// <summary>
        /// GETS the current users ID needed for playlist creation
        /// </summary>
        /// <returns>User object</returns>
        public async Task<User> GetCurrentUser()
        {
            var getString = "https://api.spotify.com/v1/me";

            var res = await SpotApi(HttpMethod.Get, getString);

            return JsonConvert.DeserializeObject<User>(res);
        }

        //TODO
        public async Task<ActionResult> GetRecsFromPlaylist(string href)
        {
            var trackRec = new AdvTrack();
            var tempList = new List<Track>();
            var user = await GetCurrentUser();
            var listTracks = await GetPlaylistTracks("https://api.spotify.com/v1/playlists/2Ql3TagKirJQJDG93GqMiE");

            var trackIds = new List<string>();
            //analyze each track in a playlist
            foreach (var t in listTracks)
            {
                trackIds.Add(t.Track.Id);
            }
            var audioFeat = await GetAudioFeatures(trackIds.ToArray());
            //GetAudioFeatures
            //send to recommendationengine
            trackRec = RecommendationEngine(audioFeat);
            //would be running Populateplaylist multiple times
            foreach (var t in listTracks)
            {
                trackRec.Artist = t.Track.Artists.FirstOrDefault().Name;
                //need to think of a better way to do this, want to prevent duplicates somehow
                tempList.AddRange(await PopulatePlaylist(trackRec, 1));
            }
            var pc = new PlayCreate()
            {
                Name = "Test 4",
                UserId = user.Id
            };
            var newList = await CreateNewPlaylist(pc);

            var x = await AddTracksToPlaylist(tempList, newList.Id);
            
            return RedirectToAction("ViewPlaylist", "Home", new { href = newList.Id }); ;
        }

        //TODO Doesn't work with songs same artist
        public AdvTrack RecommendationEngine(List<AdvTrack> advTracks)
        {
            var math = new AdvTrack()
            {
                Instrumentalness = 0,
                Liveness = 0,
                Loudness = 0,
                Energy = 0,
                Danceability = 0,
                Speechiness = 0,
                Valence = 0
            };
            //Figure out how to analyze the tracks
            //would need standard deviation, mean, etc
            //Would pass in advtrack data
            foreach (var adv in advTracks)
            {
                math.Instrumentalness += adv.Instrumentalness;
                math.Liveness += adv.Liveness;
                math.Loudness += adv.Loudness;
                math.Energy += adv.Energy;
                math.Danceability += adv.Danceability;
                math.Speechiness += adv.Speechiness;
                math.Valence += adv.Valence;
            }
            //start with averages
            //then add in some noticeable outliers

            math.Instrumentalness/=advTracks.Count;
            math.Liveness /= advTracks.Count;
            math.Loudness /= advTracks.Count;
            math.Energy /= advTracks.Count;
            math.Danceability /= advTracks.Count;
            math.Speechiness /= advTracks.Count;
            math.Valence /= advTracks.Count;

            return math;
        }
        /// <summary>
        /// Get AdvTrackCalc from a playlist href
        /// </summary>
        /// <param name="href"></param>
        /// <returns></returns>
        public async Task<AdvTrackCalc> GetAdvAudioFeatures(string href)
        {
            var advTrackCalc = new AdvTrackCalc();
            var allArtId = new List<string>();
            var allArt = new List<ArtistFull>();
            var testTrack = new List<AdvTrack>();
            var res = await GetPlaylistTracks("https://api.spotify.com/v1/playlists/" + href);
            var tracks = new List<string>();
            foreach (var t in res)
            {
                tracks.Add(t.Track.Id);
                testTrack.Add(new AdvTrack()
                {
                    Id = t.Track.Id,
                    Artist = t.Track.Artists.FirstOrDefault().Name,
                    Name = t.Track.Name
                });
                allArtId.Add(t.Track.Artists.FirstOrDefault().Id);
            }

            foreach (var id in allArtId)
            {
                allArt.Add(await GetArtistFull(id));
            }

            foreach (var art in allArt)
            {
                foreach (var g in art.Genres)
                {
                    if (!advTrackCalc.GenreCount.ContainsKey(g))
                    {
                        advTrackCalc.GenreCount.Add(g, 0);
                    }
                    if (advTrackCalc.GenreCount.ContainsKey(g))
                    {
                        advTrackCalc.GenreCount[g]++;
                    }
                }
            }

            var genreList = advTrackCalc.GenreCount.ToList();

            genreList.Sort((x, y) => x.Value.CompareTo(y.Value));

            advTrackCalc.GenreCount.Clear();

            var limit = 5;

            if (genreList.Count>limit)
            {
                genreList.RemoveRange(0, genreList.Count - limit);
            }


            foreach (var g in genreList)
            {
                advTrackCalc.GenreCount.Add(g.Key, g.Value);
            }

            //Jesus this is ugly

            var advTracks = await GetAudioFeatures(tracks.ToArray());

            advTrackCalc.Total = advTracks.Count;
            foreach (var track in advTracks)
            {
                foreach (var t in testTrack)
                {
                    if (track.Id == t.Id)
                    {
                        track.Name = t.Name;
                        track.Artist = t.Artist;
                    }
                }
                advTrackCalc.Acousticness.Add(track.Acousticness);
                advTrackCalc.TotAcousticness += track.Acousticness;
                advTrackCalc.Dance.Add(track.Danceability);
                advTrackCalc.TotDance += track.Danceability;
                advTrackCalc.Energy.Add(track.Energy);
                advTrackCalc.TotEnergy += track.Energy;
                advTrackCalc.Key.Add(track.Key);
                advTrackCalc.TotKey += track.Key;
                advTrackCalc.Mode.Add(track.Mode);
                advTrackCalc.TotMode += track.Mode;
                advTrackCalc.Loudness.Add(track.Loudness);
                advTrackCalc.TotLoudness += track.Loudness;
                advTrackCalc.Speechiness.Add(track.Speechiness);
                advTrackCalc.TotSpeechiness += track.Speechiness;
                advTrackCalc.Instrumentalness.Add(track.Instrumentalness);
                advTrackCalc.TotInstrumentalness += track.Instrumentalness;
                advTrackCalc.Liveness.Add(track.Liveness);
                advTrackCalc.TotLiveness += track.Liveness;
                advTrackCalc.Valence.Add(track.Valence);
                advTrackCalc.TotValence += track.Valence;
                advTrackCalc.Duration.Add(track.Duration_ms);
                advTrackCalc.TotDuration += track.Duration_ms;
                advTrackCalc.Tempo.Add(track.Tempo);
                advTrackCalc.TotTempo += track.Tempo;
            }
            advTrackCalc.Tracks = advTracks;

            advTrackCalc.AvgAcousticness = (advTrackCalc.TotAcousticness / advTrackCalc.Total);
            advTrackCalc.AvgDance = (advTrackCalc.TotDance / advTrackCalc.Total);
            advTrackCalc.AvgEnergy = (advTrackCalc.TotEnergy / advTrackCalc.Total);
            advTrackCalc.AvgKey = (advTrackCalc.TotKey / advTrackCalc.Total);
            advTrackCalc.AvgMode = (advTrackCalc.TotMode / advTrackCalc.Total);
            advTrackCalc.AvgLoudness = (advTrackCalc.TotLoudness / advTrackCalc.Total);
            advTrackCalc.AvgSpeechiness = (advTrackCalc.TotSpeechiness / advTrackCalc.Total);
            advTrackCalc.AvgInstrumentalness = (advTrackCalc.TotInstrumentalness / advTrackCalc.Total);
            advTrackCalc.AvgLiveness = (advTrackCalc.TotLiveness / advTrackCalc.Total);
            advTrackCalc.AvgLoudness = (advTrackCalc.TotLoudness / advTrackCalc.Total);
            advTrackCalc.AvgValence = (advTrackCalc.TotValence / advTrackCalc.Total);
            advTrackCalc.AvgDuration = (advTrackCalc.TotDuration / advTrackCalc.Total);
            advTrackCalc.AvgTempo = (advTrackCalc.TotTempo / advTrackCalc.Total);

            advTrackCalc.StdAcousticness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotAcousticness / advTrackCalc.Total, advTrackCalc.Acousticness);
            advTrackCalc.StdDance = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotDance / advTrackCalc.Total, advTrackCalc.Dance);
            advTrackCalc.StdEnergy = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotEnergy / advTrackCalc.Total, advTrackCalc.Energy);
            advTrackCalc.StdKey = (int?)advTrackCalc.GetStdDev(advTrackCalc.TotKey / advTrackCalc.Total, advTrackCalc.Key);
            advTrackCalc.StdMode = (int?)advTrackCalc.GetStdDev(advTrackCalc.TotMode / advTrackCalc.Total, advTrackCalc.Mode);
            advTrackCalc.StdLoudness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotLoudness / advTrackCalc.Total, advTrackCalc.Loudness);
            advTrackCalc.StdSpeechiness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotSpeechiness / advTrackCalc.Total, advTrackCalc.Speechiness);
            advTrackCalc.StdInstrumentalness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotInstrumentalness / advTrackCalc.Total, advTrackCalc.Instrumentalness);
            advTrackCalc.StdLiveness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotLiveness / advTrackCalc.Total, advTrackCalc.Liveness);
            advTrackCalc.StdLoudness = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotLoudness / advTrackCalc.Total, advTrackCalc.Loudness);
            advTrackCalc.StdValence = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotValence / advTrackCalc.Total, advTrackCalc.Valence);
            advTrackCalc.StdDuration = (int?)advTrackCalc.GetStdDev(advTrackCalc.TotDuration / advTrackCalc.Total, advTrackCalc.Duration);
            advTrackCalc.StdTempo = (float?)advTrackCalc.GetStdDev(advTrackCalc.TotTempo / advTrackCalc.Total, advTrackCalc.Tempo);

            return advTrackCalc;
        }

        #endregion

        //Helper functions for Spotify API
        #region Helpers

        //Param Dictionary Builder, could also use max/min instead of target
        public async Task<Dictionary<string,string>> BuildRecParamDict(AdvTrack a)
        {
            var paramDict = new Dictionary<string, string>(){{"limit", "100" }};
            if (a.Genre != null && a.Genre.Trim() !="" ){paramDict.Add("seed_genres", a.Genre);}
            if (a.Artist != null && a.Artist.Trim() != "") { paramDict.Add("seed_artists", await GetSingleArtist(a.Artist)); }
            if (a.Acousticness != null) { paramDict.Add("target_acousticness", a.Acousticness.ToString()); }
            if (a.Danceability != null) { paramDict.Add("target_danceability", a.Danceability.ToString()); }
            if (a.Energy != null) { paramDict.Add("target_energy", a.Energy.ToString()); }
            if (a.Instrumentalness != null) { paramDict.Add("target_instrumentalness", a.Instrumentalness.ToString()); }
            if (a.Key != null) { paramDict.Add("target_key", a.Key.ToString()); }
            if (a.Liveness != null) { paramDict.Add("target_liveness", a.Liveness.ToString()); }
            if (a.Loudness != null) { paramDict.Add("target_loudness", a.Loudness.ToString()); }
            if (a.Mode != null) { paramDict.Add("target_mode", a.Mode.ToString()); }
            if (a.Speechiness != null) { paramDict.Add("target_speechiness", a.Speechiness.ToString()); }
            if (a.Tempo != null) { paramDict.Add("target_tempo", a.Tempo.ToString()); }
            if (a.Time_signature != null) { paramDict.Add("target_time_signature", a.Time_signature.ToString()); }
            if (a.Valence != null) { paramDict.Add("target_valence", a.Valence.ToString()); }

            return paramDict;
        }
        //Param string builder
        public string BuildParamString(Dictionary<string, string> param)
        {
            var builder = new UriBuilder();
            var query = HttpUtility.ParseQueryString(builder.Query);
            foreach (var t in param)
            {
                query[t.Key] = t.Value;
            }
            return query.ToString();
        }

        //Generic API method
        public async Task<string> SpotApi(HttpMethod httpMethod, string url, Dictionary<string, string> param = null, bool json = false)
        {

            try
            {
                HttpClient rest = new HttpClient();

                HttpResponseMessage response = new HttpResponseMessage();

                if (url != "https://accounts.spotify.com/api/token")
                {
                    var auth = new KeyValuePair<string, string>("Authorization", "Bearer " + Session["SpotToke"]);

                    rest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(auth.Key, auth.Value);
                }

                if (httpMethod == HttpMethod.Post)
                {
                    if (!json && param != null)
                    {
                        var req = new HttpRequestMessage(httpMethod, url) { Content = new FormUrlEncodedContent(param) };
                        response = await rest.SendAsync(req);
                    }
                    else if (param !=null)
                    {
                        var j = JsonConvert.SerializeObject(param);
                        var data = new StringContent(j, System.Text.Encoding.UTF8, "application/json");
                        var req = new HttpRequestMessage(httpMethod, url) { Content = data };
                        response = await rest.SendAsync(req);
                    }
                    else
                    {
                        var req = new HttpRequestMessage(httpMethod, url){Content = new StringContent(string.Empty)};
                        response = await rest.SendAsync(req);
                    }
                }
                else if (httpMethod == HttpMethod.Get)
                {
                    if (param == null)
                    {
                        response = await rest.GetAsync(url);
                    }
                    else
                    {
                        var builder = new UriBuilder(url)
                        {
                            Query = BuildParamString(param)
                        };
                        response = await rest.GetAsync(builder.ToString());
                    }
                }
                return response.Content.ReadAsStringAsync().Result;

            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion
        
        
        //Does not work, needs a lot of work
        public async Task<ActionResult> GetMlRecs()
        {
            //get a playlist
            //try March 2019
            var listitemTest = await GetPlaylistTracks("https://api.spotify.com/v1/playlists/5h9qlv28tzvKGS3zuVSFAE");
            //get tracks
            var blah = new string[listitemTest.Length];
            var i = 0;
            foreach (var t in listitemTest)
            {
                blah[i] = t.Track.Id;
                i++;
            }

            var fun = await GetAudioFeatures(blah);

            var listitemTestA = await GetPlaylistTracks("https://api.spotify.com/v1/playlists/0q8gYT2lebXYpRnoqpyGFa");
            //get tracks
            var blahA = new string[listitemTestA.Length];
            var j = 0;
            foreach (var t in listitemTestA)
            {
                blahA[j] = t.Track.Id;
                j++;
            }

            var funA = await GetAudioFeatures(blah);

            //run through recommendation

            var b = Ml.Predict(fun, funA);


            //return playlist
            return RedirectToAction("ViewPlaylist", new { href = "" });
        }


    }
}
