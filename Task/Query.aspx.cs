using System;
using System.Web.UI;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class Query : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/xml";
            var result = new XElement("result");
            try
            {
                var root = XHelper.Load(FileHelper.GetDataPath(RouteData.GetRouteString("ID") + '.' + RouteData.GetRouteString("Type")
                    + ".task")).Root;
                if (root.AttributeCaseInsensitive("pid") != null)
                    root.SetAttributeValue("running", !TaskHelper.IsBackgroundRunnerKilled(root.GetAttributeValue<int>("pid")));
                result.Add(root);
            }
            catch (Exception exc)
            {
                result.SetAttributeValue("status", "error");
                result.SetAttributeValue("message", exc.GetMessage());
            }
            Response.Write(result.ToString());
        }
    }
}