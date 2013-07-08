using System;
using System.Linq;
using System.Web.UI;
using System.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class Create : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/xml";
            var result = new XElement("result");
            var path = RouteData.GetRelativePath();
            try
            {
                switch (RouteData.GetRouteString("Type").ToLower())
                {
                    case "offline":
                        TaskHelper.CreateOffline(Rbase64.Decode(Request.QueryString["Url"]), path);
                        break;
                    case "offline-mediafire":
                        TaskHelper.CreateOfflineMediaFire(Request.QueryString["ID"], path);
                        break;
                    case "compress":
                        TaskHelper.CreateCompress(path, Request.QueryString["Files"].Split('|').Select(file => file.UrlDecode()), 
                                                  Request.QueryString["BaseFolder"].UrlDecode(), Request.QueryString["CompressionLevel"]);
                        break;
                    case "decompress":
                        result.SetAttributeValue("id", TaskHelper.CreateDecompress(path, Request.QueryString["Target"].UrlDecode()));
                        break;
                    default:
                        throw new FormatException("无法识别的 Type！");
                }
                result.SetAttributeValue("status", "ok");
            }
            catch (Exception exc)
            {
                result.SetAttributeValue("status", "error");
                result.SetAttributeValue("message", exc.Message);
            }
            Response.Write(result.ToString());
        }
    }
}