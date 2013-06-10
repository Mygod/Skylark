using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Mygod.Xml.Linq;
using SevenZip;

// ReSharper disable ImplicitlyCapturedClosure

namespace Mygod.Skylark.BackgroundRunner
{
    public static class Program
    {
        private static void Main()
        {
            Console.WriteLine("恭喜你得到了这款无聊的程序！随便打点东西吧。");
            switch (Console.ReadLine().ToLowerInvariant())
            {
                case "offline-download":
                    OfflineDownload(Console.ReadLine(), Console.ReadLine());
                    break;
                case "decompress":
                    Decompress(Console.ReadLine());
                    break;
                case "compress":
                    Compress(Console.ReadLine());
                    break;
                default:
                    Console.WriteLine("无法识别。");
                    break;
            }
        }

        private static string GetFilePath(string path)
        {
            return Path.Combine("Files", path);
        }
        private static string GetDataPath(string path, bool isFile = true)
        {
            return Path.Combine("Data", isFile ? path + ".data" : path);
        }

        private static string GetFileName(string url)
        {
            url = url.TrimEnd('/', '\\');
            int i = url.IndexOf('?'), j = url.IndexOf('#');
            if (j >= 0 && (i < 0 || i > j)) i = j;
            if (i >= 0) url = url.Substring(0, i);
            return Path.GetFileName(url);
        }
        private static void OfflineDownload(string url, string path)
        {
            FileStream fileStream = null;
            XDocument doc = null;
            XElement root = null;
            string xmlPath = null;
            try
            {
                var request = WebRequest.Create(url);
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

                var fileName = (pos >= 0 ? disposition.Substring(pos + 9).Trim('"', '\'').UrlDecode().UrlDecode() : GetFileName(url));
                var mime = Helper.GetMime(response.ContentType);
                var extension = Helper.GetDefaultExtension(mime);
                if (!string.IsNullOrEmpty(extension) && !fileName.EndsWith(extension, StringComparison.Ordinal)) fileName += extension;

                path = Path.Combine(path, fileName);
                xmlPath = GetDataPath(path);
                path = GetFilePath(path);

                doc = new XDocument();
                root = new XElement("file", new XAttribute("url", url), new XAttribute("state", "downloading"),
                                    new XAttribute("pid", Process.GetCurrentProcess().Id), new XAttribute("fileName", fileName),
                                    new XAttribute("startTime", DateTime.UtcNow), new XAttribute("mime", mime));
                doc.Add(root);
                if (fileLength != null) root.SetAttributeValue("size", fileLength);
                doc.Save(xmlPath);

                stream.CopyTo(fileStream = File.Create(path));

                root.SetAttributeValue("endTime", DateTime.UtcNow);
                root.SetAttributeValue("state", "ready");
                doc.Save(xmlPath);
            }
            catch (Exception exc)
            {
                if (doc == null || root == null) return;
                root.SetAttributeValue("message", exc.Message);
                doc.Save(xmlPath);
            }
            finally
            {
                if (fileStream != null) fileStream.Close();
            }
        }

        private static void Decompress(string id)
        {
            var xmlPath = GetDataPath(id + ".decompress.task", false);
            if (!File.Exists(xmlPath)) return;
            var doc = XHelper.Load(xmlPath);
            var root = doc.Element("decompress");
            SevenZipExtractor extractor = null;
            try
            {
                var pid = Process.GetCurrentProcess().Id;
                root.SetAttributeValue("pid", pid);
                root.SetAttributeValue("progress", 0);
                var progress = 0;
                doc.Save(xmlPath);
                string archive = root.GetAttributeValue("archive"), directory = root.GetAttributeValue("directory"),
                       filePath = GetFilePath(directory), dataPath = GetDataPath(directory, false);
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
                extractor = new SevenZipExtractor(GetFilePath(archive));
                extractor.FileExtractionStarted += (sender, e) =>
                {
                    FileHelper.WriteAllText(GetDataPath(FileHelper.Combine(directory, e.FileInfo.FileName)),
                                            string.Format("<file state=\"decompressing\" id=\"{0}\" pid=\"{1}\" />", id, pid));
                    root.SetAttributeValue("current", e.FileInfo.FileName);
                    doc.Save(xmlPath);
                };
                extractor.FileExtractionFinished += (sender, e) =>
                {
                    FileHelper.WriteAllText(GetDataPath(FileHelper.Combine(directory, e.FileInfo.FileName)),
                        string.Format("<file mime=\"{0}\" state=\"ready\" />", Helper.GetMimeType(e.FileInfo.FileName)));
                    if (e.PercentDone == progress) return;
                    root.SetAttributeValue("progress", progress = e.PercentDone);
                    doc.Save(xmlPath);
                };
                extractor.ExtractArchive(filePath);
                var current = root.Attribute("current");
                if (current != null) current.Remove();
                root.SetAttributeValue("finished", DateTime.UtcNow.Ticks);
                doc.Save(xmlPath);
            }
            catch (SevenZipException)
            {
                if (extractor == null) throw;
                root.SetAttributeValue("message", string.Join("<br />", extractor.Exceptions.Select(e => e.Message)));
                doc.Save(xmlPath);
            }
            catch (Exception exc)
            {
                root.SetAttributeValue("message", exc.Message);
                doc.Save(xmlPath);
            }
        }

        private static void Compress(string path)
        {
            var xmlPath = GetDataPath(path);
            var doc = XHelper.Load(xmlPath);
            var root = doc.Root;
            SevenZipCompressor compressor = null;
            try
            {
                root.SetAttributeValue("pid", Process.GetCurrentProcess().Id);
                doc.Save(xmlPath);
                string baseFolder = root.GetAttributeValue("baseFolder"), baseFileFolder = GetFilePath(baseFolder);
                var queue = new Queue<string>();
                foreach (var e in root.ElementsCaseInsensitive("directory")) queue.Enqueue(FileHelper.Combine(baseFolder, e.Value));
                var files = root.ElementsCaseInsensitive("file").Select(e => FileHelper.Combine(baseFolder, e.Value)).ToList();
                while (queue.Count > 0)
                {
                    string item = queue.Dequeue(), itemFile = GetFilePath(item);
                    files.AddRange(Directory.EnumerateFiles(itemFile).Select(file => Path.Combine(item, Path.GetFileName(file))));
                    foreach (var dir in Directory.EnumerateDirectories(itemFile)) queue.Enqueue(Path.Combine(item, Path.GetFileName(dir)));
                }
                foreach (var file in files) FileHelper.WaitForReady(GetDataPath(file));
                compressor = new SevenZipCompressor
                    { CompressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel), root.GetAttributeValue("level"), true) };
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".7z":
                        compressor.ArchiveFormat = OutArchiveFormat.SevenZip;
                        break;
                    case ".zip":
                        compressor.ArchiveFormat = OutArchiveFormat.Zip;
                        break;
                    case ".tar":
                        compressor.ArchiveFormat = OutArchiveFormat.Tar;
                        break;
                }
                compressor.FileCompressionStarted += (sender, e) =>
                {
                    root.SetAttributeValue("current", e.FileName);
                    root.SetAttributeValue("progress", e.PercentDone);
                    doc.Save(xmlPath);
                };
                compressor.CompressFiles(GetFilePath(path), Path.GetFullPath(baseFileFolder).Length + 1,
                                         files.Select(file => Path.GetFullPath(GetFilePath(file))).ToArray());
                var current = root.Attribute("current");
                if (current != null) current.Remove();
                root.SetAttributeValue("state", "ready");
                root.SetAttributeValue("finishTime", DateTime.UtcNow.Ticks);
                doc.Save(xmlPath);
            }
            catch (SevenZipException)
            {
                if (compressor == null) throw;
                root.SetAttributeValue("message", string.Join("<br />", compressor.Exceptions.Select(e => e.Message)));
                doc.Save(xmlPath);
            }
            catch (Exception exc)
            {
                root.SetAttributeValue("message", exc.GetMessage());
                doc.Save(xmlPath);
            }
        }
    }
}
