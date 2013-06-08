using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Mygod.Net;
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