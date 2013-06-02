using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using Microsoft.Win32;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public static partial class Helper
    {
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

        private static readonly object Locker = new object();
        private static readonly MethodInfo GetMimeMappingMethodInfo;
        public static string GetMimeType(string fileName)
        {
            lock (Locker) return (string)GetMimeMappingMethodInfo.Invoke(null, new object[] { fileName });
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
    }

    public static partial class FileHelper
    {
        public static string Combine(params string[] paths)
        {
            var result = string.Empty;
            foreach (var path in paths.Select(path => path.Trim('/')))
            {
                if (!string.IsNullOrEmpty(result)) result += '/';
                result += path;
            }
            return result;
        }

        public static void WriteAllText(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents);
        }

        public static XElement GetElement(string path)
        {
            try
            {
                return XHelper.Load(path).Element("file");
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
                doc = XHelper.Load(path);
            }
            catch
            {
                doc = new XDocument(new XElement("file"));
            }
            var root = doc.Element("file");
            root.SetAttributeValue(attribute, value);
            doc.Save(path);
        }

        public static bool IsReady(string dataPath)
        {
            return GetFileValue(dataPath, "state") == "ready";
        }
        public static void WaitForReady(string dataPath, int timeoutSeconds = -1)
        {
            while (!IsReady(dataPath))
            {
                if (timeoutSeconds-- == 0) throw new TimeoutException();
                Thread.Sleep(1000);
            }
        }
    }
}