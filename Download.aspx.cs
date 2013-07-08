using System;
using System.IO;

namespace Mygod.Skylark
{
    public partial class Download : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = RouteData.GetRelativePath(), filePath = FileHelper.GetFilePath(path), 
                   dataPath = FileHelper.GetDataFilePath(path);
            if (!File.Exists(dataPath))
            {
                Response.StatusCode = 404;
                return;
            }
            try
            {
                int timeout;
                if (!int.TryParse(Request.QueryString["Timeout"], out timeout)) timeout = 10;
                FileHelper.WaitForReady(dataPath, timeout);
                TransmitFile(filePath, Path.GetFileName(filePath));
            }
            catch
            {
                Response.StatusCode = 503;
                Response.StatusDescription = "File is not ready yet";
            }
        }
    }
}