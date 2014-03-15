using System;
using System.IO;
using System.Threading;

namespace Mygod.Skylark
{
    public partial class Download : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Download)
            {
                Response.StatusCode = 401;
                return;
            }
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
            catch (ThreadAbortException)
            {
            }
        }
    }
}