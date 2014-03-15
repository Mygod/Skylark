using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class Query : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Browse)
            {
                Response.StatusCode = 401;
                return;
            }
            Response.Clear();
            Response.ContentType = "application/xml";
            var result = new XElement("result");
            try
            {
                var id = RouteData.GetRouteString("ID");
                if (string.IsNullOrWhiteSpace(id)) result.Add(Directory.EnumerateFiles(Server.MapPath("~/Data"),
                    "*.task").Select(path => new XElement("task", new XAttribute("id",
                        Path.GetFileNameWithoutExtension(path)))));
                else
                {
                    var root = XHelper.Load(FileHelper.GetDataPath(id + ".task")).Root;
                    if (root.AttributeCaseInsensitive("pid") != null) root.SetAttributeValue("running",
                        !CloudTask.IsBackgroundRunnerKilled(root.GetAttributeValue<int>("pid")));
                    result.Add(root);
                }
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