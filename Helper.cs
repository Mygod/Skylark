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
        public static string GetDataPath(this HttpServerUtility server, string path)
        {
            return server.MapPath("~/Data/" + path);
        }
        public static string GetDataFilePath(this HttpServerUtility server, string path)
        {
            return server.GetDataPath(path) + ".data";
        }
        public static string GetDataDirectoryPath(this HttpServerUtility server, string path)
        {
            return Path.Combine(server.GetDataPath(path), "Settings.directory");
        }

        public static long GetFileSize(this HttpServerUtility server, string path)
        {
            var root = GetElement(server.GetDataFilePath(path));
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
        }

        public enum CodecType
        {
            Unknown, Video, Audio, Subtitle
        }

        private static string root, ffmpeg, ffprobe;
        public static List<Codec> Codecs = new List<Codec>();

        private static Process CreateProcess(string path, string arguments)
        {
            var result = new Process { StartInfo = new ProcessStartInfo(path, arguments)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root } };
            result.Start();
            return result;
        }

        public static void Initialize(HttpServerUtility server)
        {
            lock (Codecs)
            {
                if (root != null) return;
                root = server.MapPath("~/");
                var dirPath = Path.Combine(root, "plugins/ffmpeg");
                ffmpeg = Path.Combine(dirPath, "ffmpeg.exe");
                ffprobe = Path.Combine(dirPath, "ffprobe.exe");
                var process = CreateProcess(ffmpeg, "-codecs");
                while (!process.StandardOutput.ReadLine().Contains("-------", StringComparison.Ordinal))
                {
                }
                while (!process.StandardOutput.EndOfStream) Codecs.Add(new Codec(process.StandardOutput.ReadLine()));
            }
        }

        public static string Analyze(string path)
        {
            try
            {
                var process = CreateProcess(ffprobe, '"' + path + '"');
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