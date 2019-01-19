using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using SpottyTry2.Models;
using Newtonsoft.Json;


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
            HttpClient rest = new HttpClient();
            try
            {
                HttpResponseMessage response = await rest.GetAsync("https://accounts.spotify.com/authorize?client_id=fa659689165644618ef6368f3d2927b2&response_type=code&redirect_uri=http://localhost:21722/Home/TokenGrabber");

                return Redirect(response.RequestMessage.RequestUri.ToString());

            }
            catch (System.Exception)
            {

                throw;
            }

        }

        public async Task<ActionResult> TokenGrabber()
        {
            var paramDict = new Dictionary<string, string>();

            var tokenResponse = HttpContext.Request.QueryString;

            paramDict.Add("client_id", "fa659689165644618ef6368f3d2927b2");
            paramDict.Add("client_secret", "d13f94fafb0c468f98a30961a5c5b468");
            paramDict.Add("grant_type", "authorization_code");
            paramDict.Add("redirect_uri", "http://localhost:21722/Home/TokenGrabber");
            paramDict.Add("code", tokenResponse["code"]);


            string postString = "https://accounts.spotify.com/api/token";

            HttpClient rest = new HttpClient();
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, postString) { Content = new FormUrlEncodedContent(paramDict) };

                HttpResponseMessage response = await rest.SendAsync(req);

                SpotAuth r = JsonConvert.DeserializeObject<SpotAuth>(response.Content.ReadAsStringAsync().Result);

                Session["SpotToke"] = r.access_token;

                return RedirectToAction("Index");
            }
            catch (System.Exception)
            {

                throw;
            }

        }

    }


}
