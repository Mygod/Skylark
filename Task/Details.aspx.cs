using System;
using System.Web.UI;

namespace Mygod.Skylark.Task
{
    public partial class Details : Page
    {
        protected CloudTask Task;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (!Request.GetUser().Browse)
            {
                Response.StatusCode = 401;
                return;
            }
            try
            {
                Task = GeneralTask.Create(ID = RouteData.GetRouteString("ID"));
                if (Task != null) Title = TaskHelper.GetName(Task.Type) + "中";
                Viewer.SetTask(Task);
            }
            catch
            {
                Viewer.Never();
            }
        }
    }
}