using System;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Forbidden : Page
    {
        protected string Head;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = int.Parse(Request.QueryString["Code"] ?? "403");
            Title = Head = Response.StatusCode + " - 咣！";
        }
    }
}