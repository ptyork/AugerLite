using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Auger
{
    public static class NetHelper
    {
        public static Uri GetBaseUri(string url)
        {
            return new Uri(new Uri(url), ".");
        }

        public static bool TestUrl(string url)
        {
            try
            {
                var req = HttpWebRequest.Create(url);
                req.Method = "HEAD";
                using (var res = req.GetResponse() as HttpWebResponse)
                {
                    return res.StatusCode == HttpStatusCode.OK;
                }
            }
            catch { }
            return false;
        }

    }
}
