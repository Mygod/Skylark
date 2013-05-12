using System;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Start : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var url = Rbase64.Decode(Server.UrlDecode(Request.QueryString["Url"]));
            var relativePath = Server.UrlDecode(Request.QueryString["Path"]);
            if (!string.IsNullOrWhiteSpace(url)) Server.NewOfflineTask(url, relativePath);
            Response.Redirect("/?/" + relativePath);
        }
    }
}