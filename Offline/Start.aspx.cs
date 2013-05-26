using System;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Start : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var url = Rbase64.Decode(Server.UrlDecode(Request.QueryString["Url"]));
            var relativePath = RouteData.GetRelativePath();
            if (!string.IsNullOrWhiteSpace(url)) Server.NewOfflineTask(url, relativePath);
            if ("True".Equals(Request.QueryString["Redirect"], StringComparison.InvariantCultureIgnoreCase))
                Response.Redirect("/Browse/" + relativePath + '/');
            else Response.Write("<script>window.opener=null;window.close();</script>");
        }
    }
}