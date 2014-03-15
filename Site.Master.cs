using System;
using System.Reflection;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Site : MasterPage
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            var cookie = Request.Cookies["Password"];
            var temp = new Privileges();
            var psw = cookie != null && temp.Contains(cookie.Value) ? cookie.Value : "ba3253876aed6bc22d4a6ff53d8406c6ad864195ed144ab5c87621b6c233b548baeae6956df346ec8c17f5ea10f35ee3cbc514797ed7ddd3145464e2a0bab413";
            if (temp.Contains(psw)) User = temp[psw];
        }

        public User User;

        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Page.Title)) Page.Title = "云雀™";
            else Page.Title += " - 云雀™";
        }

        protected static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
    }
}