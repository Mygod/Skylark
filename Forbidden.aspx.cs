using System;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Forbidden : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.StatusCode = 403;
        }
    }
}