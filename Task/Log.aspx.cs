using System;
using System.Web.UI;

namespace Mygod.Skylark.Task
{
    public partial class Log : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().Browse) Response.StatusCode = 401;
        }
    }
}