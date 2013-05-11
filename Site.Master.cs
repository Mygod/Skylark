using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Site : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Page.Title)) Page.Title = "云雀™";
            else Page.Title += " - 云雀™";
            CurrentDrive = new DriveInfo(Server.MapPath("/")[0].ToString(CultureInfo.InvariantCulture));
        }

        protected static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
        protected DriveInfo CurrentDrive;
    }
}