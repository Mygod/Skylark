using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Mygod.Skylark
{
    public partial class Default : Page
    {
        protected string RelativePath;
        protected DirectoryInfo InfoDirectory;
        protected FileInfo InfoFile;

        protected void Page_PreInit(object sender, EventArgs e)
        {
            RelativePath = Context.GetRelativePath();
            var absolutePath = Path.Combine(Server.MapPath("~/Files/"), RelativePath);
            Title = ("浏览 " + RelativePath).TrimEnd();
            InfoDirectory = new DirectoryInfo(absolutePath);
            InfoFile = new FileInfo(absolutePath);
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!IsPostBack) RebindData();
            if (InfoFile.Exists
                && FileHelper.GetFileValue(Server.GetDataPath(RelativePath), "state").StartsWith("download", StringComparison.Ordinal))
                RefreshOffline();
        }

        #region Directory

        private void RebindData()
        {
            if (!InfoDirectory.Exists) return;
            DirectoryList.DataSource = InfoDirectory.EnumerateDirectories();
            DirectoryList.DataBind();
            FileList.DataSource = InfoDirectory.EnumerateFiles();
            FileList.DataBind();
        }

        protected void WritePath()
        {
            var dirs = RelativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var tempPath = string.Empty;
            foreach (var dir in dirs)
            {
                tempPath += '/' + dir;
                Response.Write(" &gt; <a href=\"?" + tempPath);
                Response.Write("\">" + dir + "</a>");
            }
            if (InfoDirectory.Exists) Response.Write(" &gt;");
        }

        protected void DirectoryCommand(object source, RepeaterCommandEventArgs e)
        {
            var path = FileHelper.Combine(RelativePath, ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value);
            switch (e.CommandName)
            {
                case "Rename":
                    var newPath = FileHelper.Combine(RelativePath, Hidden.Value);
                    Directory.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    Directory.Move(Server.GetDataPath(path), Server.GetDataPath(newPath, false));
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

        #endregion

        #region File - downloading

        protected string Url, Status, FileSize, DownloadedFileSize, AverageDownloadSpeed, StartTime, SpentTime, RemainingTime,
                         EndingTime, Percentage;

        private void RefreshOffline()
        {
            Url = FileSize = DownloadedFileSize = AverageDownloadSpeed = StartTime = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            string path = Server.GetFilePath(RelativePath), xmlPath = Server.GetDataPath(RelativePath);
            var file = FileHelper.GetElement(xmlPath);
            Url = string.Format("<a href=\"{0}\">{0}</a>", file.Attribute("url").Value);
            var attr = file.Attribute("startTime");
            var startTime = Helper.Parse(attr.Value);
            StartTime = startTime.ToChineseString();
            attr = file.Attribute("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr.Value;
                Never();
                return;
            }
            attr = file.Attribute("size");
            if (attr == null) return;
            var fileSize = long.Parse(attr.Value);
            FileSize = Helper.GetSize(fileSize);
            var downloadedFileSize = File.Exists(path) ? new FileInfo(path).Length : 0;
            DownloadedFileSize = string.Format("{0} ({1}%)", Helper.GetSize(downloadedFileSize),
                                    Percentage = (100.0 * downloadedFileSize / fileSize).ToString(CultureInfo.InvariantCulture));
            attr = file.Attribute("endTime");
            if (attr == null)
            {
                bool impossibleEnds;
                try
                {
                    impossibleEnds = Process.GetProcessById(int.Parse(file.Attribute("id").Value)).ProcessName != "MygodOfflineDownloader";
                }
                catch
                {
                    impossibleEnds = true;
                }
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
                var endingTime = Helper.Parse(attr.Value);
                EndingTime = endingTime.ToChineseString();
                var spentTime = endingTime - startTime;
                SpentTime = spentTime.ToString("g");
                var averageDownloadSpeed = downloadedFileSize / spentTime.TotalSeconds;
                AverageDownloadSpeed = Helper.GetSize(averageDownloadSpeed);
            }
        }

        private void Never()
        {
            RemainingTime = "永远";
            EndingTime = "地球毁灭时";
        }

        #endregion
    }
}