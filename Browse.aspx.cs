using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public partial class Browse : Page
    {
        protected string RelativePath, Status, FileSize, StartTime, SpentTime, RemainingTime, EndingTime, Percentage;
        protected DirectoryInfo InfoDirectory;
        protected FileInfo InfoFile;

        protected void Page_PreInit(object sender, EventArgs e)
        {
            RelativePath = RouteData.GetRelativePath();
            var absolutePath = Server.GetFilePath(RelativePath);
            Title = ("浏览 " + RelativePath).TrimEnd();
            InfoDirectory = new DirectoryInfo(absolutePath);
            InfoFile = new FileInfo(absolutePath);
            var url = Request.RawUrl.Split('?');
            if (InfoDirectory.Exists && !url[0].EndsWith("/", StringComparison.Ordinal))
                if (url.Length > 1) Response.Redirect(url[0] + "/?" + url[1], true);
                else Response.Redirect(url[0] + '/', true);
            if (InfoFile.Exists && url[0].EndsWith("/", StringComparison.Ordinal))
                if (url.Length > 1) Response.Redirect(url[0].TrimEnd('/') + "?" + url[1], true);
                else Response.Redirect(url[0].TrimEnd('/'), true);
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (InfoDirectory.Exists)
            {
                Views.SetActiveView(DirectoryView);
                if (IsPostBack) return;
                DirectoryList.DataSource = InfoDirectory.EnumerateDirectories();
                DirectoryList.DataBind();
                FileList.DataSource = InfoDirectory.EnumerateFiles();
                FileList.DataBind();
                ArchiveFilePath.Text = FileHelper.Combine(RelativePath, "Files.7z");
            }
            else if (InfoFile.Exists)
            {
                string dataPath = Server.GetDataFilePath(RelativePath), state = FileHelper.GetFileValue(dataPath, "state");
                switch (state)
                {
                    case "ready":
                        if (Request.IsAjaxRequest()) Response.Redirect(Request.RawUrl, true);   // processing is finished
                        Views.SetActiveView(FileView);
                        Mime = FileHelper.GetDefaultMime(dataPath);
                        RefreshFile();
                        break;
                    case "downloading":
                        Views.SetActiveView(FileDownloadingView);
                        RefreshDownloading();
                        break;
                    case "decompressing":
                        Views.SetActiveView(FileDecompressingView);
                        break;
                    case "compressing":
                        Views.SetActiveView(FileCompressingView);
                        RefreshCompressing();
                        break;
                    case "converting":
                        Views.SetActiveView(FileConvertingView);
                        RefreshConverting();
                        break;
                }
            }
            else
            {
                Views.SetActiveView(GoneView);
                Response.StatusCode = 404;
            }
        }

        private void Update(DateTime startTime, double percentage, int pid)
        {
            StartTime = startTime.ToChineseString();
            var impossibleEnds = Helper.IsBackgroundRunnerKilled(pid);
            Status = impossibleEnds ? "已被咔嚓（请重新开始任务）" : "正在进行";
            if (impossibleEnds) Never();
            else
            {
                TimeSpan spentTime = DateTime.UtcNow - startTime,
                         remainingTime = new TimeSpan((long)(spentTime.Ticks * (100.0 / percentage - 1)));
                SpentTime = spentTime.ToString("g");
                RemainingTime = remainingTime.ToString("g");
                EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
            }
        }

        private void Never()
        {
            RemainingTime = "永远";
            EndingTime = "地球毁灭时";
        }

        #region Directory

        protected void DirectoryCommand(object source, RepeaterCommandEventArgs e)
        {
            var path = FileHelper.Combine(RelativePath, ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value);
            switch (e.CommandName)
            {
                case "Rename":
                    var newPath = FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode());
                    Directory.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    Directory.Move(Server.GetDataPath(path), Server.GetDataPath(newPath));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void FileCommand(object source, RepeaterCommandEventArgs e)
        {
            var path = FileHelper.Combine(RelativePath, ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value);
            switch (e.CommandName)
            {
                case "Rename":
                    var newPath = FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode());
                    File.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    File.Move(Server.GetDataFilePath(path), Server.GetDataFilePath(newPath));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void NewFolder(object sender, EventArgs e)
        {
            var path = FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode());
            Directory.CreateDirectory(Server.GetFilePath(path));
            Directory.CreateDirectory(Server.GetDataPath(path));
            Response.Redirect(Request.RawUrl);
        }

        protected void Move(object sender, EventArgs e)
        {
            foreach (var dir in DirectoryList.Items.GetSelectedFiles()) Move(dir, false);
            foreach (var file in FileList.Items.GetSelectedFiles()) Move(file, true);
            Response.Redirect(Request.RawUrl);
        }
        private void Move(string fileName, bool isFile)
        {
            string path = FileHelper.Combine(RelativePath, fileName), 
                   dataPath = isFile ? Server.GetDataFilePath(path) : Server.GetDataPath(path),
                   target = FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(path)), targetFile = Server.GetFilePath(target);
            if (Directory.Exists(targetFile) || File.Exists(targetFile)) return;
            Server.CancelControl(dataPath);
            if (isFile)
            {
                File.Move(Server.GetFilePath(path), targetFile);
                File.Move(dataPath, Server.GetDataFilePath(target));
            }
            else
            {
                Directory.Move(Server.GetFilePath(path), targetFile);
                Directory.Move(dataPath, Server.GetDataPath(target));
            }
        }

        protected void Copy(object sender, EventArgs e)
        {
            foreach (var dir in DirectoryList.Items.GetSelectedFiles()) Copy(dir, false);
            foreach (var file in FileList.Items.GetSelectedFiles()) Copy(file, true);
            Response.Redirect(Request.RawUrl);
        }
        private void Copy(string fileName, bool isFile)
        {
            string path = FileHelper.Combine(RelativePath, fileName), 
                   dataPath = isFile ? Server.GetDataFilePath(path) : Server.GetDataPath(path),
                   target = FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(path)), targetFile = Server.GetFilePath(target);
            if (Directory.Exists(targetFile) || File.Exists(targetFile)) return;
            Server.CancelControl(dataPath);
            if (isFile)
            {
                File.Copy(Server.GetFilePath(path), targetFile);
                File.Copy(dataPath, Server.GetDataFilePath(target));
            }
            else
            {
                DirectoryCopy(Server.GetFilePath(path), targetFile);
                DirectoryCopy(dataPath, Server.GetDataPath(target));
            }
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

        protected void Delete(object sender, EventArgs e)
        {
            foreach (var dir in DirectoryList.Items.GetSelectedFiles()) Delete(dir, false);
            foreach (var file in FileList.Items.GetSelectedFiles()) Delete(file, true);
            Response.Redirect(Request.RawUrl);
        }
        private void Delete(string fileName, bool isFile)
        {
            string path = FileHelper.Combine(RelativePath, fileName), 
                   dataPath = isFile ? Server.GetDataFilePath(path) : Server.GetDataPath(path);
            Server.CancelControl(dataPath);
            FileHelper.DeleteWithRetries(dataPath);
            FileHelper.DeleteWithRetries(Server.GetFilePath(path));
        }

        protected void Compress(object sender, EventArgs e)
        {
            var root = new XElement("file", new XAttribute("state", "compressing"), new XAttribute("baseFolder", RelativePath),
                                    new XAttribute("startTime", DateTime.UtcNow.Ticks),
                                    new XAttribute("mime", Helper.GetMimeType(ArchiveFilePath.Text)),
                                    new XAttribute("level", CompressionLevelList.SelectedValue));
            foreach (var dir in DirectoryList.Items.GetSelectedFiles()) root.Add(new XElement("directory", dir));
            foreach (var file in FileList.Items.GetSelectedFiles()) root.Add(new XElement("file", file));
            new XDocument(root).Save(Server.GetDataFilePath(ArchiveFilePath.Text));
            File.WriteAllText(Server.GetFilePath(ArchiveFilePath.Text), string.Empty);  // temp
            Server.StartRunner("compress\n" + ArchiveFilePath.Text);
            Response.Redirect("/Browse/" + ArchiveFilePath.Text.ToCorrectUrl());
        }

        #endregion

        #region File - ready

        protected string Mime, FFmpegResult;

        private void RefreshFile()
        {
            FFmpeg.Initialize(Server);
            FFmpegResult = FFmpeg.Analyze(Server.GetFilePath(RelativePath));
            ConvertPathBox.Text = RelativePath;
            foreach (var codec in FFmpeg.Codecs.Where(codec => codec.EncodingSupported))
            {
                var listItem = new ListItem(codec.ToString(), codec.Name);
                switch (codec.Type)
                {
                    case FFmpeg.CodecType.Video:
                        ConvertVideoCodecBox.Items.Add(listItem);
                        break;
                    case FFmpeg.CodecType.Audio:
                        ConvertAudioCodecBox.Items.Add(listItem);
                        break;
                    case FFmpeg.CodecType.Subtitle:
                        ConvertSubtitleCodecBox.Items.Add(listItem);
                        break;
                }
            }
        }

        protected static string GetMimeType(string mime)
        {
            var extension = Helper.GetDefaultExtension(mime);
            return extension != null ? string.Format("{0} ({1})", mime, extension) : mime;
        }

        protected void ModifyMime(object sender, EventArgs e)
        {
            FileHelper.SetDefaultMime(Server.GetDataFilePath(RelativePath), Hidden.Value.UrlDecode());
            Response.Redirect(Request.RawUrl);
        }

        protected void Decompress(object sender, EventArgs e)
        {
            var id = DateTime.UtcNow.Shorten();
            new XDocument(new XElement("decompress", new XAttribute("archive", RelativePath),
                          new XAttribute("directory", Hidden.Value.UrlDecode()))).Save(Server.GetDataPath(id + ".decompress.task"));
            Server.StartRunner("decompress\n" + id);
            Response.Redirect("/Task/Decompress/" + id);
        }

        private static readonly Regex DurationParser = new Regex("Duration: (.*?),", RegexOptions.Compiled);
        protected void Convert(object sender, EventArgs e)
        {
            var filePath = Server.GetFilePath(ConvertPathBox.Text);
            if (File.Exists(filePath)) return;
            File.WriteAllText(filePath, string.Empty);
            var arguments = string.Empty;
            if (!string.IsNullOrWhiteSpace(ConvertSizeBox.Text)) arguments += " -s " + ConvertSizeBox.Text;
            if (ConvertVideoCodecBox.SelectedIndex != 0) arguments += " -vcodec " + ConvertVideoCodecBox.SelectedItem.Value;
            if (ConvertAudioCodecBox.SelectedIndex != 0) arguments += " -acodec " + ConvertAudioCodecBox.SelectedItem.Value;
            if (ConvertSubtitleCodecBox.SelectedIndex != 0) arguments += " -scodec " + ConvertSubtitleCodecBox.SelectedItem.Value;
            TimeSpan duration = TimeSpan.Parse(DurationParser.Match(FFmpegResult).Groups[1].Value), 
                     start = FFmpeg.Parse(ConvertStartBox.Text), end = FFmpeg.Parse(ConvertEndBox.Text, duration);
            if (start <= TimeSpan.Zero) start = TimeSpan.Zero; else arguments += " -ss " + ConvertStartBox.Text;
            if (end >= duration) end = duration; else arguments += " -to " + ConvertEndBox.Text;
            new XDocument(new XElement("file", new XAttribute("state", "converting"), new XAttribute("input", RelativePath), 
                                       new XAttribute("startTime", DateTime.UtcNow.Ticks), new XAttribute("arguments", arguments),
                                       new XAttribute("duration", (end - start).Ticks),
                                       new XAttribute("mime", Helper.GetMimeType(ConvertPathBox.Text))))
                .Save(Server.GetDataFilePath(ConvertPathBox.Text));
            Server.StartRunner("convert\n" + ConvertPathBox.Text);
            Response.Redirect("/Browse/" + ConvertPathBox.Text.ToCorrectUrl());
        }

        #endregion

        #region File - downloading

        protected string Url, DownloadedFileSize, AverageDownloadSpeed;

        private void RefreshDownloading()
        {
            Url = FileSize = DownloadedFileSize = AverageDownloadSpeed = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            string path = Server.GetFilePath(RelativePath), xmlPath = Server.GetDataFilePath(RelativePath);
            var file = FileHelper.GetElement(xmlPath);
            Url = string.Format("<a href=\"{0}\">{0}</a>", file.GetAttributeValue("url"));
            var startTime = new DateTime(file.GetAttributeValue<long>("startTime"), DateTimeKind.Utc);
            StartTime = startTime.ToChineseString();
            var attr = file.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            attr = file.GetAttributeValue("size");
            if (attr == null) return;
            var fileSize = long.Parse(attr);
            FileSize = Helper.GetSize(fileSize);
            var downloadedFileSize = File.Exists(path) ? new FileInfo(path).Length : 0;
            DownloadedFileSize = string.Format("{0} ({1}%)", Helper.GetSize(downloadedFileSize),
                                    Percentage = (100.0 * downloadedFileSize / fileSize).ToString(CultureInfo.InvariantCulture));
            var impossibleEnds = Helper.IsBackgroundRunnerKilled(file.GetAttributeValueWithDefault<int>("pid"));
            Status = impossibleEnds ? "已被咔嚓（请删除后重新开始任务）" : "正在下载";
            if (impossibleEnds) Never();
            else
            {
                var spentTime = DateTime.UtcNow - startTime;
                SpentTime = spentTime.ToString("g");
                var averageDownloadSpeed = downloadedFileSize / spentTime.TotalSeconds;
                AverageDownloadSpeed = Helper.GetSize(averageDownloadSpeed);
                var remainingTime =
                    new TimeSpan((long)(spentTime.Ticks * (fileSize - downloadedFileSize) / (double)downloadedFileSize));
                RemainingTime = remainingTime.ToString("g");
                EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
            }
        }

        #endregion

        #region File - compressing

        protected string CurrentFile;

        private void RefreshCompressing()
        {
            Status = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            CurrentFile = "无";
            Percentage = "0";
            var xmlPath = Server.GetDataFilePath(RelativePath);
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            var attr = root.GetAttributeValue("progress");
            var pid = root.GetAttributeValueWithDefault<int>("pid");
            if (attr == null)
            {
                Status = pid == 0 ? "正在开始" : "正在初始化";
                attr = root.GetAttributeValue("message");
                if (attr != null)
                {
                    Status = "发生错误，具体信息：" + attr;
                    Never();
                }
                return;
            }
            var percentage = byte.Parse(Percentage = attr);
            attr = root.GetAttributeValue("current");
            if (!string.IsNullOrEmpty(attr))
                //CurrentFile = string.Format("<a href=\"/Browse/{0}\">{1}</a>", FileHelper.Combine(TargetDirectory, attr), attr);
                CurrentFile = attr;
            attr = root.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            Update(new DateTime(root.GetAttributeValueWithDefault<long>("startTime"), DateTimeKind.Utc), percentage, pid);
        }

        #endregion

        #region File - converting

        protected string CurrentTime, Duration;

        private void RefreshConverting()
        {
            Status = CurrentTime = Duration = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            var xmlPath = Server.GetDataFilePath(RelativePath);
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            var attr = root.GetAttributeValue("time");
            var pid = root.GetAttributeValueWithDefault<int>("pid");
            if (attr == null)
            {
                Status = pid == 0 ? "正在开始" : "正在初始化";
                attr = root.GetAttributeValue("message");
                if (attr != null)
                {
                    Status = "发生错误，具体信息：" + attr;
                    Never();
                }
                return;
            }
            TimeSpan time = new TimeSpan(long.Parse(attr)), duration = new TimeSpan(root.GetAttributeValue<long>("duration"));
            CurrentTime = time.ToString("g");
            Duration = duration.ToString("g");
            var percentage = 100.0 * time.Ticks / duration.Ticks;
            Percentage = percentage.ToString(CultureInfo.InvariantCulture);
            FileSize = Helper.GetSize(root.GetAttributeValue<long>("size"));
            attr = root.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            Update(new DateTime(root.GetAttributeValueWithDefault<long>("startTime"), DateTimeKind.Utc), percentage, pid);
        }

        #endregion
    }
}