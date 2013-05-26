using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class New : Page
    {
        protected string Path;

        protected void Page_Load(object sender, EventArgs e)
        {
            Path = RouteData.GetRelativePath();
        }

        protected void Submit(object sender, EventArgs e)
        {
            foreach (var link in LinkBox.Text.Split(new[] { '\r', '\n' }).Where(link => !string.IsNullOrWhiteSpace(link)))
                Server.NewOfflineTask(link, Path);
            Response.Redirect("/Browse/" + Path + '/');
        }

        private static readonly Regex MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);
        protected void MediaFire(object sender, EventArgs e)
        {
            Server.NewOfflineTask(MediaFireDirectLinkExtractor.Match(new WebClient().DownloadString("http://www.mediafire.com/?"
                + MediaFireBox.Text)).Groups[1].Value, Path);
            Response.Redirect("/Browse/" + Path + '/');
        }
    }
}