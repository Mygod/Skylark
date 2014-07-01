using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
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
        private static readonly string[]
            Units = { "字节", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };
        public static string GetSize(long size)
        {
            double byt = size;
            byte i = 0;
            while (byt > 1000)
            {
                byt /= 1024;
                i++;
            }
            return i == 0 ? size.ToString("N0") + " 字节"
                          : byt.ToString("N") + " " + Units[i] + " (" + size.ToString("N0") + " 字节)";
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

        public static IEnumerable<string> GetSelectedItemsID(this RepeaterItemCollection collection)
        {
            return from RepeaterItem item in collection where ((CheckBox) item.FindControl("Check")).Checked
                   select ((HtmlInputHidden)item.FindControl("Hidden")).Value;
        }

        public static string ToChineseString(this DateTime value, bool offset = true)
        {
            if (offset)
                try
                {
                    value = value.AddHours(8);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            return value.ToString("yyyy.M.d H:mm:ss.fff");
        }
        public static string ToChineseString(this DateTime? value, bool offset = true)
        {
            return value.HasValue ? value.Value.ToChineseString(offset) : Unknown;
        }
        public static string ToChineseString(this TimeSpan? value)
        {
            return value.HasValue ? value.Value.ToString("G") : Unknown;
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            var header = request.Headers["X-Requested-With"];
            return header != null && header == "XMLHttpRequest";
        }

        public static string GetPassword(this HttpRequest request)
        {
            var cookie = request.Cookies["Password"];
            return (cookie == null ? null : cookie.Value) ?? string.Empty;
        }
        public static User GetUser(this HttpRequest request)
        {
            var temp = new Privileges();
            var psw = request.GetPassword();
            if (temp.Contains(psw)) return temp[psw];
            return temp.Contains(User.AnonymousPassword) ? temp[User.AnonymousPassword] : new User();
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
            return HttpUtility.UrlDecode(data.GetRouteString("Path") ?? string.Empty).ToCorrectUrl();
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
            if (Directory.Exists(dataPath))
            {
                foreach (var stuff in Directory.EnumerateFileSystemEntries(dataPath)) CancelControl(stuff);
                return;
            }
            if (!File.Exists(dataPath) || !dataPath.EndsWith(".data", true, CultureInfo.InvariantCulture))
                return; // ignore non-data files
            var element = GetElement(dataPath);
            if (element.GetAttributeValue("state") == TaskType.NoTask) return;
            CloudTask.KillProcessTree(element.GetAttributeValueWithDefault<int>("pid"));
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
        protected static void StartRunner(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(HttpContext.Current.Server.MapPath("~/plugins/BackgroundRunner.exe"))
                    { WorkingDirectory = HttpContext.Current.Server.MapPath("~/"), RedirectStandardInput = true, 
                      UseShellExecute = false }
            };
            process.Start();
            process.StandardInput.WriteLine(Rbase64.Encode(args));
            process.StandardInput.Close();
        }

        public void Start()
        {
            Save();
            StartCore();
        }
        protected abstract void StartCore();

        public static void KillProcessTree(int pid)
        {
            if (pid == 0) return;
            foreach (var mbo in new ManagementObjectSearcher
                ("Select * From Win32_Process Where ParentProcessID=" + pid).Get())
                KillProcessTree(Int32.Parse(mbo["ProcessID"].ToString()));
            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch { }
        }
    }
    public abstract partial class GenerateFileTask
    {
        public static GenerateFileTask Create(string relativePath)
        {
            switch (FileHelper.GetState(FileHelper.GetDataFilePath(relativePath)).ToLowerInvariant())
            {
                case TaskType.UploadTask:
                    return new UploadTask(relativePath);
                case TaskType.OfflineDownloadTask:
                    return new OfflineDownloadTask(relativePath);
                case TaskType.CompressTask:
                    return new CompressTask(relativePath);
                case TaskType.ConvertTask:
                    return new ConvertTask(relativePath);
                default:
                    return null;
            }
        }

        protected override void StartCore()
        {
            StartRunner(Type + '\n' + RelativePath);
        }
    }
    public abstract partial class GeneralTask
    {
        public static GeneralTask Create(string id)
        {
            switch (XHelper.Load(FileHelper.GetTaskPath(id)).Root.Name.LocalName.ToLowerInvariant())
            {
                case TaskType.FtpUploadTask:
                    return new FtpUploadTask(id);
                case TaskType.CrossAppCopyTask:
                    return new CrossAppCopyTask(id);
                case TaskType.DecompressTask:
                    return new DecompressTask(id);
                default:
                    return null;
            }
        }

        protected override void StartCore()
        {
            StartRunner(Type + '\n' + ID);
        }
    }
    
    public sealed partial class OfflineDownloadTask
    {
        private static readonly Regex
            MediaFireDirectLinkExtractor = new Regex("kNO = \"(.*?)\";", RegexOptions.Compiled);
        public static void Create(string url, string relativePath)
        {
            StartRunner(string.Format("{2}\n{0}\n{1}", LinkConverter.Decode(url), relativePath,
                                      TaskType.OfflineDownloadTask));
        }
        public static void CreateMediaFire(string id, string relativePath)
        {
            Create(MediaFireDirectLinkExtractor.Match(new WebClient()
                        .DownloadString("http://www.mediafire.com/?" + id)).Groups[1].Value, relativePath);
        }

        protected override void StartCore()
        {
            throw new NotSupportedException();
        }
    }
    public sealed class UploadTask : GenerateFileTask
    {
        public UploadTask(string relativePath) : base(relativePath)
        {
        }

        public UploadTask(string relativePath, string identifier, int totalParts, long length)
            : base(relativePath, TaskType.UploadTask)
        {
            Identifier = identifier;
            TotalParts = totalParts;
            FileLength = length;
        }

        public string Identifier
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("identifier"); }
            set { TaskXml.SetAttributeValue("identifier", value); }
        }
        public int TotalParts
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValue<int>("totalParts"); }
            set { TaskXml.SetAttributeValue("totalParts", value); }
        }
        public HashSet<int> FinishedParts
        {
            get
            {
                return TaskXml == null ? null
                    : new HashSet<int>((TaskXml.GetAttributeValue("finishedParts") ?? string.Empty)
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
            }
            set { TaskXml.SetAttributeValue("finishedParts", string.Join(",", value)); }
        }
    }
    public sealed partial class ConvertTask
    {
        private static readonly Regex DurationParser = new Regex("Duration: (.*?),", RegexOptions.Compiled);

        public static void Create(string source, string target, string size = null, string vcodec = null,
                                  string acodec = null, string scodec = null, string audioPath = null,
                                  string startPoint = null, string endPoint = null)
        {
            var arguments = string.Empty;
            if (!string.IsNullOrWhiteSpace(size)) arguments += " -s " + size;
            if (!string.IsNullOrWhiteSpace(vcodec)) arguments += " -vcodec " + vcodec;
            if (!string.IsNullOrWhiteSpace(acodec)) arguments += " -acodec " + acodec;
            if (!string.IsNullOrWhiteSpace(scodec)) arguments += " -scodec " + scodec;
            TimeSpan duration = TimeSpan.Parse(DurationParser.Match(FFmpeg.Analyze(FileHelper.GetFilePath(source))).Groups[1].Value),
                     start = FFmpeg.Parse(startPoint), end = FFmpeg.Parse(endPoint, duration);
            if (start <= TimeSpan.Zero) start = TimeSpan.Zero;
            else arguments += " -ss " + startPoint;
            if (end >= duration) end = duration;
            else arguments += " -to " + endPoint;
            new ConvertTask(source, target, end - start, audioPath, arguments).Start();
        }
    }

    public static class TaskHelper
    {
        private static readonly Dictionary<string, string> Mappings = new Dictionary<string, string>
            { { TaskType.OfflineDownloadTask, "离线下载" }, { TaskType.CompressTask, "压缩" },
              { TaskType.ConvertTask, "转换媒体格式" }, { TaskType.CrossAppCopyTask, "跨云雀复制" },
              { TaskType.DecompressTask, "解压" }, { TaskType.FtpUploadTask, "FTP 上传" },
              { TaskType.UploadTask, "上传" } };
        public static string GetName(string id)
        {
            return Mappings.ContainsKey(id) ? Mappings[id] : "处理";
        }

        public static long CurrentWorkers { get { return Process.GetProcessesByName("BackgroundRunner").LongLength; } }
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

            public readonly bool DecodingSupported, EncodingSupported, IntraFrameOnlyCodec, LossyCompression,
                                 LosslessCompression;
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

    public sealed class User
    {
        private static char Tf(bool tf)
        {
            return tf ? 'T' : 'F';
        }

        public User()
        {
            Password = AnonymousPassword;
            Comment = "游客";
        }
        public User(XElement user)
        {
            user.GetAttributeValue(out Password, "password");
            user.GetAttributeValue(out Comment, "comment");
            user.GetAttributeValueWithDefault(out Browse, "browse");
            user.GetAttributeValueWithDefault(out Download, "download");
            OperateTasks = (OperateFiles = user.GetAttributeValueWithDefault<bool>("operateFiles"))
                                && user.GetAttributeValueWithDefault<bool>("operateTasks");
            user.GetAttributeValueWithDefault(out Admin, "admin");
        }
        public User(string value)
        {
            var columns = value.Split(',');
            Password = columns[0];
            Comment = Rbase64.Decode(columns[1]);
            Browse = columns[2][0] == 'T';
            Download = columns[2][1] == 'T';
            OperateTasks = (OperateFiles = columns[2][2] == 'T') && columns[2][3] == 'T';
            Admin = columns[2][4] == 'T';
        }

        public readonly string Password, Comment;
        public readonly bool Browse, Download, OperateFiles, OperateTasks, Admin;

        public override string ToString()
        {
            return string.Format("{0},{1},{2}{3}{4}{5}{6}", Password, Rbase64.Encode(Comment),
                                 Tf(Browse), Tf(Download), Tf(OperateFiles), Tf(OperateTasks), Tf(Admin));
        }
        public XElement ToElement()
        {
            return new XElement("user", new XAttribute("password", Password), new XAttribute("comment", Comment),
                                new XAttribute("browse", Browse), new XAttribute("download", Download),
                                new XAttribute("operateFiles", OperateFiles),
                                new XAttribute("operateTasks", OperateTasks), new XAttribute("admin", Admin));
        }

        public static readonly string AnonymousPassword = "ba3253876aed6bc22d4a6ff53d8406c6ad864195ed144ab5c87621b6c233b548baeae6956df346ec8c17f5ea10f35ee3cbc514797ed7ddd3145464e2a0bab413";
    }
    public sealed class Privileges : KeyedCollection<string, User>
    {
        private static readonly string Path = HttpContext.Current.Server.MapPath("~/Data/Site.privileges");

        private Privileges()
        {
        }
        public Privileges(XContainer root)
        {
            foreach (var user in root.ElementsCaseInsensitive("user").Select(e => new User(e))
                                     .Where(user => !Contains(user.Password))) Add(user);
        }

        public Privileges(string path = null) : this(XHelper.Load(path ?? Path).Root)
        {
        }

        public static Privileges Parse(string value)
        {
            var result = new Privileges();
            foreach (var user in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => new User(line)).Where(user => !result.Contains(user.Password))) result.Add(user);
            return result;
        }

        protected override string GetKeyForItem(User item)
        {
            return item.Password;
        }

        public override string ToString()
        {
            return string.Join(";", this);
        }
        public XElement ToElement()
        {
            return new XElement("privileges", this.Select(user => user.ToElement()));
        }

        public void Save(string path = null)
        {
            ToElement().Save(path ?? Path);
        }
    }

    public sealed class Config
    {
        private static readonly string ConfigPath = FileHelper.GetDataPath("Site.config");
        private readonly XElement element;

        public Config()
        {
            element = FileHelper.GetElement(ConfigPath);
        }

        public string Root
        {
            get { return element.GetAttributeValue("root"); }
            set { element.SetAttributeValue("root", value); }
        }

        public void Save()
        {
            element.Save(ConfigPath);
        }
    }
}