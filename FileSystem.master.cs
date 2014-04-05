using System;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class FileSystem : MasterPage
    {
        protected void WritePath()
        {
            var relativePath = Page.RouteData.GetRelativePath();
            var dirs = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (dirs.Length == 0) return;
            var tempPath = string.Empty;
            foreach (var dir in dirs)
            {
                tempPath += '/' + dir.UrlEncode();
                Response.Write("<li><a href=\"/Browse" + tempPath + "/\">" + dir + "</a></li>");
            }
        }
    }
}