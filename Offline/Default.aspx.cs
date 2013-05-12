using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Default : Page
    {
        protected void Submit(object sender, EventArgs e)
        {
            var path = Context.GetRelativePath();
            foreach (var link in LinkBox.Text.Split(new[] { '\r', '\n' }).Where(link => !string.IsNullOrWhiteSpace(link)))
                Server.NewOfflineTask(link, path);
            Response.Redirect("/?/" + path);
        }

        private static readonly Regex MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);
        protected void MediaFire(object sender, EventArgs e)
        {
            var path = Context.GetRelativePath();
            Server.NewOfflineTask(MediaFireDirectLinkExtractor.Match(new WebClient().DownloadString("http://www.mediafire.com/?"
                + LinkBox.Text)).Groups[1].Value, path);
            Response.Redirect("/?/" + path);
        }
    }
}