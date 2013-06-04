using System;

namespace Mygod.Skylark
{
    public partial class View : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = RouteData.GetRelativePath(), dataPath = Server.GetDataPath(path), 
                   mime = (RouteData.GetRouteString("Mime") ?? string.Empty).Trim('/');
            if (string.IsNullOrWhiteSpace(mime)) mime = FileHelper.GetDefaultMime(dataPath);
            try
            {
                FileHelper.WaitForReady(dataPath, 10);
                TransmitFile(Server.GetFilePath(path), mime: mime);
            }
            catch
            {
                Response.StatusCode = 503;
                Response.StatusDescription = "文件尚在处理中，请稍后再试";
            }
        }
    }
}