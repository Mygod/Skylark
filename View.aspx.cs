using System;
using System.Threading;

namespace Mygod.Skylark
{
    public partial class View : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = Context.GetRelativePath(), dataPath = Server.GetDataPath(path), 
                   mime = (RouteData.GetRouteString("Mime") ?? string.Empty).Trim('/');
            if (string.IsNullOrWhiteSpace(mime)) mime = FileHelper.GetDefaultMime(dataPath);
            while (!FileHelper.IsReady(dataPath)) Thread.Sleep(1000);   // keep sleeping until finished or being aborted
            DownloadFile(Server.GetFilePath(path), mime, true);
        }
    }
}