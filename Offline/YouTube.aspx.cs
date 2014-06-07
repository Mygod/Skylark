using System;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class YouTube : Page
    {
        protected void GetEmAll()
        {
            string url = Rbase64.Decode(Request.QueryString["Url"].UrlDecode()), path = RouteData.GetRelativePath();
            if (string.IsNullOrWhiteSpace(url)) return;
            foreach (var video in Net.YouTube.Video.GetVideoFromLink(url))
            {
                var isTarget = video.Url.Equals(url, StringComparison.InvariantCultureIgnoreCase);
                Response.Write(string.Format("<details{4}><summary title=\"{3}\"><a href=\"{1}\">{0}</a>{2}</summary>",
                    video.Title, video.Url, isTarget ? string.Empty : string.Format
                        (" <a href=\"?Url={0}\" target=\"_blank\">[查看相关视频]</a>", Rbase64.Encode(video.Url)),
                    string.Format("标题：{0}{8}上传者：{1}{8}关键字：{2}{8}平均评分：{3}{8}观看次数：{4}{8}" +
                                  "上传时间：{5}{8}时长：{6}{8}地址：{7}", video.Title, video.Author,
                                  string.Join(", ", video.Keywords), video.AverageRating, video.ViewCount,
                                  video.UploadTime.ToChineseString(), video.Length, video.Url, "&#10;"),
                    isTarget ? " open" : string.Empty));
                foreach (var link in video.Downloads)
                {
                    Response.Write(string.Format(
                        "<div title=\"{3}\"><a href=\"/Offline/Start/{2}?Url={0}\" target=\"_blank\">{1}</a></div>",
                        Rbase64.Encode(link.GetUrl(link.Parent.Title)), link,
                        FileHelper.Combine(path, (link.Parent.Title + link.Extension).ToValidPath()),
                        link.Properties.Replace(Environment.NewLine, "&#10;")));
                }
                Response.Write("</details>" + Environment.NewLine);
                Response.Flush();
            }
        }
    }
}