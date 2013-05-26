using System;
using System.IO;
using System.Threading;

namespace Mygod.Skylark
{
    public partial class Download : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = RouteData.GetRelativePath(), dataPath = Server.GetDataPath(path);
            if (!File.Exists(dataPath))
            {
                Response.StatusCode = 404;
                return;
            }
            while (!FileHelper.IsReady(dataPath)) Thread.Sleep(1000);   // keep sleeping until finished or being aborted
            DownloadFile(Server.GetFilePath(path));
        }
    }
}