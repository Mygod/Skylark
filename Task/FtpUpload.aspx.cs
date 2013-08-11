using System;
using System.IO;
using System.Web.UI;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class FtpUpload : Page
    {
        protected string Target, Status, CurrentFile, StartTime, SpentTime, RemainingTime, EndingTime, Percentage,
                         FileSize, UploadedFileSize, AverageUploadSpeed;

        protected void Page_Load(object sender, EventArgs e)
        {
            FileSize = UploadedFileSize = AverageUploadSpeed = Target = Status = SpentTime = RemainingTime = EndingTime = "未知";
            CurrentFile = "无";
            Percentage = "0";
            string id = RouteData.GetRouteString("ID"), xmlPath = FileHelper.GetDataPath(id + ".ftpUpload.task");
            var startTime = Helper.Deshorten(id);
            StartTime = startTime.ToChineseString();
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            var source = root.GetAttributeValue("source");
            Target = string.Format("<a href=\"{0}\">{0}</a>", root.GetAttributeValue("target"));
            var attr = root.GetAttributeValue("total");
            if (attr == null)
            {
                Status = "正在开始";
                return;
            }
            var total = long.Parse(attr);
            FileSize = Helper.GetSize(total);
            var completed = long.Parse(root.GetAttributeValue("completed"));
            UploadedFileSize = Helper.GetSize(completed);
            attr = root.GetAttributeValue("current");
            if (!string.IsNullOrEmpty(attr))
                CurrentFile = string.Format("<a href=\"/Browse/{0}\">{0}</a>", FileHelper.Combine(source, attr));
            attr = root.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            attr = root.GetAttributeValue("finishTime");
            if (string.IsNullOrEmpty(attr))
            {
                var impossibleEnds = TaskHelper.IsBackgroundRunnerKilled(root.GetAttributeValueWithDefault<int>("pid"));
                Status = impossibleEnds ? "已被咔嚓（请重新开始任务）" : "正在上传";
                if (impossibleEnds) Never();
                else
                {
                    var spentTime = DateTime.UtcNow - startTime;
                    SpentTime = spentTime.ToString("g");
                    var averageUploadSpeed = completed / spentTime.TotalSeconds;
                    AverageUploadSpeed = Helper.GetSize(averageUploadSpeed);
                    if (completed > 0)
                    {
                        var remainingTime =
                            new TimeSpan((long)(spentTime.Ticks * (total - completed) / (double)completed));
                        RemainingTime = remainingTime.ToString("g");
                        EndingTime = (startTime + spentTime + remainingTime).ToChineseString();
                    }
                }
            }
            else
            {
                Status = "下载完毕";
                RemainingTime = new TimeSpan(0).ToString("g");
                var endingTime = new DateTime(long.Parse(attr), DateTimeKind.Utc);
                EndingTime = endingTime.ToChineseString();
                var spentTime = endingTime - startTime;
                SpentTime = spentTime.ToString("g");
                AverageUploadSpeed = Helper.GetSize(completed / spentTime.TotalSeconds);
            }
        }

        private void Never()
        {
            RemainingTime = "永远";
            EndingTime = "地球毁灭时";
        }

        protected void CleanUp(object sender, EventArgs e)
        {
            TaskHelper.CleanUp();
        }
    }
}