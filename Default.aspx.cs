using System;
using System.Diagnostics;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Mygod.Skylark
{
    public partial class Default : Page
    {
        protected string RelativePath;
        protected DirectoryInfo InfoDirectory;
        protected FileInfo InfoFile;

        protected void Page_Load(object sender, EventArgs e)
        {
            RelativePath = Context.GetRelativePath();
            var absolutePath = Path.Combine(Server.MapPath("~/Files/"), RelativePath);
            Title = ("浏览 " + RelativePath).TrimEnd();
            InfoDirectory = new DirectoryInfo(absolutePath);
            InfoFile = new FileInfo(absolutePath);
            if (IsPostBack || !InfoDirectory.Exists) return;
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
            var path = Helper.Combine(RelativePath, e.CommandArgument.ToString());
            switch (e.CommandName)
            {
                case "Delete":
                    Directory.Delete(Server.GetFilePath(path), true);
                    Directory.Delete(Server.GetDataPath(path), true);
                    Response.Redirect(Request.RawUrl);
                    break;
                case "Rename":
                    var newPath = Helper.Combine(RelativePath, Hidden.Value);
                    Directory.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    Directory.Move(Server.GetDataPath(path), Server.GetDataPath(newPath));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void FileCommand(object source, RepeaterCommandEventArgs e)
        {
            var path = Helper.Combine(RelativePath, e.CommandArgument.ToString());
            switch (e.CommandName)
            {
                case "Delete":
                    File.Delete(Server.GetFilePath(path));
                    File.Delete(Server.GetDataPath(path));
                    Response.Redirect(Request.RawUrl);
                    break;
                case "Rename":
                    var newPath = Helper.Combine(RelativePath, Hidden.Value);
                    File.Move(Server.GetFilePath(path), Server.GetFilePath(newPath));
                    File.Move(Server.GetDataPath(path), Server.GetDataPath(newPath));
                    Response.Redirect(Request.RawUrl);
                    break;
            }
        }

        protected void NewFolder(object sender, EventArgs e)
        {
            var path = Helper.Combine(RelativePath, Hidden.Value);
            Directory.CreateDirectory(Server.GetFilePath(path));
            Directory.CreateDirectory(Server.GetDataPath(path));
            Response.Redirect(Request.RawUrl);
        }

        protected void OfflineDownload(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(Server.MapPath("~/MygodOfflineDownloader.exe"),
                string.Format("\"{0}\" \"{1}\"", Hidden.Value, RelativePath)) { WorkingDirectory = Server.MapPath("~") });
            Response.Redirect(Request.RawUrl);
        }

        protected static string GetMimeType(string mime)
        {
            var extension = Helper.GetDefaultExtension(mime);
            return extension != null ? string.Format("{0} ({1})", mime, extension) : mime;
        }

        protected void ModifyMime(object sender, EventArgs e)
        {
            Helper.SetDefaultMime(Server.GetDataPath(RelativePath), Hidden.Value);
            Response.Redirect(Request.RawUrl);
        }
    }
}