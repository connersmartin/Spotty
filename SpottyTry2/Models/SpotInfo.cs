using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SpottyTry2.Models
{
    public class SpotInfo
    {
        /*
         * paramDict.Add("client_id", "fa659689165644618ef6368f3d2927b2");
            paramDict.Add("client_secret", "d13f94fafb0c468f98a30961a5c5b468");
            paramDict.Add("grant_type", "authorization_code");
            paramDict.Add("redirect_uri", "http://localhost:21722/Home/TokenGrabber");
         */
        private string ClientID { get;set;}
        private string ClientSecret { get; set; }
        private string GrantType { get; set; }
        private string RedirectURI { get; set; }


    }
}