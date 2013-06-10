using System;
using System.Net;
using System.Web.UI;
using Mygod.Net;

namespace Mygod.Skylark.Offline
{
    public partial class YouTube : Page
    {
        protected void GetEmAll()
        {
            string url = Server.UrlDecode(Request.QueryString["Url"]), path = RouteData.GetRelativePath();
            if (string.IsNullOrWhiteSpace(url)) return;
            foreach (var video in Net.YouTube.Video.GetVideoFromLink(Client, LinkConverter.Decode(Rbase64.Decode(url))))
            {
                Response.Write(string.Format("<h3><a href=\"{1}\">{0}</a></h3>{2}", video.Title, video.Url, Environment.NewLine));
                foreach (var link in video.FmtStreamMap)
                {
                    Response.Write(string.Format("<div><a href=\"/Offline/Start/{2}?Url={0}\" target=\"_blank\">{1}</a></div>{3}",
                        Rbase64.Encode(link.GetUrl(link.Parent.Title)), link, string.IsNullOrEmpty(path) ? string.Empty : (path + '/'), 
                        Environment.NewLine));
                    Response.Flush();
                }
            }
        }

        public static readonly WebClient Client = new WebClient();
    }
}