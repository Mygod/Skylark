using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Mygod.Skylark
{
    public class DownloadablePage : Page
    {
        /// <summary>  
        /// 下载文件，支持大文件、续传。支持续传的响应头Accept-Ranges、ETag，请求头Range 。  
        /// Accept-Ranges：响应头，向客户端指明，此进程支持可恢复下载.实现后台智能传输服务（BITS），值为：bytes；  
        /// ETag：响应头，用于对客户端的初始（200）响应，以及来自客户端的恢复请求，  
        /// 必须为每个文件提供一个唯一的ETag值（可由文件名和文件最后被修改的日期组成），这使客户端软件能够验证它们已经下载的字节块是否仍然是最新的。  
        /// Range：续传的起始位置，即已经下载到客户端的字节数，值如：bytes=1474560- 。  
        /// 另外：UrlEncode编码后会把文件名中的空格转换中+（+转换为%2b），但是浏览器是不能理解加号为空格的，
        /// 所以在浏览器下载得到的文件，空格就变成了加号；  
        /// 解决办法：UrlEncode 之后, 将 "+" 替换成 "%20"，因为浏览器将%20转换为空格  
        /// </summary> 
        /// <param name="filePath">下载文件的路径，含路径、文件名</param>
        /// <returns>true下载成功，false下载失败</returns>  
        protected bool DownloadFile(string filePath, string mime = "application/octet-stream", bool showNormally = false)
        {
            var ret = true;
            try
            {
                #region --验证请求的文件是否存在
                if (!File.Exists(filePath))
                {
                    Response.StatusCode = 404;
                    return false;
                }
                #endregion

                #region 定义局部变量
                long startBytes = 0;
                const int packSize = 1024 * 10; //分块读取，每块10K bytes  
                var myFile = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var br = new BinaryReader(myFile);
                var fileLength = myFile.Length;

                var lastUpdateTiemStr = File.GetLastWriteTimeUtc(filePath).ToString("r");
                var fileName = Path.GetFileName(filePath);
                var eTag = HttpUtility.UrlEncode(fileName, Encoding.UTF8) + lastUpdateTiemStr;//便于恢复下载时提取请求头;  
                #endregion

                #region--验证：文件是否太大，是否是续传，且在上次被请求的日期之后是否被修
                if (myFile.Length > Int32.MaxValue)
                {//-------文件太大了-------  
                    Response.StatusCode = 413;//请求实体太大  
                    return false;
                }

                if (Request.Headers["If-Range"] != null)    //对应响应头ETag：文件名+文件最后修改时间  
                {
                    //----------上次被请求的日期之后被修改过--------------  
                    if (Request.Headers["If-Range"].Replace("\"", "") != eTag)  //文件修改过  
                    {
                        Response.StatusCode = 412;  //预处理失败  
                        return false;
                    }
                }
                #endregion

                try
                {
                    #region -------添加重要响应头、解析请求头、相关验证-------------------
                    Response.Clear();
                    Response.Buffer = false;
                    //Response.AddHeader("Content-MD5", GetMD5Hash(myFile));//用于验证文件  
                    Response.AddHeader("Accept-Ranges", "bytes");//重要：续传必须  
                    Response.AppendHeader("ETag", "\"" + eTag + "\"");//重要：续传必须  
                    Response.AppendHeader("Last-Modified", lastUpdateTiemStr);//把最后修改日期写入响应                  
                    Response.ContentType = mime;//MIME类型：匹配任意文件类型  
                    if (!showNormally) Response.AddHeader("Content-Disposition", "attachment;filename="
                        + HttpUtility.UrlEncode(fileName, Encoding.UTF8).Replace("+", "%20"));
                    Response.AddHeader("Content-Length", (fileLength - startBytes).ToString(CultureInfo.InvariantCulture));
                    Response.AddHeader("Connection", "Keep-Alive");
                    Response.ContentEncoding = Encoding.UTF8;
                    if (Request.Headers["Range"] != null)   //------如果是续传请求，则获取续传的起始位置，即已经下载到客户端的字节数------  
                    {
                        Response.StatusCode = 206;//重要：续传必须，表示局部范围响应。初始下载时默认为200  
                        string[] range = Request.Headers["Range"].Split(new[] { '=', '-' });//"bytes=1474560-"  
                        startBytes = Convert.ToInt64(range[1]);//已经下载的字节数，即本次下载的开始位置    
                        if (startBytes < 0 || startBytes >= fileLength)
                        {//无效的起始位置  
                            return false;
                        }
                    }
                    if (startBytes > 0)
                    {//------如果是续传请求，告诉客户端本次的开始字节数，总长度，以便客户端将续传数据追加到startBytes位置后----------  
                        Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
                    }
                    #endregion

                    #region -------向客户端发送数据块-------------------
                    br.BaseStream.Seek(startBytes, SeekOrigin.Begin);
                    var maxCount = (int)Math.Ceiling((fileLength - startBytes + 0.0) / packSize);//分块下载，剩余部分可分成的块数  
                    for (var i = 0; i < maxCount && Response.IsClientConnected; i++)    //客户端中断连接，则暂停  
                    {
                        Response.BinaryWrite(br.ReadBytes(packSize));
                        Response.Flush();
                    }
                    #endregion
                }
                catch
                {
                    ret = false;
                }
                finally
                {
                    br.Close();
                    myFile.Close();
                }
            }
            catch
            {
                ret = false;
            }
            return ret;
        }
    }
}