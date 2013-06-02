using System;
using System.IO;
using System.Web.UI;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class Decompress : Page
    {
        protected string Archive, Status, TargetDirectory, CurrentFile, StartTime, SpentTime, RemainingTime, EndingTime, Percentage;

        protected void Page_Load(object sender, EventArgs e)
        {
            Archive = Status = TargetDirectory = SpentTime = RemainingTime = EndingTime = "未知";
            CurrentFile = "无";
            Percentage = "0";
            string id = RouteData.GetRouteString("ID"), xmlPath = Server.GetDataPath(id + ".decompress.task", false);
            var startTime = Helper.Deshorten(id);
            StartTime = startTime.ToChineseString();
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            Archive = root.GetAttributeValue("archive");
            var targetDirectory = root.GetAttributeValue("directory");
            TargetDirectory = string.Format("<a href=\"/Browse/{0}\">{0}</a>", TargetDirectory);
            var attr = root.GetAttributeValue("progress");
            if (attr == null)
            {
                Status = "正在开始";
                return;
            }
            var percentage = byte.Parse(Percentage = attr);
            attr = root.GetAttributeValue("current");
            if (!string.IsNullOrEmpty(attr))
                CurrentFile = string.Format("<a href=\"/Browse/{0}\">{1}</a>", FileHelper.Combine(targetDirectory, attr), attr);
            attr = root.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            attr = root.GetAttributeValue("finished");
            if (string.IsNullOrEmpty(attr))
            {
                var impossibleEnds = Helper.IsBackgroundRunnerKilled(root.GetAttributeValueWithDefault<int>("pid"));
                Status = impossibleEnds ? "已被咔嚓（请重新开始任务）" : "正在解压";
                if (impossibleEnds) Never();
                else
                {
                    var spentTime = DateTime.UtcNow - startTime;
                    SpentTime = spentTime.ToString("g");
                    var remainingTime = new TimeSpan((long)(spentTime.Ticks * (100 - percentage) / 100.0));
                    RemainingTime = remainingTime.ToString("g");
                    EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
                }
            }
            else
            {
                Status = "解压完毕";
                RemainingTime = new TimeSpan(0).ToString("g");
                var endingTime = new DateTime(long.Parse(attr), DateTimeKind.Utc);
                EndingTime = endingTime.ToChineseString();
                var spentTime = endingTime - startTime;
                SpentTime = spentTime.ToString("g");
            }
        }

        private void Never()
        {
            RemainingTime = "永远";
            EndingTime = "地球毁灭时";
        }

        protected void CleanUp(object sender, EventArgs e)
        {
            foreach (var path in Directory.EnumerateFiles(Server.GetDataPath(string.Empty, false), "*.task"))
            {
                var pid = XHelper.Load(path).Root.GetAttributeValueWithDefault<int>("pid");
                if (pid != 0) Helper.KillProcess(pid);
                FileHelper.DeleteWithRetries(path);
            }
        }
    }
}