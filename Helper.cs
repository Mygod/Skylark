using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Mygod.Skylark.Offline;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public static partial class Helper
    {
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

        public static IEnumerable<string> GetSelectedFiles(this RepeaterItemCollection collection)
        {
            return from RepeaterItem item in collection where ((CheckBox) item.FindControl("Check")).Checked
                   select ((HtmlInputHidden)item.FindControl("Hidden")).Value;
        }

        public static void StartRunner(this HttpServerUtility server, string args)
        {
            var info = new ProcessStartInfo(server.MapPath("~/plugins/BackgroundRunner.exe"))
                { WorkingDirectory = server.MapPath("~/"), RedirectStandardInput = true, UseShellExecute = false };
            var process = new Process { StartInfo = info };
            process.Start();
            process.StandardInput.WriteLine(args);
            process.StandardInput.Close();
        }

        public static void NewOfflineTask(this HttpServerUtility server, string url, string relativePath)
        {
            server.StartRunner(string.Format("offline-download\n{0}\n{1}", LinkConverter.Decode(url), relativePath));
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
            return HttpUtility.UrlEncode(str);
        }

        public static string GetVideoFileName(this VideoLinkBase link, bool ignoreExtensions = false)
        {
            return "%T%E".Replace("%T", link.Parent.Title).Replace("%A", link.Parent.Author)
                .Replace("%E", ignoreExtensions ? String.Empty : link.Extension).Replace("%", "%0").Replace("\\", "%1").Replace("/", "%2")
                .Replace(":", "%3").Replace("*", "%4").Replace("?", "%5").Replace("\"", "%6").Replace("<", "%7").Replace(">", "%8")
                .Replace("|", "%9");
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            var header = request.Headers["X-Requested-With"];
            return header != null && header == "XMLHttpRequest";
        }

        public static string Shorten(this DateTime value)
        {
            return Convert.ToBase64String(BitConverter.GetBytes(value.Ticks));
        }

        public static DateTime Deshorten(string value)
        {
            return new DateTime(BitConverter.ToInt64(Convert.FromBase64String(value), 0), DateTimeKind.Utc);
        }

        public static void KillProcess(int pid)
        {
            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch { }
        }
        public static bool IsBackgroundRunnerKilled(int pid)
        {
            try
            {
                return Process.GetProcessById(pid).ProcessName != "BackgroundRunner";
            }
            catch
            {
                return true;
            }
        }
    }

    public static partial class FileHelper
    {
        public static string ToCorrectUrl(this string value)
        {
            return value.Replace('\\', '/').Trim('/');
        }

        public static string GetRelativePath(this RouteData data)
        {
            return (data.GetRouteString("Path") ?? string.Empty).ToCorrectUrl();
        }

        public static string GetFilePath(this HttpServerUtility server, string path)
        {
            return server.MapPath("~/Files/" + path);
        }
        public static string GetDataPath(this HttpServerUtility server, string path, bool isFile = true)
        {
            return server.MapPath("~/Data/" + (isFile ? path + ".data" : path));
        }

        public static long GetFileSize(this HttpServerUtility server, string path)
        {
            var root = GetElement(server.GetDataPath(path));
            if (root != null && root.GetAttributeValue("state") != "ready")
            {
                long result;
                if (long.TryParse(root.GetAttributeValue("size"), out result)) return result;
            }
            return new FileInfo(server.GetFilePath(path)).Length;
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
                if (!dataPath.EndsWith(".data", true, CultureInfo.InvariantCulture)) return;    // ignore non-data files
                var element = GetElement(dataPath);
                if (element.GetAttributeValue("state") != "ready")
                {
                    var pid = element.GetAttributeValueWithDefault<int>("pid");
                    if (pid != 0) Helper.KillProcess(pid);
                }
            }
            else if (Directory.Exists(dataPath)) foreach (var stuff in Directory.EnumerateFileSystemEntries(dataPath))
                server.CancelControl(stuff);
        }

        public static void DeleteWithRetries(string path)
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