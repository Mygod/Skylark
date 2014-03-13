using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        private static readonly string[] Units = { "字节", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };
        public static string GetSize(long size)
        {
            double byt = size;
            byte i = 0;
            while (byt > 1000)
            {
                byt /= 1024;
                i++;
            }
            if (i == 0) return size.ToString("N0") + " 字节";
            return byt.ToString("N") + " " + Units[i] + " (" + size.ToString("N0") + " 字节)";
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
            if (i == 0) return byt.ToString("N") + " 字节";
            return byt.ToString("N") + " " + Units[i] + " (" + size.ToString("N") + " 字节)";
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
        public static string ToChineseString(this DateTime? value, bool offset = true)
        {
            return value.HasValue ? value.Value.ToChineseString(offset) : "未知";
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            var header = request.Headers["X-Requested-With"];
            return header != null && header == "XMLHttpRequest";
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
            if (path.Contains("%", StringComparison.InvariantCultureIgnoreCase)
                || path.Contains("#", StringComparison.InvariantCultureIgnoreCase)) throw new FormatException();
            return HttpContext.Current.Server.MapPath("~/Files/" + path);
        }
        public static string GetDataPath(string path)
        {
            if (path.Contains("%", StringComparison.InvariantCultureIgnoreCase)
                || path.Contains("#", StringComparison.InvariantCultureIgnoreCase)) throw new FormatException();
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
        public static string GetTaskPath(string id)
        {
            return GetDataPath(id + ".task");
        }

        public static long GetFileSize(string path)
        {
            var root = GetElement(GetDataFilePath(path));
            if (root != null && root.GetAttributeValue("state") != TaskType.NoTask)
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
                if (element.GetAttributeValue("state") != TaskType.NoTask)
                {
                    var pid = element.GetAttributeValueWithDefault<int>("pid");
                    if (pid != 0) CloudTask.KillProcess(pid);
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

    public abstract partial class CloudTask
    {
        public static void StartRunner(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(HttpContext.Current.Server.MapPath("~/plugins/BackgroundRunner.exe"))
                                { WorkingDirectory = HttpContext.Current.Server.MapPath("~/"), RedirectStandardInput = true, 
                                  UseShellExecute = false }
            };
            process.Start();
            process.StandardInput.WriteLine(args);
            process.StandardInput.Close();
        }

        public void Start()
        {
            Save();
            StartCore();
        }
        protected abstract void StartCore();
    }
    public abstract partial class GenerateFileTask
    {
        protected override void StartCore()
        {
            StartRunner(Type + '\n' + RelativePath);
        }
    }
    public abstract partial class GeneralTask
    {
        protected override void StartCore()
        {
            StartRunner(Type + '\n' + ID);
        }
    }
    
    public sealed partial class OfflineDownloadTask
    {
        private static readonly Regex MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);
        public static void Create(string url, string relativePath)
        {
            StartRunner(string.Format("{2}\n{0}\n{1}", LinkConverter.Decode(url), relativePath, TaskType.OfflineDownloadTask));
        }
        public static void CreateMediaFire(string id, string relativePath)
        {
            Create(MediaFireDirectLinkExtractor.Match(new WebClient().DownloadString("http://www.mediafire.com/?" + id))
                                               .Groups[1].Value, relativePath);
        }

        protected override void StartCore()
        {
            throw new NotSupportedException();
        }
    }
    public sealed partial class ConvertTask
    {
        private static readonly Regex DurationParser = new Regex("Duration: (.*?),", RegexOptions.Compiled);

        public static void Create(string source, string target, string size = null, string vcodec = null, string acodec = null,
                                  string scodec = null, string startPoint = null, string endPoint = null)
        {
            var arguments = string.Empty;
            if (!string.IsNullOrWhiteSpace(size)) arguments += " -s " + size;
            if (!string.IsNullOrWhiteSpace(vcodec)) arguments += " -vcodec " + vcodec;
            if (!string.IsNullOrWhiteSpace(acodec)) arguments += " -acodec " + acodec;
            if (!string.IsNullOrWhiteSpace(scodec)) arguments += " -scodec " + scodec;
            TimeSpan duration = TimeSpan.Parse(DurationParser.Match(FFmpeg.Analyze(FileHelper.GetFilePath(source))).Groups[1].Value),
                     start = FFmpeg.Parse(startPoint), end = FFmpeg.Parse(endPoint, duration);
            if (start <= TimeSpan.Zero) start = TimeSpan.Zero; else arguments += " -ss " + startPoint;
            if (end >= duration) end = duration; else arguments += " -to " + endPoint;
            new ConvertTask(source, target, end - start, arguments).Start();
        }
    }

    public static class TaskHelper
    {
        private static readonly Dictionary<string, string> Mappings = new Dictionary<string, string>
            { { TaskType.OfflineDownloadTask, "离线下载" }, { TaskType.CompressTask, "压缩" },
              { TaskType.BitTorrentTask, "离线下载 BT 种子" },  { TaskType.ConvertTask, "转换媒体格式" },
              { TaskType.CrossAppCopyTask, "跨云雀复制" }, { TaskType.DecompressTask, "解压" }, { TaskType.FtpUploadTask, "FTP 上传" } };
        public static string GetName(string id)
        {
            return Mappings.ContainsKey(id) ? Mappings[id] : "处理";
        }

        public static void CleanUp()
        {
            foreach (var path in Directory.EnumerateFiles(FileHelper.GetDataPath(String.Empty), "*.task"))
            {
                var pid = XHelper.Load(path).Root.GetAttributeValueWithDefault<int>("pid");
                if (pid != 0) CloudTask.KillProcess(pid);
                FileHelper.DeleteWithRetries(path);
            }
        }

        public static long CurrentWorkers { get { return Process.GetProcessesByName("BackgroundRunner").LongLength; } }
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
        public struct Codec
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
                    default:
                        Type = CodecType.Unknown;
                        break;
                }
                IntraFrameOnlyCodec = input[4] == 'I';
                LossyCompression = input[5] == 'L';
                LosslessCompression = input[6] == 'S';
                Name = input.Substring(8, 21).TrimEnd();
                Description = input.Substring(29);
            }

            public readonly bool DecodingSupported, EncodingSupported, IntraFrameOnlyCodec, LossyCompression, LosslessCompression;
            public readonly CodecType Type;
            public readonly string Name, Description;

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
                Ffprobe = Path.Combine(dirPath, "ffprobe.exe");
                var process = CreateProcess(Path.Combine(dirPath, "ffmpeg.exe"), "-codecs");
                while (!process.StandardOutput.ReadLine().Contains("-------", StringComparison.Ordinal))
                {
                }
                while (!process.StandardOutput.EndOfStream) Codecs.Add(new Codec(process.StandardOutput.ReadLine()));
            }
        }

        private static readonly string Root, Ffprobe;
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