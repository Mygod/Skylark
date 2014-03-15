using System;
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

        }
    }
}