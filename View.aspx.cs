using System;

namespace Mygod.Skylark
{
    public partial class View : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = RouteData.GetRelativePath(), dataPath = FileHelper.GetDataFilePath(path), 
                   mime = (RouteData.GetRouteString("Mime") ?? string.Empty).Trim('/');
            if (string.IsNullOrWhiteSpace(mime)) mime = FileHelper.GetDefaultMime(dataPath);
            try
            {
                int timeout;
                if (!int.TryParse(Request.QueryString["Timeout"], out timeout)) timeout = 10;
                FileHelper.WaitForReady(dataPath, timeout);
                TransmitFile(FileHelper.GetFilePath(path), mime: mime);
            }
            catch
            {
                Response.StatusCode = 503;
                Response.StatusDescription = "File is not ready yet";
            }
        }
    }
}