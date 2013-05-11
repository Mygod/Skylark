using System;
using System.Threading;

namespace Mygod.Skylark
{
    public partial class Download : DownloadablePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string path = Context.GetRelativePath(), dataPath = Server.GetDataPath(path);
            while (!Helper.IsReady(dataPath)) Thread.Sleep(1000);   // keep sleeping until finished or being aborted
            DownloadFile(Server.GetFilePath(path));
        }
    }
}