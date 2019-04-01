using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
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

            var auth = await SpotApi(HttpMethod.Post, postString, new KeyValuePair<string, string>(), paramDict);

            SpotAuth r = JsonConvert.DeserializeObject<SpotAuth>(auth.ToString());

            Session["SpotToke"] = r.access_token;

            return RedirectToAction("Index");

        }

        //Gets current users playlists
        public async Task<ActionResult> CurrentPlaylist()
        {
            var playlists = new List<SimplePlaylist>();
            var auth = new KeyValuePair<string, string>("Authorization", "Bearer " + Session["SpotToke"]);

            string getString = "https://api.spotify.com/v1/me/playlists";

            var playlist = await SpotApi(HttpMethod.Get, getString, auth);
            //gets playlists
            var res = JsonConvert.DeserializeObject<ListResponse>(playlist.ToString());

            //then get individual playlists to a list using a get on each playlist
            foreach (var pList in res.Items)
            {
                var pLength = 0;
                try
                {
                    //get the playlist detail
                    var getPlaylist = await SpotApi(HttpMethod.Get, pList.Href, auth);
                    
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
            //then add up tracks for length using a get on each track



            return PartialView("_CurrentPlaylists", playlists);
   
        }

        [HttpPost]
        //Creates a new playlist
        public async Task<ActionResult> NewPlaylist(PlayCreate playCreate)
        {
            var auth = new KeyValuePair<string, string>("Authorization", "Bearer " + Session["SpotToke"]);
            var paramDict = new Dictionary<string, string>()
            {
                { "name", playCreate.Name }
            };
            string postString = string.Format("https://api.spotify.com/v1/users/{0}/playlists", playCreate.UserId);
            var playlist = await SpotApi(HttpMethod.Post, postString, auth, paramDict, true);
            //creates new playlist
            var res = JsonConvert.DeserializeObject<SpotList>(playlist);

            //TODO populate playlist
            //Like previous spotify app
            //Based on genre BPM, etc

            //TODO figure out what the hell to do here
            //The idea is to show the tracks
            return PartialView("_Success", res);
        }
        [HttpGet]
        public async Task<ActionResult> CurrentUser()
        {
            var auth = new KeyValuePair<string, string>("Authorization", "Bearer " + Session["SpotToke"]);
            var getString = "https://api.spotify.com/v1/me";

            var res = await SpotApi(HttpMethod.Get, getString, auth);

            var user = JsonConvert.DeserializeObject<User>(res);

            return PartialView("_NewPlaylist", new PlayCreate() { UserId=user.Id });
        }

        //General API caller
        public async Task<string> SpotApi(HttpMethod httpMethod, string url, KeyValuePair<string, string> auth, Dictionary<string, string> param = null, bool json = false)
        {
            try
            {
                HttpClient rest = new HttpClient();

                HttpResponseMessage response = new HttpResponseMessage();

                if (auth.Key != null)
                {
                    rest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(auth.Key, auth.Value);
                }

                if (httpMethod == HttpMethod.Post)
                {
                    if (!json)
                    {
                        var req = new HttpRequestMessage(httpMethod, url) { Content = new FormUrlEncodedContent(param) };
                        response = await rest.SendAsync(req);
                    }
                    else
                    {
                        var j = JsonConvert.SerializeObject(param);
                        var data = new StringContent(j, System.Text.Encoding.UTF8, "application/json");
                        var req = new HttpRequestMessage(httpMethod, url) { Content = data };
                        response = await rest.SendAsync(req);
                    }
                }
                else if (httpMethod == HttpMethod.Get)
                {
                    response = await rest.GetAsync(url);
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
