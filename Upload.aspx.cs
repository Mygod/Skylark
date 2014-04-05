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
            var upload = Request.HttpMethod == "POST";
            if (upload && !Request.GetUser().OperateFiles)
            {
                Response.StatusCode = 401;
                return;
            }
            var data = upload ? Request.Form : Request.QueryString;
            string id = data["resumableIdentifier"], path = FileHelper.Combine
                        (RouteData.GetRelativePath(), data["resumableRelativePath"].ToValidPath(false)),
                   filePath = FileHelper.GetFilePath(path), dataPath = FileHelper.GetDataFilePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
            UploadTask task = null;
            if (FileHelper.GetState(dataPath) == TaskType.UploadTask
                && (task = new UploadTask(path)).Identifier != id) task = null;
            if (task == null)
            {
                FileHelper.CancelControl(dataPath);
                FileHelper.DeleteWithRetries(filePath);
                FileHelper.DeleteWithRetries(dataPath);
                (task = new UploadTask(path, id, int.Parse(data["resumableTotalChunks"]),
                                       long.Parse(data["resumableTotalSize"]))).Save();
            }
            else task = new UploadTask(path);
            var index = int.Parse(data["resumableChunkNumber"]) - 1;
            if (upload)
            {
                long chunkSize = long.Parse(data["resumableChunkSize"]), offset = chunkSize * index;
                using (var input = Request.Files[Request.Files.AllKeys.Single()].InputStream)
                using (var output = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    if (output.Length < offset) output.SetLength(offset);
                    output.Position = offset;
                    input.CopyTo(output);
                }
                task.ProcessedFileLength += chunkSize;
                var parts = task.FinishedParts;
                parts.Add(index);
                if ((task.FinishedParts = parts).Count == task.TotalParts) task.Finish();
                else task.Save();
            }
            else Response.StatusCode = task.FinishedParts.Contains(index) ? 200 : 204;  // test
        }
    }
}
