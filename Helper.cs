using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
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
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Ticks.ToString(CultureInfo.InvariantCulture)));
        }

        public static DateTime Deshorten(string value)
        {
            return new DateTime(long.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value))), DateTimeKind.Utc);
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

        public static string GetFilePath(string path)
        {
            return HttpContext.Current.Server.MapPath("~/Files/" + path);
        }
        public static string GetDataPath(string path)
        {
            return HttpContext.Current.Server.MapPath("~/Data/" + path);
        }
        public static string GetDataFilePath(string path)
        {
            return GetDataPath(path) + ".data";
        }
        public static string GetDataDirectoryPath(string path)
        {
            return Path.Combine(GetDataPath(path), "Settings.directory");
        }

        public static long GetFileSize(string path)
        {
            var root = GetElement(GetDataFilePath(path));
            if (root != null && root.GetAttributeValue("state") != "ready")
            {
                long result;
                if (long.TryParse(root.GetAttributeValue("size"), out result)) return result;
            }
            return new FileInfo(GetFilePath(path)).Length;
        }

        public static string GetDefaultMime(string path)
        {
            return GetFileValue(path, "mime");
        }

        private static bool CheckFile(string path, bool? overwrite = null)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return true;
            if (overwrite.HasValue) return overwrite.Value; 
            throw new IOException("文件/目录已存在！");
        }
        private static bool? IsFileExtended(string path)
        {
            if (File.Exists(path)) return true;
            if (Directory.Exists(path)) return false;
            return null;
        }
        public static bool IsFile(string path)
        {
            var result = IsFileExtended(path);
            if (result.HasValue) return result.Value;
            throw new FileNotFoundException();
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
        {
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var path = Path.Combine(destDirName, file.Name);
                file.CopyTo(path, false);
            }
            if (copySubDirs) foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath);
                }
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

        public static void CancelControl(string dataPath)
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
            else if (Directory.Exists(dataPath))
                foreach (var stuff in Directory.EnumerateFileSystemEntries(dataPath)) CancelControl(stuff);
        }
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(GetFilePath(path));
            Directory.CreateDirectory(GetDataPath(path));
        }
        public static void Move(string source, string target, bool? overwrite = null)
        {
            var filePath = GetFilePath(source);
            var isFile = IsFile(filePath);
            string dataPath = isFile ? GetDataFilePath(source) : GetDataPath(source), targetFile = GetFilePath(target);
            if (!CheckFile(targetFile, overwrite)) return;
            CancelControl(dataPath);
            if (isFile)
            {
                File.Move(GetFilePath(source), GetFilePath(target));
                File.Move(dataPath, GetDataFilePath(target));
            }
            else
            {
                Directory.Move(GetFilePath(source), GetFilePath(target));
                Directory.Move(dataPath, GetDataPath(target));
            }
        }
        public static void Copy(string source, string target, bool? overwrite = null)
        {
            var filePath = GetFilePath(source);
            var isFile = IsFile(filePath);
            string dataPath = isFile ? GetDataFilePath(source) : GetDataPath(source),
                   targetFile = GetFilePath(target);
            if (!CheckFile(targetFile, overwrite)) return;
            CancelControl(dataPath);
            if (isFile)
            {
                File.Copy(GetFilePath(source), GetFilePath(target));
                File.Copy(dataPath, GetDataFilePath(target));
            }
            else
            {
                DirectoryCopy(GetFilePath(source), GetFilePath(target));
                DirectoryCopy(dataPath, GetDataPath(target));
            }
        }
        public static void Delete(string path)
        {
            var filePath = GetFilePath(path);
            var isFile = IsFileExtended(filePath);
            if (!isFile.HasValue) return;
            var dataPath = isFile.Value ? GetDataFilePath(path) : GetDataPath(path);
            CancelControl(dataPath);
            DeleteWithRetries(GetFilePath(path));
            DeleteWithRetries(dataPath);
        }
    }

    public static class TaskHelper
    {
        private static readonly Regex DurationParser = new Regex("Duration: (.*?),", RegexOptions.Compiled),
                                      MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);

        private static void StartRunner(string args)
        {
            var info = new ProcessStartInfo(HttpContext.Current.Server.MapPath("~/plugins/BackgroundRunner.exe"))
                { WorkingDirectory = HttpContext.Current.Server.MapPath("~/"), RedirectStandardInput = true, UseShellExecute = false };
            var process = new Process { StartInfo = info };
            process.Start();
            process.StandardInput.WriteLine(args);
            process.StandardInput.Close();
        }

        public static void CreateOffline(string url, string relativePath)
        {
            StartRunner(string.Format("offline-download\n{0}\n{1}", LinkConverter.Decode(url), relativePath));
        }
        public static void CreateOfflineMediaFire(string id, string relativePath)
        {
            CreateOffline(MediaFireDirectLinkExtractor.Match(new WebClient().DownloadString("http://www.mediafire.com/?" + id))
                .Groups[1].Value, relativePath);
        }

        public static void CreateCompress(string archiveFilePath, IEnumerable<string> files, string baseFolder = null, 
                                          string compressionLevel = null)
        {
            baseFolder = baseFolder ?? string.Empty;
            var root = new XElement("file", new XAttribute("state", "compressing"), new XAttribute("startTime", DateTime.UtcNow.Ticks),
                                    new XAttribute("baseFolder", baseFolder), new XAttribute("mime", Helper.GetMimeType(archiveFilePath)),
                                    new XAttribute("level", compressionLevel ?? "Ultra"));
            foreach (var file in files) root.Add(new XElement(
                FileHelper.IsFile(FileHelper.GetFilePath(FileHelper.Combine(baseFolder, file))) ? "file" : "directory", file));
            new XDocument(root).Save(FileHelper.GetDataFilePath(archiveFilePath));
            File.WriteAllText(FileHelper.GetFilePath(archiveFilePath), string.Empty);   // temp
            StartRunner("compress\n" + archiveFilePath);
        }
        public static string CreateDecompress(string path, string target)
        {
            var id = DateTime.UtcNow.Shorten();
            new XDocument(new XElement("decompress", new XAttribute("archive", path), new XAttribute("directory", target)))
                .Save(FileHelper.GetDataPath(id + ".decompress.task"));
            StartRunner("decompress\n" + id);
            return id;
        }

        public static void CreateConvert(string source, string target, string size = null, string vcodec = null, string acodec = null, 
                                         string scodec = null, string startPoint = null, string endPoint = null)
        {
            var filePath = FileHelper.GetFilePath(target);
            if (File.Exists(filePath)) return;
            File.WriteAllText(filePath, string.Empty);
            var arguments = string.Empty;
            if (!string.IsNullOrWhiteSpace(size)) arguments += " -s " + size;
            if (!string.IsNullOrWhiteSpace(vcodec)) arguments += " -vcodec " + vcodec;
            if (!string.IsNullOrWhiteSpace(acodec)) arguments += " -acodec " + acodec;
            if (!string.IsNullOrWhiteSpace(scodec)) arguments += " -scodec " + scodec;
            TimeSpan duration = TimeSpan.Parse(DurationParser.Match(FFmpeg.Analyze(FileHelper.GetFilePath(source))).Groups[1].Value),
                     start = FFmpeg.Parse(startPoint), end = FFmpeg.Parse(endPoint, duration);
            if (start <= TimeSpan.Zero) start = TimeSpan.Zero; else arguments += " -ss " + startPoint;
            if (end >= duration) end = duration; else arguments += " -to " + endPoint;
            new XDocument(new XElement("file", new XAttribute("state", "converting"), new XAttribute("input", source),
                                       new XAttribute("startTime", DateTime.UtcNow.Ticks), new XAttribute("arguments", arguments),
                                       new XAttribute("duration", (end - start).Ticks),
                                       new XAttribute("mime", Helper.GetMimeType(target))))
                .Save(FileHelper.GetDataFilePath(target));
            StartRunner("convert\n" + target);
        }

        public static string CreateCrossAppCopy(string domain, string path, string target)
        {
            var id = DateTime.UtcNow.Shorten();
            new XDocument(new XElement("crossAppCopy", new XAttribute("domain", domain), new XAttribute("path", path.ToCorrectUrl()), 
                new XAttribute("target", target))).Save(FileHelper.GetDataPath(id + ".crossAppCopy.task"));
            StartRunner("cross-app-copy\n" + id);
            return id;
        }

        public static void CleanUp()
        {
            foreach (var path in Directory.EnumerateFiles(FileHelper.GetDataPath(string.Empty), "*.task"))
            {
                var pid = XHelper.Load(path).Root.GetAttributeValueWithDefault<int>("pid");
                if (pid != 0) Helper.KillProcess(pid);
                FileHelper.DeleteWithRetries(path);
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

    public static partial class FFmpeg
    {
        public sealed class Codec
        {
            public Codec(string input)
            {
                DecodingSupported = input[1] == 'D';
                EncodingSupported = input[2] == 'E';
                switch (input[3])
                {
                    case 'V':
                        Type = CodecType.Video;
                        break;
                    case 'A':
                        Type = CodecType.Audio;
                        break;
                    case 'S':
                        Type = CodecType.Subtitle;
                        break;
                }
                IntraFrameOnlyCodec = input[4] == 'I';
                LossyCompression = input[5] == 'L';
                LosslessCompression = input[6] == 'S';
                Name = input.Substring(8, 21).TrimEnd();
                Description = input.Substring(29);
            }

            public bool DecodingSupported, EncodingSupported, IntraFrameOnlyCodec, LossyCompression, LosslessCompression;
            public CodecType Type;
            public string Name, Description;

            public override string ToString()
            {
                var result = Description;
                if (IntraFrameOnlyCodec) result += " (Intra frame-only codec)";
                if (LossyCompression) result += " (Lossy compression)";
                if (LosslessCompression) result += " (Lossless compression)";
                return result;
            }

            public XElement ToElement()
            {
                var element = new XElement("codec");
                element.SetAttributeValueWithDefault("name", Name);
                element.SetAttributeValueWithDefault("description", Description);
                element.SetAttributeValueWithDefault("decodingSupported", DecodingSupported);
                element.SetAttributeValueWithDefault("encodingSupported", EncodingSupported);
                element.SetAttributeValueWithDefault("type", Type);
                element.SetAttributeValueWithDefault("intraFrameOnlyCodec", IntraFrameOnlyCodec);
                element.SetAttributeValueWithDefault("lossyCompression", LossyCompression);
                element.SetAttributeValueWithDefault("losslessCompression", LosslessCompression);
                return element;
            }
        }

        public enum CodecType
        {
            Unknown, Video, Audio, Subtitle
        }

        static FFmpeg()
        {
            lock (Codecs)
            {
                if (Root != null) return;
                Root = HttpContext.Current.Server.MapPath("~/");
                var dirPath = Path.Combine(Root, "plugins/ffmpeg");
                Ffmpeg = Path.Combine(dirPath, "ffmpeg.exe");
                Ffprobe = Path.Combine(dirPath, "ffprobe.exe");
                var process = CreateProcess(Ffmpeg, "-codecs");
                while (!process.StandardOutput.ReadLine().Contains("-------", StringComparison.Ordinal))
                {
                }
                while (!process.StandardOutput.EndOfStream) Codecs.Add(new Codec(process.StandardOutput.ReadLine()));
            }
        }

        private static readonly string Root, Ffmpeg, Ffprobe;
        public static readonly List<Codec> Codecs = new List<Codec>();

        private static Process CreateProcess(string path, string arguments)
        {
            var result = new Process { StartInfo = new ProcessStartInfo(path, arguments)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Root } };
            result.Start();
            return result;
        }

        public static string Analyze(string path)
        {
            try
            {
                var process = CreateProcess(Ffprobe, '"' + path + '"');
                while (!process.StandardError.ReadLine().StartsWith("Input", StringComparison.Ordinal))
                {
                }
                return process.StandardError.ReadToEnd();
            }
            catch
            {
                return "分析失败。";
            }
        }
    }
}