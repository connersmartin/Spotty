using System;
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
        
        public ActionResult Index()
        {
            //now that we pass in the authorization we should be able to get things started
            return View();
        }

        //Assists in authorization
        public async Task<RedirectResult> Spotify()
        {
            //Not running this through API function since we require a specific response on this
            HttpClient rest = new HttpClient();
            try
            {
                // TODO parameterize this to use config values
                HttpResponseMessage response = await rest.GetAsync("https://accounts.spotify.com/authorize?client_id=fa659689165644618ef6368f3d2927b2&scope=playlist-modify-public&response_type=code&redirect_uri=http://localhost:21722/Home/TokenGrabber");

                return Redirect(response.RequestMessage.RequestUri.ToString());

            }
            catch (System.Exception)
            {

                throw;
            }

        }

        //Gets the token from the spotify redirect and auths
        public async Task<ActionResult> TokenGrabber()
        {
            //TODO Parameterize this shit for config
            var paramDict = new Dictionary<string, string>();

            var tokenResponse = HttpContext.Request.QueryString;

            paramDict.Add("client_id", "fa659689165644618ef6368f3d2927b2");
            paramDict.Add("client_secret", "d13f94fafb0c468f98a30961a5c5b468");
            paramDict.Add("grant_type", "authorization_code");
            paramDict.Add("redirect_uri", "http://localhost:21722/Home/TokenGrabber");
            paramDict.Add("code", tokenResponse["code"]);


            var postString = "https://accounts.spotify.com/api/token";

            var auth = await SpotApi(HttpMethod.Post, postString, paramDict);

            SpotAuth r = JsonConvert.DeserializeObject<SpotAuth>(auth.ToString());

            Session["SpotToke"] = r.access_token;

            return RedirectToAction("Index");

        }

        //Gets current users playlists
        //TODO try and cache this
        public async Task<ActionResult> CurrentPlaylist()
        {
            var playlists = new List<SimplePlaylist>();

            string getString = "https://api.spotify.com/v1/me/playlists";

            var playlist = await SpotApi(HttpMethod.Get, getString);
            //gets playlists
            var res = JsonConvert.DeserializeObject<ListResponse>(playlist.ToString());

            //then get individual playlists to a list using a get on each playlist
            foreach (var pList in res.Items)
            {
                var pLength = 0;
                try
                {
                    //get the playlist detail


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
            var res = new SpotList();
            //get tracks to populate the playlist with
            var tracks = await PopulatePlaylist(playCreate);
            
            //create playlist after getting tracks in case there is an error
            var playlist = await CreateNewPlaylist(playCreate);

            var postString = string.Format("https://api.spotify.com/v1/playlists/{0}/tracks", playlist.Id);
            //add the tracks
            foreach (var t in tracks)
            {
                var paramDict = new Dictionary<string,string>()
                {
                    {"uris",t.Uri }
                };
                var url = postString +"?"+ BuildParamString(paramDict);
               
                var response = await SpotApi(HttpMethod.Post, url);
            }
            //grab the finished playlist
           
            //TODO figure out what the hell to do here
            //The idea is to show the tracks
            return RedirectToAction("ViewPlaylist","Home", new { href = playlist.Id });
        }

        //Reusable View playlist function
        public async Task<ActionResult> ViewPlaylist(string href)
        {
            var getPlaylist = await SpotApi(HttpMethod.Get, "https://api.spotify.com/v1/playlists/"+href);

            return View("ViewPlaylist", JsonConvert.DeserializeObject<SpotList>(getPlaylist));
        }

        //returns array of playlist items
        public async Task<PlaylistItems[]> GetPlaylistTracks(string href)
        {
            var getPlaylist = await SpotApi(HttpMethod.Get, href);

            var list = JsonConvert.DeserializeObject<SpotList>(getPlaylist);

            //get the track detail
            return list.Tracks.Items;
        }

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

        //Gets the current user id for playlist creation
        public async Task<ActionResult> CurrentUser()
        {
            //gets the id of the user
            var user = await GetCurrentUser();
            //gets currently available genres
            var g = await GetCurrentGenres();
            
            return PartialView("_AdvTrackFeatures", new AdvTrack() { UserId=user.Id.ToString(), Genres = g});
        }

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

            //run through recommendation

            var b = Ml.Predict(fun, fun);
                

            //return playlist
            return RedirectToAction("ViewPlaylist", new { href = "" });
        }


        //Creates the new playlist
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

        //Gets current user id to make playlist
        public async Task<User> GetCurrentUser()
        {
            var getString = "https://api.spotify.com/v1/me";

            var res = await SpotApi(HttpMethod.Get, getString);

            return JsonConvert.DeserializeObject<User>(res);
        }

        //Gets current genre seeds
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

        //Gets tracks to populate a playlist
        public async Task<List<Track>> PopulatePlaylist(AdvTrack playCreate)
        {
           
            //set a default length
            var totalTime = playCreate.Length <= 0 ? 3600000 : 60000 * playCreate.Length;
            var trackList = new List<Track>();
            var getString = "https://api.spotify.com/v1/recommendations";

            var paramDict = await BuildRecParamDict(playCreate);
                        
            //get recommended tracks based on seeds
            var res = await SpotApi(HttpMethod.Get, getString, paramDict);

            var list = JsonConvert.DeserializeObject<SeedResult>(res).Tracks.ToList();

            foreach (var l in list)
            {
                if (totalTime>0)
                {
                    trackList.Add(l);
                }
                totalTime -= l.Duration_Ms;
            }
            return trackList;
        }

        //Get an artist id based on text search
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

        //Param Dictionary Builder
        public async Task<Dictionary<string,string>> BuildRecParamDict(AdvTrack a)
        {
            var paramDict = new Dictionary<string, string>(){{"limit", "100" }};
            if (a.Genre != null){paramDict.Add("seed_genres", a.Genre);}
            if (a.Artist != null){ paramDict.Add("seed_artists", await GetSingleArtist(a.Artist)); }
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
            var auth = new KeyValuePair<string, string>("Authorization", "Bearer " + Session["SpotToke"]);

            try
            {
                HttpClient rest = new HttpClient();

                HttpResponseMessage response = new HttpResponseMessage();

                if (url != "https://accounts.spotify.com/api/token")
                {
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
    }
}
