using System;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect(new Config().Root);
        }
    }
}