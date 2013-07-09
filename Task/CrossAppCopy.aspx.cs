using System;
using System.Globalization;
using System.IO;
using System.Web.UI;
using Mygod.Xml.Linq;

namespace Mygod.Skylark.Task
{
    public partial class CrossAppCopy : Page
    {
        protected string Status, Source, Target, CurrentFile, FileCopied, SizeCopied, StartTime, SpentTime, EndingTime, Message;

        protected void Page_Load(object sender, EventArgs e)
        {
            Status = Source = Target = CurrentFile = SpentTime = EndingTime = "未知";
            FileCopied = "0";
            SizeCopied = Helper.GetSize(0);
            Message = string.Empty;
            string id = RouteData.GetRouteString("ID"), xmlPath = FileHelper.GetDataPath(id + ".crossAppCopy.task");
            var startTime = Helper.Deshorten(id);
            StartTime = startTime.ToChineseString();
            if (!File.Exists(xmlPath)) return;
            var root = XHelper.Load(xmlPath).Root;
            Source = string.Format("<a href=\"{0}\">{0}</a>", string.Format("http://{0}/Browse/{1}",
                root.GetAttributeValue("domain"), root.GetAttributeValue("path")));
            Target = string.Format("<a href=\"/Browse/{0}\">{0}</a>", root.GetAttributeValue("target"));
            var attr = root.GetAttributeValue("pid");
            if (attr == null)
            {
                Status = "正在开始";
                return;
            }
            var pid = int.Parse(attr);
            attr = root.GetAttributeValue("current");
            if (!string.IsNullOrEmpty(attr)) CurrentFile = string.Format("<a href=\"/Browse/{0}\">{0}</a>", attr);
            FileCopied = root.GetAttributeValueWithDefault<long>("fileCopied").ToString(CultureInfo.InvariantCulture);
            SizeCopied = Helper.GetSize(root.GetAttributeValueWithDefault<long>("sizeCopied"));
            Message = root.GetAttributeValue("message") ?? "没有错误发生。";
            attr = root.GetAttributeValue("finished");
            if (string.IsNullOrEmpty(attr))
            {
                var impossibleEnds = Helper.IsBackgroundRunnerKilled(pid);
                Status = impossibleEnds ? "已被咔嚓（请重新开始任务）" : "正在复制";
                if (impossibleEnds) EndingTime = "地球毁灭时";
                else SpentTime = (DateTime.UtcNow - startTime).ToString("g");
            }
            else
            {
                Status = "复制完毕";
                var endingTime = new DateTime(long.Parse(attr), DateTimeKind.Utc);
                EndingTime = endingTime.ToChineseString();
                SpentTime = (endingTime - startTime).ToString("g");
                CurrentFile = "无";
            }
        }

        protected void CleanUp(object sender, EventArgs e)
        {
            TaskHelper.CleanUp();
        }
    }
}