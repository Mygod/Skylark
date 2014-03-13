using System;
using System.Net;
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
                Response.Write(string.Format("<h3 title=\"{4}\"><a href=\"{1}\">{0}</a>{3}</h3>{2}", video.Title,
                    video.Url, Environment.NewLine, video.Url.Equals(url, StringComparison.InvariantCultureIgnoreCase)
                        ? string.Empty : string.Format(" <a href=\"?Url={0}\" target=\"_blank\">[查看相关视频]</a>",
                                                       Rbase64.Encode(video.Url)),
                    string.Format("标题：{0}{8}上传者：{1}{8}关键字：{2}{8}平均评分：{3}{8}观看次数：{4}{8}" +
                                  "上传时间：{5}{8}时长：{6}{8}地址：{7}", video.Title, video.Author,
                                  string.Join(", ", video.Keywords), video.AverageRating, video.ViewCount,
                                  video.UploadTime.ToChineseString(), video.Length, video.Url, "&#10;")));
                foreach (var link in video.FmtStreamMap)
                {
                    Response.Write(string.Format(
                        "<div title=\"{4}\"><a href=\"/Offline/Start/{2}?Url={0}\" target=\"_blank\">{1}</a></div>{3}",
                        Rbase64.Encode(link.GetUrl(link.Parent.Title)), link,
                        string.IsNullOrEmpty(path) ? string.Empty : (path + '/'), Environment.NewLine,
                        link.Properties.Replace(Environment.NewLine, "&#10;")));
                    Response.Flush();
                }
            }
        }
    }
}