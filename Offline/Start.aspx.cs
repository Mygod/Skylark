using System;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class Start : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            var url = Rbase64.Decode(Request.QueryString["Url"].UrlDecode());
            var relativePath = RouteData.GetRelativePath();
            if (!string.IsNullOrWhiteSpace(url)) OfflineDownloadTask.Create(url, relativePath);
            Response.Write(relativePath);
            return;
            if ("True".Equals(Request.QueryString["Redirect"], StringComparison.InvariantCultureIgnoreCase))
                Response.Redirect("/Browse/" + relativePath + '/');
            else Response.Write("<script>window.opener=null;window.close();</script>");
        }
    }
}