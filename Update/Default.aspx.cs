using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
            var id = DateTime.UtcNow.Shorten();
            File.WriteAllText(Server.MapPath("~/Update/" + id + ".log"),
                              "处理已开始，刷新此页面查看进度。" + Environment.NewLine, Encoding.UTF8);
            CloudTask.StartRunner("update\n" + id);
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