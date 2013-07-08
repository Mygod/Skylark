using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace Mygod.Skylark
{
    public class DownloadablePage : Page
    {
        protected void TransmitFile(string filePath, string fileName = null, string mime = null)
        {
            var fileInfo = new FileInfo(filePath);
            var responseLength = fileInfo.Exists ? fileInfo.Length : 0;
            var startIndex = 0;
            var etag = '"' + HttpUtility.UrlEncode(filePath, Encoding.UTF8) + File.GetLastWriteTimeUtc(filePath).ToString("r") + '"';

            // if the "If-Match" exists and is different to etag (or is equal to any "*" with no resource)
            if (Request.Headers["If-Match"] == "*" && !fileInfo.Exists ||
                Request.Headers["If-Match"] != null && Request.Headers["If-Match"] != "*" && Request.Headers["If-Match"] != etag)
            {
                Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
                Response.End();
            }

            if (!fileInfo.Exists)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                Response.End();
            }

            if (Request.Headers["If-None-Match"] == etag)
            {
                Response.StatusCode = (int)HttpStatusCode.NotModified;
                Response.End();
            }

            if (Request.Headers["Range"] != null && (Request.Headers["If-Range"] == null || Request.Headers["If-Range"] == etag))
            {
                var match = Regex.Match(Request.Headers["Range"], @"bytes=(\d*)-(\d*)");
                startIndex = Parse<int>(match.Groups[1].Value);
                responseLength = (Parse<int?>(match.Groups[2].Value) + 1 ?? fileInfo.Length) - startIndex;
                Response.StatusCode = (int)HttpStatusCode.PartialContent;
                Response.Headers["Content-Range"] = "bytes " + startIndex + "-" + (startIndex + responseLength - 1)
                                                             + "/" + fileInfo.Length;
            }

            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Length"] = responseLength.ToString(CultureInfo.InvariantCulture);
            Response.Cache.SetCacheability(HttpCacheability.Public); //required for etag output
            Response.Cache.SetETag(etag); //required for IE9 resumable downloads
            Response.ContentType = mime ?? "application/octet-stream";
            if (!string.IsNullOrEmpty(fileName)) Response.AddHeader("Content-Disposition", "attachment;filename="
                + HttpUtility.UrlEncode(fileName, Encoding.UTF8).Replace("+", "%20"));
            Response.TransmitFile(filePath, startIndex, responseLength);
        }

        private static T Parse<T>(object value)
        {
            //convert value to string to allow conversion from types like float to int
            //converter.IsValid only works since .NET4 but still returns invalid values for a few cases like NULL for uint
            //and not respecting locale for date validation
            try
            {
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value.ToString());
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}