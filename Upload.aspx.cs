using System;
using System.IO;
using System.Linq;
using System.Web.UI;

namespace Mygod.Skylark
{
    public partial class Upload : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Request.GetUser().OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            try
            {
                string fileName = Request.Form["qqfilename"].ToValidPath(),
                       path = FileHelper.Combine(RouteData.GetRelativePath(), fileName),
                       dataPath = FileHelper.GetDataFilePath(path), filePath = FileHelper.GetFilePath(path);
                UploadTask task;
                if (FileHelper.GetState(dataPath) != TaskType.UploadTask)
                {
                    FileHelper.CancelControl(dataPath);
                    FileHelper.DeleteWithRetries(filePath);
                    FileHelper.DeleteWithRetries(dataPath);
                    (task = new UploadTask(path, int.Parse(Request.Form["qqtotalparts"]),
                                           long.Parse(Request.Form["qqtotalfilesize"]))).Save();
                }
                else task = new UploadTask(path);
                using (var input = Request.Files[Request.Files.AllKeys.Single()].InputStream)
                using (var output = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    var offset = long.Parse(Request.Form["qqpartbyteoffset"]);
                    if (output.Length < offset) output.SetLength(offset);
                    output.Position = offset;
                    input.CopyTo(output);
                }
                task.ProcessedFileLength += long.Parse(Request.Form["qqchunksize"]);
                var parts = task.FinishedParts;
                parts.Add(int.Parse(Request.Form["qqpartindex"]));
                if ((task.FinishedParts = parts).Count == task.TotalParts) task.Finish();
                else task.Save();
                Response.Write("{\"success\":true}");
            }
            catch (Exception exc)
            {
                Response.Write("{\"error\":\"" + exc.Message + "\"}");
            }
        }
    }
}
