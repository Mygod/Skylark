using System;
using System.Web;
using System.Web.Routing;

namespace Mygod.Skylark
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.MapPageRoute("Admin", "Admin/{*Path}", "~/Admin.aspx", false);
            RouteTable.Routes.MapPageRoute("Browse", "Browse/{*Path}", "~/Browse.aspx", false);
            RouteTable.Routes.MapPageRoute("Download", "Download/{*Path}", "~/Download.aspx", false);
            RouteTable.Routes.MapPageRoute("Edit", "Edit/{*Path}", "~/Edit.aspx", false);
            RouteTable.Routes.MapPageRoute("OfflineNew", "Offline/New/{*Path}", "~/Offline/New.aspx", false);
            RouteTable.Routes.MapPageRoute("OfflineNiGuan", "Offline/NiGuan/{*Path}", "~/Offline/YouTube.aspx", false);
            RouteTable.Routes.MapPageRoute("OfflineStart", "Offline/Start/{*Path}", "~/Offline/Start.aspx", false);
            RouteTable.Routes.MapPageRoute("Upload", "Upload/{*Path}", "~/Upload.aspx", false);
            RouteTable.Routes.MapPageRoute("View", "View/{*Path}", "~/View.aspx", false);

            RouteTable.Routes.MapPageRoute("Api", "Api/{Action}/{*Path}", "~/Api.aspx", false);

            RouteTable.Routes.MapPageRoute("TaskCreate", "Task/Create/{Type}/{*Path}", "~/Task/Create.aspx", false);
            RouteTable.Routes.MapPageRoute("TaskDetails", "Task/Details/{ID}", "~/Task/Details.aspx", false);
            RouteTable.Routes.MapPageRoute("TaskLog", "Task/Log/", "~/Task/Log.aspx", false);
            RouteTable.Routes.MapPageRoute("TasksQuery", "Task/Query", "~/Task/Query.aspx", false);
            RouteTable.Routes.MapPageRoute("TaskQuery", "Task/Query/{ID}", "~/Task/Query.aspx", false);
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