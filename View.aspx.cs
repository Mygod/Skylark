using System;
using System.Threading;

namespace Mygod.Skylark
{
    public partial class View : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Download)
            {
                Response.StatusCode = 401;
                return;
            }
            string path = RouteData.GetRelativePath(), dataPath = FileHelper.GetDataFilePath(path), 
                   mime = Request.QueryString["Mime"];
            if (string.IsNullOrWhiteSpace(mime)) mime = FileHelper.GetDefaultMime(dataPath);
            try
            {
                int timeout;
                if (!int.TryParse(Request.QueryString["Timeout"], out timeout)) timeout = 10;
                FileHelper.WaitForReady(dataPath, timeout);
                TransmitFile(FileHelper.GetFilePath(path), mime: mime);
            }
            catch (ThreadAbortException)
            {
            }
        }
    }
}