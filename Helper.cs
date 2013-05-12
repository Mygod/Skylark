using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Web;
using System.Web.Routing;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Mygod.Skylark
{
    public static class Helper
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

        public static string GetDefaultExtension(string mimeType)
        {
            try
            {
                var key = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + mimeType, false);
                var value = key != null ? key.GetValue("Extension", null) : null;
                return value != null ? value.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        public static string GetMime(string contentType)
        {
            try
            {
                return contentType.Split(';')[0]; // kill that "; charset=utf-8" stupid stuff
            }
            catch
            {
                return contentType;
            }
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
        public static string GetDataPath(this HttpServerUtility server, string path)
        {
            return server.MapPath("~/Data/" + path);
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
    }

    public static class R
    {
        private static readonly TimeSpan Offset = NtpClient.GetNetworkTime("time.windows.com") - DateTime.UtcNow;
        public static DateTime UtcNow { get { return DateTime.SpecifyKind(DateTime.UtcNow.Add(Offset), DateTimeKind.Unspecified); } }
    }

    /// <summary>
    /// Static class to receive the time from a NTP server.
    /// </summary>
    public static class NtpClient
    {
        /// <summary>
        /// Gets the current DateTime from <paramref name="ntpServer"/>.
        /// </summary>
        /// <param name="ntpServer">The hostname of the NTP server.</param>
        /// <returns>A DateTime containing the current time.</returns>
        public static DateTime GetNetworkTime(string ntpServer)
        {
            var address = Dns.GetHostEntry(ntpServer).AddressList;
            if (address == null || address.Length == 0)
                throw new ArgumentException("Could not resolve ip address from '" + ntpServer + "'.", "ntpServer");
            var ep = new IPEndPoint(address[0], 123);
            return GetNetworkTime(ep);
        }

        /// <summary>
        /// Gets the current DateTime form <paramref name="ep"/> IPEndPoint.
        /// </summary>
        /// <param name="ep">The IPEndPoint to connect to.</param>
        /// <returns>A DateTime containing the current time.</returns>
        private static DateTime GetNetworkTime(EndPoint ep)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(ep);
            var ntpData = new byte[48]; // RFC 2030 
            ntpData[0] = 0x1B;
            for (var i = 1; i < 48; i++) ntpData[i] = 0;
            s.Send(ntpData);
            s.Receive(ntpData);
            const byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;
            for (var i = 0; i <= 3; i++) intpart = 256 * intpart + ntpData[offsetTransmitTime + i];
            for (var i = 4; i <= 7; i++) fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];
            var milliseconds = (intpart * 1000 + fractpart * 1000 / 0x100000000L);
            s.Close();
            return new DateTime(1900, 1, 1) + TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
        }
    }
}