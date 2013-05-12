using System;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Start : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var url = Rbase64.Decode(RouteData.GetRouteString("Rbase64"));
            var relativePath = Context.GetRelativePath();
            if (!string.IsNullOrWhiteSpace(url)) Server.NewOfflineTask(url, relativePath);
            Response.Redirect("/?/" + relativePath);
        }
    }
}