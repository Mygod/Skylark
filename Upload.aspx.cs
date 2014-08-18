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
                   filePath = FileHelper.GetFilePath(path), dataPath = FileHelper.GetDataFilePath(path),
                   state = FileHelper.GetState(dataPath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
            if (state != TaskType.UploadTask || state == TaskType.UploadTask && new UploadTask(path).Identifier != id)
            {
                FileHelper.Delete(path);
                File.WriteAllBytes(filePath, new byte[0]);
                try
                {
                    new UploadTask(path, id, int.Parse(data["resumableTotalChunks"]),
                                   long.Parse(data["resumableTotalSize"])).Save();
                }
                catch (IOException) { } // another save in progress
            }
            var index = int.Parse(data["resumableChunkNumber"]) - 1;
            if (upload)
            {
                string basePath = FileHelper.GetDataPath(path), partSuffix = ".part" + index,
                       partPath = basePath + ".incomplete" + partSuffix;
                try
                {
                    using (var output = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        Request.Files[Request.Files.AllKeys.Single()].InputStream.CopyTo(output);
                    File.Move(partPath, basePath + ".complete" + partSuffix);
                }
                catch
                {
                    FileHelper.DeleteWithRetries(partPath); // delete imcomplete file
                }
                var task = new UploadTask(path);
                if (task.FinishedParts != task.TotalParts) return;
                using (var output = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    for (var i = 0; i < task.TotalParts; ++i)
                    {
                        using (var input = new FileStream(
                            partPath = FileHelper.GetDataPath(path + ".complete.part" + i),
                            FileMode.Open, FileAccess.Read, FileShare.Read)) input.CopyTo(output);
                        FileHelper.DeleteWithRetries(partPath);
                    }
                task.Finish();
            }
            else Response.StatusCode = File.Exists(FileHelper.GetDataPath(path + ".complete.part" + index))
                                           ? 200 : 204;  // test
        }
    }
}
