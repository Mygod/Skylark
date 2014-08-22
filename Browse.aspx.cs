using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Mygod.Skylark
{
    public partial class Browse : Page
    {
        protected string RelativePath;
        protected DirectoryInfo InfoDirectory;
        protected FileInfo InfoFile;
        protected CloudTask Task;

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

        protected User CurrentUser;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!(CurrentUser = Request.GetUser()).Browse)
            {
                Response.StatusCode = 401;
                return;
            }
            if (InfoDirectory.Exists)
            {
                Views.SetActiveView(DirectoryView);
                if (IsPostBack) return;
                var dirs = InfoDirectory.EnumerateDirectories().ToList();
                DirectoryList.DataSource = dirs;
                DirectoryList.DataBind();
                DirectoryCount = dirs.Count.ToString(CultureInfo.InvariantCulture);
                var files = InfoDirectory.EnumerateFiles().ToList();
                FileList.DataSource = files;
                FileList.DataBind();
                FileCount = files.Count.ToString(CultureInfo.InvariantCulture);
                ArchiveFilePath.Text = string.IsNullOrWhiteSpace(RelativePath) ? "Files.7z" : (RelativePath + ".7z");
            }
            else if (InfoFile.Exists)
            {
                string dataPath = FileHelper.GetDataFilePath(RelativePath), state = FileHelper.GetFileValue(dataPath, "state");
                Task = GenerateFileTask.Create(RelativePath);
                if (Task == null)
                    if (state == TaskType.NoTask)
                    {
                        if (Request.IsAjaxRequest()) Response.Redirect(Request.RawUrl, true); // processing is finished
                        Views.SetActiveView(FileView);
                        Mime = FileHelper.GetDefaultMime(dataPath);
                        RefreshFile();
                    }
                    else Views.SetActiveView(GeneralTaskProcessingView);
                else if (state == TaskType.UploadTask) Views.SetActiveView(FileUploadingView);
                else
                {
                    Viewer.SetTask(Task);
                    Views.SetActiveView(FileProcessingView);
                }
            }
            else
            {
                Views.SetActiveView(GoneView);
                Response.StatusCode = 404;
            }
        }

        #region Directory

        protected string DirectoryCount, FileCount;
        private IEnumerable<string> SelectedPaths
        {
            get
            {
                return DirectoryList.Items.GetSelectedItemsID()
                    .Union(FileList.Items.GetSelectedItemsID()).Select(name => FileHelper.Combine(RelativePath, name));
            }
        }

        protected void DirectoryCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            switch (e.CommandName)
            {
                case "Rename":
                    FileHelper.Move(FileHelper.Combine(RelativePath,
                                                       ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value), 
                                    FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void FileCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            switch (e.CommandName)
            {
                case "Rename":
                    FileHelper.Move(FileHelper.Combine(RelativePath,
                                                       ((HtmlInputHidden)e.Item.FindControl("Hidden")).Value),
                                    FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void NewFolder(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            FileHelper.CreateDirectory(FileHelper.Combine(RelativePath, Hidden.Value.UrlDecode()));
            Response.Redirect(Request.RawUrl);
        }

        protected void Move(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            foreach (var source in SelectedPaths) FileHelper.Move(source,
                FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(source)), false);
            Response.Redirect(Request.RawUrl);
        }
        protected void Copy(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            foreach (var source in SelectedPaths) FileHelper.Copy(source,
                FileHelper.Combine(Hidden.Value.UrlDecode(), Path.GetFileName(source)), false);
            Response.Redirect(Request.RawUrl);
        }
        protected void Delete(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            foreach (var path in SelectedPaths) FileHelper.Delete(path);
            Response.Redirect(Request.RawUrl);
        }

        protected void Compress(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            new CompressTask(ArchiveFilePath.Text, SelectedPaths, RelativePath,
                             CompressionLevelList.SelectedValue).Start();
            Response.Redirect("/Browse/" + ArchiveFilePath.Text.ToCorrectUrl());
        }

        private static readonly Regex
            AppParser = new Regex(@"^http:\/\/((.*?)@)?(.*?)\/Browse\/(.*)$", RegexOptions.Compiled);
        protected void CrossAppCopy(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            var match = AppParser.Match(Hidden.Value);
            if (!match.Success) return;
            var task = new CrossAppCopyTask(match.Groups[3].Value, match.Groups[4].Value.UrlDecode(), RelativePath,
                                            match.Groups[2].Success ? match.Groups[2].Value : Request.GetPassword());
            task.Start();
            Response.Redirect("/Task/Details/" + task.ID);
        }

        protected void FtpUpload(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            Response.Redirect("/Task/Details/" + new FtpUploadTask(RelativePath, SelectedPaths, Hidden.Value));
        }

        #endregion

        #region File

        protected string Mime, FFmpegResult;

        private void RefreshFile()
        {
            FFmpegResult = FFmpeg.Analyze(FileHelper.GetFilePath(RelativePath));
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
            if (!CurrentUser.OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            FileHelper.SetDefaultMime(FileHelper.GetDataFilePath(RelativePath), Hidden.Value.UrlDecode());
            Response.Redirect(Request.RawUrl);
        }

        protected void Decompress(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            var task = new DecompressTask(RelativePath, Hidden.Value.UrlDecode());
            task.Start();
            Response.Redirect("/Task/Details/" + task.ID);
        }

        protected void Convert(object sender, EventArgs e)
        {
            if (!CurrentUser.OperateTasks)
            {
                Response.StatusCode = 401;
                return;
            }
            ConvertTask.Create(RelativePath, ConvertPathBox.Text, ConvertSizeBox.Text,
                               ConvertVideoCodecBox.SelectedValue, ConvertAudioCodecBox.SelectedValue,
                               ConvertSubtitleCodecBox.SelectedValue, ConvertAudioPathBox.Text,
                               ConvertStartBox.Text, ConvertEndBox.Text);
            Response.Redirect("/Browse/" + ConvertPathBox.Text.ToCorrectUrl());
        }

        #endregion
    }
}