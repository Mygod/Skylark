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
                    string path = Helper.Combine(Context.GetRelativePath(), file.FileName), dataPath = Server.GetDataPath(path);
                    File.Delete(dataPath);
                    using (var stream = new FileStream(Server.GetFilePath(path), FileMode.Create, FileAccess.Write, 
                        FileShare.Read)) file.InputStream.CopyTo(stream);
                    File.WriteAllText(dataPath,
                                      string.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?>{0}<file mime=\"{1}\" state=\"ready\" />",
                                                    Environment.NewLine, Helper.GetMimeType(file.FileName)));
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