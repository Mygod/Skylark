using System;
using System.IO;
using System.Text;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Edit : Page
    {
        private string relativePath, absolutePath;

        protected void Page_Load(object sender, EventArgs e)
        {
            relativePath = RouteData.GetRelativePath();
            Title = ("编辑 " + relativePath).TrimEnd();
            absolutePath = FileHelper.GetFilePath(relativePath);
            if (!File.Exists(absolutePath))
            {
                Response.Redirect("/Browse/" + relativePath, true);
                return;
            }
            if (!IsPostBack) TextArea.Value = File.ReadAllText(absolutePath);
        }

        protected void Save(object sender, EventArgs e)
        {
            File.WriteAllText(absolutePath, TextArea.Value, Encoding.UTF8);
            Response.Redirect("/Browse/" + relativePath, true);
        }
    }
}