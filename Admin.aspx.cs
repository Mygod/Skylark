using System;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Admin : Page
    {
        protected string Data;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Admin)
            {
                Response.StatusCode = 401;
                return;
            }
            if (!IsPostBack) RootBox.Text = new Config().Root;
            if (Request.Form["hidden"] != null) Privileges.Parse(Request.Form["hidden"]).Save();
            Data = new Privileges().ToString();
        }

        protected void UpdateConfig(object sender, EventArgs e)
        {
            new Config { Root = RootBox.Text }.Save();
        }
    }
}