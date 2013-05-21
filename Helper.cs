using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Mygod.Skylark.Offline;

namespace Mygod.Skylark
{
    public static partial class Helper
    {
        private static readonly object Locker = new object();
        private static readonly MethodInfo GetMimeMappingMethodInfo;

        static Helper()
        {
            var mimeMappingType = Assembly.GetAssembly(typeof(HttpRuntime)).GetType("System.Web.MimeMapping");
            if (mimeMappingType == null) throw new SystemException("Couldn't find MimeMapping type");
            GetMimeMappingMethodInfo = mimeMappingType.GetMethod("GetMimeMapping",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (GetMimeMappingMethodInfo == null) throw new SystemException("Couldn't find GetMimeMapping method");
            if (GetMimeMappingMethodInfo.ReturnType != typeof(string))
                throw new SystemException("GetMimeMapping method has invalid return type");
            if (GetMimeMappingMethodInfo.GetParameters().Length != 1
                && GetMimeMappingMethodInfo.GetParameters()[0].ParameterType != typeof(string))
                throw new SystemException("GetMimeMapping method has invalid parameters");
        }
        public static string GetMimeType(string fileName)
        {
            lock (Locker) return (string) GetMimeMappingMethodInfo.Invoke(null, new object[] { fileName });
        }

        private static readonly string[] Units = new[] { "字节", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };
        public static string GetSize(long size)
        {
            double byt = size;
            byte i = 0;
            while (byt > 1000)
            {
                byt /= 1024;
                i++;
            }
            var bytesstring = size.ToString("N");
            return byt.ToString("N") + " " + Units[i] + " (" + bytesstring.Remove(bytesstring.Length - 3) + " 字节)";
        }
        public static string GetSize(double size)
        {
            var byt = size;
            byte i = 0;
            while (byt > 1000)
            {
                byt /= 1024;
                i++;
            }
            var bytesstring = size.ToString("N");
            return byt.ToString("N") + " " + Units[i] + " (" + bytesstring.Remove(bytesstring.Length - 3) + " 字节)";
        }

        public static string GetRouteString(this RouteData data, string valueName)
        {
            try
            {
                return data.GetRequiredString(valueName);
            }
            catch
            {
                return null;
            }
        }

        public static string GetAttributeValue(this XElement e, XName name)
        {
            var attr = e.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        public static IEnumerable<string> GetSelectedFiles(this RepeaterItemCollection collection)
        {
            return from RepeaterItem item in collection where ((CheckBox) item.FindControl("Check")).Checked
                   select ((HtmlInputHidden)item.FindControl("Hidden")).Value;
        }

        public static void NewOfflineTask(this HttpServerUtility server, string url, string relativePath)
        {
            Process.Start(new ProcessStartInfo(server.MapPath("~/Offline/MygodOfflineDownloader.exe"),
                String.Format("\"{0}\" \"{1}\"", LinkConverter.Decode(url), relativePath)) { WorkingDirectory = server.MapPath("~/") });
        }

        public static DateTime Parse(string value)
        {
            return DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Unspecified);
        }

        public static string ToChineseString(this DateTime value, bool offset = true)
        {
            if (offset) value = value.AddHours(8);
            return value.ToString("yyyy.M.d H:mm:ss.fff");
        }

        public static string UrlDecode(this string str)
        {
            return HttpUtility.UrlDecode(str);
        }

        public static string UrlEncode(this string str)
        {
            return HttpUtility.UrlEncodeUnicode(str);
        }

        public static string GetVideoFileName(this VideoLinkBase link, bool ignoreExtensions = false)
        {
            return "%T%E".Replace("%T", link.Parent.Title).Replace("%A", link.Parent.Author)
                .Replace("%E", ignoreExtensions ? string.Empty : link.Extension).Replace("\\", "＼").Replace("/", "／")
                .Replace(":", "：").Replace("*", "＊").Replace("?", "？").Replace("\"", "＂").Replace("<", "＜").Replace(">", "＞")
                .Replace("|", "｜");
        }
    }

    public static class FileHelper
    {
        public static string GetRelativePath(this HttpContext context)
        {
            return (context.Server.UrlDecode(context.Request.QueryString.ToString()) ?? String.Empty).Replace('\\', '/').Trim('/');
        }

        public static string Combine(params string[] paths)
        {
            var result = String.Empty;
            foreach (var path in paths.Select(path => path.Trim('/')))
            {
                if (!String.IsNullOrEmpty(result)) result += '/';
                result += path;
            }
            return result;
        }

        public static string GetFilePath(this HttpServerUtility server, string path)
        {
            return server.MapPath("~/Files/" + path);
        }
        public static string GetDataPath(this HttpServerUtility server, string path, bool isFile = true)
        {
            return server.MapPath("~/Data/" + (isFile ? path + ".data" : path));
        }

        public static XElement GetElement(string path)
        {
            try
            {
                return XDocument.Load(path).Element("file");
            }
            catch
            {
                return null;
            }
        }
        public static string GetFileValue(string path, string attribute)
        {
            try
            {
                return GetElement(path).GetAttributeValue(attribute);
            }
            catch
            {
                return null;
            }
        }
        public static void SetFileValue(string path, string attribute, string value)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(path);
            }
            catch
            {
                doc = new XDocument(new XElement("file"));
            }
            var root = doc.Element("file");
            root.SetAttributeValue(attribute, value);
            doc.Save(path);
        }

        public static bool IsReady(string path)
        {
            return GetFileValue(path, "state") == "ready";
        }

        public static string GetDefaultMime(string path)
        {
            return GetFileValue(path, "mime");
        }
        public static void SetDefaultMime(string path, string value)
        {
            SetFileValue(path, "mime", value);
        }

        public static void CancelControl(this HttpServerUtility server, string dataPath)
        {
            if (File.Exists(dataPath))
            {
                var element = GetElement(dataPath);
                if (element.GetAttributeValue("state") != "ready")
                    try
                    {
                        Process.GetProcessById(int.Parse(element.GetAttributeValue("id"))).Kill();
                    }
                    catch { }
            }
            else if (Directory.Exists(dataPath)) foreach (var stuff in Directory.EnumerateFileSystemEntries(dataPath))
                server.CancelControl(stuff);
        }

        internal static void DeleteWithRetries(string path)
        {
        retry:
            try
            {
                if (File.Exists(path)) File.Delete(path);
                else if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
                Thread.Sleep(100);
                goto retry;
            }
        }
    }

    public static class LinkConverter
    {
        public static string Base64Encode(string str)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(str));
        }

        public static string Base64Decode(string str)
        {
            return Encoding.Default.GetString(Convert.FromBase64String(str));
        }

        public static string PublicEncode(string link, string linkpre, string prefix, string suffix, string name)
        {
            if (String.IsNullOrEmpty(link)) throw new ArgumentNullException("link");
            if (link.ToLower().StartsWith(linkpre.ToLower(), StringComparison.Ordinal))
                throw new ArgumentException("该链接已经是" + name + "下载链接。");
            return linkpre + Base64Encode(prefix + link + suffix);
        }

        public static string PublicDecode(string link, string linkpre, string prefix, string suffix, string name)
        {
            if (String.IsNullOrEmpty(link)) throw new ArgumentNullException("link");
            if (!link.ToLower().StartsWith(linkpre.ToLower(), StringComparison.Ordinal))
                throw new ArgumentException("该链接不是" + name + "下载链接。");
            link = link.TrimEnd('\\', '/', ' ', '\t', '\r', '\n');
            var and = link.IndexOf('&');
            if (and >= 0) link = link.Substring(0, and);
            var result = Base64Decode(link.Substring(linkpre.Length));
            return result.Substring(prefix.Length, result.Length - prefix.Length - suffix.Length);
        }

        public static string ThunderEncode(string link)
        {
            return PublicEncode(link, "thunder://", "AA", "ZZ", "迅雷");
        }

        public static string ThunderDecode(string link)
        {
            return PublicDecode(link, "thunder://", "AA", "ZZ", "迅雷");
        }

        public static string FlashGetEncode(string link)
        {
            return PublicEncode(link, "flashget://", "[FLASHGET]", "[FLASHGET]", "快车");
        }

        public static string FlashGetDecode(string link)
        {
            return PublicDecode(link, "flashget://", "[FLASHGET]", "[FLASHGET]", "快车");
        }

        public static string QQDLEncode(string link)
        {
            return PublicEncode(link, "qqdl://", String.Empty, String.Empty, "旋风");
        }

        public static string QQDLDecode(string link)
        {
            return PublicDecode(link, "qqdl://", String.Empty, String.Empty, "旋风");
        }

        public static string RayFileEncode(string link)
        {
            return PublicEncode(link, "fs2you://", String.Empty, String.Empty, "RayFile");
        }

        public static string RayFileDecode(string link)
        {
            return PublicDecode(link, "fs2you://", String.Empty, String.Empty, "RayFile");
        }

        public static string Reverse(string value)
        {
            return string.Join(null, value.Reverse());
        }

        public static string Encode(LinkType to, string i)
        {
            switch (to)
            {
                case LinkType.Normal:
                    return i;
                case LinkType.Thunder:
                    return ThunderEncode(i);
                case LinkType.FlashGet:
                    return FlashGetEncode(i);
                case LinkType.QQDL:
                    return QQDLEncode(i);
                case LinkType.RayFile:
                    return RayFileEncode(i);
                default:
                    throw new ArgumentException("未知的链接格式！");
            }
        }

        private static string Decode(LinkType from, string i)
        {
            switch (from)
            {
                case LinkType.Normal:
                    return i;
                case LinkType.Thunder:
                    return ThunderDecode(i);
                case LinkType.FlashGet:
                    return FlashGetDecode(i);
                case LinkType.QQDL:
                    return QQDLDecode(i);
                case LinkType.RayFile:
                    return RayFileDecode(i);
                default:
                    throw new ArgumentException("未知的链接格式！");
            }
        }

        public static string Decode(string i)
        {
            var result = i;
            var type = GetUrlType(result);
            while (type != LinkType.Normal)
            {
                result = Decode(type, result);
                type = GetUrlType(result);
            }
            return result;
        }

        public static LinkType GetUrlType(string i)
        {
            var l = i.ToLower();
            if (l.StartsWith("thunder://", StringComparison.Ordinal)) return LinkType.Thunder;
            if (l.StartsWith("flashget://", StringComparison.Ordinal)) return LinkType.FlashGet;
            if (l.StartsWith("qqdl://", StringComparison.Ordinal)) return LinkType.QQDL;
            return l.StartsWith("fs2you://", StringComparison.Ordinal) ? LinkType.RayFile : LinkType.Normal;
        }
    }

    public enum LinkType
    {
        Normal, Thunder, FlashGet, QQDL, RayFile
    }

    public static class Rbase64
    {
        public static string Encode(string value)
        {
            return LinkConverter.Base64Encode(LinkConverter.Reverse(value));
        }

        public static string Decode(string value)
        {
            return LinkConverter.Reverse(LinkConverter.Base64Decode(value));
        }
    }
}