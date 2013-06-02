using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public partial class Browse : Page
    {
        protected string RelativePath, Mime, Status, StartTime, SpentTime, RemainingTime, EndingTime, Percentage;
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
                ArchiveFilePath.Text = RelativePath + ".7z";
            }
            else if (InfoFile.Exists)
            {
                string dataPath = Server.GetDataPath(RelativePath), state = FileHelper.GetFileValue(dataPath, "state");
                switch (state)
                {
                    case "ready":
                        if (Request.IsAjaxRequest()) Response.Redirect(Request.RawUrl, true);
                        Views.SetActiveView(FileView);
                        Mime = FileHelper.GetDefaultMime(dataPath);
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
                }
            }
            else
            {
                Views.SetActiveView(GoneView);
                Response.StatusCode = 404;
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
                    var newPath = FileHelper.Combine(RelativePath, Hidden.Value);
                    Directory.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    Directory.Move(Server.GetDataPath(path, false), Server.GetDataPath(newPath, false));
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
                    var newPath = FileHelper.Combine(RelativePath, Hidden.Value);
                    File.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    File.Move(Server.GetDataPath(path), Server.GetDataPath(newPath));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void NewFolder(object sender, EventArgs e)
        {
            var path = FileHelper.Combine(RelativePath, Hidden.Value);
            Directory.CreateDirectory(Server.GetFilePath(path));
            Directory.CreateDirectory(Server.GetDataPath(path, false));
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
            string path = FileHelper.Combine(RelativePath, fileName), dataPath = Server.GetDataPath(path, isFile), 
                   target = FileHelper.Combine(Hidden.Value, Path.GetFileName(path)), targetFile = Server.GetFilePath(target);
            if (Directory.Exists(targetFile) || File.Exists(targetFile)) return;
            Server.CancelControl(dataPath);
            if (isFile)
            {
                File.Move(Server.GetFilePath(path), targetFile);
                File.Move(dataPath, Server.GetDataPath(target));
            }
            else
            {
                Directory.Move(Server.GetFilePath(path), targetFile);
                Directory.Move(dataPath, Server.GetDataPath(target, false));
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
            string path = FileHelper.Combine(RelativePath, fileName), dataPath = Server.GetDataPath(path, isFile),
                   target = FileHelper.Combine(Hidden.Value, Path.GetFileName(path)), targetFile = Server.GetFilePath(target);
            if (Directory.Exists(targetFile) || File.Exists(targetFile)) return;
            Server.CancelControl(dataPath);
            if (isFile)
            {
                File.Copy(Server.GetFilePath(path), targetFile);
                File.Copy(dataPath, Server.GetDataPath(target));
            }
            else
            {
                DirectoryCopy(Server.GetFilePath(path), targetFile);
                DirectoryCopy(dataPath, Server.GetDataPath(target, false));
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
            string path = FileHelper.Combine(RelativePath, fileName), dataPath = Server.GetDataPath(path, isFile);
            Server.CancelControl(dataPath);
            FileHelper.DeleteWithRetries(dataPath);
            FileHelper.DeleteWithRetries(Server.GetFilePath(path));
        }

        protected void Compress(object sender, EventArgs e)
        {
            var root = new XElement("file", new XAttribute("state", "compressing"), new XAttribute("baseFolder", RelativePath),
                                    new XAttribute("startTime", DateTime.UtcNow.Ticks),
                                    new XAttribute("level", CompressionLevelList.SelectedValue));
            foreach (var dir in DirectoryList.Items.GetSelectedFiles()) root.Add(new XElement("directory", dir));
            foreach (var file in FileList.Items.GetSelectedFiles()) root.Add(new XElement("file", file));
            new XDocument(root).Save(Server.GetDataPath(ArchiveFilePath.Text));
            File.WriteAllText(Server.GetFilePath(ArchiveFilePath.Text), string.Empty);  // temp
            Server.StartRunner("compress\n" + ArchiveFilePath.Text);
            Response.Redirect("/Browse/" + ArchiveFilePath.Text.ToCorrectUrl());
        }

        #endregion

        #region File - ready

        protected static string GetMimeType(string mime)
        {
            var extension = Helper.GetDefaultExtension(mime);
            return extension != null ? string.Format("{0} ({1})", mime, extension) : mime;
        }

        protected void ModifyMime(object sender, EventArgs e)
        {
            FileHelper.SetDefaultMime(Server.GetDataPath(RelativePath), Hidden.Value);
            Response.Redirect(Request.RawUrl);
        }

        protected void Decompress(object sender, EventArgs e)
        {
            var id = DateTime.UtcNow.Shorten();
            new XDocument(new XElement("decompress", new XAttribute("archive", RelativePath), 
                          new XAttribute("directory", Hidden.Value))).Save(Server.GetDataPath(id + ".decompress.task", false));
            Server.StartRunner("decompress\n" + id);
            Response.Redirect("/Task/Decompress/" + id);
        }

        #endregion

        #region File - downloading

        protected string Url, FileSize, DownloadedFileSize, AverageDownloadSpeed;

        private void RefreshDownloading()
        {
            Url = FileSize = DownloadedFileSize = AverageDownloadSpeed = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            string path = Server.GetFilePath(RelativePath), xmlPath = Server.GetDataPath(RelativePath);
            var file = FileHelper.GetElement(xmlPath);
            Url = string.Format("<a href=\"{0}\">{0}</a>", file.GetAttributeValue("url"));
            var attr = file.GetAttributeValue("startTime");
            var startTime = Helper.Parse(attr);
            StartTime = startTime.ToChineseString();
            attr = file.GetAttributeValue("message");
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
            attr = file.GetAttributeValue("endTime");
            if (attr == null)
            {
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
            else
            {
                Status = "下载完毕，刷新开始下载";
                RemainingTime = new TimeSpan(0).ToString("g");
                var endingTime = Helper.Parse(attr);
                EndingTime = endingTime.ToChineseString();
                var spentTime = endingTime - startTime;
                SpentTime = spentTime.ToString("g");
                var averageDownloadSpeed = downloadedFileSize / spentTime.TotalSeconds;
                AverageDownloadSpeed = Helper.GetSize(averageDownloadSpeed);
            }
        }

        #endregion

        #region File - compressing

        protected string CurrentFile;

        private void RefreshCompressing()
        {
            Url = CurrentFile = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            CurrentFile = "无";
            Percentage = "0";
            var xmlPath = Server.GetDataPath(RelativePath);
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
            var startTime = new DateTime(root.GetAttributeValueWithDefault<long>("startTime"), DateTimeKind.Utc);
            attr = root.GetAttributeValue("finishTime");
            if (string.IsNullOrEmpty(attr))
            {
                var impossibleEnds = Helper.IsBackgroundRunnerKilled(pid);
                Status = impossibleEnds ? "已被咔嚓（请重新开始任务）" : "正在压缩";
                if (impossibleEnds) Never();
                else
                {
                    var spentTime = DateTime.UtcNow - startTime;
                    SpentTime = spentTime.ToString("g");
                    var remainingTime = new TimeSpan((long)(spentTime.Ticks * (100 - percentage) / 100.0));
                    RemainingTime = remainingTime.ToString("g");
                    EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
                }
            }
            else
            {
                Status = "压缩完毕";
                RemainingTime = new TimeSpan(0).ToString("g");
                var endingTime = new DateTime(long.Parse(attr), DateTimeKind.Utc);
                EndingTime = endingTime.ToChineseString();
                var spentTime = endingTime - startTime;
                SpentTime = spentTime.ToString("g");
            }
        }

        #endregion
    }
}