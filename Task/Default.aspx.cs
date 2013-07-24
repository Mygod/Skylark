using System;
using System.Globalization;
using System.IO;
using System.Web.UI;

namespace Mygod.Skylark.Task
{
    public partial class Default : Page
    {
        protected string LogSize;
        private string logPath;

        protected void Page_Load(object sender, EventArgs e)
        {
            var info = new FileInfo(logPath = FileHelper.GetDataPath("error.log"));
            LogSize = Helper.GetSize(info.Exists ? info.Length : 0);
            if (!IsPostBack) WorkerCountBox.Text = Config.MaxWorkerCount.ToString(CultureInfo.InvariantCulture);
        }

        protected void UpdateArguments(object sender, EventArgs e)
        {
            Config.MaxWorkerCount = long.Parse(WorkerCountBox.Text);
        }

        protected void DestroyLog(object sender, EventArgs e)
        {
            FileHelper.DeleteWithRetries(logPath);
        }
    }
}