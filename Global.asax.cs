using System;
using System.Web;
using System.Web.Routing;

namespace Mygod.Skylark
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.MapPageRoute("Browse", "Browse", "~/");
            RouteTable.Routes.MapPageRoute("Download", "Download", "~/Download.aspx");
            RouteTable.Routes.MapPageRoute("NiGuan", "NiGuan/{*Rbase64}", "~/Offline/YouTube.aspx");
            RouteTable.Routes.MapPageRoute("Offline", "Offline/{Rbase64}", "~/Offline/Start.aspx", true);
            RouteTable.Routes.MapPageRoute("Upload", "Upload", "~/Upload.aspx");
            RouteTable.Routes.MapPageRoute("View", "View/{*Mime}", "~/View.aspx");
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}