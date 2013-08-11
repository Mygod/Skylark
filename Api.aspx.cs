using System;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public partial class Api : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/xml";
            var result = new XElement("result");
            var path = RouteData.GetRelativePath();
            try
            {
                switch (RouteData.GetRouteString("Action").ToLower())
                {
                    case "list":
                        List(path, result);
                        break;
                    case "createdirectory":
                        FileHelper.CreateDirectory(path);
                        break;
                    case "copy":
                        FileHelper.Copy(path, Request.QueryString["Target"].UrlDecode());
                        break;
                    case "move":
                        FileHelper.Move(path, Request.QueryString["Target"].UrlDecode());
                        break;
                    case "delete":
                        FileHelper.Delete(path);
                        break;
                    case "details":
                        Details(path, result);
                        break;
                    case "niguan":
                        NiGuan(path, result);
                        Response.Write(result.ToString());
                        return;
                    case "codecs":
                        Codecs(result);
                        break;
                    default:
                        throw new FormatException("无法识别的 Action！");
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

        private static void Codecs(XElement result)
        {
            if (result == null) throw new ArgumentNullException("result");
            foreach (var codec in FFmpeg.Codecs) result.Add(codec.ToElement());
        }

        private void List(string path, XElement result)
        {
            var info = new DirectoryInfo(FileHelper.GetFilePath(path));
            if (!info.Exists) throw new DirectoryNotFoundException();
            result.SetAttributeValue("lastWriteTimeUtc", info.LastWriteTimeUtc);
            foreach (var dir in info.EnumerateDirectories()) result.Add(new XElement("directory", new XAttribute("name", dir.Name),
                new XAttribute("lastWriteTimeUtc", dir.LastWriteTimeUtc)));
            foreach (var file in info.EnumerateFiles()) result.Add(new XElement("file", new XAttribute("name", file.Name), 
                new XAttribute("size", file.Length), new XAttribute("lastWriteTimeUtc", file.LastWriteTimeUtc)));
        }

        private void NiGuan(string path, XContainer result)
        {
            Response.Write("<!-- Processing");
            Response.Flush();
            var url = Rbase64.Decode(Request.QueryString["Url"].UrlDecode());
            if (string.IsNullOrWhiteSpace(url)) return;
            var client = new WebClient();
            foreach (var video in Net.YouTube.Video.GetVideoFromLink(client, url))
            {
                var element = new XElement("video", new XAttribute("title", video.Title), new XAttribute("url", video.Url));
                foreach (var link in video.FmtStreamMap)
                {
                    element.Add(new XElement("download", new XAttribute("type", link.ToString()), new XAttribute("link",
                        string.Format("{0}://{1}/Task/Create/Offline/{2}?Url={3}", Request.Url.Scheme, Request.Url.Host, path,
                                      Rbase64.Encode(link.GetUrl(link.Parent.Title))))));
                }
                result.Add(element);
                Response.Write('.');    // prevent the thread from getting killed
                Response.Flush();
            }
            Response.Write(" -->" + Environment.NewLine);
        }

        private void Details(string path, XContainer result)
        {
            var element = FileHelper.GetElement(FileHelper.GetDataFilePath(path));
            var info = new FileInfo(FileHelper.GetFilePath(path));
            if (element.GetAttributeValue("state") == "ready" || element.GetAttributeValue("size") == null)
                element.SetAttributeValue("size", info.Length);
            element.SetAttributeValue("lastWriteTimeUtc", info.LastWriteTimeUtc);
            element.Add(new XElement("ffmpeg", FFmpeg.Analyze(info.FullName)));
            result.Add(element);
        }
    }
}