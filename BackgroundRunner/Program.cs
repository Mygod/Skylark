using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using Mygod.Skylark.BackgroundRunner;
using Mygod.Xml.Linq;
using SevenZip;

// ReSharper disable ImplicitlyCapturedClosure

namespace Mygod.Skylark
{
    public static partial class FileHelper
    {
        public static string GetFilePath(string path)
        {
            if (path.Contains("%", StringComparison.InvariantCultureIgnoreCase)
                || path.Contains("#", StringComparison.InvariantCultureIgnoreCase)) throw new FormatException();
            return Path.Combine("Files", path);
        }
        public static string GetDataPath(string path)
        {
            if (path.Contains("%", StringComparison.InvariantCultureIgnoreCase)
                || path.Contains("#", StringComparison.InvariantCultureIgnoreCase)) throw new FormatException();
            return Path.Combine("Data", path);
        }
        public static string GetDataFilePath(string path)
        {
            return GetDataPath(path) + ".data";
        }
        public static string GetTaskPath(string id)
        {
            return GetDataPath(id) + ".task";
        }

        public static IEnumerable<string> GetAllSources(this IMultipleSources task)
        {
            foreach (var file in task.Sources)
            {
                var path = GetFilePath(file);
                if (IsFile(path)) yield return file;
                else foreach (var sub in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                        yield return sub.Substring(6);
            }
        }
    }

    public abstract partial class CloudTask
    {
        public void Execute()
        {
            try
            {
                PID = Process.GetCurrentProcess().Id;
                Save();
                ExecuteCore();
                if (!string.IsNullOrWhiteSpace(ErrorMessage)) Finish();
            }
            catch (Exception exc)
            {
                ErrorMessage = exc.GetMessage();
                Save();
            }
        }
        protected abstract void ExecuteCore();
    }
    public abstract partial class MultipleFilesTask
    {
        protected void StartFile(string relativePath)
        {
            FileHelper.WriteAllText(FileHelper.GetDataFilePath(relativePath),
                                    string.Format("<file state=\"{0}\" id=\"{1}\" pid=\"{2}\" />", Type, ID, PID));
            CurrentFile = relativePath;
            Save();
        }
        protected void FinishFile(string relativePath)
        {
            FileHelper.WriteAllText(FileHelper.GetDataFilePath(relativePath),
                                    string.Format("<file mime=\"{0}\" state=\"ready\" />", relativePath));
            Save();
        }
    }
    public abstract partial class MultipleToOneFileTask
    {
        public override sealed void Finish()
        {
            CurrentSource = null;
            base.Finish();
        }
    }
    public abstract partial class OneToMultipleFilesTask
    {
        public override sealed void Finish()
        {
            CurrentFile = null;
            base.Finish();
        }
    }

    public sealed partial class OfflineDownloadTask
    {
        public OfflineDownloadTask(string url, string relativePath)
            : base(relativePath, TaskType.OfflineDownloadTask)
        {
            Url = url;
        }

        protected override void ExecuteCore()
        {
            throw new NotSupportedException();
        }
    }
    public sealed partial class CompressTask
    {
        protected override void ExecuteCore()
        {
            SevenZipCompressor compressor = null;
            try
            {
                var files = this.GetAllSources().ToList();
                foreach (var file in files) FileHelper.WaitForReady(FileHelper.GetDataFilePath(file));
                long nextLength = 0, nextFile = 0;
                FileLength = files.Sum(file => new FileInfo(FileHelper.GetFilePath(file)).Length);
                compressor = new SevenZipCompressor
                {
                    CompressionLevel = (CompressionLevel)Enum.Parse(typeof(CompressionLevel),
                        TaskXml.GetAttributeValue("compressionLevel"), true)
                };
                switch (Path.GetExtension(RelativePath).ToLowerInvariant())
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
                var filesStart = Path.GetFullPath(FileHelper.GetFilePath(string.Empty)).Length + 1;
                compressor.FileCompressionStarted += (sender, e) =>
                {
                    ProcessedSourceCount += nextFile;
                    ProcessedFileLength += nextLength;
                    nextFile = 1;
                    nextLength = new FileInfo(e.FileName).Length;
                    CurrentSource = e.FileName.Substring(filesStart);
                    Save();
                };
                compressor.CompressFiles(FileHelper.GetFilePath(RelativePath),
                    Path.GetFullPath(FileHelper.GetFilePath(BaseFolder)).Length + 1,
                    files.Select(file => Path.GetFullPath(FileHelper.GetFilePath(file))).ToArray());
                ProcessedSourceCount += nextFile;
                ProcessedFileLength += nextLength;
                Finish();
            }
            catch (SevenZipException)
            {
                if (compressor == null) throw;
                throw new AggregateException(compressor.Exceptions);
            }
        }
    }
    public sealed partial class ConvertTask
    {
        private static readonly Regex TimeParser = new Regex(@"size=(.*)kB time=(.*)bitrate=", RegexOptions.Compiled);
        protected override void ExecuteCore()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("plugins/ffmpeg/ffmpeg.exe",
                    string.Format("-i \"{0}\"{2} \"{1}\" -y", FileHelper.GetFilePath(Source),
                                  FileHelper.GetFilePath(RelativePath), Arguments ?? string.Empty)) { UseShellExecute = false, RedirectStandardError = true }
            };
            process.Start();
            while (!process.StandardError.EndOfStream)
            {
                var line = process.StandardError.ReadLine();
                var match = TimeParser.Match(line);
                if (!match.Success) continue;
                try
                {
                    ProcessedFileLength = long.Parse(match.Groups[1].Value.Trim()) << 10;
                    ProcessedDuration = TimeSpan.Parse(match.Groups[2].Value.Trim());
                    Save();
                }
                catch { }
            }
            Finish();
        }
    }
    public sealed partial class DecompressTask
    {
        protected override void ExecuteCore()
        {
            SevenZipExtractor extractor = null;
            try
            {
                string directory = Target.Replace('/', '\\'),
                       filePath = FileHelper.GetFilePath(directory), dataPath = FileHelper.GetDataPath(directory);
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
                extractor = new SevenZipExtractor(FileHelper.GetFilePath(Source));
                var singleFileName = extractor.ArchiveFileNames.Count == 1
                                        && extractor.ArchiveFileNames[0] == "[no name]"
                                        ? Path.GetFileNameWithoutExtension(Source) : null;
                FileCount = extractor.FilesCount;
                FileLength = extractor.ArchiveFileData.Sum(data => (long)data.Size);
                long nextLength = 0, nextFile = 0;
                extractor.FileExtractionStarted += (sender, e) =>
                {
                    ProcessedFileCount += nextFile;
                    ProcessedFileLength += nextLength;
                    nextLength = (long)e.FileInfo.Size;
                    nextFile = 1;
                    StartFile(FileHelper.Combine(directory, singleFileName ?? e.FileInfo.FileName));
                };
                extractor.FileExtractionFinished +=
                    (sender, e) => FinishFile(FileHelper.Combine(directory, singleFileName ?? e.FileInfo.FileName));
                extractor.ExtractArchive(filePath);
                Finish();
            }
            catch (SevenZipException)
            {
                if (extractor == null) throw;
                throw new AggregateException(extractor.Exceptions);
            }
        }
    }
    public sealed partial class BitTorrentTask
    {
        protected override void ExecuteCore()
        {
            var filePath = FileHelper.GetFilePath(Target);
            long length = 0;
            var torrents = this.GetAllSources().Select(source => Torrent.Load(FileHelper.GetFilePath(source)))
                                               .ToList();
            foreach (var torrent in torrents)
            {
                FileCount += torrent.Files.Length;
                foreach (var file in torrent.Files)
                {
                    length += file.Length;
                    StartFile(FileHelper.Combine(Target, file.Path));
                }
            }
            FileLength = length;
            Save();

            var listenedPorts = new HashSet<int>(IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners().Select(endPoint => endPoint.Port));
            var port = 10000;
            while (listenedPorts.Contains(port)) port++;
            var engine = new ClientEngine(new EngineSettings(filePath, port) { PreferEncryption = false, AllowedEncryption = EncryptionTypes.All });
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
            var listener = new DhtListener(new IPEndPoint(IPAddress.Any, port));
            engine.RegisterDht(new DhtEngine(listener));
            listener.Start();
            engine.DhtEngine.Start();
            if (!Directory.Exists(engine.Settings.SavePath)) Directory.CreateDirectory(engine.Settings.SavePath);
            var allManagers = new List<TorrentManager>();
            foreach (var manager in
                torrents.Select(torrent => new TorrentManager(torrent, filePath, new TorrentSettings(1, 150, 0, 0))))
            {
                engine.Register(manager);
                manager.PieceHashed += (sender, e) =>
                {
                    ProcessedFileLength = allManagers.Sum(m => (long)(m.Progress * length / 100));
                    Save();
                };
                manager.Start();
                allManagers.Add(manager);
            }
            var managers = new LinkedList<TorrentManager>(allManagers);
            while (managers.Count > 0)
            {
                Thread.Sleep(1000);
                var i = managers.First;
                while (i != null)
                    if (i.Value.Complete)
                    {
                        var temp = i;
                        i = i.Next;
                        managers.Remove(temp);
                        foreach (var file in i.Value.Torrent.Files) FinishFile(FileHelper.Combine(Target, file.Path));
                        ProcessedSourceCount++;
                        Save();
                    }
                    else i = i.Next;
            }
            ProcessedFileLength = length;
            Finish();
        }
    }
    public sealed partial class CrossAppCopyTask
    {
        private readonly CookieAwareWebClient client = new CookieAwareWebClient();
        private bool CopyFile(string domain, string source, string target, bool logging = true)
        {
            var targetFile = FileHelper.Combine(target, Path.GetFileName(source));
            CurrentFile = targetFile;
            try
            {
                var root = XDocument.Parse(client.DownloadString(
                    string.Format("http://{0}/Api/Details/{1}", domain, source))).Root;
                if (root.GetAttributeValue("status") != "ok")
                    throw new ExternalException(root.GetAttributeValue("message"));
                Program.OfflineDownload(string.Format("http://{0}/Download/{1}", domain, source), target, client);
                var file = root.Element("file");
                FileHelper.SetDefaultMime(FileHelper.GetDataFilePath(targetFile), file.GetAttributeValue("mime"));
                ProcessedFileCount++;
                ProcessedFileLength += file.GetAttributeValue<long>("size");
                Save();
                return true;
            }
            catch (Exception exc)
            {
                if (logging)
                {
                    ErrorMessage += string.Format("复制 /{0} 时发生了错误：{2}{1}{2}", target, exc.GetMessage(),
                                                  Environment.NewLine);
                    Save();
                }
                return false;
            }
        }
        private void CopyDirectory(string domain, string source, string target)
        {
            CurrentFile = FileHelper.Combine(target, Path.GetFileName(source));
            Save();
            try
            {
                var root = XDocument.Parse(client.DownloadString(
                    string.Format("http://{0}/Api/List/{1}", domain, source))).Root;
                if (root.GetAttributeValue("status") != "ok")
                    throw new ExternalException(root.GetAttributeValue("message"));
                foreach (var element in root.Elements())
                {
                    var name = element.GetAttributeValue("name");
                    switch (element.Name.LocalName)
                    {
                        case "directory":
                            var dir = FileHelper.Combine(target, name);
                            Directory.CreateDirectory(FileHelper.GetFilePath(dir));
                            Directory.CreateDirectory(FileHelper.GetDataPath(dir));
                            CopyDirectory(domain, FileHelper.Combine(source, name), dir);
                            break;
                        case "file":
                            CopyFile(domain, FileHelper.Combine(source, name), target);
                            break;
                    }
                }
            }
            catch (Exception exc)
            {
                ErrorMessage += string.Format("复制 /{0} 时发生了错误：{2}{1}{2}", target, exc.GetMessage(),
                                              Environment.NewLine);
                Save();
            }
        }

        protected override void ExecuteCore()
        {
            ErrorMessage = string.Empty;
            client.CookieContainer.Add(new Cookie("Password", Password, "/", Domain));
            // Password = null;
            Save();
            if (!CopyFile(Domain, Source, Target, false)) CopyDirectory(Domain, Source, Target);
            Finish();
        }
    }
    public sealed partial class FtpUploadTask
    {
        protected override void ExecuteCore()
        {
            var url = UrlFull;
            Url = Url;  // clear the user account data!!!
            Save();
            var sources = this.GetAllSources().ToArray();
            SourceCount = sources.Length;
            FileLength = sources.Sum(source => new FileInfo(FileHelper.GetFilePath(source)).Length);
            foreach (var file in sources) FileHelper.WaitForReady(FileHelper.GetDataFilePath(file));
            var set = new HashSet<string>(new[] { url });
            foreach (var file in sources)
            {
                CurrentSource = file;
                Save();
                string targetUrl = Path.Combine(url, string.IsNullOrEmpty(BaseFolder)
                                                        ? file : file.Substring(BaseFolder.Length + 1)),
                       targetDir = Path.GetDirectoryName(targetUrl);
                FtpWebRequest request;
                if (!set.Contains(targetDir))
                {
                    request = (FtpWebRequest)WebRequest.Create(targetDir);
                    request.Timeout = Timeout.Infinite;
                    request.UseBinary = true;
                    request.UsePassive = true;
                    request.KeepAlive = true;
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.GetResponse().Close();
                }
                request = (FtpWebRequest)WebRequest.Create(Path.Combine(url, file));
                request.Timeout = Timeout.Infinite;
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = true;
                request.Method = WebRequestMethods.Ftp.UploadFile;
                using (var src = File.OpenRead(FileHelper.GetFilePath(file)))
                using (var dst = request.GetRequestStream())
                {
                    var byteBuffer = new byte[1048576];
                    var bytesSent = src.Read(byteBuffer, 0, 1048576);
                    while (bytesSent != 0)
                    {
                        dst.Write(byteBuffer, 0, bytesSent);
                        ProcessedFileLength += bytesSent;
                        Save();
                        bytesSent = src.Read(byteBuffer, 0, 1048576);
                    }
                }
                request.GetResponse().Close();
                ProcessedSourceCount++;
            }
            Finish();
        }
    }

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; private set; }

        public CookieAwareWebClient(CookieContainer cookies = null)
        {
            CookieContainer = (cookies ?? new CookieContainer());
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            ProcessRequest(request);
            return request;
        }
        public void ProcessRequest(WebRequest request)
        {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest == null) return;
            httpRequest.CookieContainer = CookieContainer;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
    }
}

namespace Mygod.Skylark.BackgroundRunner
{
    public static class Program
    {
        private static void Main()
        {
            try
            {
                Console.WriteLine("恭喜你得到了这款无聊的程序！随便打点东西吧。");
                switch (Console.ReadLine().ToLowerInvariant())
                {
                    case TaskType.OfflineDownloadTask:
                        OfflineDownload(Console.ReadLine(), Console.ReadLine());
                        break;
                    case TaskType.FtpUploadTask:
                        new FtpUploadTask(Console.ReadLine()).Execute();
                        break;
                    case TaskType.DecompressTask:
                        new DecompressTask(Console.ReadLine()).Execute();
                        break;
                    case TaskType.CompressTask:
                        new CompressTask(Console.ReadLine()).Execute();
                        break;
                    case TaskType.ConvertTask:
                        new ConvertTask(Console.ReadLine()).Execute();
                        break;
                    case TaskType.CrossAppCopyTask:
                        new CrossAppCopyTask(Console.ReadLine()).Execute();
                        break;
                    case TaskType.BitTorrentTask:
                        new BitTorrentTask(Console.ReadLine()).Execute();
                        break;
                    default:
                        Console.WriteLine("无法识别。");
                        break;
                }
            }
            catch (Exception exc)
            {
                File.AppendAllText(@"Data\error.log", string.Format("[{0}] {1}{2}{2}", DateTime.UtcNow,
                                                                    exc.GetMessage(), Environment.NewLine));
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
        public static void OfflineDownload(string url, string path, CookieAwareWebClient client = null)
        {
            FileStream fileStream = null;
            OfflineDownloadTask task = null;
            try
            {
                var retried = false;
                var protocol = url.Remove(url.IndexOf(':')).ToLowerInvariant();
                switch (protocol)
                {
                    case "http":
                    case "https":
                    case "ftp":
                    case "file":
                        var request = WebRequest.Create(url);
                        var httpWebRequest = request as HttpWebRequest;
                        if (httpWebRequest != null)
                        {
                            httpWebRequest.Referer = url;
                            if (client != null) client.ProcessRequest(request);
                        }
                        request.Timeout = Timeout.Infinite;
                        var response = request.GetResponse();
                        if (!retried && url.StartsWith("http://goo.im", true, CultureInfo.InvariantCulture)
                            && response.ContentType == "text/html")
                        {
                            retried = true;
                            Thread.Sleep(15000);
                            goto case "file";
                        }
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
                        if (fileLength < 0) fileLength = null;

                        var fileName = (pos >= 0 ? disposition.Substring(pos + 9).Trim('"', '\'').UrlDecode()
                                                 : GetFileName(url)).ToValidPath();
                        string mime, extension;
                        try
                        {
                            mime = Helper.GetMime(response.ContentType);
                            extension = Helper.GetDefaultExtension(mime);
                        }
                        catch
                        {
                            extension = Path.GetExtension(fileName);
                            mime = Helper.GetDefaultExtension(extension);
                        }
                        if (!string.IsNullOrEmpty(extension) && !fileName.EndsWith(extension, StringComparison.Ordinal))
                            fileName += extension;

                        task = new OfflineDownloadTask(url, path = FileHelper.Combine(path, fileName))
                            { PID = Process.GetCurrentProcess().Id };
                        if (!string.IsNullOrWhiteSpace(mime)) task.Mime = mime;
                        if (fileLength != null) task.FileLength = fileLength;
                        task.Save();
                        stream.CopyTo(fileStream = File.Create(FileHelper.GetFilePath(path)));
                        task.Finish();
                        break;
                    case "magnet":
                        var magnet = new MagnetLink(url);
                        var torrentPath = FileHelper.Combine(path, magnet.Name + ".torrent");
                        task = new OfflineDownloadTask(url, torrentPath)
                            { PID = Process.GetCurrentProcess().Id, FileLength = magnet.Length };
                        task.Save();
                        var listenedPorts = new HashSet<int>(IPGlobalProperties.GetIPGlobalProperties()
                            .GetActiveTcpListeners().Select(endPoint => endPoint.Port));
                        var port = 10000;
                        while (listenedPorts.Contains(port)) port++;
                        var filePath = FileHelper.GetFilePath(path);
                        var engine = new ClientEngine(new EngineSettings(filePath, port)
                            { PreferEncryption = false, AllowedEncryption = EncryptionTypes.All });
                        engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
                        var listener = new DhtListener(new IPEndPoint(IPAddress.Any, port));
                        engine.RegisterDht(new DhtEngine(listener));
                        listener.Start();
                        engine.DhtEngine.Start();
                        if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);
                        var manager = new TorrentManager(magnet, filePath, new TorrentSettings(1, 150, 0, 0), FileHelper.GetFilePath(torrentPath));
                        engine.Register(manager);
                        var stopped = false;
                        manager.TorrentStateChanged += (sender, e) =>
                        {
                            if (e.NewState == TorrentState.Metadata) return;
                            stopped = true;
                            manager.Stop();
                        };
                        manager.Start();
                        while (!stopped) Thread.Sleep(1000);
                        if (magnet.Length.HasValue) task.ProcessedFileLength = magnet.Length.Value;
                        task.Finish();
                        break;
                    default:
                        throw new NotSupportedException("不支持的协议：" + protocol);
                }
            }
            catch (Exception exc)
            {
                if (task == null) throw;
                task.ErrorMessage = exc.Message;
                task.Save();
            }
            finally
            {
                if (fileStream != null) fileStream.Close();
            }
        }
    }
}
