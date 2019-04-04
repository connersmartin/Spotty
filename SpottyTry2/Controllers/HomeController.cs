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
                    var getPlaylist = await SpotApi(HttpMethod.Get, pList.Href);
                    
                    var list = JsonConvert.DeserializeObject<SpotList>(getPlaylist);

                    //get the track detail
                    var getTracks = list.Tracks.Items;

                    foreach (var t in getTracks)
                    {
                        pLength += t.Track.Duration_Ms;
                    }
                    playlists.Add(new SimplePlaylist
                    {
                        Name = pList.Name,
                        Count = pList.Tracks.Total,
                        Length = pLength / 60000
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

        [HttpPost]
        //Creates a new playlist and populates it
        public async Task<ActionResult> NewPlaylist(PlayCreate playCreate)
        {
            var playlist = await CreateNewPlaylist(playCreate);

            //TODO populate playlist
            //Like previous spotify app
            //Based on genre BPM, etc

            //https://api.spotify.com/v1/recommendations/available-genre-seeds
            //https://api.spotify.com/v1/recommendations
            //using artists/genre seeds

            var res = new SpotList();

            var tracks = await PopulatePlaylist(playCreate.Genre, 0, 60);

            var postString = string.Format("https://api.spotify.com/v1/playlists/{0}/tracks", playlist.Id);

            foreach (var t in tracks)
            {
                var paramDict = new Dictionary<string,string>()
                {
                    {"uris",t.Uri }
                };
                var url = postString +"?"+ BuildParamString(paramDict);
               
                var response = await SpotApi(HttpMethod.Post, url);
            }

            var getPlaylist = await SpotApi(HttpMethod.Get, playlist.Href);

            res = JsonConvert.DeserializeObject<SpotList>(getPlaylist);
            //TODO figure out what the hell to do here
            //The idea is to show the tracks
            return PartialView("_Success", res);
        }

        [HttpGet]
        public async Task<ActionResult> CurrentUser()
        {
            var user = await GetCurrentUser();

            //var genres = await GetCurrentGenres();

            var g = new List<string>();
                
            //g = genres.ToList();
            
            return PartialView("_NewPlaylist", new PlayCreate() { UserId=user.Id.ToString(), Genres = g});
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
        public async Task<string[]> GetCurrentGenres()
        {
            var getString = "https://api.spotify.com/v1/recommendations/available-genre-seeds";

            var res = await SpotApi(HttpMethod.Get, getString);

            return JsonConvert.DeserializeObject<string[]>(res);
        }

        //Gets tracks to populate a playlist
        public async Task<List<Track>> PopulatePlaylist(string seeds, int bpm, int length)
        {
            var trackList = new List<Track>();
            var getString = "https://api.spotify.com/v1/recommendations";

            var paramDict = new Dictionary<string,string>()
            {
                {"limit","50" }
            };

            paramDict.Add("seed_genres",seeds);


            var res = await SpotApi(HttpMethod.Get, getString, paramDict);

            var list = JsonConvert.DeserializeObject<SeedResult>(res).Tracks.ToList();

            var totalTime = 1000 * 60 * length;

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
