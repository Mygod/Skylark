using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class TaskViewer : UserControl
    {
        protected CloudTask Task;
        protected bool NeverEnds;

        public void SetTask(CloudTask task)
        {
            try
            {
                NeverEnds = false;
                Task = task;
            }
            catch
            {
                Never();
            }
        }

        public void Never()
        {
            NeverEnds = true;
        }
    }
}