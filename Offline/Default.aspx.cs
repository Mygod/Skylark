using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Default : Page
    {
        protected string Path;

        protected void Page_Load(object sender, EventArgs e)
        {
            Path = Server.UrlDecode(Request.QueryString["Path"]);
        }

        protected void Submit(object sender, EventArgs e)
        {
            foreach (var link in LinkBox.Text.Split(new[] { '\r', '\n' }).Where(link => !string.IsNullOrWhiteSpace(link)))
                Server.NewOfflineTask(link, Path);
            Response.Redirect("/?/" + Path);
        }

        private static readonly Regex MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);
        protected void MediaFire(object sender, EventArgs e)
        {
            Server.NewOfflineTask(MediaFireDirectLinkExtractor.Match(new WebClient().DownloadString("http://www.mediafire.com/?"
                + MediaFireBox.Text)).Groups[1].Value, Path);
            Response.Redirect("/?/" + Path);
        }
    }
}