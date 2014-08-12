using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.UI;

namespace Mygod.Skylark.Update
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Admin) Response.StatusCode = 401;
        }

        protected void Update(object sender, EventArgs e)
        {
            string id = DateTime.UtcNow.Shorten(), path = Server.MapPath("~/Update/SkylarkUpdater.exe");
            foreach (var file in "7z.dll,SevenZipSharp.dll".Split(','))
                File.Copy(Server.MapPath("~/plugins/" + file), Server.MapPath("~/Update/" + file), true);
            new WebClient().DownloadFile("http://mygod.tk/skylark/SkylarkUpdater.exe", path);
            File.WriteAllText(Server.MapPath("~/Update/" + id + ".log"),
                              "处理已开始，刷新此页面查看进度。" + Environment.NewLine, Encoding.UTF8);
            Process.Start(new ProcessStartInfo(path, id) { WorkingDirectory = Server.MapPath("~/") });
            Response.Redirect(id + ".log");
        }

        protected void Cleanup(object sender, EventArgs e)
        {
            foreach (var file in Directory.EnumerateFiles(Server.MapPath("~/Update"))
                .Where(file => file.EndsWith(".zip", true, CultureInfo.InvariantCulture)
                            || file.EndsWith(".log", true, CultureInfo.InvariantCulture)))
                FileHelper.DeleteWithRetries(file);
        }

        protected static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
    }
}