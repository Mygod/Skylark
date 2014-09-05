using System;
using System.IO;
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
                        if (!Request.GetUser().Browse)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
                        List(path, result);
                        break;
                    case "createdirectory":
                        if (!Request.GetUser().OperateFiles)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
                        FileHelper.CreateDirectory(path);
                        break;
                    case "copy":
                        if (!Request.GetUser().OperateFiles)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
                        FileHelper.Copy(path, Request.QueryString["Target"].UrlDecode());
                        break;
                    case "move":
                        if (!Request.GetUser().OperateFiles)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
                        FileHelper.Move(path, Request.QueryString["Target"].UrlDecode());
                        break;
                    case "delete":
                        if (!Request.GetUser().OperateFiles)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
                        FileHelper.Delete(path);
                        break;
                    case "details":
                        if (!Request.GetUser().Browse)
                        {
                            Response.StatusCode = 401;
                            return;
                        }
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
            foreach (var video in Net.YouTube.Video.GetVideoFromLink(url))
            {
                var element = new XElement("video", new XAttribute("title", video.Title),
                    new XAttribute("url", video.Url), new XAttribute("author", video.Author),
                    new XAttribute("keywords", string.Join(", ", video.Keywords)),
                    new XAttribute("rating", video.AverageRating), new XAttribute("viewCount", video.ViewCount),
                    new XAttribute("uploadTime", video.UploadTime.Ticks), new XAttribute("length", video.Length));
                foreach (var link in video.Downloads)
                {
                    var e = new XElement("download", new XAttribute("type", link.ToString()),
                                         new XAttribute("information", link.Properties));
                    if (link.UrlUnavailableException == null) e.SetAttributeValue("link",
                        string.Format("{0}://{1}/Task/Create/Offline/{2}?Url={3}", Request.Url.Scheme,
                            Request.Url.Host, FileHelper.Combine(path, (link.Parent.Title + link.Extension)
                                .ToValidPath()), Rbase64.Encode(link.GetUrl(link.Parent.Title))));
                    element.Add(e);
                }
                result.Add(element);
                Response.Write('.');    // prevent the thread from getting killed, how evil I am MUAHAHA
                Response.Flush();
            }
            Response.Write(" -->" + Environment.NewLine);
        }

        private void Details(string path, XContainer result)
        {
            var element = FileHelper.GetElement(FileHelper.GetDataFilePath(path));
            var info = new FileInfo(FileHelper.GetFilePath(path));
            if (element.GetAttributeValue("state") == TaskType.NoTask || element.GetAttributeValue("size") == null)
                element.SetAttributeValue("size", info.Length);
            element.SetAttributeValue("lastWriteTimeUtc", info.LastWriteTimeUtc);
            element.Add(new XElement("ffmpeg", FFmpeg.Analyze(info.FullName)));
            result.Add(element);
        }
    }
}