using System;
using System.Linq;
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
                TaskHelper.CreateOffline(link, Path);
            Response.Redirect("/Browse/" + Path + '/');
        }

        protected void MediaFire(object sender, EventArgs e)
        {
            TaskHelper.CreateOfflineMediaFire(MediaFireBox.Text, Path);
            Response.Redirect("/Browse/" + Path + '/');
        }
    }
}