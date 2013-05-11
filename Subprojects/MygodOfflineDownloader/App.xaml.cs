using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Xml.Linq;

namespace Mygod.Skylark.OfflineDownloader
{
    public sealed partial class App
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            FileStream fileStream = null;
            XDocument doc = null;
            XElement root = null;
            string path = e.Args[1], xmlPath = null;
            try
            {
                var request = WebRequest.Create(e.Args[0]);
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var disposition = response.Headers["Content-Disposition"] ?? string.Empty;
                var pos = disposition.IndexOf("filename=", StringComparison.Ordinal);
                long? fileLength;
                if (stream.CanSeek) fileLength = stream.Length;
                else
                    try
                    {
                        fileLength = response.ContentLength;
                    }
                    catch
                    {
                        fileLength = null;
                    }

                var fileName = pos >= 0 ? disposition.Substring(pos + 9).Trim('"', '\'') : GetFileName(e.Args[0]);
                var mime = Helper.GetMime(response.ContentType);
                var extension = Helper.GetDefaultExtension(mime);
                if (!string.IsNullOrEmpty(extension) && !fileName.EndsWith(extension, StringComparison.Ordinal)) fileName += extension;

                path = Path.Combine(path, fileName);
                xmlPath = Path.Combine("Data", path);
                path = Path.Combine("Files", path);

                doc = new XDocument();
                root = new XElement("file", new XAttribute("url", e.Args[0]), new XAttribute("state", "downloading"),
                                    new XAttribute("id", Process.GetCurrentProcess().Id), new XAttribute("fileName", fileName),
                                    new XAttribute("startTime", R.UtcNow), new XAttribute("mime", mime));
                doc.Add(root);
                if (fileLength != null) root.SetAttributeValue("size", fileLength);
                doc.Save(xmlPath);

                stream.CopyTo(fileStream = File.Create(path));

                root.SetAttributeValue("endTime", R.UtcNow);
                root.SetAttributeValue("state", "ready");
                doc.Save(xmlPath);
            }
            catch (Exception exc)
            {
                if (doc == null || root == null) return;
                root.SetAttributeValue("state", "error");
                root.SetAttributeValue("message", exc.Message);
                doc.Save(xmlPath);
            }
            finally
            {
                if (fileStream != null) fileStream.Close();
                Shutdown();
            }
        }

        private static string GetFileName(string url)
        {
            url = url.TrimEnd('/', '\\');
            int i = url.IndexOf('?'), j = url.IndexOf('#');
            if (j >= 0 && (i < 0 || i > j)) i = j;
            if (i >= 0) url = url.Substring(0, i);
            return Path.GetFileName(url);
        }
    }
}
