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
            try
            {
                foreach (var file in Request.Files.AllKeys.Select(key => Request.Files[key]))
                {
                    string path = FileHelper.Combine(RouteData.GetRelativePath(), file.FileName), dataPath = Server.GetDataFilePath(path);
                    if (File.Exists(dataPath))
                    {
                        Server.CancelControl(dataPath);
                        File.Delete(dataPath);
                    }
                    using (var stream = new FileStream(Server.GetFilePath(path), FileMode.Create, FileAccess.Write, 
                        FileShare.Read)) file.InputStream.CopyTo(stream);
                    FileHelper.WriteAllText(dataPath, 
                                            string.Format("<file mime=\"{0}\" state=\"ready\" />", Helper.GetMimeType(file.FileName)));
                }
                Response.Write("{\"success\":true}");
            }
            catch (Exception exc)
            {
                Response.Write("{\"error\":\"" + exc.Message + "\"}");
            }
        }
    }
}