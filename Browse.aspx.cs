using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
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
            var absolutePath = FileHelper.GetFilePath(RelativePath);
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
                string dataPath = FileHelper.GetDataFilePath(RelativePath), state = FileHelper.GetFileValue(dataPath, "state");
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
                var spentTime = DateTime.UtcNow - startTime;
                SpentTime = spentTime.ToString("g");
                if (percentage > 0)
                {
                    var remainingTime = new TimeSpan((long)(spentTime.Ticks * (100.0 / percentage - 1)));
                    RemainingTime = remainingTime.ToString("g");
                    EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
                }
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
            switch (e.CommandName)
            {
                case "Rename":
                    FileHelper.Move(FileHelper.Combine(RelativePath, ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value), 
                                    FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void FileCommand(object source, RepeaterCommandEventArgs e)
        {
            switch (e.CommandName)
            {
                case "Rename":
                    FileHelper.Move(FileHelper.Combine(RelativePath, ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value),
                                    FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void NewFolder(object sender, EventArgs e)
        {
            FileHelper.CreateDirectory(FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
            Response.Redirect(Request.RawUrl);
        }

        protected void Move(object sender, EventArgs e)
        {
            foreach (var source in DirectoryList.Items.GetSelectedFiles().Union(FileList.Items.GetSelectedFiles())
                .Select(fileName => FileHelper.Combine(RelativePath, fileName)))
                FileHelper.Move(source, FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(source)), false);
            Response.Redirect(Request.RawUrl);
        }
        protected void Copy(object sender, EventArgs e)
        {
            foreach (var source in DirectoryList.Items.GetSelectedFiles().Union(FileList.Items.GetSelectedFiles())
                .Select(fileName => FileHelper.Combine(RelativePath, fileName)))
                FileHelper.Copy(source, FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(source)), false);
            Response.Redirect(Request.RawUrl);
        }
        protected void Delete(object sender, EventArgs e)
        {
            foreach (var path in DirectoryList.Items.GetSelectedFiles().Union(FileList.Items.GetSelectedFiles())
                .Select(fileName => FileHelper.Combine(RelativePath, fileName))) FileHelper.Delete(path);
            Response.Redirect(Request.RawUrl);
        }

        protected void Compress(object sender, EventArgs e)
        {
            TaskHelper.CreateCompress(ArchiveFilePath.Text, DirectoryList.Items.GetSelectedFiles()
                .Union(FileList.Items.GetSelectedFiles()), RelativePath, CompressionLevelList.SelectedValue);
            Response.Redirect("/Browse/" + ArchiveFilePath.Text.ToCorrectUrl());
        }

        private static readonly Regex AppParser = new Regex(@"^http:\/\/(.*?)\/Browse\/(.*?)$", RegexOptions.Compiled);
        protected void CrossAppCopy(object sender, EventArgs e)
        {
            var match = AppParser.Match(Hidden.Value);
            if (!match.Success) return;
            Response.Redirect("/Task/CrossAppCopy/" +
                TaskHelper.CreateCrossAppCopy(match.Groups[1].Value, match.Groups[2].Value.UrlDecode(), RelativePath));
        }

        #endregion

        #region File - ready

        protected string Mime, FFmpegResult;

        private void RefreshFile()
        {
            FFmpegResult = FFmpeg.Analyze(FileHelper.GetFilePath(RelativePath));
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
            FileHelper.SetDefaultMime(FileHelper.GetDataFilePath(RelativePath), Hidden.Value.UrlDecode());
            Response.Redirect(Request.RawUrl);
        }

        protected void Decompress(object sender, EventArgs e)
        {
            Response.Redirect("/Task/Decompress/" + TaskHelper.CreateDecompress(RelativePath, Hidden.Value.UrlDecode()));
        }

        protected void Convert(object sender, EventArgs e)
        {
            TaskHelper.CreateConvert(RelativePath, ConvertPathBox.Text, ConvertSizeBox.Text, ConvertVideoCodecBox.SelectedValue, 
                                 ConvertAudioCodecBox.SelectedValue, ConvertSubtitleCodecBox.SelectedValue, 
                                 ConvertStartBox.Text, ConvertEndBox.Text);
            Response.Redirect("/Browse/" + ConvertPathBox.Text.ToCorrectUrl());
        }

        #endregion

        #region File - downloading

        protected string Url, DownloadedFileSize, AverageDownloadSpeed;

        private void RefreshDownloading()
        {
            Url = FileSize = DownloadedFileSize = AverageDownloadSpeed = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            string path = FileHelper.GetFilePath(RelativePath), xmlPath = FileHelper.GetDataFilePath(RelativePath);
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
                if (downloadedFileSize > 0)
                {
                    var remainingTime =
                        new TimeSpan((long)(spentTime.Ticks * (fileSize - downloadedFileSize) / (double)downloadedFileSize));
                    RemainingTime = remainingTime.ToString("g");
                    EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
                }
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
            var xmlPath = FileHelper.GetDataFilePath(RelativePath);
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
            if (!string.IsNullOrEmpty(attr)) CurrentFile = string.Format("<a href=\"/Browse/{0}\">{1}</a>", 
                FileHelper.Combine(root.GetAttributeValue("baseFolder"), attr), attr);
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
            var xmlPath = FileHelper.GetDataFilePath(RelativePath);
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