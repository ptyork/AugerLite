using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Auger
{
    public static class RequestHelper
    {
        public static WebRequest GetMultipartFormDataRequest(string url, NameValueCollection form)
        {
            var req = HttpWebRequest.CreateHttp(url);
            req.Method = "POST";
            req.UserAgent = "Auger/1.0";
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            req.ContentType = "multipart/form-data; boundary=" + boundary;


            //adding form data
            string formDataHeaderTemplate = Environment.NewLine + "--" + boundary + Environment.NewLine +
                                            "Content-Disposition: form-data; name=\"{0}\";" + Environment.NewLine +
                                            Environment.NewLine + "{1}";

            using (var postStream = new MemoryStream())
            {
                foreach (var field in form.Keys)
                {
                    var formFieldBytes = Encoding.UTF8.GetBytes(string.Format(formDataHeaderTemplate, field, form[(string)field]));
                    postStream.Write(formFieldBytes, 0, formFieldBytes.Length);
                }

                var endBoundaryBytes = Encoding.UTF8.GetBytes(Environment.NewLine + "--" + boundary + "--");
                postStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
                postStream.Position = 0;

                req.ContentLength = postStream.Length;

                Stream reqStream = req.GetRequestStream();
                postStream.CopyTo(reqStream, 1024);
            }
            return req;
        }

        public static bool GetPageText(Uri pageUri, out string pageText)
        {
            pageText = "";
            try
            {
                var req = HttpWebRequest.Create(pageUri);
                using (var rsp = req.GetResponse())
                {
                    using (var rdr = new StreamReader(rsp.GetResponseStream()))
                    {
                        pageText = rdr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                    return false;

                var iex = ex.InnerException;
                while (iex != null)
                {
                    if (iex is FileNotFoundException)
                        return false;
                    iex = iex.InnerException;
                }

                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return false;
            }
            return true;
        }
    }
}
