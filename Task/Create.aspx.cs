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
            if (!Request.GetUser().OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            Response.Clear();
            Response.ContentType = "application/xml";
            var result = new XElement("result");
            var path = RouteData.GetRelativePath();
            try
            {
                GeneralTask task;
                switch (RouteData.GetRouteString("Type").ToLowerInvariant())
                {
                    case "offline":
                        OfflineDownloadTask.Create(Rbase64.Decode(Request.QueryString["Url"]), path);
                        break;
                    case "offline-mediafire":
                        OfflineDownloadTask.CreateMediaFire(Request.QueryString["ID"], path);
                        break;
                    case "ftpupload":
                        result.SetAttributeValue("id", (task = new FtpUploadTask(path,
                            Request.QueryString["Files"].Split('|').Select(file => file.UrlDecode()),
                            Request.QueryString["Target"].UrlDecode())));
                        task.Start();
                        break;
                    case "compress":
                        new CompressTask(path,
                                         Request.QueryString["Files"].Split('|').Select(file => file.UrlDecode()), 
                                         Request.QueryString["BaseFolder"].UrlDecode(),
                                         Request.QueryString["CompressionLevel"]).Start();
                        break;
                    case "decompress":
                        result.SetAttributeValue("id", (task = new DecompressTask
                            (path, Request.QueryString["Target"].UrlDecode())).ID);
                        task.Start();
                        break;
                    case "convert":
                        ConvertTask.Create(path, Request.QueryString["Target"].UrlDecode(),
                                           Request.QueryString["Size"].UrlDecode(),
                                           Request.QueryString["VCodec"].UrlDecode(),
                                           Request.QueryString["ACodec"].UrlDecode(),
                                           Request.QueryString["SCodec"].UrlDecode(),
                                           Request.QueryString["Start"].UrlDecode(),
                                           Request.QueryString["End"].UrlDecode());
                        break;
                    case "crossappcopy":
                        result.SetAttributeValue("id", (task = new CrossAppCopyTask(
                            Request.QueryString["Domain"].UrlDecode(), Request.QueryString["Path"].UrlDecode(),
                            path, Request.QueryString["Password"].UrlDecode() ?? Request.GetPassword())));
                        task.Start();
                        break;
                    default:
                        throw new FormatException("无法识别的 Type！");
                }
                result.SetAttributeValue("status", "ok");
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