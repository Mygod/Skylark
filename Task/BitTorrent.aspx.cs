using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class BitTorrent : Page
    {
        protected string Torrent, Status, TargetDirectory, StartTime, SpentTime, RemainingTime, EndingTime,
                         FileSize, DownloadedFileSize, AverageDownloadSpeed, Percentage;

        protected void Page_Load(object sender, EventArgs e)
        {
            Torrent = FileSize = DownloadedFileSize = AverageDownloadSpeed = Status = TargetDirectory
                = SpentTime = RemainingTime = EndingTime = "未知";
            Percentage = "0";
            string id = RouteData.GetRouteString("ID"), xmlPath = FileHelper.GetDataPath(id + ".bitTorrent.task");
            var startTime = Helper.Deshorten(id);
            StartTime = startTime.ToChineseString();
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            Torrent = root.GetAttributeValue("torrent");
            var targetDirectory = root.GetAttributeValue("directory");
            TargetDirectory = string.Format("<a href=\"/Browse/{0}\">{0}</a>", targetDirectory);
            var attr = root.GetAttributeValue("message");
            if (attr != null)
            {
                Status = "发生错误，具体信息：" + attr;
                Never();
                return;
            }
            attr = root.GetAttributeValue("length");
            if (attr == null)
            {
                Status = "正在开始";
                return;
            }
            attr = root.GetAttributeValue("length");
            if (attr == null) return;
            var fileSize = long.Parse(attr);
            FileSize = Helper.GetSize(fileSize);
            var downloadedFileSize = root.GetAttributeValue<long>("downloaded");
            DownloadedFileSize = string.Format("{0} ({1}%)", Helper.GetSize(downloadedFileSize),
                                    Percentage = (100.0 * downloadedFileSize / fileSize).ToString(CultureInfo.InvariantCulture));
            attr = root.GetAttributeValue("finished");
            if (string.IsNullOrEmpty(attr))
            {
                var impossibleEnds = TaskHelper.IsBackgroundRunnerKilled(root.GetAttributeValueWithDefault<int>("pid"));
                Status = impossibleEnds ? "已被咔嚓（请删除后重新开始任务）" : "正在下载";
                if (impossibleEnds) Never();
                else
                {
                    var spentTime = DateTime.UtcNow - startTime;
                    SpentTime = spentTime.ToString("g");
                    var averageDownloadSpeed = downloadedFileSize / spentTime.TotalSeconds;
                    AverageDownloadSpeed = Helper.GetSize(averageDownloadSpeed);
                    if (downloadedFileSize > 0)
                    {
                        var remainingTime =
                            new TimeSpan((long) (spentTime.Ticks * (fileSize - downloadedFileSize) / (double) downloadedFileSize));
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
                AverageDownloadSpeed = Helper.GetSize(fileSize / spentTime.TotalSeconds);
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