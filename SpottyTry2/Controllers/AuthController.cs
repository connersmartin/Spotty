using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class AuthController : Controller
    {
        // GET: Auth
        //Assists in authorization
        public async Task<RedirectResult> Spotify()
        {
            //Not running this through API function since we require a specific response on this
            //Need to better parameterize this
            HttpClient rest = new HttpClient();
            try
            {
                var clientId = ConfigurationManager.AppSettings["spotClientId"];
                var localHostUri = ConfigurationManager.AppSettings["localHost"]+"/Auth/TokenGrabber";
                var getString = string.Format("https://accounts.spotify.com/authorize?client_id={0}&scope=playlist-modify-public%20playlist-modify-private%20playlist-read-private&response_type=code&redirect_uri={1}",clientId,localHostUri);
                // TODO parameterize this to use config values
                HttpResponseMessage response = await rest.GetAsync(getString);

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
            var spot = new HomeController();
            //TODO Parameterize this shit for config
            var paramDict = new Dictionary<string, string>();

            var tokenResponse = HttpContext.Request.QueryString;

            paramDict.Add("client_id", ConfigurationManager.AppSettings["spotClientId"]);
            paramDict.Add("client_secret", ConfigurationManager.AppSettings["spotClientSecret"]);
            paramDict.Add("grant_type", "authorization_code");
            paramDict.Add("redirect_uri", ConfigurationManager.AppSettings["localHost"]+"/Auth/TokenGrabber");
            paramDict.Add("code", tokenResponse["code"]);

            var postString = "https://accounts.spotify.com/api/token";

            var auth = await spot.SpotApi(HttpMethod.Post, postString, paramDict);

            SpotAuth r = JsonConvert.DeserializeObject<SpotAuth>(auth.ToString());

            Session["SpotToke"] = r.access_token;

            return RedirectToAction("Index", "Home", null);

        }
    }
}