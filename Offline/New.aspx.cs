using System;
using System.Linq;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class New : Page
    {
        private string path;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            path = RouteData.GetRelativePath();
        }

        protected void Submit(object sender, EventArgs e)
        {
            foreach (var link in LinkBox.Text.Split(new[] { '\r', '\n' })
                .Where(link => !string.IsNullOrWhiteSpace(link))) OfflineDownloadTask.Create(link, path);
            Response.Redirect("/Browse/" + path + '/');
        }
    }
}